namespace VoltStream.WPF.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;
using VoltStream.WPF.Debitors.Views;
using VoltStream.WPF.Home.Views;
using VoltStream.WPF.Payments.Views;
using VoltStream.WPF.Products.Views;
using VoltStream.WPF.Sales.Views;
using VoltStream.WPF.Sales_history.Views;
using VoltStream.WPF.Settings.Views;
using VoltStream.WPF.Supplies.Views;
using VoltStream.WPF.Turnovers.Views;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService navigationService;
    private readonly IServiceProvider services;
    private readonly NamozTimeService namozService;

    [ObservableProperty] private ApiConnectionViewModel apiConnection;
    [ObservableProperty] private object? currentChildView;
    [ObservableProperty] private string currentPageTitle = string.Empty;
    [ObservableProperty] private bool isSidebarCollapsed = false;

    private string currentTitleKey = "Nav.Dashboard";

    [ObservableProperty] private string bomdod = string.Empty;
    [ObservableProperty] private string quyosh = string.Empty;
    [ObservableProperty] private string peshin = string.Empty;
    [ObservableProperty] private string asr = string.Empty;
    [ObservableProperty] private string shom = string.Empty;
    [ObservableProperty] private string xufton = string.Empty;
    [ObservableProperty] private string dateLabel = string.Empty;
    [ObservableProperty] private string regionName = string.Empty;

    public MainViewModel(IServiceProvider services, INavigationService navigationService, NamozTimeService namozService)
    {
        this.services = services;
        this.navigationService = navigationService;
        this.namozService = namozService;

        ApiConnection = services.GetRequiredService<ApiConnectionViewModel>();

        if (this.navigationService is NavigationService navImpl)
        {
            navImpl.PropertyChanged += NavigationService_PropertyChanged;
        }

        LocalizationManager.LanguageChanged += _ => CurrentPageTitle = TranslationSource.T(currentTitleKey);
        CurrentPageTitle = TranslationSource.T(currentTitleKey);

        this.navigationService.Navigate(this.services.GetRequiredService<DashboardPage>());
    }

    public async Task LoadNamozTimesAsync()
    {
        var data = await namozService.GetFullDataAsync();
        if (data == null || data.PeriodTable == null) return;

        string todayStr = DateTime.Now.ToString("dd.MM.yyyy");
        var todayData = data.PeriodTable.FirstOrDefault(x => x.Date == todayStr);
        var times = todayData?.Times ?? data.Today.Times;

        Bomdod = times.Bomdod;
        Quyosh = times.Quyosh;
        Peshin = times.Peshin;
        Asr = times.Asr;
        Shom = times.Shom;
        Xufton = times.Xufton;

        DateLabel = todayData?.Date ?? DateTime.Now.ToString("dd.MM.yyyy");
        RegionName = data.Meta.Region.Name;
    }

    private void NavigationService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NavigationService.CurrentView))
            CurrentChildView = navigationService.CurrentView;
    }

    [RelayCommand] private void ShowDashboardView() => NavigateTo<DashboardPage>("Nav.Dashboard");
    [RelayCommand] private void ShowSalesView() => NavigateTo<SalesPage>("Nav.Sales");
    [RelayCommand] private void ShowSuppliesView() => NavigateTo<SuppliesPage>("Nav.Production");
    [RelayCommand] private void ShowPaymentView() => NavigateTo<PaymentsPage>("Nav.Payment");
    [RelayCommand] private void ShowProductView() => NavigateTo<ProductsPage>("Nav.Products");
    [RelayCommand] private void ShowSalesHistoryView() => NavigateTo<SalesHistoryPage>("Nav.SalesAnalytics");
    [RelayCommand] private void ShowDebitorCreditor() => NavigateTo<DebitorCreditorPage>("Nav.Counterparties");
    [RelayCommand] private void ShowTurnoversPage() => NavigateTo<TurnoversPage>("Nav.CustomerReport");
    [RelayCommand] private void ShowSettings() => NavigateTo<SettingsPage>("Nav.Settings");

    private void NavigateTo<T>(string titleKey) where T : notnull
    {
        navigationService.Navigate(services.GetRequiredService<T>());
        currentTitleKey = titleKey;
        CurrentPageTitle = TranslationSource.T(titleKey);
    }
}