namespace VoltStream.WPF;

using Forex.Wpf.Common.Services;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;
using VoltStream.WPF.Configurations;
using VoltStream.WPF.LoginPages.Models;
using VoltStream.WPF.LoginPages.Views;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    private IHost? host;

    private bool mainShown;

    protected override async void OnStartup(StartupEventArgs e)
    {
        RegisterGlobalExceptionHandlers();
        ThemeManager.Initialize();
        base.OnStartup(e);

        host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureUiServices(services);
                ConfigureCoreServices(services);
            }).Build();

        Services = host.Services;
        await host.StartAsync();

        var loginWindow = Services.GetRequiredService<LoginWindow>();
        var vm = (LoginViewModel)loginWindow.DataContext!;
        vm.LoginSucceeded += () => Dispatcher.Invoke(() => ShowMainWindow(loginWindow));
        loginWindow.Show();

        _ = TryAutoConnectAndLoginAsync(vm);
    }

    private async Task TryAutoConnectAndLoginAsync(LoginViewModel vm)
    {
        try
        {
            var secureCreds = DevKeyService.TryGetSecureCredentials();
            if (secureCreds.HasValue)
            {
                vm.Username = secureCreds.Value.login;
                vm.Password = secureCreds.Value.password;
            }
            else if (!vm.RememberMe || string.IsNullOrWhiteSpace(vm.Username) || string.IsNullOrWhiteSpace(vm.Password))
                return;

            var apiConnection = Services!.GetRequiredService<ApiConnectionViewModel>();

            for (var i = 0; i < 30 && !mainShown; i++)
            {
                if (await DiscoveryClient.IsAliveAsync(apiConnection.Url) && await vm.TryAutoLoginAsync())
                    return;

                await Task.Delay(1000);
            }
        }
        catch { }
    }

    private void ShowMainWindow(Window loginWindow)
    {
        if (mainShown)
            return;

        mainShown = true;
        var mainWindow = Services!.GetRequiredService<MainWindow>();
        mainWindow.Show();
        loginWindow.Close();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            ShowError(ev.ExceptionObject as Exception);

        DispatcherUnhandledException += (s, ev) =>
        {
            ShowError(ev.Exception);
            ev.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, ev) => ev.SetObserved();
    }

    private void ShowError(Exception? ex)
    {
        if (ex is null)
            return;

        Dispatcher.Invoke(() => MessageBox.Show(ex.Message, "Jiddiy Xato!"));
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (host is not null)
            await host.StopAsync();

        host?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureUiServices(IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        var assembly = Assembly.GetExecutingAssembly();

        void RegisterByBaseType<TBase>(ServiceLifetime lifetime)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(TBase).IsAssignableFrom(t));

            foreach (var type in types)
                services.Add(new ServiceDescriptor(type, type, lifetime));
        }

        RegisterByBaseType<Window>(ServiceLifetime.Singleton);
        RegisterByBaseType<Page>(ServiceLifetime.Transient);
        RegisterByBaseType<ViewModelBase>(ServiceLifetime.Transient);
    }

    private static void ConfigureCoreServices(IServiceCollection services)
    {
        services.AddSingleton<DiscoveryClient>();
        services.AddSingleton<CredentialStore>();
        services.AddSingleton<Sales.ViewModels.SaleSession>();
        services.AddHostedService<ConnectionMonitor>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<NamozTimeService>();
        ApiService.ConfigureServices(services);
    }
}
