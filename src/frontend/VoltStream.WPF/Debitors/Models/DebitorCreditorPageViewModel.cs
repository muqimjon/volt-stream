namespace VoltStream.WPF.Debitors.Models;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;

public partial class DebitorCreditorPageViewModel : ViewModelBase
{
    private readonly ICustomersApi customersApi;
    private readonly IMapper mapper;

    public DebitorCreditorPageViewModel(IServiceProvider service)
    {
        customersApi = service.GetRequiredService<ICustomersApi>();
        mapper = service.GetRequiredService<IMapper>();
        Pagination = new PaginationViewModel(LoadPageAsync);
        _ = LoadInitialAsync();
    }

    public PaginationViewModel Pagination { get; }

    [ObservableProperty] private ObservableCollection<CustomerViewModel> availableCustomers = [];
    [ObservableProperty] private ObservableCollection<DebitorCreditorItemViewModel> pagedDebitorCreditorItems = [];
    [ObservableProperty] private CustomerViewModel? selectedCustomer;
    [ObservableProperty] private decimal finalDebitor;
    [ObservableProperty] private decimal finalKreditor;
    [ObservableProperty] private decimal finalAmount;
    [ObservableProperty] private decimal finalDiscount;

    [ObservableProperty] private string? sign = "Barchasi";
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private string selectedBalanceType = "Barchasi";

    public List<string> Signs { get; } = ["Barchasi", ">", ">=", "=", "<", "<=", "<>"];
    public List<string> BalanceTypes { get; } = ["Barchasi", "Debitorlar", "Kreditorlar"];

    partial void OnSignChanged(string? value) => _ = ReloadAsync();
    partial void OnAmountChanged(decimal value) => _ = ReloadAsync();
    partial void OnSelectedBalanceTypeChanged(string value) => _ = ReloadAsync();
    partial void OnSelectedCustomerChanged(CustomerViewModel? value) => _ = ReloadAsync();

    private async Task LoadInitialAsync()
    {
        await LoadCustomers();
        await ReloadAsync();
    }

    private async Task LoadCustomers()
    {
        var response = await customersApi.GetAllAsync().Handle(isLoading => IsLoading = isLoading);
        if (!response.IsSuccess)
        {
            Error = response.Message ?? TranslationSource.T("Debitors.LoadCustomersError");
            return;
        }

        AvailableCustomers = mapper.Map<ObservableCollection<CustomerViewModel>>(response.Data);
        AvailableCustomers.Insert(0, new CustomerViewModel { Name = "Barchasi" });
        SelectedCustomer = AvailableCustomers[0];
    }

    private CustomerBalanceRequest BuildRequest() => new()
    {
        CustomerId = SelectedCustomer?.Id > 0 ? SelectedCustomer.Id : null,
        Sign = Sign is null or "Barchasi" ? null : Sign,
        Amount = Amount,
        Type = SelectedBalanceType switch
        {
            "Debitorlar" => "Debitor",
            "Kreditorlar" => "Creditor",
            _ => null
        }
    };

    private async Task ReloadAsync()
    {
        Pagination.Reset();
        await LoadPageAsync();
        await LoadSummaryAsync();
    }

    private async Task LoadPageAsync()
    {
        var request = BuildRequest();
        request.Page = Pagination.Page;
        request.PageSize = Pagination.PageSize;

        Response<List<CustomerBalanceResponse>> response;
        using (PagingScope.Begin())
        {
            response = await customersApi.FilterBalances(request).Handle(l => IsLoading = l);
            if (response.IsSuccess) Pagination.Apply(PagingScope.Result);
        }

        if (!response.IsSuccess)
        {
            Error = response.Message ?? TranslationSource.T("Debitors.LoadDataError");
            return;
        }

        PagedDebitorCreditorItems = new ObservableCollection<DebitorCreditorItemViewModel>(response.Data!.Select(Map));
    }

    private async Task LoadSummaryAsync()
    {
        var response = await customersApi.BalancesSummary(BuildRequest()).Handle(l => IsLoading = l);
        if (!response.IsSuccess) return;

        FinalDiscount = response.Data!.Discount;
        FinalDebitor = response.Data.Debitor;
        FinalKreditor = response.Data.Creditor;
        FinalAmount = FinalDebitor - FinalKreditor - FinalDiscount;
    }

    private async Task<List<DebitorCreditorItemViewModel>> LoadAllAsync()
    {
        var request = BuildRequest();
        request.Page = 0;
        request.PageSize = 0;
        var response = await customersApi.FilterBalances(request).Handle(l => IsLoading = l);
        return response.IsSuccess ? response.Data!.Select(Map).ToList() : [];
    }

    private static DebitorCreditorItemViewModel Map(CustomerBalanceResponse x) => new()
    {
        Customer = x.Customer ?? string.Empty,
        Phone = x.Phone ?? string.Empty,
        Address = x.Address ?? string.Empty,
        Discount = x.Discount,
        Debitor = x.Debitor,
        Creditor = x.Creditor,
        TotalBalance = x.Creditor - x.Debitor
    };

    [RelayCommand]
    private void ClearFilter()
    {
        SelectedCustomer = AvailableCustomers.FirstOrDefault();
        Sign = "Barchasi";
        Amount = 0;
        SelectedBalanceType = "Barchasi";
        _ = ReloadAsync();
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        var items = await LoadAllAsync();
        if (items.Count == 0) { Info = TranslationSource.T("Debitors.NoDataForExport"); return; }
        try { if (ReportService.ExportExcel(BuildReport(items))) Success = TranslationSource.T("Debitors.ExportSuccess"); }
        catch (Exception ex) { Error = $"{TranslationSource.T("Debitors.ExcelExportError")}: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task Print()
    {
        var items = await LoadAllAsync();
        if (items.Count == 0) { Info = TranslationSource.T("Debitors.NoDataForPrint"); return; }
        ReportService.Print(BuildReport(items));
    }

    [RelayCommand]
    private async Task Preview()
    {
        var items = await LoadAllAsync();
        if (items.Count == 0) { Info = TranslationSource.T("Debitors.NoDataForPreview"); return; }
        ReportService.Preview(BuildReport(items));
    }

    private ReportDefinition<DebitorCreditorItemViewModel> BuildReport(List<DebitorCreditorItemViewModel> items)
    {
        var sumDiscount = items.Sum(x => x.Discount);
        var sumDebitor = items.Sum(x => x.Debitor);
        var sumCreditor = items.Sum(x => x.Creditor);
        return new ReportDefinition<DebitorCreditorItemViewModel>
        {
            Title = TranslationSource.T("Debitors.ReportTitle"),
            Subtitle = $"{TranslationSource.T("Debitors.OverallBalance")} {(sumDebitor - sumDiscount - sumCreditor):N2}",
            SheetName = "DebitorKreditor",
            FileName = "Debitor va Kreditorlar",
            Columns =
            [
                new() { Header = TranslationSource.T("Debitors.Customer"), Width = 138, Value = x => x.Customer },
                new() { Header = TranslationSource.T("Debitors.Phone"), Width = 92, Value = x => x.Phone },
                new() { Header = TranslationSource.T("Debitors.Address"), Width = 108, Value = x => x.Address },
                new() { Header = TranslationSource.T("Debitors.Bonus"), Width = 115, Align = ReportAlign.Right, IsNumber = true, Format = "N2", Value = x => x.Discount },
                new() { Header = TranslationSource.T("Debitors.Debitor"), Width = 130, Align = ReportAlign.Right, IsNumber = true, Format = "N2", Value = x => x.Debitor },
                new() { Header = TranslationSource.T("Debitors.Kreditor"), Width = 130, Align = ReportAlign.Right, IsNumber = true, Format = "N2", Value = x => x.Creditor },
            ],
            Rows = items,
            Totals = new ReportTotals { Label = TranslationSource.T("Debitors.TotalLabel"), LabelSpan = 3, Cells = [
                new() { Column = 3, Value = sumDiscount, Format = "N2" },
                new() { Column = 4, Value = sumDebitor, Format = "N2" },
                new() { Column = 5, Value = sumCreditor, Format = "N2" },
            ] },
        };
    }
}
