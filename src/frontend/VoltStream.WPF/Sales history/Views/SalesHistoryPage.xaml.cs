namespace VoltStream.WPF.Sales_history.Views;

using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Sales_history.Models;

public partial class SalesHistoryPage : Page
{
    private readonly SalesHistoryPageViewModel vm;
    
    public SalesHistoryPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        vm = serviceProvider.GetRequiredService<SalesHistoryPageViewModel>();
        DataContext = vm;
        
        Loaded += (s, e) => RegisterFocusNavigation();
    }
    
    private void RegisterFocusNavigation()
    {
        FocusNavigator.RegisterElements([
            cbxCustomer,
            beginDate.TextBox,
            endDate.TextBox,
            cbxCategory,
            cbxProductName
        ]);
    }

    private async void BeginDate_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(beginDate.TextBox.Text))
        {
            beginDate.Focus();
            return;
        }

        string[] formats = { "dd.MM.yyyy", "dd-MM-yyyy", "dd/MM/yyyy" };

        if (DateTime.TryParseExact(beginDate.TextBox.Text, formats,
                                   CultureInfo.InvariantCulture,
                                   DateTimeStyles.None,
                                   out DateTime parsedDate))
        {
            beginDate.SelectedDate = parsedDate;
        }
        else
        {
            MessageBox.Show(TranslationSource.T("SalesHistory.InvalidDateFormat"), TranslationSource.T("SalesHistory.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            beginDate.Focus();
            return;
        }
        await vm.ReloadAsync();

    }

    private async void EndDate_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(endDate.TextBox.Text))
        {
            endDate.Focus();
            return;
        }

        string[] formats = { "dd.MM.yyyy", "dd-MM-yyyy", "dd/MM/yyyy" };

        if (DateTime.TryParseExact(endDate.TextBox.Text, formats,
                                   CultureInfo.InvariantCulture,
                                   DateTimeStyles.None,
                                   out DateTime parsedDate))
        {
            endDate.SelectedDate = parsedDate;
        }
        else
        {
            MessageBox.Show(TranslationSource.T("SalesHistory.InvalidDateFormat"), TranslationSource.T("SalesHistory.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            endDate.Focus();
            return;
        }
        await vm.ReloadAsync();
    }
}
