namespace VoltStream.WPF.Commons.Services;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using VoltStream.WPF.Commons.Dialogs;
using VoltStream.WPF.Commons.Enums;
using VoltStream.WPF.Commons.Localization;

public enum ReceiptMode { Money, Weight }

public static class ReceiptDocumentBuilder
{
    public static FixedDocument Build(ReceiptResponse receipt, ReceiptMode mode, string paper = "A4")
        => ReportService.BuildFixedDocument(Define(receipt, mode, ReportService.PaperSize(paper)));

    public static void Preview(ReceiptResponse receipt, bool allowMoney, IServiceProvider services)
    {
        bool money = allowMoney && receipt.HasPrices;

        IReadOnlyList<(string Label, Func<FixedDocument> Build)>? variants = money
            ?
            [
                (TranslationSource.T("Receipt.ModeMoney"), () => Build(receipt, ReceiptMode.Money)),
                (TranslationSource.T("Receipt.ModeWeight"), () => Build(receipt, ReceiptMode.Weight))
            ]
            : null;

        IReadOnlyList<(string Label, Func<Button, Task> Run)>? actions =
            services.GetRequiredService<ISessionService>().Has("print.request")
                ? [(TranslationSource.T("Receipt.SendToBranch"), button => SendToBranchAsync(receipt, services, button))]
                : null;

        var document = Build(receipt, money ? ReceiptMode.Money : ReceiptMode.Weight);
        ReportService.Preview(document, FileName(receipt), Title(receipt), variants, actions);
    }

    private static async Task SendToBranchAsync(ReceiptResponse receipt, IServiceProvider services, Button button)
    {
        if (!services.GetRequiredService<ISessionService>().Has("branches.all"))
        {
            await SendAsync(receipt, services, null, button);
            return;
        }

        var branchContext = services.GetRequiredService<BranchContext>();
        if (branchContext.Branches.Count == 0)
            await branchContext.LoadAsync();

        var branches = branchContext.Branches.Where(b => b.Id > 0).ToList();
        if (branches.Count == 0)
        {
            await SendAsync(receipt, services, null, button);
            return;
        }

        var own = branches.FirstOrDefault(b => b.Name == receipt.Branch);
        var menu = new ContextMenu { PlacementTarget = button, Placement = PlacementMode.Bottom };
        menu.SetResourceReference(Control.BackgroundProperty, "Surface");
        menu.SetResourceReference(Control.BorderBrushProperty, "Border");

        foreach (var branch in own is null ? branches : [own, .. branches.Where(b => b != own)])
        {
            var entry = new MenuItem
            {
                Header = branch.Name,
                FontWeight = branch == own ? FontWeights.Bold : FontWeights.Normal,
                IsChecked = branch == own
            };
            entry.Click += async (_, _) => await SendAsync(receipt, services, branch.Id, button);
            menu.Items.Add(entry);
        }

        menu.IsOpen = true;
    }

    private static async Task SendAsync(ReceiptResponse receipt, IServiceProvider services, long? branchId, Button button)
    {
        var response = await services.GetRequiredService<IPrintJobsApi>()
            .CreateAsync(new PrintJobRequest { SaleId = receipt.SaleId, Copies = 1, BranchId = branchId }).Handle();

        var owner = Window.GetWindow(button);
        var title = TranslationSource.T("Receipt.SendToBranch");

        if (response.IsSuccess)
            AppDialog.Info(TranslationSource.T("Receipt.SentToBranch"), title, owner);
        else
            AppDialog.Warning(
                string.IsNullOrWhiteSpace(response.Message) ? TranslationSource.T("Receipt.SendFailed") : response.Message,
                title, owner);
    }

    public static string FileName(ReceiptResponse receipt) => $"Chek-{receipt.SaleId}";

    public static string Title(ReceiptResponse receipt)
        => string.Format(TranslationSource.T("Receipt.Title"), receipt.SaleId);

    private static ReportDefinition<ReceiptItemResponse> Define(ReceiptResponse receipt, ReceiptMode mode, Size page)
    {
        bool money = mode == ReceiptMode.Money && receipt.HasPrices;
        bool hasComposition = receipt.Items.Any(i => !string.IsNullOrWhiteSpace(i.Composition));
        double contentWidth = ReportService.ContentWidth(page);

        List<ReportColumn<ReceiptItemResponse>> columns =
        [
            new() { Header = TranslationSource.T("Receipt.Product"), Width = contentWidth - (money ? (hasComposition ? 435 : 495) : (hasComposition ? 335 : 405)), Value = Product }
        ];

        if (!hasComposition)
            columns.Add(new() { Header = TranslationSource.T("Receipt.PerRoll"), Width = 70, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => i.LengthPerRoll });

        columns.Add(new() { Header = TranslationSource.T("Receipt.RollCount"), Width = 70, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => i.RollCount });
        columns.Add(new() { Header = TranslationSource.T("Receipt.TotalLength"), Width = 85, Align = ReportAlign.Right, Value = i => $"{i.TotalLength:N0} {i.Unit}" });

        int rollCountColumn = columns.Count - 2;
        List<ReportTotalCell> totals =
        [
            new() { Column = rollCountColumn, Value = receipt.TotalRolls, Format = "N0" },
            new() { Column = rollCountColumn + 1, Value = $"{receipt.TotalLength:N0}" }
        ];

        if (!money)
            columns.Add(new() { Header = TranslationSource.T("Receipt.WeightPerUnit"), Width = 95, Align = ReportAlign.Right, IsNumber = true, Format = "N3", Value = i => i.WeightPerUnit });

        columns.Add(new() { Header = TranslationSource.T("Receipt.Weight"), Width = 85, Align = ReportAlign.Right, IsNumber = true, Format = "N3", Value = i => i.Weight });
        totals.Add(new() { Column = columns.Count - 1, Value = receipt.TotalWeight, Format = "N3" });

        if (money)
        {
            columns.Add(new() { Header = TranslationSource.T("Receipt.Price"), Width = 90, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => (object?)i.UnitPrice ?? "—" });
            columns.Add(new() { Header = TranslationSource.T("Receipt.Amount"), Width = 105, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = i => i.TotalAmount });
            totals.Add(new() { Column = columns.Count - 1, Value = receipt.Amount, Format = "N0" });
        }

        return new ReportDefinition<ReceiptItemResponse>
        {
            Title = Title(receipt),
            Subtitle = Subtitle(receipt, money),
            SheetName = Title(receipt),
            FileName = FileName(receipt),
            Rows = receipt.Items,
            Columns = columns,
            PageSize = page,
            Totals = new ReportTotals
            {
                Label = TranslationSource.T("Receipt.Total"),
                LabelSpan = rollCountColumn,
                Cells = totals
            }
        };
    }

    private static string Product(ReceiptItemResponse item)
    {
        string name = string.IsNullOrWhiteSpace(item.Category) ? item.Product ?? string.Empty : $"{item.Category} {item.Product}";
        return string.IsNullOrWhiteSpace(item.Composition) ? name : $"{name}\n{item.Composition}";
    }

    private static string Subtitle(ReceiptResponse receipt, bool money)
        => string.Join("  |  ", new[]
        {
            receipt.Branch,
            receipt.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm"),
            receipt.Customer,
            receipt.Operator,
            money && receipt.Discount is > 0 ? $"{TranslationSource.T("Receipt.Discount")}: {receipt.Discount:N0}" : null
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
