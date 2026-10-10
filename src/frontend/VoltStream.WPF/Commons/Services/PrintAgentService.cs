namespace VoltStream.WPF.Commons.Services;

using ApiServices.Enums;
using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Windows;
using VoltStream.WPF.Commons.Dialogs;
using VoltStream.WPF.Commons.Enums;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.ViewModels;

public enum AgentStatus { Off, Connecting, Connected, Reconnecting }

public sealed class ForeverRetryPolicy : IRetryPolicy
{
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    ];

    public TimeSpan? NextRetryDelay(RetryContext context)
        => context.PreviousRetryCount < Delays.Length
            ? Delays[context.PreviousRetryCount]
            : TimeSpan.FromSeconds(30);
}

public partial class PrintAgentService(
    IServiceProvider services,
    ISessionService session,
    PrinterSettings settings,
    ApiConnectionViewModel apiConnection) : ObservableObject
{
    public long? AgentBranchId => settings.BranchId;

    public string? AgentBranchName => settings.BranchName;

    private static readonly TimeSpan PrintTimeout = TimeSpan.FromMinutes(2);

    private readonly SemaphoreSlim gate = new(1, 1);
    private HubConnection? connection;
    private bool stopping;
    private long lastFailedJobId;

    [ObservableProperty] private AgentStatus status;

    public ObservableCollection<PrintJobResponse> RecentJobs { get; } = [];

    public bool CanRun => settings.Enabled
        && !string.IsNullOrWhiteSpace(settings.PrinterName)
        && session.Has("print.agent")
        && AgentBranchId is not null;

    public async Task StartAsync()
    {
        if (connection is not null) return;

        if (!CanRun)
        {
            Status = AgentStatus.Off;
            return;
        }

        Status = AgentStatus.Connecting;
        stopping = false;

        var hub = new HubConnectionBuilder()
            .WithUrl($"{apiConnection.Url}hubs/print{(AgentBranchId is long id ? $"?branchId={id}" : string.Empty)}", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(session.Token);
                options.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is HttpClientHandler client)
                        client.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                    return handler;
                };
            })
            .WithAutomaticReconnect(new ForeverRetryPolicy())
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        hub.On<PrintJobResponse>("PrintJobCreated", job => HandleAsync(job));

        hub.Reconnecting += _ =>
        {
            Status = AgentStatus.Reconnecting;
            return Task.CompletedTask;
        };

        hub.Reconnected += async _ =>
        {
            Status = AgentStatus.Connected;
            await DrainPendingAsync();
        };

        hub.Closed += _ => stopping || connection is null ? Task.CompletedTask : ConnectAsync();

        connection = hub;
        await ConnectAsync();
    }

    public async Task StopAsync()
    {
        stopping = true;
        var hub = connection;
        connection = null;

        if (hub is not null)
        {
            try { await hub.StopAsync(); } catch { }
            await hub.DisposeAsync();
        }

        Status = AgentStatus.Off;
    }

    public async Task RestartAsync()
    {
        await StopAsync();
        await StartAsync();
    }

    public async Task TestPrintAsync()
    {
        if (string.IsNullOrWhiteSpace(settings.PrinterName))
        {
            Notify(TranslationSource.T("Printer.NoPrinter"), NotificationType.Warning);
            return;
        }

        try
        {
            var sample = SampleReceipt();
            await Application.Current.Dispatcher.InvokeAsync(() => PrintService.Print(
                ReceiptDocumentBuilder.Build(sample, ReceiptMode.Weight, settings.Layout, settings.Paper),
                settings.PrinterName!,
                Math.Max(1, settings.Copies),
                ReceiptDocumentBuilder.Title(sample),
                settings.Paper));

            Notify(TranslationSource.T("Printer.TestSent"), NotificationType.Success);
        }
        catch (Exception ex)
        {
            Notify($"{TranslationSource.T("Printer.TestFailed")}: {ex.Message}", NotificationType.Error);
        }
    }

    private async Task ConnectAsync()
    {
        while (!stopping && connection is not null)
        {
            try
            {
                await connection.StartAsync();
                Status = AgentStatus.Connected;
                await DrainPendingAsync();
                return;
            }
            catch
            {
                Status = AgentStatus.Reconnecting;
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }

    private async Task DrainPendingAsync()
    {
        if (!CanRun) return;

        var response = await services.GetRequiredService<IPrintJobsApi>()
            .GetPendingAsync(AgentBranchId).Handle();

        if (!response.IsSuccess) return;

        foreach (var job in response.Data)
            await HandleAsync(job);
    }

    private async Task HandleAsync(PrintJobResponse job)
    {
        if (!CanRun) return;

        var printJobs = services.GetRequiredService<IPrintJobsApi>();
        var printer = settings.PrinterName!;

        await gate.WaitAsync();
        try
        {
            await Task.Delay(ClaimDelay(job));

            var claim = await printJobs.ClaimAsync(job.Id, new PrintJobClaimRequest { PrinterName = printer }).Handle();
            if (!claim.IsSuccess) return;

            var claimed = claim.Data;

            if (claimed.PrintPolicy == PrintPolicy.Ask && !await AskAsync(claimed))
            {
                await printJobs.RejectAsync(claimed.Id, new PrintJobRejectRequest { Reason = TranslationSource.T("Printer.RejectedByOperator") }).Handle();
                Track(claimed with { Status = PrintJobStatus.Rejected });
                return;
            }

            var receipt = claimed.SaleDraftId is long draftId
                ? await services.GetRequiredService<ISaleDraftsApi>().GetReceipt(draftId).Handle()
                : await services.GetRequiredService<ISaleApi>().GetReceipt(claimed.SaleId!.Value).Handle();
            if (!receipt.IsSuccess)
                throw new InvalidOperationException(receipt.Message ?? TranslationSource.T("Receipt.LoadError"));

            await PrintAsync(receipt.Data, printer, Math.Max(1, settings.Copies) * Math.Max(1, claimed.Copies), settings.Layout, settings.Paper);

            if ((await printJobs.CompleteAsync(claimed.Id).Handle()).StatusCode == 409)
            {
                Notify(TranslationSource.T("Printer.JobNoLongerActive"), NotificationType.Info);
                return;
            }

            Track(claimed with { Status = PrintJobStatus.Completed });
            Notify(string.Format(TranslationSource.T(JobKey(claimed, "Printed")), claimed.SaleDraftId ?? claimed.SaleId), NotificationType.Success);
        }
        catch (Exception ex)
        {
            lastFailedJobId = job.Id;

            if ((await printJobs.FailAsync(job.Id, new PrintJobFailRequest { Error = ex.Message }).Handle()).StatusCode == 409)
            {
                Notify(TranslationSource.T("Printer.JobNoLongerActive"), NotificationType.Info);
                return;
            }

            Track(job with { Status = PrintJobStatus.Failed, Error = ex.Message });
            Notify(string.Format(TranslationSource.T(JobKey(job, "Failed")), job.SaleDraftId ?? job.SaleId, ex.Message), NotificationType.Error);
        }
        finally
        {
            gate.Release();
        }
    }

    private TimeSpan ClaimDelay(PrintJobResponse job)
    {
        int ms = (Math.Clamp(settings.Priority, 1, 5) - 1) * 700;
        if (job.Attempts > 0)
            ms += lastFailedJobId == job.Id ? 8000 : 3000;
        return TimeSpan.FromMilliseconds(ms);
    }

    private static async Task PrintAsync(ReceiptResponse receipt, string printer, int copies, ReceiptLayout layout, string paper)
    {
        var operation = Application.Current.Dispatcher.InvokeAsync(() => PrintService.Print(
            ReceiptDocumentBuilder.Build(receipt, ReceiptMode.Weight, layout, paper),
            printer,
            copies,
            ReceiptDocumentBuilder.Title(receipt),
            paper));

        if (await Task.WhenAny(operation.Task, Task.Delay(PrintTimeout)) != operation.Task)
        {
            operation.Abort();
            throw new TimeoutException(TranslationSource.T("Printer.TimeoutError"));
        }

        await operation.Task;
    }

    private static Task<bool> AskAsync(PrintJobResponse job)
        => Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var message = string.Format(TranslationSource.T(JobKey(job, "AskMessage")), job.SaleDraftId ?? job.SaleId, job.RequestedBy);

            return AppDialog.Confirm(message, TranslationSource.T("Printer.AskTitle"), Application.Current.MainWindow);
        }).Task;

    private static string JobKey(PrintJobResponse job, string name)
        => $"Printer.{name}{(job.SaleDraftId is null ? string.Empty : "Draft")}";

    private void Track(PrintJobResponse job)
        => Application.Current.Dispatcher.Invoke(() =>
        {
            RecentJobs.Insert(0, job);
            while (RecentJobs.Count > 20)
                RecentJobs.RemoveAt(RecentJobs.Count - 1);
        });

    private static void Notify(string message, NotificationType type)
        => Application.Current.Dispatcher.Invoke(() => NotificationService.Show(message, type));

    private static ReceiptResponse SampleReceipt() => new()
    {
        SaleId = 0,
        Date = DateTimeOffset.Now,
        Branch = TranslationSource.T("Printer.TestPrint"),
        Customer = TranslationSource.T("Printer.TestPrint"),
        Operator = TranslationSource.T("Printer.TestPrint"),
        Items =
        [
            new()
            {
                Category = "VVG", Product = "3x2.5", LengthPerRoll = 100, RollCount = 2, TotalLength = 200, Unit = "m", Weight = 25,
                Composition = "2 × 100", Rolls = [new() { Length = 100, Count = 2 }]
            }
        ],
        TotalRolls = 2,
        TotalLength = 200,
        TotalWeight = 25,
        HasPrices = false
    };
}
