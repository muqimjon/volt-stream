namespace VoltStream.WPF.SalesJournal.Views;

using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using VoltStream.WPF.SalesJournal.Models;

public partial class SalesJournalPage : Page
{
    public SalesJournalPage(IServiceProvider services)
    {
        InitializeComponent();

        var vm = services.GetRequiredService<SalesJournalPageViewModel>();
        DataContext = vm;

        if (!vm.CanViewPrices)
            amountColumn.Visibility = Visibility.Collapsed;
    }
}
