namespace VoltStream.WPF.Settings.Views;

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VoltStream.WPF.Settings.ViewModels;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsPageViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += SettingsPage_Loaded;
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (SettingsTabControl.SelectedItem is TabItem { Visibility: Visibility.Visible }) return;

        var firstVisible = SettingsTabControl.Items.OfType<TabItem>().FirstOrDefault(t => t.Visibility == Visibility.Visible);
        if (firstVisible is not null) SettingsTabControl.SelectedItem = firstVisible;
    }
}
