namespace VoltStream.WPF.Sales_history.Models;

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
using System.Threading.Tasks;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;

public partial class SalesHistoryPageViewModel : ViewModelBase
{
    private readonly IServiceProvider services;
    private readonly IMapper mapper;

    public PaginationViewModel Pagination { get; }

    [ObservableProperty] private ObservableCollection<ProductItemViewModel> pagedSaleItems = [];

    public SalesHistoryPageViewModel(IMapper mapper, IServiceProvider services)
    {
        this.services = services;
        this.mapper = mapper;
        Pagination = new PaginationViewModel(LoadPageAsync);
        _ = LoadInitialDataAsync();
    }

    [ObservableProperty] private CustomerResponse? selectedCustomer;
    [ObservableProperty] private ObservableCollection<CustomerResponse> customers = [];

    [ObservableProperty] private CategoryResponse? selectedCategory;
    [ObservableProperty] private ObservableCollection<CategoryResponse> categories = [];

    [ObservableProperty] private ProductResponse? selectedProduct;
    [ObservableProperty] private ObservableCollection<ProductResponse> allProducts = [];
    [ObservableProperty] private ObservableCollection<ProductResponse> products = [];

    [ObservableProperty] private decimal? finalAmount;
    [ObservableProperty] private DateTime beginDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime endDate = DateTime.Today;

    #region Load Data

    private async Task LoadInitialDataAsync()
    {
        await Task.WhenAll(LoadCategoriesAsync(), LoadProductsAsync(), LoadCustomersAsync());
        SelectedCustomer = Customers.FirstOrDefault();
        SelectedCategory = Categories.FirstOrDefault();
        await LoadPageAsync();
    }

    public async Task LoadCategoriesAsync()
    {
        var response = await services.GetRequiredService<ICategoriesApi>().GetAllAsync().Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess)
        {
            Categories = mapper.Map<ObservableCollection<CategoryResponse>>(response.Data!);
            Categories.Insert(0, new CategoryResponse { Name = TranslationSource.T("Common.All") });
        }
        else Error = response.Message ?? TranslationSource.T("SalesHistory.LoadCategoriesError");
    }

    public async Task LoadProductsAsync()
    {
        FilteringRequest request = new() { Filters = new() { ["Category"] = ["include"] } };

        var response = await services.GetRequiredService<IProductsApi>().Filter(request).Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess)
            AllProducts = mapper.Map<ObservableCollection<ProductResponse>>(response.Data!);
        else Error = response.Message ?? TranslationSource.T("SalesHistory.LoadProductsError");
    }

    public async Task LoadCustomersAsync()
    {
        var response = await services.GetRequiredService<ICustomersApi>().GetAllAsync().Handle();
        if (response.IsSuccess)
        {
            Customers = mapper.Map<ObservableCollection<CustomerResponse>>(response.Data!);
            Customers.Insert(0, new CustomerResponse { Name = TranslationSource.T("Common.All") });
        }
        else Error = response.Message ?? TranslationSource.T("SalesHistory.LoadCustomersError");
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

        var api = services.GetRequiredService<ISaleApi>();
        Response<List<SaleItemHistoryResponse>> response;
        using (PagingScope.Begin())
        {
            response = await api.FilterItems(request).Handle(l => IsLoading = l);
            if (response.IsSuccess) Pagination.Apply(PagingScope.Result);
        }

        if (!response.IsSuccess)
        {
            Error = response.Message ?? TranslationSource.T("SalesHistory.LoadError");
            return;
        }

        PagedSaleItems = new ObservableCollection<ProductItemViewModel>(response.Data!.Select(Map));
    }

    private async Task<List<ProductItemViewModel>> LoadAllItemsAsync()
    {
        var response = await services.GetRequiredService<ISaleApi>().FilterItems(BuildRequest()).Handle(l => IsLoading = l);
        return response.IsSuccess ? response.Data!.Select(Map).ToList() : [];
    }

    private SaleItemHistoryRequest BuildRequest() => new()
    {
        BeginDate = BeginDate,
        EndDate = EndDate,
        CustomerId = SelectedCustomer?.Id > 0 ? SelectedCustomer.Id : null,
        CategoryId = SelectedCategory?.Id > 0 ? SelectedCategory.Id : null,
        ProductId = SelectedProduct?.Id > 0 ? SelectedProduct.Id : null,
    };

    private static ProductItemViewModel Map(SaleItemHistoryResponse r) => new()
    {
        OperationDate = r.Date.LocalDateTime,
        Category = r.Category,
        Name = r.Name,
        RollLength = r.LengthPerRoll,
        Quantity = r.RollCount,
        Price = r.UnitPrice,
        Unit = r.Unit,
        TotalCount = (int)r.TotalLength,
        Customer = r.Customer,
    };

    #endregion Load Data

    #region Commands

    [RelayCommand]
    private async Task ClearFilter()
    {
        SelectedCategory = Categories.FirstOrDefault();
        SelectedProduct = Products.FirstOrDefault();
        SelectedCustomer = Customers.FirstOrDefault();
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        var items = await LoadAllItemsAsync();
        if (items.Count == 0) { Info = TranslationSource.T("SalesHistory.ExportNoData"); return; }
        try { if (ReportService.ExportExcel(BuildReport(items))) Success = TranslationSource.T("SalesHistory.ExportSuccess"); }
        catch { Error = TranslationSource.T("SalesHistory.ExportError"); }
    }

    [RelayCommand]
    private async Task Print()
    {
        var items = await LoadAllItemsAsync();
        if (items.Count == 0) { Info = TranslationSource.T("SalesHistory.PrintNoData"); return; }
        ReportService.Print(BuildReport(items));
    }

    [RelayCommand]
    private async Task Preview()
    {
        var items = await LoadAllItemsAsync();
        if (items.Count == 0) { Info = TranslationSource.T("SalesHistory.PreviewNoData"); return; }
        ReportService.Preview(BuildReport(items));
    }

    private ReportDefinition<ProductItemViewModel> BuildReport(List<ProductItemViewModel> items)
    {
        FinalAmount = items.Sum(x => x.TotalAmount ?? 0);
        var subtitle = $"{BeginDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}";
        if (SelectedCustomer is { Id: > 0 }) subtitle += $"  |  {TranslationSource.T("SalesHistory.CustomerLabel")} {SelectedCustomer.Name}";
        return new ReportDefinition<ProductItemViewModel>
        {
            Title = TranslationSource.T("SalesHistory.ReportTitle"),
            Subtitle = subtitle,
            SheetName = TranslationSource.T("SalesHistory.ReportName"),
            FileName = TranslationSource.T("SalesHistory.ReportName"),
            Columns =
            [
                new() { Header = TranslationSource.T("Common.Date"), Width = 60, Align = ReportAlign.Center, Value = x => x.OperationDate?.ToString("dd.MM.yyyy") },
                new() { Header = TranslationSource.T("SalesHistory.Customer"), Width = 82, Value = x => x.Customer },
                new() { Header = TranslationSource.T("SalesHistory.ProductType"), Width = 80, Value = x => x.Category },
                new() { Header = TranslationSource.T("Common.Name"), Width = 82, Value = x => x.Name },
                new() { Header = TranslationSource.T("SalesHistory.InRoll"), Width = 68, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = x => x.RollLength },
                new() { Header = TranslationSource.T("SalesHistory.RollCount"), Width = 60, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = x => x.Quantity },
                new() { Header = TranslationSource.T("Common.Total"), Width = 50, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = x => x.TotalCount },
                new() { Header = TranslationSource.T("SalesHistory.MeasureShort"), Width = 70, Align = ReportAlign.Center, Value = x => x.Unit },
                new() { Header = TranslationSource.T("Common.Price"), Width = 80, Align = ReportAlign.Right, IsNumber = true, Format = "N2", Value = x => x.Price },
                new() { Header = TranslationSource.T("SalesHistory.TotalAmount"), Width = 100, Align = ReportAlign.Right, IsNumber = true, Format = "N2", Value = x => x.TotalAmount },
            ],
            Rows = items,
            Totals = new ReportTotals { Label = TranslationSource.T("SalesHistory.GrandTotal"), LabelSpan = 9, Cells = [new() { Column = 9, Value = FinalAmount ?? 0, Format = "N2" }] },
        };
    }

    #endregion Commands

    #region Helpers

    private void UpdateProductList(CategoryResponse? category)
    {
        var source = category is { Id: > 0 }
            ? AllProducts.Where(p => p.CategoryId == category.Id)
            : AllProducts;
        Products = new ObservableCollection<ProductResponse>(source);
        Products.Insert(0, new ProductResponse { Name = TranslationSource.T("Common.All") });
        SelectedProduct = Products[0];
    }

    partial void OnSelectedCategoryChanged(CategoryResponse? value)
    {
        UpdateProductList(value);
        _ = ReloadAsync();
    }

    partial void OnSelectedProductChanged(ProductResponse? value) => _ = ReloadAsync();
    partial void OnSelectedCustomerChanged(CustomerResponse? value) => _ = ReloadAsync();

    #endregion Helpers
}
