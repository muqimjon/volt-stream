namespace VoltStream.WPF.Turnovers.Models;

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
using System.ComponentModel;
using System.Windows;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Messages;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;
using VoltStream.WPF.Sales.ViewModels;

public partial class SaleEditViewModel : ViewModelBase
{
    private readonly IMapper mapper;
    private readonly ISaleApi saleApi;
    private readonly ICustomersApi customersApi;
    private readonly ICurrenciesApi currenciesApi;
    private readonly ICategoriesApi categoriesApi;
    private readonly IProductsApi productsApi;
    private readonly IWarehouseStocksApi warehouseStocksApi;
    private readonly INavigationService navigationService;

    private SaleItemViewModel? originalItem;
    private int originalItemIndex = -1;
    private bool isCalculating = false;
    private bool suppressLoading = false;
    private decimal? previousTotalLength = null;
    private int? previousRollCount = null;

    public SaleEditViewModel(IServiceProvider services, SaleResponse saleData)
    {
        mapper = services.GetRequiredService<IMapper>();
        saleApi = services.GetRequiredService<ISaleApi>();
        customersApi = services.GetRequiredService<ICustomersApi>();
        currenciesApi = services.GetRequiredService<ICurrenciesApi>();
        categoriesApi = services.GetRequiredService<ICategoriesApi>();
        productsApi = services.GetRequiredService<IProductsApi>();
        warehouseStocksApi = services.GetRequiredService<IWarehouseStocksApi>();
        navigationService = services.GetRequiredService<INavigationService>();

        Sale = mapper.Map<SaleViewModel>(saleData);
        Sale.PropertyChanged += OnSalePropertyChanged;
        CurrentItem.PropertyChanged += OnCurrentItemPropertyChanged;

        _ = LoadPageAsync();
        RecalculateSaleTotals();
    }

    [ObservableProperty] private SaleViewModel sale = new();
    [ObservableProperty] private SaleItemViewModel currentItem = new();
    [ObservableProperty] private ObservableCollection<CustomerViewModel> customers = [];
    [ObservableProperty] private ObservableCollection<CurrencyViewModel> currencies = [];
    [ObservableProperty] private ObservableCollection<CategoryViewModel> categories = [];
    [ObservableProperty] private ObservableCollection<ProductViewModel> products = [];
    [ObservableProperty] private ObservableCollection<WarehouseStockViewModel> warehouseStocks = [];
    [ObservableProperty] private CustomerViewModel? selectedCustomer;
    [ObservableProperty] private CurrencyViewModel? selectedCurrency;
    [ObservableProperty] private CategoryViewModel? selectedCategory;
    [ObservableProperty] private ProductViewModel? selectedProduct;
    [ObservableProperty] private WarehouseStockViewModel? selectedWarehouseStock;
    [ObservableProperty] private string categorySearchText = string.Empty;
    [ObservableProperty] private string productSearchText = string.Empty;
    [ObservableProperty] private string warehouseStockSearchText = string.Empty;
    [ObservableProperty] private decimal beginBalance;
    [ObservableProperty] private decimal lastBalance;
    [ObservableProperty] private decimal totalSum;

    #region Load Data

    private async Task LoadPageAsync()
    {
        await Task.WhenAll(
            LoadCustomersAsync(),
            LoadCurrenciesAsync(),
            LoadCategoriesAsync(),
            LoadProductsAsync()
        );

        SelectedCustomer = Customers.FirstOrDefault(c => c.Id == Sale.CustomerId);
        SelectedCurrency = Currencies.FirstOrDefault(c => c.Id == Sale.CurrencyId);
    }

    private async Task LoadCustomersAsync()
    {
        var request = new FilteringRequest
        {
            Filters = new() { ["accounts"] = ["include:currency"] }
        };

        var response = await customersApi.FilterAsync(request)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
        {
            Customers = mapper.Map<ObservableCollection<CustomerViewModel>>(response.Data!);
            RestoreOriginalCustomerBalance();
        }
        else Error = response.Message ?? TranslationSource.T("Turnovers.CustomersLoadFailed");
    }

    private void RestoreOriginalCustomerBalance()
    {
        var account = Customers.FirstOrDefault(c => c.Id == Sale.CustomerId)?
            .Accounts.FirstOrDefault(a => a.CurrencyId == Sale.CurrencyId);

        if (account is not null)
        {
            account.Balance += Sale.Amount;
            if (!Sale.IsDiscountApplied)
            {
                account.Discount -= Sale.Discount;
                account.Balance += Sale.Discount;
            }
        }
    }

    private async Task LoadCurrenciesAsync()
    {
        var request = new FilteringRequest
        {
            Filters = new() { ["isactive"] = ["true"] }
        };

        var response = await currenciesApi.Filter(request)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
            Currencies = mapper.Map<ObservableCollection<CurrencyViewModel>>(response.Data!);
        else Error = response.Message ?? TranslationSource.T("Turnovers.CurrenciesLoadFailed");
    }

    private async Task LoadCategoriesAsync()
    {
        var response = await categoriesApi.GetAllAsync()
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
            Categories = mapper.Map<ObservableCollection<CategoryViewModel>>(response.Data!);
        else Error = response.Message ?? TranslationSource.T("Turnovers.CategoriesLoadFailed");
    }

    private async Task LoadProductsAsync()
    {
        var request = new FilteringRequest
        {
            Filters = new() { ["category"] = ["include"] }
        };

        if (SelectedCategory?.Id > 0)
            request.Filters["CategoryId"] = [SelectedCategory.Id.ToString()];

        var response = await productsApi.Filter(request)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
            Products = mapper.Map<ObservableCollection<ProductViewModel>>(response.Data!);
        else Error = response.Message ?? TranslationSource.T("Turnovers.ProductsLoadFailed");
    }

    private async Task LoadWarehouseStocksAsync()
    {
        if (SelectedProduct?.Id <= 0) return;

        var response = await warehouseStocksApi.GetProductDetailsFromWarehouseAsync(SelectedProduct!.Id)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
            WarehouseStocks = mapper.Map<ObservableCollection<WarehouseStockViewModel>>(response.Data!);
        else Error = response.Message ?? TranslationSource.T("Turnovers.WarehouseLoadFailed");
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void Add()
    {
        if (!ValidateCurrentItem()) return;

        CurrentItem.ProductId = SelectedProduct!.Id;
        CurrentItem.Product = MapProduct(SelectedProduct);

        if (IsEditing && originalItem is not null)
        {
            Sale.Items.Insert(originalItemIndex, CurrentItem);
            ResetEditMode();
        }
        else Sale.Items.Insert(0, CurrentItem);

        RecalculateSaleTotals();
        ClearCurrentItem();
    }

    [RelayCommand]
    public async Task EditItem(SaleItemViewModel? item)
    {
        if (item is null) return;

        await LoadItemRelatedDataAsync(item);

        originalItemIndex = Sale.Items.IndexOf(item);
        originalItem = item;
        CurrentItem = CloneItem(item);
        IsEditing = true;

        Sale.Items.RemoveAt(originalItemIndex);
        RecalculateSaleTotals();

        previousTotalLength = item.TotalLength;
        previousRollCount = item.RollCount;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        if (IsEditing && originalItem is not null)
        {
            Sale.Items.Insert(originalItemIndex, originalItem);
            ResetEditMode();
            RecalculateSaleTotals();
            ClearCurrentItem();
        }
    }

    [RelayCommand]
    private void DeleteItem(SaleItemViewModel? item)
    {
        if (item is null) return;

        if (ShowConfirmation(TranslationSource.T("Turnovers.ConfirmDeleteProduct")))
        {
            Sale.Items.Remove(item);
            RecalculateSaleTotals();
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!ValidateSale()) return;
        if (!ShowConfirmation(TranslationSource.T("Turnovers.ConfirmSaveChanges"))) return;

        var request = new SaleRequest
        {
            Id = Sale.Id,
            Date = Sale.Date,
            CustomerId = Sale.CustomerId,
            CurrencyId = Sale.CurrencyId,
            Amount = Sale.Amount,
            Discount = Sale.Discount,
            Description = Sale.Description,
            Length = Sale.Length,
            RollCount = Sale.RollCount,
            IsDiscountApplied = Sale.IsDiscountApplied,
            Items = mapper.Map<List<SaleItemRequest>>(Sale.Items)
        };

        var response = await saleApi.Update(request)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
        {
            Success = TranslationSource.T("Turnovers.SaleUpdated");
            WeakReferenceMessenger.Default.Send(new EntityUpdatedMessage<string>("OperationUpdated"));
            navigationService.GoBack();
        }
        else Error = response.Message ?? TranslationSource.T("Turnovers.SaleUpdateFailed");
    }

    [RelayCommand]
    private void Cancel()
    {
        if (ShowConfirmation(TranslationSource.T("Turnovers.ConfirmDiscardChanges")))
            navigationService.GoBack();
    }

    #endregion

    #region Event Handlers

    partial void OnSelectedCustomerChanged(CustomerViewModel? value)
    {
        if (value is not null)
        {
            Sale.CustomerId = value.Id;
            UpdateCustomerBalance();
        }
    }

    partial void OnSelectedCurrencyChanged(CurrencyViewModel? value)
    {
        if (value is not null)
        {
            Sale.CurrencyId = value.Id;
            UpdateCustomerBalance();
        }
    }

    partial void OnSelectedProductChanged(ProductViewModel? value)
    {
        if (isCalculating) return;

        ExecuteWithCalculationLock(() =>
        {
            if (value is not null)
            {
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == value.Category?.Id);
                ProductSearchText = value.Name;
                _ = LoadWarehouseStocksAsync();
            }
            else
            {
                ProductSearchText = string.Empty;
            }
        });
    }

    partial void OnSelectedCategoryChanged(CategoryViewModel? value)
    {
        if (isCalculating) return;

        if (value is not null)
        {
            ExecuteWithCalculationLock(() =>
            {
                CategorySearchText = value.Name;
                if (!suppressLoading)
                    _ = LoadProductsAsync();
            });
        }
        else
        {
            CategorySearchText = string.Empty;
        }
    }

    partial void OnSelectedWarehouseStockChanging(WarehouseStockViewModel? value)
    {
        if (value is null) return;

        ExecuteWithCalculationLock(() =>
        {
            CurrentItem.LengthPerRoll = value.LengthPerRoll;
            CurrentItem.UnitPrice = value.UnitPrice;
            if (!IsEditing)
            {
                CurrentItem.DiscountRate = value.DiscountRate;
            }
            WarehouseStockSearchText = value.LengthPerRoll.ToString();
        });

        CalculateFromRollCount();
    }

    partial void OnCategorySearchTextChanged(string value) =>
        UpdateSearchSelection(value, Categories, c => c.Name, v => CategorySearchText = v, v => SelectedCategory = v);

    partial void OnProductSearchTextChanged(string value) =>
        UpdateSearchSelection(value, Products, p => p.Name, v => ProductSearchText = v, v => SelectedProduct = v);

    partial void OnWarehouseStockSearchTextChanged(string value)
    {
        if (isCalculating) return;

        if (!decimal.TryParse(value, out decimal enteredLength))
        {
            SelectedWarehouseStock = null;
            WarehouseStockSearchText = string.Empty;
            return;
        }

        var exactMatch = WarehouseStocks.FirstOrDefault(s => s.LengthPerRoll == enteredLength);
        if (exactMatch is not null)
        {
            SelectedWarehouseStock = exactMatch;
            return;
        }

        var nearest = WarehouseStocks
            .OrderBy(s => Math.Abs(s.LengthPerRoll - enteredLength))
            .FirstOrDefault();

        SelectedWarehouseStock = nearest;
        if (SelectedWarehouseStock is null)
            WarehouseStockSearchText = string.Empty;
    }

    partial void OnCurrentItemChanged(SaleItemViewModel? oldValue, SaleItemViewModel newValue)
    {
        if (oldValue is not null)
            oldValue.PropertyChanged -= OnCurrentItemPropertyChanged;

        if (newValue is not null)
        {
            newValue.PropertyChanged += OnCurrentItemPropertyChanged;
            previousTotalLength = newValue.TotalLength;
            previousRollCount = newValue.RollCount;
        }
    }

    private void OnCurrentItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (isCalculating || sender is not SaleItemViewModel) return;

        ExecuteWithCalculationLock(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(SaleItemViewModel.RollCount):
                    CalculateFromRollCount();
                    break;
                case nameof(SaleItemViewModel.TotalLength):
                    CalculateFromTotalLength();
                    break;
                case nameof(SaleItemViewModel.UnitPrice):
                    CalculateFromUnitPrice();
                    break;
                case nameof(SaleItemViewModel.TotalAmount):
                    CalculateFromTotalAmount();
                    break;
                case nameof(SaleItemViewModel.DiscountRate):
                    CalculateFromDiscountRate();
                    break;
                case nameof(SaleItemViewModel.DiscountAmount):
                    CalculateFromDiscountAmount();
                    break;
                case nameof(SaleItemViewModel.FinalAmount):
                    CalculateFromFinalAmount();
                    break;
            }
        });
    }

    private void OnSalePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SaleViewModel.IsDiscountApplied))
            RecalculateSaleTotals();
    }

    #endregion

    #region Calculation Methods

    private void CalculateFromRollCount()
    {
        if (!CurrentItem.RollCount.HasValue || !CurrentItem.LengthPerRoll.HasValue) return;

        var newTotalLength = CurrentItem.RollCount.Value * CurrentItem.LengthPerRoll.Value;

        if (!CheckWarehouseAvailabilityForLength(newTotalLength, CurrentItem.RollCount.Value))
        {
            isCalculating = true;
            CurrentItem.RollCount = previousRollCount;
            isCalculating = false;
            return;
        }

        CurrentItem.TotalLength = newTotalLength;
        previousTotalLength = newTotalLength;
        previousRollCount = CurrentItem.RollCount;

        CalculateTotalAndDiscounts();
    }

    private void CalculateFromTotalLength()
    {
        if (!CurrentItem.TotalLength.HasValue || !CurrentItem.LengthPerRoll.HasValue) return;

        var newTotalLength = CurrentItem.TotalLength.Value;
        var newRollCount = CurrentItem.LengthPerRoll.Value > 0
            ? (int)Math.Ceiling(newTotalLength / CurrentItem.LengthPerRoll.Value)
            : 0;

        if (!CheckWarehouseAvailabilityForLength(newTotalLength, newRollCount))
        {
            isCalculating = true;
            CurrentItem.TotalLength = previousTotalLength;
            isCalculating = false;
            return;
        }

        var remainder = newTotalLength % CurrentItem.LengthPerRoll.Value;
        if (remainder > 0 && !ConfirmRemainder(remainder))
        {
            isCalculating = true;
            CurrentItem.TotalLength = previousTotalLength;
            isCalculating = false;
            return;
        }

        if (CurrentItem.LengthPerRoll.Value > 0)
        {
            var fullRolls = (int)(newTotalLength / CurrentItem.LengthPerRoll.Value);
            CurrentItem.RollCount = fullRolls + (remainder > 0 ? 1 : 0);
        }

        previousTotalLength = newTotalLength;
        previousRollCount = CurrentItem.RollCount;

        CalculateTotalAndDiscounts();
    }

    private void CalculateFromUnitPrice()
    {
        if (!CurrentItem.UnitPrice.HasValue || !CurrentItem.TotalLength.HasValue) return;

        CurrentItem.TotalAmount = CurrentItem.TotalLength.Value * CurrentItem.UnitPrice.Value;
        CalculateDiscounts();
    }

    private void CalculateFromTotalAmount()
    {
        if (!CurrentItem.TotalAmount.HasValue || !CurrentItem.TotalLength.HasValue) return;

        if (CurrentItem.TotalLength.Value > 0)
            CurrentItem.UnitPrice = CurrentItem.TotalAmount.Value / CurrentItem.TotalLength.Value;

        CalculateDiscounts();
    }

    private void CalculateFromDiscountRate()
    {
        if (!CurrentItem.DiscountRate.HasValue || !CurrentItem.TotalAmount.HasValue) return;

        if (CurrentItem.DiscountRate.Value > 100)
        {
            CurrentItem.DiscountRate = 100;
            Warning = TranslationSource.T("Turnovers.DiscountMax100");
        }

        CurrentItem.DiscountAmount = CurrentItem.TotalAmount.Value * (CurrentItem.DiscountRate.Value / 100);
        CurrentItem.FinalAmount = CurrentItem.TotalAmount.Value - CurrentItem.DiscountAmount.Value;
    }

    private void CalculateFromDiscountAmount()
    {
        if (!CurrentItem.DiscountAmount.HasValue || !CurrentItem.TotalAmount.HasValue) return;

        if (CurrentItem.DiscountAmount.Value > CurrentItem.TotalAmount.Value)
        {
            CurrentItem.DiscountAmount = CurrentItem.TotalAmount.Value;
            Warning = TranslationSource.T("Turnovers.DiscountExceedsTotal");
        }

        if (CurrentItem.TotalAmount.Value > 0)
            CurrentItem.DiscountRate = Math.Round((CurrentItem.DiscountAmount.Value / CurrentItem.TotalAmount.Value) * 100, 2);

        CurrentItem.FinalAmount = CurrentItem.TotalAmount.Value - CurrentItem.DiscountAmount.Value;
    }

    private void CalculateFromFinalAmount()
    {
        if (!CurrentItem.FinalAmount.HasValue || !CurrentItem.TotalAmount.HasValue) return;

        CurrentItem.DiscountAmount = CurrentItem.TotalAmount.Value - CurrentItem.FinalAmount.Value;

        if (CurrentItem.DiscountAmount.Value < 0)
        {
            CurrentItem.DiscountAmount = 0;
            CurrentItem.FinalAmount = CurrentItem.TotalAmount.Value;
            Warning = TranslationSource.T("Turnovers.FinalExceedsTotal");
            return;
        }

        if (CurrentItem.TotalAmount.Value > 0)
        {
            CurrentItem.DiscountRate = Math.Round((CurrentItem.DiscountAmount.Value / CurrentItem.TotalAmount.Value) * 100, 2);

            if (CurrentItem.DiscountRate.Value > 100)
            {
                CurrentItem.DiscountRate = 100;
                CurrentItem.DiscountAmount = CurrentItem.TotalAmount.Value;
                CurrentItem.FinalAmount = 0;
                Warning = TranslationSource.T("Turnovers.DiscountMax100");
            }
        }
    }

    private void CalculateTotalAndDiscounts()
    {
        if (CurrentItem.UnitPrice.HasValue && CurrentItem.TotalLength.HasValue)
        {
            CurrentItem.TotalAmount = CurrentItem.TotalLength.Value * CurrentItem.UnitPrice.Value;
            CalculateDiscounts();
        }
    }

    private void CalculateDiscounts()
    {
        if (CurrentItem.DiscountRate.HasValue && CurrentItem.TotalAmount.HasValue)
        {
            CurrentItem.DiscountAmount = CurrentItem.TotalAmount.Value * (CurrentItem.DiscountRate.Value / 100);
            CurrentItem.FinalAmount = CurrentItem.TotalAmount.Value - CurrentItem.DiscountAmount.Value;
        }
    }

    private void RecalculateSaleTotals()
    {
        if (Sale.Items.Count == 0)
        {
            Sale.Amount = Sale.Discount = TotalSum = Sale.RollCount = 0;
            Sale.Length = 0;
        }
        else
        {
            var grossAmount = Sale.Items.Sum(x => x.TotalAmount ?? 0);
            var discountAmount = Sale.Items.Sum(x => x.DiscountAmount ?? 0);

            TotalSum = grossAmount;
            Sale.Discount = discountAmount;
            Sale.Amount = Sale.IsDiscountApplied ? grossAmount - discountAmount : grossAmount;
            Sale.RollCount = Sale.Items.Sum(x => x.RollCount ?? 0);
            Sale.Length = Sale.Items.Sum(x => x.TotalLength ?? 0);
        }

        UpdateCustomerBalance();
    }

    #endregion

    #region Helpers

    private bool CheckWarehouseAvailabilityForLength(decimal requestedLength, int requestedRolls)
    {
        var selectedStock = SelectedWarehouseStock;
        if (selectedStock is null || !CurrentItem.LengthPerRoll.HasValue) return true;

        var totalRollsInWarehouse = (int)(selectedStock.TotalLength / selectedStock.LengthPerRoll);

        if (requestedRolls > totalRollsInWarehouse)
        {
            var result = MessageBox.Show(
                string.Format(TranslationSource.T("Turnovers.WarehouseRollsLeft"), SelectedProduct?.Name, totalRollsInWarehouse)
                    + Environment.NewLine + TranslationSource.T("Turnovers.ContinueConfirm"),
                TranslationSource.T("Turnovers.Sale"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            return result == MessageBoxResult.Yes;
        }

        if (requestedLength > selectedStock.TotalLength)
        {
            var result = MessageBox.Show(
                string.Format(TranslationSource.T("Turnovers.WarehouseMetersLeft"), SelectedProduct?.Name, selectedStock.LengthPerRoll, selectedStock.TotalLength.ToString("N2"))
                    + Environment.NewLine + TranslationSource.T("Turnovers.ContinueConfirm"),
                TranslationSource.T("Turnovers.Sale"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            return result == MessageBoxResult.Yes;
        }

        return true;
    }

    private bool ConfirmRemainder(decimal remainder)
    {
        if (!CurrentItem.LengthPerRoll.HasValue) return true;

        var newRollLength = CurrentItem.LengthPerRoll.Value - remainder;

        var result = MessageBox.Show(
            string.Format(TranslationSource.T("Turnovers.RemainderConfirm"),
                SelectedProduct?.Name,
                CurrentItem.LengthPerRoll.Value,
                CurrentItem.TotalLength!.Value.ToString("N2"),
                remainder.ToString("N2"),
                newRollLength.ToString("N2"))
                + Environment.NewLine + TranslationSource.T("Turnovers.ContinueConfirm"),
            TranslationSource.T("Turnovers.Sale"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }

    private void UpdateCustomerBalance()
    {
        if (SelectedCustomer is null || SelectedCurrency is null)
        {
            BeginBalance = LastBalance = 0;
            return;
        }

        var account = SelectedCustomer.Accounts.FirstOrDefault(a => a.CurrencyId == SelectedCurrency.Id);

        if (account is not null)
        {
            BeginBalance = account.Balance;
        }
        else
        {
            var rate = SelectedCurrency.ExchangeRate > 0 ? SelectedCurrency.ExchangeRate : 1;
            BeginBalance = SelectedCustomer.Balance / rate;
        }

        LastBalance = BeginBalance - Sale.Amount;
    }

    private void ClearCurrentItem()
    {
        CurrentItem = new SaleItemViewModel();
        SelectedCategory = null;
        SelectedProduct = null;
        SelectedWarehouseStock = null;
        CategorySearchText = ProductSearchText = WarehouseStockSearchText = string.Empty;
        previousTotalLength = null;
        previousRollCount = null;
    }

    private bool ValidateCurrentItem()
    {
        if (SelectedProduct is null)
        {
            Warning = TranslationSource.T("Turnovers.ProductNotSelected");
            return false;
        }

        if (!CurrentItem.TotalLength.HasValue || CurrentItem.TotalLength.Value <= 0)
        {
            Warning = TranslationSource.T("Turnovers.QuantityNotEntered");
            return false;
        }

        return true;
    }

    private bool ValidateSale()
    {
        if (Sale.CustomerId <= 0)
        {
            Warning = TranslationSource.T("Turnovers.CustomerNotSelected");
            return false;
        }

        if (Sale.Items.Count == 0)
        {
            Warning = TranslationSource.T("Turnovers.NoSaleItems");
            return false;
        }

        return true;
    }

    private void ResetEditMode()
    {
        originalItem = null;
        originalItemIndex = -1;
        IsEditing = false;
    }

    private SaleItemViewModel CloneItem(SaleItemViewModel item) => new()
    {
        Id = item.Id,
        SaleId = item.SaleId,
        ProductId = item.ProductId,
        RollCount = item.RollCount,
        LengthPerRoll = item.LengthPerRoll,
        TotalLength = item.TotalLength,
        UnitPrice = item.UnitPrice,
        DiscountRate = item.DiscountRate,
        DiscountAmount = item.DiscountAmount,
        TotalAmount = item.TotalAmount,
        FinalAmount = item.FinalAmount,
        Product = item.Product
    };

    private static ProductViewModel MapProduct(ProductViewModel source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        CategoryId = source.CategoryId,
        Category = source.Category is not null ? new CategoryViewModel
        {
            Id = source.Category.Id,
            Name = source.Category.Name
        } : new CategoryViewModel()
    };

    private async Task LoadItemRelatedDataAsync(SaleItemViewModel item)
    {
        suppressLoading = true;
        try
        {
            if (item.Product?.Category is not null)
            {
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == item.Product.Category.Id);
                await LoadProductsAsync();
            }

            if (item.Product is not null)
            {
                SelectedProduct = Products.FirstOrDefault(p => p.Id == item.ProductId);
                await LoadWarehouseStocksAsync();
            }

            if (item.LengthPerRoll.HasValue)
            {
                SelectedWarehouseStock = WarehouseStocks.FirstOrDefault(w => w.LengthPerRoll == item.LengthPerRoll.Value);
            }
        }
        finally
        {
            suppressLoading = false;
        }
    }

    private void UpdateSearchSelection<T>(
        string searchText,
        ObservableCollection<T> collection,
        Func<T, string> nameSelector,
        Action<string> setSearchText,
        Action<T?> setSelected) where T : class
    {
        if (isCalculating) return;

        T? matched = null;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            matched = collection.FirstOrDefault(item =>
                nameSelector(item).Trim().Equals(searchText.Trim(), StringComparison.OrdinalIgnoreCase));

            matched ??= collection.FirstOrDefault(item =>
                nameSelector(item).Trim().StartsWith(searchText.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        setSelected(matched ?? collection.FirstOrDefault());

        var selected = matched ?? collection.FirstOrDefault();
        if (selected is not null && !string.Equals(nameSelector(selected), searchText, StringComparison.OrdinalIgnoreCase))
            setSearchText(nameSelector(selected));
        else if (selected is null)
            setSearchText(string.Empty);
    }

    private void ExecuteWithCalculationLock(Action action)
    {
        isCalculating = true;
        try { action(); }
        finally { isCalculating = false; }
    }

    private static bool ShowConfirmation(string message) =>
        MessageBox.Show(message, TranslationSource.T("Turnovers.ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    #endregion 
}