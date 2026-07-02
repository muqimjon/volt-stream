namespace VoltStream.WPF.Payments.ViewModels;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Requests;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Messages;
using VoltStream.WPF.Commons.ViewModels;
using VoltStream.WPF.Payments.PayDiscountWindow.Modela;

partial class PaymentPageViewModel : ViewModelBase
{
    public readonly ICustomersApi customersApi;
    public readonly ICurrenciesApi currenciesApi;
    public readonly IPaymentApi paymentApi;
    public readonly IMapper mapper;

    [ObservableProperty] private ObservableCollection<PaymentViewModel> pagedHistoryPayments = [];
    [ObservableProperty] private ObservableCollection<CurrencyViewModel> availableCurrencies = [];
    [ObservableProperty] private ObservableCollection<CustomerViewModel> availableCustomers = [];
    [ObservableProperty] private PaymentViewModel payment;
    [ObservableProperty] private CustomerViewModel? customer;

    private long customerId;

    public PaginationViewModel Pagination { get; }

    public PaymentPageViewModel(IServiceProvider services)
    {
        customersApi = services.GetRequiredService<ICustomersApi>();
        currenciesApi = services.GetRequiredService<ICurrenciesApi>();
        paymentApi = services.GetRequiredService<IPaymentApi>();
        mapper = services.GetRequiredService<IMapper>();

        Pagination = new PaginationViewModel(LoadPageAsync);

        payment = new();
        payment.PropertyChanged += OnPaymentFieldsChanged;

        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (Customer is null)
        {
            Warning = TranslationSource.T("Payments.WarningSelectPerson");
            WeakReferenceMessenger.Default.Send(new FocusRequestMessage("customer"));
            return;
        }
        if (Payment.PaidAt is null)
        {
            Warning = TranslationSource.T("Payments.WarningDateRequired");
            WeakReferenceMessenger.Default.Send(new FocusRequestMessage("date"));
            return;
        }
        if ((Payment.IncomeAmount ?? 0) + (Payment.ExpenseAmount ?? 0) <= 0)
        {
            Warning = TranslationSource.T("Payments.WarningFixAmount");
            WeakReferenceMessenger.Default.Send(new FocusRequestMessage(Payment.IsIncomeEnabled ? "income" : "expense"));
            return;
        }

        if (Payment.PaidAt.Value.Date == DateTime.Today)
            Payment.PaidAt = DateTime.Now;

        var request = mapper.Map<PaymentRequest>(Payment);
        request.CustomerId = Customer.Id;

        var response = await paymentApi.CreateAsync(request).Handle(l => IsLoading = l);

        if (response.IsSuccess)
        {
            Success = TranslationSource.T("Payments.SuccessRegistered");
            ResetPaymentForm();
            await LoadDataAsync();
        }
        else Error = response.Message ?? TranslationSource.T("Payments.TechnicalError");
    }

    [RelayCommand]
    private Task OpenDiscountsWindow()
    {
        if (Customer is null || Payment.Discount is null || Payment.Discount <= 0)
        {
            Warning = TranslationSource.T("Payments.WarningNoDiscount");
            WeakReferenceMessenger.Default.Send(new FocusRequestMessage("customer"));
            return Task.CompletedTask;
        }

        if (Payment.PaidAt is null)
        {
            Warning = TranslationSource.T("Payments.WarningDiscountDate");
            WeakReferenceMessenger.Default.Send(new FocusRequestMessage("date"));
            return Task.CompletedTask;
        }

        var discountData = new PayDiscountData(customerId, Customer.Name, Payment.Discount.Value, Payment.PaidAt!.Value);
        WeakReferenceMessenger.Default.Send(new OpenDialogMessage<PayDiscountData> { ViewModelData = discountData });
        return Task.CompletedTask;
    }

    private async Task LoadDataAsync() => await Task.WhenAll(LoadCustomersAsync(), LoadCurrenciesAsync(), LoadDatagrid());

    private async Task LoadDatagrid()
    {
        Pagination.Reset();
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        var request = new FilteringRequest
        {
            Page = Pagination.Page,
            PageSize = Pagination.PageSize,
            Filters = new() { ["paidAt"] = [$"{Payment.PaidAt ?? DateTime.Today:yyyy.MM.dd}"], ["customer"] = ["include"], ["currency"] = ["include"] }
        };

        using var scope = PagingScope.Begin();
        var response = await paymentApi.FilterAsync(request).Handle(l => IsLoading = l);
        if (!response.IsSuccess) return;

        Pagination.Apply(PagingScope.Result);
        PagedHistoryPayments = mapper.Map<ObservableCollection<PaymentViewModel>>(response.Data);
    }

    private async Task LoadCurrenciesAsync()
    {
        var response = await currenciesApi.Filter(new FilteringRequest { Filters = new() { ["isactive"] = ["true"] } }).Handle(l => IsLoading = l);
        if (response.IsSuccess)
        {
            AvailableCurrencies = mapper.Map<ObservableCollection<CurrencyViewModel>>(response.Data);
            Payment.Currency = AvailableCurrencies.FirstOrDefault(c => c.IsDefault)!;
        }
    }

    public async Task LoadCustomersAsync()
    {
        var response = await customersApi.GetAllAsync().Handle(l => IsLoading = l);
        if (response.IsSuccess)
            AvailableCustomers = mapper.Map<ObservableCollection<CustomerViewModel>>(response.Data);
    }

    private async Task LoadCustomerDetailsAsync()
    {
        var request = new FilteringRequest { Filters = new() { ["id"] = [customerId.ToString()], ["accounts"] = ["include:currency"] } };
        var response = await customersApi.FilterAsync(request).Handle();
        if (response.IsSuccess)
        {
            var data = response.Data.FirstOrDefault();
            if (data != null)
            {
                var mapped = mapper.Map<CustomerViewModel>(data);
                var uzsAccount = mapped.Accounts?.FirstOrDefault(a => a.Currency?.Code == "UZS");
                if (uzsAccount != null)
                {
                    Payment.Balance = uzsAccount.Balance;
                    Payment.Discount = uzsAccount.Discount;
                }
                ResetCalculations();
            }
        }
    }

    partial void OnCustomerChanged(CustomerViewModel? oldValue, CustomerViewModel? newValue)
    {
        if (newValue is null || oldValue?.Id == newValue.Id) return;

        customerId = newValue.Id;
        Payment.IncomeAmount = null;
        Payment.ExpenseAmount = null;
        Payment.Amount = 0;

        _ = LoadCustomerDetailsAsync();
    }

    partial void OnPaymentChanged(PaymentViewModel? oldValue, PaymentViewModel newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= OnPaymentFieldsChanged;
        if (newValue != null) newValue.PropertyChanged += OnPaymentFieldsChanged;
    }

    private void OnPaymentFieldsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PaymentViewModel.IncomeAmount):
                HandleTransactionType(true);
                break;
            case nameof(PaymentViewModel.ExpenseAmount):
                HandleTransactionType(false);
                break;
            case nameof(PaymentViewModel.ExchangeRate):
                UpdateBalances();
                break;
            case nameof(PaymentViewModel.Currency):
                if (Payment.Currency != null) Payment.ExchangeRate = Payment.Currency.ExchangeRate;
                break;
            case nameof(PaymentViewModel.PaidAt):
                _ = LoadDatagrid();
                break;
        }
    }

    private void HandleTransactionType(bool isIncome)
    {
        var amount = isIncome ? Payment.IncomeAmount : Payment.ExpenseAmount;
        if (amount > 0)
        {
            if (isIncome) Payment.IsExpenseEnabled = false; else Payment.IsIncomeEnabled = false;
            Payment.NetAmount = isIncome ? amount.Value : -amount.Value;
            UpdateBalances();
            if (isIncome) WeakReferenceMessenger.Default.Send(new FocusRequestMessage("description"));
        }
        else
        {
            Payment.IsIncomeEnabled = true;
            Payment.IsExpenseEnabled = true;
            ResetCalculations();
            WeakReferenceMessenger.Default.Send(new FocusRequestMessage(isIncome ? "income" : "expense"));
        }
    }

    private void UpdateBalances()
    {
        Payment.Amount = Payment.NetAmount * Payment.ExchangeRate;
        Payment.LastBalance = Payment.Balance + Payment.Amount;
    }

    private void ResetCalculations()
    {
        Payment.NetAmount = 0;
        Payment.Amount = 0;
        Payment.LastBalance = Payment.Balance;
    }

    private void ResetPaymentForm()
    {
        Payment = new();
        Customer = null;
    }

    public async Task ApplyDiscountResultAsync(dynamic? result)
    {
        if (result is null) return;

        var request = new ApplyDiscountRequest
        {
            Date = Payment.PaidAt!.Value.Date == DateTime.Today ? DateTime.Now : Payment.PaidAt.Value,
            CustomerId = customerId,
            DiscountAmount = result!.discountSum,
            IsCash = result.discountCash,
            Description = result.discountInfo ?? string.Empty
        };

        var response = await paymentApi.ApplyAsync(request).Handle(l => IsLoading = l);

        if (response.IsSuccess)
        {
            Success = $"{TranslationSource.T("Payments.SuccessDiscountApplied")}\n\n{TranslationSource.T("Payments.TypeLabel")} {(result.discountCash ? TranslationSource.T("Payments.Cash") : TranslationSource.T("Payments.Settlement"))}\n{TranslationSource.T("Payments.AmountLabel")} {result.discountSum:N2}";
            await LoadCustomerDetailsAsync();
            Payment.PaidAt = null;
        }
        else Error = response.Message ?? TranslationSource.T("Payments.ErrorApplyDiscount");
    }
}