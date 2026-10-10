namespace VoltStream.WPF.SaleDrafts.Views;

using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.SaleDrafts.Models;

public partial class SaleDraftsPage : Page
{
    public SaleDraftsPage(IServiceProvider services)
    {
        InitializeComponent();
        DataContext = services.GetRequiredService<SaleDraftsPageViewModel>();

        if (!services.GetRequiredService<BranchContext>().CanSelect)
            branchColumn.Visibility = Visibility.Collapsed;
    }
}
