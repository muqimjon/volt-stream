namespace VoltStream.Mobile;

using VoltStream.Mobile.Views;

public class AppShell : Shell
{
    public AppShell()
    {
        var connection = new ShellContent { Route = "connection", ContentTemplate = new DataTemplate(typeof(ConnectionPage)) };

        Items.Add(new ShellContent { Route = "browser", ContentTemplate = new DataTemplate(typeof(BrowserPage)) });
        Items.Add(connection);

        FlyoutBehavior = FlyoutBehavior.Disabled;

        if (!Settings.HasAddress)
            CurrentItem = connection;
    }
}
