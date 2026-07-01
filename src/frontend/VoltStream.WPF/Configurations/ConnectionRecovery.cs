namespace VoltStream.WPF.Configurations;

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VoltStream.WPF.Settings.ViewModels;
using VoltStream.WPF.Settings.Views;

public class ConnectionRecovery(IServiceProvider services)
{
    private int prompting;

    public bool Prompt()
    {
        if (Interlocked.Exchange(ref prompting, 1) == 1)
            return false;

        try
        {
            var app = Application.Current;
            if (app is null)
                return false;

            return app.Dispatcher.Invoke(() =>
            {
                var vm = services.GetRequiredService<ConnectionSettingsViewModel>();
                var window = new ConnectionSettingsWindow(vm)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                return window.ShowDialog() == true;
            });
        }
        finally
        {
            Interlocked.Exchange(ref prompting, 0);
        }
    }
}
