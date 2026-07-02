namespace VoltStream.WPF.Payments.Views;

using ApiServices.Extensions;
using ApiServices.Models.Requests;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Messages;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.Utils;
using VoltStream.WPF.Customer;
using VoltStream.WPF.Payments.PayDiscountWindow.Modela;
using VoltStream.WPF.Payments.PayDiscountWindow.Views;
using VoltStream.WPF.Payments.ViewModels;

public partial class PaymentsPage : Page
{
    PaymentPageViewModel vm;
    public PaymentsPage(IServiceProvider services)
    {
        InitializeComponent();
        vm = new PaymentPageViewModel(services);
        DataContext = vm;
        Loaded += PaymentsPage_Loaded;
        Unloaded += PaymentsPage_Unloaded;
    }

    private async void CustomerName_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!ComboBoxHelper.BeforeUpdate(sender, e, TranslationSource.T("Payments.Buyer"), true))
        {
            if (string.IsNullOrEmpty(CustomerName.Text))
            {
                lastBalans.Clear();
                beginBalans.Clear();
                Discount.Clear();
            }
            return;
        }

        var win = new CustomerWindow(CustomerName.Text) { Owner = Window.GetWindow(this) };

        if (win.ShowDialog() == true)
        {
            var result = win.Result!;
            var newCustomer = new CustomerRequest
            {
                Name = result.name,
                Phone = result.phone,
                Address = result.address,
                Description = result.description,
                Accounts = [ new() {
                OpeningBalance = result.beginningSum,
                Balance = result.beginningSum,
                CurrencyId = 1
            }]
            };

            var response = await vm.customersApi.CreateAsync(newCustomer).Handle();
            if (response.IsSuccess)
            {
                await vm.LoadCustomersAsync();
                CustomerName.SelectedItem = vm.AvailableCustomers.FirstOrDefault(c => c.Id == response.Data);
            }
            else
            {
                e.Handled = true;
                MessageBox.Show($"{TranslationSource.T("Payments.Error")}: {response.Message}", TranslationSource.T("Payments.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else e.Handled = true;
    }

    #region Messenger for Focus

    private void PaymentsPage_Loaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Register<FocusRequestMessage>(this, OnFocusRequestMessage);
        WeakReferenceMessenger.Default.Register<OpenDialogMessage<PayDiscountData>>(this, OnOpenDiscountWindowMessage);
        RegisterFocusNavigation();
    }

    private void RegisterFocusNavigation()
    {
        FocusNavigator.RegisterElements([
            PaymentDate.TextBox,
            CustomerName,
            CurrencyType,
            Kurs,
            Summa,
            Kirim,
            Chiqim,
            Discription,
            btnPaymant,
            btnDiscount
        ]);
        
        FocusNavigator.SetFocusRedirect(btnPaymant, PaymentDate.TextBox);
        FocusNavigator.SetFocusRedirect(btnDiscount, PaymentDate.TextBox);
    }

    private void PaymentsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<FocusRequestMessage>(this);
        WeakReferenceMessenger.Default.Unregister<OpenDialogMessage<PayDiscountData>>(this);
    }

    private async void OnOpenDiscountWindowMessage(object recipient, OpenDialogMessage<PayDiscountData> m)
    {
        var data = m.ViewModelData;

        var discountsWindow = new PayDiscountWindow(data.CustomerId, data.CustomerName, data.Discount)
        {
            Owner = Window.GetWindow(this)
        };

        if (discountsWindow.ShowDialog() == true)
        {
            await vm.ApplyDiscountResultAsync(discountsWindow.ResultOfDiscount);
        }
    }

    private async void OnFocusRequestMessage(object recipient, FocusRequestMessage m)
    {
        UIElement element = m.Value switch
        {
            "income" => Kirim,
            "expense" => Chiqim,
            "description" => Discription,
            "date" => PaymentDate.TextBox,
            "customer" => CustomerName,
            _ => null!
        };

        if (element is { IsEnabled: true })
            FocusNavigator.FocusElement(element);
    }

    #endregion Messenger for Focus
}