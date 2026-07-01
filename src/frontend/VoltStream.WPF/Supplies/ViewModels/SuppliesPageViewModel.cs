namespace VoltStream.WPF.Supplies.ViewModels;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Messages;
using VoltStream.WPF.Commons.ViewModels;
using VoltStream.WPF.Sales.ViewModels;

public partial class SuppliesPageViewModel : ViewModelBase
{
    private readonly IProductsApi productsApi;
    private readonly ICategoriesApi categoriesApi;
    private readonly ISuppliesApi suppliesApi;
    private readonly IWarehouseStocksApi warehouseItemsApi;
    private readonly IMapper mapper;

    public PaginationViewModel Pagination { get; }

    public SuppliesPageViewModel(IServiceProvider services)
    {
        productsApi = services.GetRequiredService<IProductsApi>();
        categoriesApi = services.GetRequiredService<ICategoriesApi>();
        suppliesApi = services.GetRequiredService<ISuppliesApi>();
        warehouseItemsApi = services.GetRequiredService<IWarehouseStocksApi>();
        mapper = services.GetRequiredService<IMapper>();

        Pagination = new PaginationViewModel(LoadPageAsync);

        SelectedDate = DateTime.Now;

        _ = Task.Run(LoadDataAsync);
    }

    [ObservableProperty] private DateTime selectedDate;

    [ObservableProperty] private ObservableCollection<CategoryViewModel> categories = [];
    [ObservableProperty] private CategoryViewModel? selectedCategory;
    [ObservableProperty] private string categoryText = string.Empty;

    [ObservableProperty] private ObservableCollection<ProductViewModel> products = [];
    [ObservableProperty] private ProductViewModel? selectedProduct;
    [ObservableProperty] private string productText = string.Empty;

    [ObservableProperty] private ObservableCollection<WarehouseStockViewModel> warehouseStocks = [];
    [ObservableProperty] private WarehouseStockViewModel? selectedWarehouseStock;
    [ObservableProperty] private decimal? warehouseStockValue;

    [ObservableProperty] private int? rollCount;
    [ObservableProperty] private decimal? unitPrice;
    [ObservableProperty] private decimal? discountRate;
    [ObservableProperty] private string? unit;

    [ObservableProperty] private ObservableCollection<SupplyViewModel> pagedSupplies = [];
    [ObservableProperty] private SupplyViewModel? selectedSupply;

    [ObservableProperty] private SupplyViewModel? editingItemBackup;
    private bool _isFilingForm;

    public decimal? TotalQuantity => WarehouseStockValue * RollCount;

    #region Property Change Handlers

    partial void OnSelectedDateChanged(DateTime value) => _ = ReloadAsync();

    partial void OnSelectedCategoryChanged(CategoryViewModel? value)
    {
        if (_isFilingForm || value == null) return;
        CategoryText = value.Name;
        _ = LoadProductsAsync(value.Id);
    }

    partial void OnCategoryTextChanged(string? oldValue, string newValue)
    {
        if (_isFilingForm || string.IsNullOrWhiteSpace(newValue) || (SelectedCategory?.Name == newValue)) return;

        Categories.Remove(Categories.FirstOrDefault(c => c.Name == oldValue && c.Id < 1)!);
        var existing = Categories.FirstOrDefault(c => string.Equals(c.Name, newValue, StringComparison.OrdinalIgnoreCase));

        if (existing != null) SelectedCategory = existing;
        else if (Confirm($"'{newValue}' yangi kategoriya sifatida qo'shilsinmi?"))
        {
            Categories.Add(new() { Name = newValue });
            SelectedCategory = null;
        }
        else { CategoryText = string.Empty; WeakReferenceMessenger.Default.Send(new FocusControlMessage("Category")); }
    }

    partial void OnSelectedProductChanged(ProductViewModel? value)
    {
        if (_isFilingForm || value == null) return;
        ProductText = value.Name;
        Unit = value.Unit ?? "metr";
        _ = LoadWarehouseStocksAsync(value.Id);
    }

    partial void OnProductTextChanged(string? oldValue, string newValue)
    {
        if (_isFilingForm || string.IsNullOrWhiteSpace(newValue) || (SelectedProduct?.Name == newValue)) return;

        Products.Remove(Products.FirstOrDefault(p => p.Name == oldValue && p.Id < 1)!);
        var existing = Products.FirstOrDefault(p => string.Equals(p.Name, newValue, StringComparison.OrdinalIgnoreCase));

        if (existing != null) SelectedProduct = existing;
        else if (Confirm($"'{newValue}' yangi mahsulot sifatida qo'shilsinmi?"))
        {
            Products.Add(new() { Name = newValue });
            SelectedProduct = null;
            WarehouseStocks = [];
        }
        else { ProductText = string.Empty; WeakReferenceMessenger.Default.Send(new FocusControlMessage("Product")); }
    }

    partial void OnSelectedWarehouseStockChanged(WarehouseStockViewModel? value)
    {
        if (value == null) return;
        UnitPrice = value.UnitPrice;
        DiscountRate = value.DiscountRate;
        OnPropertyChanged(nameof(TotalQuantity));
    }

    partial void OnWarehouseStockValueChanged(decimal? oldValue, decimal? newValue)
    {
        if (_isFilingForm || newValue == null || SelectedWarehouseStock?.LengthPerRoll == newValue) return;

        WarehouseStocks.Remove(WarehouseStocks.FirstOrDefault(w => w.LengthPerRoll == oldValue && w.Id < 1)!);
        var existing = WarehouseStocks.FirstOrDefault(w => w.LengthPerRoll == newValue);

        if (existing != null) SelectedWarehouseStock = existing;
        else if (Confirm($"'{newValue}' yangi to'plam o'lchami sifatida qo'shilsinmi?"))
        {
            WarehouseStocks.Add(new() { LengthPerRoll = newValue.Value });
            SelectedWarehouseStock = null;
            UnitPrice = null;
            DiscountRate = null;
        }
        else { WarehouseStockValue = null; WeakReferenceMessenger.Default.Send(new FocusControlMessage("WarehouseStock")); }

        OnPropertyChanged(nameof(TotalQuantity));
    }

    partial void OnDiscountRateChanged(decimal? value)
    {
        if (value > 100)
        {
            Warning = "Chegirma 100% dan yuqori bo'lishi mumkin emas!";
            DiscountRate = 100;
        }
    }

    partial void OnRollCountChanged(int? value) => OnPropertyChanged(nameof(TotalQuantity));

    #endregion Property Change Handlers

    #region Load Data

    private async Task LoadDataAsync()
    {
        await Task.WhenAll(
            LoadCategoriesAsync(),
            LoadPageAsync()
        );
    }

    private async Task LoadCategoriesAsync()
    {
        var result = await categoriesApi.GetAllAsync().Handle(isLoading => IsLoading = isLoading);
        if (result.IsSuccess) Categories = mapper.Map<ObservableCollection<CategoryViewModel>>(result.Data);
        else Error = result.Message ?? "Kategoriyalarni yuklashda xatolik";
    }

    private async Task LoadProductsAsync(long? categoryId)
    {
        if (categoryId is null)
        {
            var result = await productsApi.GetAllAsync().Handle(isLoading => IsLoading = isLoading);
            if (result.IsSuccess) Products = mapper.Map<ObservableCollection<ProductViewModel>>(result.Data);
            else Error = result.Message ?? "Mahsulotlarni yuklashda xatolik";
        }
        else
        {
            var result = await productsApi.GetAllByCategoryIdAsync(categoryId.Value).Handle(isLoading => IsLoading = isLoading);
            if (result.IsSuccess) Products = mapper.Map<ObservableCollection<ProductViewModel>>(result.Data);
            else Error = result.Message ?? "Mahsulotlarni yuklashda xatolik";
        }
    }

    private FilteringRequest BuildFilters() => new()
    {
        Filters = new()
        {
            ["date"] = [$"{SelectedDate:yyyy-MM-dd}"],
            ["product"] = ["include:category"]
        },
        Descending = true
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

        using var scope = PagingScope.Begin();
        var response = await suppliesApi.Filter(request).Handle(isLoading => IsLoading = isLoading);
        if (!response.IsSuccess) { Error = response.Message ?? "Ta'minotlarni yuklashda xatolik"; return; }

        Pagination.Apply(PagingScope.Result);
        PagedSupplies = new ObservableCollection<SupplyViewModel>(response.Data!.Select(MapToViewModel));
    }

    private async Task LoadWarehouseStocksAsync(long productId)
    {
        FilteringRequest request = new() { Filters = new() { ["productId"] = [productId.ToString()] } };
        var response = await warehouseItemsApi.Filter(request).Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess) WarehouseStocks = mapper.Map<ObservableCollection<WarehouseStockViewModel>>(response.Data);
        else Error = response.Message ?? "Ombor mahsulotlarini yuklashda xatolik yuz berdi.";
    }

    #endregion Load Data

    #region Commands

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(CategoryText) || string.IsNullOrWhiteSpace(ProductText))
        {
            Error = "Kategoriya va Mahsulot nomi kiritilishi shart!";
            return;
        }

        var request = new SupplyRequest
        {
            Id = IsEditing && EditingItemBackup is not null ? EditingItemBackup.Id : 0,
            Date = (SelectedDate.Date == DateTime.Today ? DateTime.Now : SelectedDate).ToUniversalTime(),
            CategoryId = SelectedCategory?.Id ?? 0,
            ProductId = SelectedProduct?.Id ?? 0,
            RollCount = RollCount ?? 0,
            Unit = Unit!,
            LengthPerRoll = WarehouseStockValue ?? 0,
            TotalLength = TotalQuantity ?? 0,
            ProductName = ProductText,
            CategoryName = CategoryText,
            UnitPrice = UnitPrice ?? 0,
            DiscountRate = DiscountRate ?? 0
        };

        var isSuccess = false;
        string errorMsg = "";

        if (IsEditing)
        {
            var result = await suppliesApi.UpdateSupplyAsync(request).Handle(isLoading => IsLoading = isLoading);
            if (result.IsSuccess) isSuccess = true;
            else errorMsg = result.Message ?? "Mahsulotni yangilashda xatolik yuz berdi.";
        }
        else
        {
            var result = await suppliesApi.CreateSupplyAsync(request).Handle(isLoading => IsLoading = isLoading);
            if (result.IsSuccess) isSuccess = true;
            else errorMsg = result.Message ?? "Mahsulot qo'shishda xatolik yuz berdi.";
        }

        if (isSuccess)
        {
            if (IsEditing) ResetEditState();
            else ClearForm();
            await ReloadAsync();
        }
        else Error = errorMsg;
    }

    [RelayCommand]
    private async Task EditItem(SupplyViewModel item)
    {
        if (item is null) return;

        if (IsFormDirty() && !Confirm("Formada saqlanmagan ma'lumotlar mavjud. Agar davom ettirsangiz, ular o'chib ketadi. Davom ettirishni istaysizmi?"))
            return;

        IsEditing = true;
        EditingItemBackup = item;

        await FillFormFromItem(item);
    }

    [RelayCommand]
    private void CancelEdit() => ResetEditState();

    private void ResetEditState()
    {
        if (!IsEditing) return;
        ClearForm();
        IsEditing = false;
        EditingItemBackup = null;
    }

    [RelayCommand]
    private async Task DeleteItem(SupplyViewModel item)
    {
        if (item is null) return;

        if (!Confirm("Haqiqatan ham o'chirmoqchimisiz?")) return;

        var response = await suppliesApi.DeleteSupplyAsync(item.Id).Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess && response.Data) await LoadPageAsync();
        else Error = response.Message ?? "O'chirishda xatolik yuz berdi.";
    }

    #endregion Commands

    #region Helper Methods

    private bool IsFormDirty()
    {
        if (!string.IsNullOrWhiteSpace(CategoryText) ||
            !string.IsNullOrWhiteSpace(ProductText) ||
            RollCount > 0 ||
            WarehouseStockValue > 0 ||
            UnitPrice > 0 ||
            DiscountRate > 0)
        {
            return true;
        }
        return false;
    }

    private async Task FillFormFromItem(SupplyViewModel item)
    {
        _isFilingForm = true;
        try
        {
            if (SelectedCategory?.Id != item.CategoryId)
            {
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == item.CategoryId);
                CategoryText = SelectedCategory?.Name ?? item.CategoryName;
                if (SelectedCategory != null) await LoadProductsAsync(SelectedCategory.Id);
            }

            if (SelectedProduct?.Id != item.ProductId)
            {
                SelectedProduct = Products.FirstOrDefault(p => p.Id == item.ProductId) ?? item.Product;
                ProductText = SelectedProduct?.Name ?? item.ProductName;
            }

            if (SelectedProduct != null) await LoadWarehouseStocksAsync(SelectedProduct.Id);

            WarehouseStockValue = item.LengthPerRoll;
            SelectedWarehouseStock = WarehouseStocks.FirstOrDefault(w => w.LengthPerRoll == item.LengthPerRoll);

            RollCount = item.RollCount;
            UnitPrice = item.UnitPrice;
            DiscountRate = item.DiscountRate;
            Unit = item.Unit;
            OnPropertyChanged(nameof(TotalQuantity));
        }
        finally { _isFilingForm = false; }
    }

    private void ClearForm()
    {
        SelectedCategory = null;
        CategoryText = string.Empty;
        SelectedProduct = null;
        ProductText = string.Empty;
        WarehouseStockValue = null;
        SelectedWarehouseStock = null;
        RollCount = null;
        UnitPrice = null;
        DiscountRate = null;
        Unit = null!;
        OnPropertyChanged(nameof(TotalQuantity));
    }

    private SupplyViewModel MapToViewModel(SupplyResponse viewModel)
    {
        var vm = mapper.Map<SupplyViewModel>(viewModel);
        vm.CategoryName = viewModel.Product?.Category?.Name ?? "";
        vm.ProductName = viewModel.Product?.Name ?? "";
        vm.Product = mapper.Map<ProductViewModel>(viewModel.Product!);
        return vm;
    }

    private static bool Confirm(string message) =>
        MessageBox.Show(message, "Tasdiqlash", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    #endregion Helper Methods
}
