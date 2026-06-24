namespace VoltStream.WPF.Home.Views;

using System.Windows.Controls;
using VoltStream.WPF.Home.Models;

public partial class DashboardPage : Page
{
    public DashboardPage(IServiceProvider services)
    {
        InitializeComponent();
        DataContext = new DashboardPageViewModel(services);
    }
}
