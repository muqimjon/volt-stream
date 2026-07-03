namespace VoltStream.WPF.Turnovers.Views;

using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Turnovers.Models;

public partial class TurnoversPage : Page
{
    public TurnoversPage()
    {
        InitializeComponent();
        DataContext = App.Services!.GetRequiredService<TurnoversPageViewModel>();

        Loaded += (s, e) => RegisterFocusNavigation();
        Unloaded += (s, e) => (DataContext as TurnoversPageViewModel)?.Cleanup();
    }
    
    private void RegisterFocusNavigation()
    {
        FocusNavigator.RegisterElements([
            cbxCustomer,
            MyDataGrid
        ]);
    }
}
