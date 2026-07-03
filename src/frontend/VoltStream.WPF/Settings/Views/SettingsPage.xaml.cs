namespace VoltStream.WPF.Settings.Views;

using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Settings.ViewModels;

public partial class SettingsPage : Page
{
    private readonly ISessionService session;

    public SettingsPage(SettingsPageViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        session = App.Services!.GetRequiredService<ISessionService>();
    }

    private void SettingsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource != SettingsTabControl) return;
        if (session.IsAdmin) return;

        if (CurrencyLock != null) CurrencyLock.IsLocked = true;
        if (CategoryLock != null) CategoryLock.IsLocked = true;
        if (ProductLock != null) ProductLock.IsLocked = true;
        if (CustomerLock != null) CustomerLock.IsLocked = true;
    }
}
