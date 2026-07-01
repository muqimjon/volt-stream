namespace VoltStream.WPF.Products.Models;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;

public partial class ProductPageViewModel : ViewModelBase
{
    private readonly IServiceProvider services;
    private readonly IMapper mapper;

    public PaginationViewModel Pagination { get; }

    public ProductPageViewModel(IServiceProvider services)
    {
        mapper = services.GetRequiredService<IMapper>();
        this.services = services;
        Pagination = new PaginationViewModel(LoadPageAsync);
        _ = LoadInitialDataAsync();
    }

    [ObservableProperty] private CategoryResponse? selectedCategory;
    [ObservableProperty] private ObservableCollection<CategoryResponse> categories = [];

    [ObservableProperty] private ProductResponse? selectedProduct;
    [ObservableProperty] private ObservableCollection<ProductResponse> allProducts = [];
    [ObservableProperty] private ObservableCollection<ProductResponse> products = [];

    [ObservableProperty] private ObservableCollection<ProductItemViewModel> pagedProductItems = [];

    [ObservableProperty] private bool showAllBalances;

    partial void OnShowAllBalancesChanged(bool value) => _ = ReloadAsync();

    private async Task LoadInitialDataAsync()
    {
        await Task.WhenAll(LoadCategoriesAsync(), LoadProductsAsync());
        SelectedCategory = Categories.FirstOrDefault();
        await ReloadAsync();
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SelectedCategory = Categories.FirstOrDefault();
        SelectedProduct = Products.FirstOrDefault();
        ShowAllBalances = false;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        var items = await LoadAllItemsAsync();
        if (items.Count == 0) { Error = "Eksport qilish uchun ma'lumot topilmadi."; return; }
        try { if (ReportService.ExportExcel(BuildReport(items))) Success = "Ma'lumotlar muvaffaqiyatli Excel faylga eksport qilindi ✅"; }
        catch (Exception ex) { Error = $"Xatolik yuz berdi: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task Print()
    {
        var items = await LoadAllItemsAsync();
        if (items.Count == 0) { Info = "Chop etish uchun ma’lumot topilmadi."; return; }
        ReportService.Print(BuildReport(items));
    }

    [RelayCommand]
    private async Task Preview()
    {
        var items = await LoadAllItemsAsync();
        if (items.Count == 0) { Info = "Ko‘rsatish uchun ma’lumot yo‘q!"; return; }
        ReportService.Preview(BuildReport(items));
    }

    private ReportDefinition<ProductItemViewModel> BuildReport(List<ProductItemViewModel> items)
        => new()
        {
            Title = "Omborxonadagi mahsulotlar qoldiqlari",
            Subtitle = SelectedCategory is { Id: > 0 } ? $"Mahsulot turi: {SelectedCategory.Name}" : null,
            SheetName = "Mahsulotlar",
            FileName = "Mahsulotlar",
            Columns =
            [
                new() { Header = "Mahsulot turi", Width = 90, Value = x => x.Category },
                new() { Header = "Nomi", Width = 150, Value = x => x.Name },
                new() { Header = "To'plamda", Width = 80, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = x => x.RollLength },
                new() { Header = "To'plam soni", Width = 80, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = x => x.Quantity },
                new() { Header = "Jami", Width = 70, Align = ReportAlign.Right, IsNumber = true, Format = "N0", Value = x => x.TotalCount },
                new() { Header = "O'lchov birligi", Width = 60, Align = ReportAlign.Center, Value = x => x.Unit },
                new() { Header = "Narxi", Width = 90, Align = ReportAlign.Right, IsNumber = true, Format = "N2", Value = x => x.Price },
                new() { Header = "Umumiy summa", Width = 100, Align = ReportAlign.Right, IsNumber = true, Format = "N2", Value = x => x.TotalAmount },
            ],
            Rows = items,
            Totals = new ReportTotals { Label = "JAMI:", LabelSpan = 7, Cells = [new() { Column = 7, Value = items.Sum(x => x.TotalAmount ?? 0), Format = "N2" }] },
        };

    private async Task ReloadAsync()
    {
        Pagination.Reset();
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        var request = BuildFilters();
        request.Page = Pagination.Page;
        request.PageSize = Pagination.PageSize;

        var api = services.GetRequiredService<IWarehouseStocksApi>();
        Response<List<WarehouseStockResponse>> response;
        using (PagingScope.Begin())
        {
            response = await api.Filter(request).Handle(l => IsLoading = l);
            if (response.IsSuccess) Pagination.Apply(PagingScope.Result);
        }

        if (!response.IsSuccess)
        {
            Error = response.Message ?? "Ombor qoldiqlarini yuklashda xatolik yuz berdi.";
            return;
        }

        PagedProductItems = new ObservableCollection<ProductItemViewModel>(response.Data!.Select(Map));
    }

    private async Task<List<ProductItemViewModel>> LoadAllItemsAsync()
    {
        var response = await services.GetRequiredService<IWarehouseStocksApi>().Filter(BuildFilters()).Handle(l => IsLoading = l);
        return response.IsSuccess ? response.Data!.Select(Map).ToList() : [];
    }

    private FilteringRequest BuildFilters()
    {
        var filters = new Dictionary<string, List<string>> { ["Product"] = ["include:Category"] };

        if (!ShowAllBalances) filters["TotalLength"] = [">0"];

        if (SelectedProduct is { Id: > 0 })
            filters["ProductId"] = [$"={SelectedProduct.Id}"];
        else if (SelectedCategory is { Id: > 0 })
            filters["ProductId"] = [$"in:{string.Join(',', AllProducts.Where(p => p.CategoryId == SelectedCategory.Id).Select(p => p.Id))}"];

        return new FilteringRequest { Filters = filters };
    }

    private static ProductItemViewModel Map(WarehouseStockResponse item) => new()
    {
        Category = item.Product.Category?.Name,
        Name = item.Product.Name,
        RollLength = item.LengthPerRoll,
        Quantity = item.RollCount,
        Price = item.UnitPrice,
        Unit = item.Product.Unit,
        TotalCount = (int)item.TotalLength,
    };

    partial void OnSelectedCategoryChanged(CategoryResponse? value)
    {
        UpdateProductList(value);
        _ = ReloadAsync();
    }

    private void UpdateProductList(CategoryResponse? category)
    {
        var source = category is { Id: > 0 }
            ? AllProducts.Where(p => p.CategoryId == category.Id)
            : AllProducts;
        Products = new ObservableCollection<ProductResponse>(source);
        Products.Insert(0, new ProductResponse { Name = "Barchasi" });
        SelectedProduct = Products[0];
    }

    partial void OnSelectedProductChanged(ProductResponse? value) => _ = ReloadAsync();

    public async Task LoadCategoriesAsync()
    {
        var response = await services.GetRequiredService<ICategoriesApi>().GetAllAsync().Handle(l => IsLoading = l);
        if (response.IsSuccess)
        {
            Categories = mapper.Map<ObservableCollection<CategoryResponse>>(response.Data!);
            Categories.Insert(0, new CategoryResponse { Name = "Barchasi" });
        }
        else Error = response.Message ?? "Kategoriyalar yuklanmadi.";
    }

    public async Task LoadProductsAsync()
    {
        var response = await services.GetRequiredService<IProductsApi>().GetAllAsync().Handle(l => IsLoading = l);
        if (response.IsSuccess)
            AllProducts = mapper.Map<ObservableCollection<ProductResponse>>(response.Data!);
        else Error = response.Message ?? "Mahsulotlar yuklanmadi.";
    }
}
