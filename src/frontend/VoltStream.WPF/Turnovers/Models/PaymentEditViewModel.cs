namespace VoltStream.WPF.Payments.ViewModels;

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

public partial class PaymentEditViewModel : ViewModelBase
{
    private readonly IMapper mapper;
    private readonly IPaymentApi paymentApi;
    private readonly ICustomersApi customersApi;
    private readonly ICurrenciesApi currenciesApi;
    private readonly INavigationService navigationService;

    public PaymentEditViewModel(IServiceProvider services, PaymentResponse paymentData)
    {
        mapper = services.GetRequiredService<IMapper>();
        paymentApi = services.GetRequiredService<IPaymentApi>();
        customersApi = services.GetRequiredService<ICustomersApi>();
        currenciesApi = services.GetRequiredService<ICurrenciesApi>();
        navigationService = services.GetRequiredService<INavigationService>();

        Payment = mapper.Map<PaymentViewModel>(paymentData);

        if (Payment.Amount > 0)
            Payment.IncomeAmount = Payment.NetAmount;
        else if (Payment.Amount < 0)
            Payment.ExpenseAmount = Math.Abs(Payment.NetAmount);

        Payment.PropertyChanged += Payment_PropertyChanged;

        _ = LoadPageAsync();
    }

    [ObservableProperty] private PaymentViewModel payment = new();
    [ObservableProperty] private decimal? beginBalance;
    [ObservableProperty] private decimal? lastBalance;

    [ObservableProperty] private ObservableCollection<CustomerViewModel> customers = [];
    [ObservableProperty] private ObservableCollection<CurrencyViewModel> currencies = [];

    #region Property Changes

    private async void Payment_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Payment.IncomeAmount) ||
            e.PropertyName == nameof(Payment.ExpenseAmount))
        {
            CalculateLastBalance();
        }
    }

    #endregion Property Changes

    #region Load Data

    private async Task LoadPageAsync()
    {
        await Task.WhenAll(
            LoadCustomersAsync(),
            LoadCurrenciesAsync(),
            LoadCustomerBalance()
        );
    }

    private async Task LoadCustomersAsync()
    {
        var response = await customersApi.GetAllAsync()
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
        {
            Customers = mapper.Map<ObservableCollection<CustomerViewModel>>(response.Data!);

            if (Payment.CustomerId > 0)
            {
                var currentCustomer = Customers.FirstOrDefault(c => c.Id == Payment.CustomerId);
                if (currentCustomer is not null)
                    Payment.Customer = currentCustomer;
            }
        }
        else Error = response.Message ?? TranslationSource.T("Turnovers.CustomersLoadFailed");
    }

    private async Task LoadCurrenciesAsync()
    {
        FilteringRequest request = new()
        {
            Filters = new() { ["isactive"] = ["true"] }
        };

        var response = await currenciesApi.Filter(request)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
        {
            Currencies = mapper.Map<ObservableCollection<CurrencyViewModel>>(response.Data!);

            if (Payment.CurrencyId > 0)
            {
                var currentCurrency = Currencies.FirstOrDefault(c => c.Id == Payment.CurrencyId);
                if (currentCurrency is not null)
                    Payment.Currency = currentCurrency;
            }
        }
        else Error = response.Message ?? TranslationSource.T("Turnovers.CurrenciesLoadFailed");
    }

    private async Task LoadCustomerBalance()
    {
        if (Payment.Customer is null) return;

        FilteringRequest request = new()
        {
            Filters = new()
            {
                ["id"] = [Payment.Customer.Id.ToString()],
                ["accounts"] = ["include:currency"]
            }
        };

        var response = await customersApi.FilterAsync(request)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
        {
            var customer = response.Data.First();
            if (customer.Accounts is not null)
            {
                var uzsAccount = customer.Accounts.FirstOrDefault(a => a.Currency?.Code == "UZS");
                if (uzsAccount is not null)
                {
                    BeginBalance = uzsAccount.Balance;
                    CalculateLastBalance();
                }
            }
        }
    }

    #endregion Load Data

    #region Commands

    [RelayCommand]
    private async Task Save()
    {
        if (Payment.Customer is null)
        {
            Warning = TranslationSource.T("Turnovers.CustomerNotSelected");
            return;
        }

        if (!Payment.IncomeAmount.HasValue && !Payment.ExpenseAmount.HasValue)
        {
            Warning = TranslationSource.T("Turnovers.IncomeOrExpenseRequired");
            return;
        }

        var result = MessageBox.Show(
            TranslationSource.T("Turnovers.ConfirmSaveChanges"),
            TranslationSource.T("Turnovers.ConfirmTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.No)
            return;

        var request = mapper.Map<PaymentRequest>(Payment);

        var response = await paymentApi.UpdateAsync(request)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
        {
            Success = TranslationSource.T("Turnovers.PaymentUpdated");
            WeakReferenceMessenger.Default.Send(new EntityUpdatedMessage<string>("OperationUpdated"));
            navigationService.GoBack();
        }
        else Error = response.Message ?? TranslationSource.T("Turnovers.PaymentUpdateFailed");
    }

    [RelayCommand]
    private void Cancel()
    {
        var result = MessageBox.Show(
            TranslationSource.T("Turnovers.ConfirmDiscardChanges"),
            TranslationSource.T("Turnovers.ConfirmTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            navigationService.GoBack();
    }

    #endregion Commands

    #region Private Helpers

    private void CalculateLastBalance()
    {
        LastBalance = BeginBalance + Payment.Amount;
    }

    #endregion Private Helpers
}