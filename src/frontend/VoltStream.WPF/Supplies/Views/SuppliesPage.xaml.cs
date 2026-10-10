namespace VoltStream.WPF.Supplies.Views;

using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Controls;
using VoltStream.WPF.Commons.Messages;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.Utils;
using VoltStream.WPF.Supplies.ViewModels;

public partial class SuppliesPage : Page
{
    public SuppliesPage(IServiceProvider services)
    {
        InitializeComponent();
        DataContext = new SuppliesPageViewModel(services);

        if (!Permission.Has("sales.view_prices"))
        {
            priceLabelCell.Visibility = Visibility.Collapsed;
            priceInputCell.Visibility = Visibility.Collapsed;
        }

        WeakReferenceMessenger.Default.Register<FocusControlMessage>(this, (r, m) =>
        {
            OnFocusRequestReceived(m.ControlName);
        });

        Loaded += Page_Loaded;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        RegisterFocusNavigation();
    }

    private void RegisterFocusNavigation()
    {
        FocusNavigator.RegisterElements([
            date.TextBox,
            cbxCategory,
            cbxProduct,
            cbxWarehouseStock,
            tbxRollCount,
            cbxUnit,
            txtUnitPrice,
            tbxDiscountRate,
            addSupplyBtn,
            cancelBtn
        ]);

        FocusNavigator.SetFocusRedirect(addSupplyBtn, cbxCategory);
        FocusNavigator.SetFocusRedirect(cancelBtn, cbxCategory);
    }

    private void OnFocusRequestReceived(string controlName)
    {
        if (controlName == "Category")
            FocusNavigator.FocusElement(cbxCategory);
        else if (controlName == "Product")
            FocusNavigator.FocusElement(cbxProduct);
        else if (controlName == "WarehouseStock")
            FocusNavigator.FocusElement(cbxWarehouseStock);
    }
}
