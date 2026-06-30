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

    protected override async void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            MessageBox.Show(ev.ExceptionObject.ToString(), "Jiddiy Xato!");

        base.OnStartup(e);

        host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureUiServices(services);
                ConfigureCoreServices(services);
            }).Build();

        Services = host.Services;

        // Server manzilini login/API chaqiruvidan OLDIN aniqlab olamiz va TO'LIQ kutamiz.
        // Saqlangan URL ishlasa — o'shani ishlatamiz; aks holda tarmoqdan avtomatik topamiz.
        // Shu sabab dev (localhost) ham, production (Docker) ham static sozlovsiz ishlaydi.
        // Discovery ichki jihatdan cheklangan (broadcast + skan parallel, har bir probe timeoutli),
        // shuning uchun cheksiz osilib qolmaydi; topgunicha kutamiz, keyin login.
        await EnsureServerDiscoveredAsync();

        var secureCreds = DevKeyService.TryGetSecureCredentials();
        bool autoLoginSuccess = false;

        if (secureCreds.HasValue)
        {
            var loginViewModel = Services.GetRequiredService<LoginViewModel>();
            loginViewModel.Username = secureCreds.Value.login;
            loginViewModel.Password = secureCreds.Value.password;

            var sessionService = Services.GetRequiredService<ISessionService>();

            loginViewModel.LoginSucceeded += () =>
            {
                autoLoginSuccess = true;
            };

            await loginViewModel.Login();

            if (autoLoginSuccess && sessionService.CurrentUser != null)
            {
                var mainWindow = Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
                return;
            }
        }

        var loginWindow = Services.GetRequiredService<LoginWindow>();
        var vm = (LoginViewModel)loginWindow.DataContext!;

        vm.LoginSucceeded += () =>
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            loginWindow.Close();
        };

        loginWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (host is not null)
            await host.StopAsync();

        host?.Dispose();
        base.OnExit(e);
    }

    // Serverni topib, ApiConnectionViewModel.Url ni o'rnatadi (auto-save config'ga yozadi).
    private static async Task EnsureServerDiscoveredAsync()
    {
        if (Services is null)
            return;

        var apiConnection = Services.GetRequiredService<ApiConnectionViewModel>();
        DiscoveryClient.ResetDiag();

        // 1) Saqlangan URL hali ishlayaptimi? (tez yo'l — har safar qaytadan qidirmaymiz)
        var savedAlive = await DiscoveryClient.IsAliveAsync(apiConnection.Url);
        DiscoveryClient.Diag($"Startup: saqlangan url='{apiConnection.Url}' tirik={savedAlive}");
        if (savedAlive)
            return;

        // 2) Aks holda tarmoqdan avtomatik topamiz (broadcast + LAN skan parallel).
        var uri = await DiscoveryClient.DiscoverAsync();
        if (uri is not null)
        {
            apiConnection.Url = $"{uri.Scheme}://{uri.Host}:{uri.Port}/";
            DiscoveryClient.Diag($"Startup: url o'rnatildi = {apiConnection.Url}");
        }
        else
        {
            DiscoveryClient.Diag("Startup: server TOPILMADI — eski url o'zgarmadi");
        }
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
        services.AddHostedService<ConnectionMonitor>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<NamozTimeService>();
        ApiService.ConfigureServices(services);
    }
}
