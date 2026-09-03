namespace VoltStream.WPF.Debitors.Views;

using System.Windows;
using System.Windows.Controls;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.Utils;
using VoltStream.WPF.Debitors.Models;

public partial class DebitorCreditorPage : Page
{
    private readonly IServiceProvider service;

    public DebitorCreditorPage(IServiceProvider service)
    {
        InitializeComponent();
        this.service = service;
        DataContext = new DebitorCreditorPageViewModel(service);

        if (!Permission.Has("sales.view_prices"))
            discountColumn.Visibility = debitorColumn.Visibility =
                creditorColumn.Visibility = totalsPanel.Visibility = Visibility.Collapsed;

        Loaded += (s, e) => RegisterFocusNavigation();
    }
    
    private void RegisterFocusNavigation()
    {
        FocusNavigator.RegisterElements([
            cbxCustomer,
            cbxSign
        ]);
    }
}
