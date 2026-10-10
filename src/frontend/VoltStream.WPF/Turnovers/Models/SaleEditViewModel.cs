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
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Dialogs;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Messages;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.Utils;
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
    private readonly SalePolicy policy;

    private readonly Dictionary<long, List<WarehouseStockViewModel>> stocksByProduct = [];
    private bool isCalculating = false;
    private decimal? previousTotalLength = null;
    private int? previousRollCount = null;
    private readonly long originalCustomerId;
    private readonly long originalCurrencyId;
    private readonly decimal originalAmount;
    private readonly Dictionary<(long ProductId, decimal LengthPerRoll), (int Rolls, decimal Length)> originalUsage;

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
        policy = services.GetRequiredService<SalePolicy>();

        originalCustomerId = saleData.CustomerId;
        originalCurrencyId = saleData.CurrencyId;
        originalAmount = saleData.Amount;
        originalUsage = saleData.Items
            .GroupBy(i => (i.ProductId, i.LengthPerRoll))
            .ToDictionary(g => g.Key, g => (Rolls: g.Sum(i => i.RollCount), Length: g.Sum(i => i.TotalLength)));

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

    // Kalendar faqat sanani biladi: kun o'zgarmasa savdo vaqti ham o'zgarmasin.
    public DateTime? SaleDate
    {
        get => Sale.Date.LocalDateTime.Date;
        set
        {
            if (value is { } day && day.Date != Sale.Date.LocalDateTime.Date)
                Sale.Date = new DateTimeOffset(day.Date + Sale.Date.LocalDateTime.TimeOfDay);
        }
    }

    #region Load Data

    private async Task LoadPageAsync()
    {
        await policy.LoadAsync(Sale.BranchId);

        await Task.WhenAll(
            LoadCustomersAsync(),
            LoadCurrenciesAsync(),
            LoadCategoriesAsync(),
            LoadProductsAsync()
        );

        SelectedCustomer = Customers.FirstOrDefault(c => c.Id == Sale.CustomerId);
        SelectedCurrency = Currencies.FirstOrDefault(c => c.Id == Sale.CurrencyId);

        await LoadItemStocksAsync();
    }

    // Katak tahriri CellEditEnding'da sinxron tekshiriladi, shuning uchun qoldiq oldindan xotirada bo'lishi kerak.
    private async Task LoadItemStocksAsync()
    {
        foreach (var productId in Sale.Items.Select(i => i.ProductId).Distinct())
        {
            var response = await warehouseStocksApi.GetProductDetailsFromWarehouseAsync(productId, Sale.BranchId)
                .Handle(isLoading => IsLoading = isLoading);

            if (response.IsSuccess)
                stocksByProduct[productId] = mapper.Map<List<WarehouseStockViewModel>>(response.Data!);
            else Error = response.Message ?? TranslationSource.T("Turnovers.WarehouseLoadFailed");
        }
    }

    private async Task LoadCustomersAsync()
    {
        var request = new FilteringRequest
        {
            Filters = new() { ["accounts"] = ["include:currency"] }
        };

        var response = await customersApi.FilterAsync(request)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess) Customers = mapper.Map<ObservableCollection<CustomerViewModel>>(response.Data!);
        else Error = response.Message ?? TranslationSource.T("Turnovers.CustomersLoadFailed");
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
            Categories = mapper.Map<ObservableCollection<CategoryViewModel>>(response.Data!.Where(c => policy.ShowCategory(c.Id)).ToList());
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
            Products = mapper.Map<ObservableCollection<ProductViewModel>>(response.Data!.Where(p => policy.ShowProduct(p.Id)).ToList());
        else Error = response.Message ?? TranslationSource.T("Turnovers.ProductsLoadFailed");
    }

    private async Task LoadWarehouseStocksAsync()
    {
        if (SelectedProduct?.Id <= 0) return;

        var response = await warehouseStocksApi.GetProductDetailsFromWarehouseAsync(SelectedProduct!.Id, Sale.BranchId)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
            WarehouseStocks = mapper.Map<ObservableCollection<WarehouseStockViewModel>>(response.Data!.Where(policy.ShowStock).ToList());
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

        Sale.Items.Add(CurrentItem);
        stocksByProduct.TryAdd(CurrentItem.ProductId, [.. WarehouseStocks]);

        RecalculateSaleTotals();
        ClearCurrentItem();
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
            BranchId = Sale.BranchId,
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
                _ = LoadProductsAsync();
            });
        }
        else
        {
            CategorySearchText = string.Empty;
        }
    }

    partial void OnSelectedWarehouseStockChanged(WarehouseStockViewModel? value)
    {
        if (value is null) return;

        ExecuteWithCalculationLock(() =>
        {
            CurrentItem.LengthPerRoll = value.LengthPerRoll;
            CurrentItem.UnitPrice = value.UnitPrice;
            CurrentItem.DiscountRate = value.DiscountRate;
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

        if (!CheckWarehouseAvailabilityForLength(SelectedProduct?.Name, SelectedWarehouseStock, newTotalLength, CurrentItem.RollCount.Value))
        {
            isCalculating = true;
            CurrentItem.RollCount = previousRollCount;
            isCalculating = false;
            return;
        }

        CurrentItem.TotalLength = newTotalLength;
        previousTotalLength = newTotalLength;
        previousRollCount = CurrentItem.RollCount;

        CalculateTotalAndDiscounts(CurrentItem);
    }

    private void CalculateFromTotalLength()
    {
        if (!CurrentItem.TotalLength.HasValue || !CurrentItem.LengthPerRoll.HasValue) return;

        var newTotalLength = CurrentItem.TotalLength.Value;
        var newRollCount = CurrentItem.LengthPerRoll.Value > 0
            ? (int)Math.Ceiling(newTotalLength / CurrentItem.LengthPerRoll.Value)
            : 0;

        if (!CheckWarehouseAvailabilityForLength(SelectedProduct?.Name, SelectedWarehouseStock, newTotalLength, newRollCount))
        {
            isCalculating = true;
            CurrentItem.TotalLength = previousTotalLength;
            isCalculating = false;
            return;
        }

        var remainder = newTotalLength % CurrentItem.LengthPerRoll.Value;
        if (remainder > 0 && !ConfirmRemainder(SelectedProduct?.Name, CurrentItem.LengthPerRoll.Value, newTotalLength, remainder))
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

        CalculateTotalAndDiscounts(CurrentItem);
    }

    private void CalculateFromUnitPrice()
    {
        if (!CurrentItem.UnitPrice.HasValue || !CurrentItem.TotalLength.HasValue) return;

        CurrentItem.TotalAmount = CurrentItem.TotalLength.Value * CurrentItem.UnitPrice.Value;
        CalculateDiscounts(CurrentItem);
    }

    private void CalculateFromTotalAmount()
    {
        if (!CurrentItem.TotalAmount.HasValue || !CurrentItem.TotalLength.HasValue) return;

        if (CurrentItem.TotalLength.Value > 0)
            CurrentItem.UnitPrice = CurrentItem.TotalAmount.Value / CurrentItem.TotalLength.Value;

        CalculateDiscounts(CurrentItem);
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

    private static void CalculateTotalAndDiscounts(SaleItemViewModel item)
    {
        if (item.UnitPrice.HasValue && item.TotalLength.HasValue)
        {
            item.TotalAmount = item.TotalLength.Value * item.UnitPrice.Value;
            CalculateDiscounts(item);
        }
    }

    private static void CalculateDiscounts(SaleItemViewModel item)
    {
        if (item.DiscountRate.HasValue && item.TotalAmount.HasValue)
        {
            item.DiscountAmount = item.TotalAmount.Value * (item.DiscountRate.Value / 100);
            item.FinalAmount = item.TotalAmount.Value - item.DiscountAmount.Value;
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

    #region Cell Editing

    public bool ApplyRollCount(SaleItemViewModel item, decimal value)
    {
        if (value <= 0 || value != Math.Truncate(value) || item.LengthPerRoll is not > 0)
            return Reject("Sales.RollCountRequired");

        var rollCount = (int)value;
        var totalLength = rollCount * item.LengthPerRoll.Value;
        var (netRolls, netLength) = NetDemand(item, rollCount, totalLength);
        if (!CheckWarehouseAvailabilityForLength(item.Product?.Name, StockOf(item), netLength, netRolls))
            return false;

        Commit(item, rollCount, totalLength);
        return true;
    }

    public bool ApplyTotalLength(SaleItemViewModel item, decimal totalLength)
    {
        if (totalLength <= 0 || item.LengthPerRoll is not > 0)
            return Reject("Sales.TotalQuantityRequired");

        var lengthPerRoll = item.LengthPerRoll.Value;
        var rollCount = (int)Math.Ceiling(totalLength / lengthPerRoll);
        var (netRolls, netLength) = NetDemand(item, rollCount, totalLength);
        if (!CheckWarehouseAvailabilityForLength(item.Product?.Name, StockOf(item), netLength, netRolls))
            return false;

        var remainder = totalLength % lengthPerRoll;
        if (remainder > 0 && !ConfirmRemainder(item.Product?.Name, lengthPerRoll, totalLength, remainder))
            return false;

        Commit(item, rollCount, totalLength);
        return true;
    }

    public bool ApplyUnitPrice(SaleItemViewModel item, decimal value)
    {
        if (value <= 0)
            return Reject("Sales.PriceRequired");

        item.UnitPrice = value;
        CalculateTotalAndDiscounts(item);
        RecalculateSaleTotals();
        return true;
    }

    public bool ApplyDiscountRate(SaleItemViewModel item, decimal value)
    {
        if (value < 0)
            return Reject("Sales.DiscountPercentRange");

        if (value > 100)
            return Reject("Turnovers.DiscountMax100");

        item.DiscountRate = value;
        CalculateDiscounts(item);
        RecalculateSaleTotals();
        return true;
    }

    public bool Reject(string messageKey)
    {
        Warning = TranslationSource.T(messageKey);
        return false;
    }

    private void Commit(SaleItemViewModel item, int rollCount, decimal totalLength)
    {
        item.RollCount = rollCount;
        item.TotalLength = totalLength;
        CalculateTotalAndDiscounts(item);
        RecalculateSaleTotals();
    }

    // Server savdoni tahrirlaganda eski miqdorni avval omborga qaytaradi — shuning uchun qoldiqdan faqat farq olinadi.
    private (int Rolls, decimal Length) NetDemand(SaleItemViewModel item, int rolls, decimal length)
    {
        var others = Sale.Items.Where(i => i != item && i.ProductId == item.ProductId && i.LengthPerRoll == item.LengthPerRoll).ToList();
        var returned = originalUsage.GetValueOrDefault((item.ProductId, item.LengthPerRoll ?? 0));

        return (rolls + others.Sum(i => i.RollCount ?? 0) - returned.Rolls, length + others.Sum(i => i.TotalLength ?? 0) - returned.Length);
    }

    private WarehouseStockViewModel? StockOf(SaleItemViewModel item) =>
        stocksByProduct.GetValueOrDefault(item.ProductId)?.FirstOrDefault(s => s.LengthPerRoll == item.LengthPerRoll);

    #endregion

    #region Helpers

    private bool CheckWarehouseAvailabilityForLength(string? productName, WarehouseStockViewModel? stock, decimal requestedLength, int requestedRolls)
    {
        if (stock is null) return true;

        var totalRollsInWarehouse = (int)(stock.TotalLength / stock.LengthPerRoll);

        if (requestedRolls > totalRollsInWarehouse)
            return ConfirmOrRefuse(string.Format(TranslationSource.T("Turnovers.WarehouseRollsLeft"), productName, totalRollsInWarehouse));

        if (requestedLength > stock.TotalLength)
            return ConfirmOrRefuse(string.Format(TranslationSource.T("Turnovers.WarehouseMetersLeft"), productName, stock.LengthPerRoll, stock.TotalLength.ToString("N2")));

        return true;
    }

    private bool ConfirmOrRefuse(string shortage)
    {
        if (policy.AllowNegativeStock)
            return AppDialog.Confirm(
                shortage + Environment.NewLine + TranslationSource.T("Turnovers.ContinueConfirm"),
                TranslationSource.T("Turnovers.Sale"));

        Warning = $"{TranslationSource.T("Sales.StockNotEnough")} {shortage}";
        return false;
    }

    private static bool ConfirmRemainder(string? productName, decimal lengthPerRoll, decimal totalLength, decimal remainder)
    {
        return AppDialog.Confirm(
            string.Format(TranslationSource.T("Turnovers.RemainderConfirm"),
                productName,
                lengthPerRoll,
                totalLength.ToString("N2"),
                remainder.ToString("N2"),
                (lengthPerRoll - remainder).ToString("N2"))
                + Environment.NewLine + TranslationSource.T("Turnovers.ContinueConfirm"),
            TranslationSource.T("Turnovers.Sale"));
    }

    private void UpdateCustomerBalance()
    {
        if (SelectedCustomer is null || SelectedCurrency is null)
        {
            BeginBalance = LastBalance = 0;
            return;
        }

        BeginBalance = CustomerBalance.Current(SelectedCustomer.Accounts.Select(a => a.Balance));

        // Mijoz qoldig'i shu savdoning o'zini ham o'z ichiga oladi — tahrirda uni qaytarib olib tashlaymiz.
        if (SelectedCustomer.Id == originalCustomerId)
            BeginBalance += CustomerBalance.InBase(originalAmount, Currencies.FirstOrDefault(c => c.Id == originalCurrencyId)?.ExchangeRate ?? 1);

        LastBalance = CustomerBalance.After(BeginBalance, Sale.Amount, SelectedCurrency.ExchangeRate);
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
        AppDialog.Confirm(message, TranslationSource.T("Turnovers.ConfirmTitle"));

    #endregion 
}