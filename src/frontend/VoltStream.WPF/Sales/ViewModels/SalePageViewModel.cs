namespace VoltStream.WPF.Sales.ViewModels;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.ViewModels;

public partial class SalePageViewModel : ViewModelBase
{
    private readonly IMapper mapper;
    private readonly ICurrenciesApi currenciesApi;
    private readonly ICustomersApi customersApi;
    private readonly ICategoriesApi categoriesApi;
    private readonly IWarehouseStocksApi stocksApi;

    public SalePageViewModel(IServiceProvider services)
    {
        mapper = services.GetRequiredService<IMapper>();
        currenciesApi = services.GetRequiredService<ICurrenciesApi>();
        customersApi = services.GetRequiredService<ICustomersApi>();
        categoriesApi = services.GetRequiredService<ICategoriesApi>();
        stocksApi = services.GetRequiredService<IWarehouseStocksApi>();

        _ = LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        await Task.WhenAll(
            LoadCurrenciesAsync(),
            LoadCategoryAndProductsAsync(),
            LoadCustomersAsync()
        );
    }

    [ObservableProperty] private long id;
    [ObservableProperty] private DateTime date = DateTime.Now;
    [ObservableProperty] private decimal? amount;
    [ObservableProperty] private int rollCount;
    [ObservableProperty] private decimal length;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private decimal? finalSum;

    [ObservableProperty] private long customerOperationId;
    [ObservableProperty] private CustomerOperationViewModel customerOperation = new();

    [ObservableProperty] private decimal? discount;
    [ObservableProperty] private bool isApplied;

    [ObservableProperty] private long currencyId;
    [ObservableProperty] private CurrencyViewModel currency = new();

    [ObservableProperty] private long customerId;
    [ObservableProperty] private CustomerViewModel? customer;

    [ObservableProperty] private ObservableCollection<SaleItemViewModel> items = [];

    #region Commands

    [ObservableProperty] private SaleItemViewModel currentItem = new();
    [RelayCommand]
    public void Add()
    {
        Items.Add(CurrentItem);
        CurrentItem = new();
    }

    #endregion Commands

    #region Currencies combobox

    [ObservableProperty] private ObservableCollection<CurrencyViewModel> currencies = [];

    private async Task LoadCurrenciesAsync()
    {
        var response = await currenciesApi.GetAllAsync().Handle(l => IsLoading = l);
        if (response.IsSuccess)
        {
            Currencies = new(mapper.Map<ObservableCollection<CurrencyViewModel>>(response.Data!));
            if (CurrencyId > 0)
                Currency = Currencies.FirstOrDefault(c => c.Id == CurrencyId)!;
        }
        else Error = response.Message ?? TranslationSource.T("Sales.CurrenciesLoadError");
    }

    partial void OnCurrencyChanged(CurrencyViewModel value)
    {
        CurrencyId = value.Id;
    }

    #endregion Currencies combobox

    #region Customer combobox

    [ObservableProperty] private ObservableCollection<CustomerViewModel> customers = [];

    private async Task LoadCustomersAsync()
    {
        FilteringRequest request = new() { Filters = new() { ["Accounts"] = ["include:Currency"] } };

        var response = await customersApi.FilterAsync(request).Handle();
        if (response.IsSuccess)
        {
            Customers = new(mapper.Map<ObservableCollection<CustomerViewModel>>(response.Data!));
            if (CustomerId > 0)
                Customer = Customers.FirstOrDefault(c => c.Id == CustomerId)!;
        }
        else Error = response.Message ?? TranslationSource.T("Sales.CustomersLoadFailed");
    }

    #endregion Customer combobox

    #region Categories combobox

    [ObservableProperty] private ObservableCollection<CategoryViewModel> categories = [];
    [ObservableProperty] private CategoryViewModel? selectedCategory;
    [ObservableProperty] private long? selectedCategoryId;
    partial void OnSelectedCategoryIdChanged(long? value)
    {
        RefreshProducts();
    }

    partial void OnSelectedCategoryChanged(CategoryViewModel? value)
    {
        selectedCategoryId = value?.Id;
        RefreshProducts();
    }

    private async Task LoadCategoryAndProductsAsync()
    {
        var request = new FilteringRequest { Filters = new() { ["products"] = ["include"] } };

        var response = await categoriesApi.Filter(request).Handle(l => IsLoading = l);
        if (!response.IsSuccess) { Error = response.Message ?? TranslationSource.T("Sales.CategoriesLoadFailed"); return; }

        Categories = mapper.Map<ObservableCollection<CategoryViewModel>>(response.Data!);
        if (SelectedCategoryId > 0)
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == SelectedCategoryId);
        RefreshProducts();
    }

    #endregion Categories combobox

    #region Products combobox

    [ObservableProperty] private ObservableCollection<ProductViewModel> products = [];

    private void RefreshProducts()
    {
        if (SelectedCategoryId > 0)
        {
            if (SelectedCategory is null)
            {
                Products = mapper.Map<ObservableCollection<ProductViewModel>>(Categories.SelectMany(c => c.Products));
                Error = TranslationSource.T("Sales.CategoryNotExists");
            }
            else Products = SelectedCategory.Products;
        }
        else Products = mapper.Map<ObservableCollection<ProductViewModel>>(Categories.SelectMany(c => c.Products));
    }

    #endregion Products combobox
}
