namespace VoltStream.WPF.SalesJournal.Models;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;

public partial class SalesJournalPageViewModel : ViewModelBase
{
    private readonly IServiceProvider services;
    private readonly BranchContext branchContext;
    private readonly ISessionService session;

    public PaginationViewModel Pagination { get; }

    public bool CanViewPrices => session.Has("sales.view_prices");

    [ObservableProperty] private ObservableCollection<SaleJournalItemViewModel> pagedSales = [];
    [ObservableProperty] private ObservableCollection<CustomerResponse> customers = [];
    [ObservableProperty] private CustomerResponse? selectedCustomer;
    [ObservableProperty] private DateTime beginDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime endDate = DateTime.Today;

    public SalesJournalPageViewModel(IServiceProvider services)
    {
        this.services = services;
        branchContext = services.GetRequiredService<BranchContext>();
        session = services.GetRequiredService<ISessionService>();
        Pagination = new PaginationViewModel(LoadPageAsync);
        _ = LoadInitialDataAsync();
    }

    #region Load Data

    private async Task LoadInitialDataAsync()
    {
        await LoadCustomersAsync();
        await LoadPageAsync();
    }

    private async Task LoadCustomersAsync()
    {
        var response = await services.GetRequiredService<ICustomersApi>().GetAllAsync().Handle();
        if (!response.IsSuccess)
        {
            Error = response.Message ?? TranslationSource.T("SalesJournal.LoadCustomersError");
            return;
        }

        Customers = new ObservableCollection<CustomerResponse>(response.Data!);
        Customers.Insert(0, new CustomerResponse { Name = TranslationSource.T("Common.All") });
        selectedCustomer = Customers[0];
        OnPropertyChanged(nameof(SelectedCustomer));
    }

    public async Task ReloadAsync()
    {
        Pagination.Reset();
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        var request = BuildRequest();
        request.Page = Pagination.Page;
        request.PageSize = Pagination.PageSize;

        using (PagingScope.Begin())
        {
            var response = await services.GetRequiredService<ISaleApi>().Filtering(request).Handle(l => IsLoading = l);
            if (!response.IsSuccess)
            {
                Error = response.Message ?? TranslationSource.T("SalesJournal.LoadError");
                return;
            }

            Pagination.Apply(PagingScope.Result);
            PagedSales = new ObservableCollection<SaleJournalItemViewModel>(response.Data!.Select(Map));
        }
    }

    private async Task<List<SaleJournalItemViewModel>> LoadAllSalesAsync()
    {
        var response = await services.GetRequiredService<ISaleApi>().Filtering(BuildRequest()).Handle(l => IsLoading = l);
        return response.IsSuccess ? [.. response.Data!.Select(Map)] : [];
    }

    private FilteringRequest BuildRequest()
    {
        var request = new FilteringRequest
        {
            Filters = new()
            {
                ["Date"] = [$">={BeginDate:yyyy-MM-dd}", $"<{EndDate.AddDays(1):yyyy-MM-dd}"],
                ["customer"] = ["include"],
                ["items"] = ["include:product"]
            },
            SortBy = "Date",
            Descending = true
        };

        if (SelectedCustomer is { Id: > 0 } customer)
            request.Filters["CustomerId"] = [$"={customer.Id}"];

        branchContext.ApplyTo(request);
        return request;
    }

    private static SaleJournalItemViewModel Map(SaleResponse sale) => new()
    {
        Id = sale.Id,
        Date = sale.Date.LocalDateTime,
        Customer = sale.Customer?.Name,
        RollCount = sale.RollCount,
        Length = sale.Length,
        Weight = sale.Items?.Sum(item => item.Weight) ?? 0,
        Amount = sale.Amount,
        PrintStatus = sale.PrintJob is null ? null : TranslationSource.T($"PrintJob.{sale.PrintJob.Status}")
    };

    #endregion Load Data

    #region Commands

    [RelayCommand]
    private async Task Receipt(SaleJournalItemViewModel? item)
    {
        if (item is null) return;

        var response = await services.GetRequiredService<ISaleApi>().GetReceipt(item.Id).Handle(l => IsLoading = l);
        if (!response.IsSuccess)
        {
            Error = response.Message ?? TranslationSource.T("Receipt.LoadError");
            return;
        }

        ReceiptDocumentBuilder.Preview(response.Data, CanViewPrices);
    }

    [RelayCommand]
    private async Task ClearFilter()
    {
        BeginDate = DateTime.Today.AddDays(-7);
        EndDate = DateTime.Today;
        SelectedCustomer = Customers.FirstOrDefault();
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        var sales = await LoadAllSalesAsync();
        if (sales.Count == 0) { Info = TranslationSource.T("SalesJournal.NoData"); return; }
        try { if (ReportService.ExportExcel(BuildReport(sales))) Success = TranslationSource.T("SalesJournal.ExportSuccess"); }
        catch { Error = TranslationSource.T("SalesJournal.ExportError"); }
    }

    [RelayCommand]
    private async Task Print()
    {
        var sales = await LoadAllSalesAsync();
        if (sales.Count == 0) { Info = TranslationSource.T("SalesJournal.NoData"); return; }
        ReportService.Print(BuildReport(sales));
    }

    [RelayCommand]
    private async Task Preview()
    {
        var sales = await LoadAllSalesAsync();
        if (sales.Count == 0) { Info = TranslationSource.T("SalesJournal.NoData"); return; }
        ReportService.Preview(BuildReport(sales));
    }

    private ReportDefinition<SaleJournalItemViewModel> BuildReport(List<SaleJournalItemViewModel> sales)
    {
        var subtitle = $"{BeginDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}";
        if (SelectedCustomer is { Id: > 0 } customer) subtitle += $"  |  {customer.Name}";

        List<ReportColumn<SaleJournalItemViewModel>> columns =
        [
            new() { Header = TranslationSource.T("Common.Date"), Width = 100, Align = ReportAlign.Center, Value = x => x.Date.ToString("dd.MM.yyyy HH:mm") },
            new() { Header = TranslationSource.T("SalesJournal.Customer"), Width = 160, Value = x => x.Customer },
            new() { Header = TranslationSource.T("SalesJournal.RollCount"), Width = 80, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = x => x.RollCount },
            new() { Header = TranslationSource.T("SalesJournal.TotalLength"), Width = 90, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = x => x.Length },
            new() { Header = TranslationSource.T("SalesJournal.Weight"), Width = 90, Align = ReportAlign.Right, IsNumber = true, Format = "N3", Value = x => x.Weight }
        ];

        if (CanViewPrices)
            columns.Add(new() { Header = TranslationSource.T("SalesJournal.Amount"), Width = 110, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = x => x.Amount });

        return new ReportDefinition<SaleJournalItemViewModel>
        {
            Title = TranslationSource.T("SalesJournal.ReportTitle"),
            Subtitle = subtitle,
            SheetName = TranslationSource.T("SalesJournal.ReportName"),
            FileName = TranslationSource.T("SalesJournal.ReportName"),
            Columns = columns,
            Rows = sales
        };
    }

    #endregion Commands

    partial void OnBeginDateChanged(DateTime value) => _ = ReloadAsync();
    partial void OnEndDateChanged(DateTime value) => _ = ReloadAsync();
    partial void OnSelectedCustomerChanged(CustomerResponse? value) => _ = ReloadAsync();
}
