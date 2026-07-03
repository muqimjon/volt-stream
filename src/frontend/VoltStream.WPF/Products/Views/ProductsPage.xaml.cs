namespace VoltStream.WPF.Products.Views;

using System.Windows.Controls;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Products.Models;

public partial class ProductsPage : Page
{
    private ProductPageViewModel vm;

    public ProductsPage(IServiceProvider services)
    {
        InitializeComponent();
        vm = new ProductPageViewModel(services);
        DataContext = vm;
        
        Loaded += (s, e) => RegisterFocusNavigation();
    }
    
    private void RegisterFocusNavigation()
    {
        FocusNavigator.RegisterElements([
            cbxCategory,
            cbxProductName
        ]);
    }
}
