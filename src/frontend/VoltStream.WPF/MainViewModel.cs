namespace VoltStream.WPF.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FontAwesome.Sharp;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Messages;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;
using VoltStream.WPF.Debitors.Views;
using VoltStream.WPF.Home.Views;
using VoltStream.WPF.Payments.Views;
using VoltStream.WPF.Products.Views;
using VoltStream.WPF.SaleDrafts.Views;
using VoltStream.WPF.Sales.Views;
using VoltStream.WPF.SalesJournal.Views;
using VoltStream.WPF.Sales_history.Views;
using VoltStream.WPF.Settings.ViewModels;
using VoltStream.WPF.Settings.Views;
using VoltStream.WPF.Supplies.Views;
using VoltStream.WPF.Turnovers.Views;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService navigationService;
    private readonly IServiceProvider services;
    private readonly NamozTimeService namozService;
    private readonly ISessionService session;

    public BranchContext BranchContext { get; }

    [ObservableProperty] private ApiConnectionViewModel apiConnection;
    [ObservableProperty] private object? currentChildView;
    [ObservableProperty] private string currentPageTitle = string.Empty;
    [ObservableProperty] private bool isSidebarCollapsed = false;
    [ObservableProperty] private string activePageKey = "Nav.Dashboard";

    [ObservableProperty] private bool isPaletteOpen;
    [ObservableProperty] private string paletteQuery = string.Empty;
    [ObservableProperty] private int paletteSelectedIndex;
    public ObservableCollection<PaletteItem> PaletteResults { get; } = [];
    private readonly List<PaletteItem> paletteItems;

    private string currentTitleKey = "Nav.Dashboard";

    [ObservableProperty] private string bomdod = string.Empty;
    [ObservableProperty] private string quyosh = string.Empty;
    [ObservableProperty] private string peshin = string.Empty;
    [ObservableProperty] private string asr = string.Empty;
    [ObservableProperty] private string shom = string.Empty;
    [ObservableProperty] private string xufton = string.Empty;
    [ObservableProperty] private string dateLabel = string.Empty;
    [ObservableProperty] private string regionName = string.Empty;

    [ObservableProperty] private string userDisplayName = string.Empty;
    [ObservableProperty] private string userLogin = string.Empty;
    [ObservableProperty] private string userRoleName = string.Empty;
    [ObservableProperty] private string userBranchName = string.Empty;

    public MainViewModel(IServiceProvider services, INavigationService navigationService, NamozTimeService namozService)
    {
        this.services = services;
        this.navigationService = navigationService;
        this.namozService = namozService;

        ApiConnection = services.GetRequiredService<ApiConnectionViewModel>();
        BranchContext = services.GetRequiredService<BranchContext>();
        session = services.GetRequiredService<ISessionService>();
        _ = BranchContext.LoadAsync();

        WeakReferenceMessenger.Default.Register<EntityUpdatedMessage<string>>(this, static (r, m) =>
        {
            if (m.Value == BranchSettingsViewModel.BranchesUpdatedMessage) _ = ((MainViewModel)r).BranchContext.LoadAsync();
        });

        if (this.navigationService is NavigationService navImpl)
        {
            navImpl.PropertyChanged += NavigationService_PropertyChanged;
        }

        paletteItems =
        [
            .. new (string Key, string Permission, IconChar Icon, Action Show)[]
            {
                ("Nav.Dashboard", "reports.view", IconChar.Home, ShowDashboardView),
                ("Nav.Production", "supplies.create", IconChar.ProjectDiagram, ShowSuppliesView),
                ("Nav.Sales", "sales.create", IconChar.ShoppingCart, ShowSalesView),
                ("Nav.Drafts", "drafts.confirm", IconChar.ClipboardList, ShowDraftsView),
                ("Nav.SalesJournal", "sales.view", IconChar.Receipt, ShowSalesJournalView),
                ("Nav.Payment", "payments.manage", IconChar.MoneyBillWave, ShowPaymentView),
                ("Nav.Products", "stock.view", IconChar.Boxes, ShowProductView),
                ("Nav.SalesAnalytics", "reports.view", IconChar.FileAlt, ShowSalesHistoryView),
                ("Nav.Counterparties", "customers.manage", IconChar.BalanceScale, ShowDebitorCreditor),
                ("Nav.CustomerReport", "reports.view", IconChar.ArrowsRotate, ShowTurnoversPage),
                ("Nav.Settings", string.Empty, IconChar.Sliders, ShowSettings),
            }
            .Where(p => p.Permission.Length == 0 || session.Has(p.Permission))
            .Select(p => new PaletteItem(p.Key, p.Icon, p.Show))
        ];
        FilterPalette();

        LocalizationManager.LanguageChanged += _ =>
        {
            CurrentPageTitle = TranslationSource.T(currentTitleKey);
            UpdateProfile();
        };

        UpdateProfile();
        (paletteItems.FirstOrDefault()?.Invoke ?? ShowDashboardView)();
    }

    private void UpdateProfile()
    {
        var user = session.CurrentUser;
        UserLogin = user?.Username ?? string.Empty;
        UserDisplayName = string.IsNullOrWhiteSpace(user?.Name) ? UserLogin : user!.Name!;
        UserRoleName = user is null ? string.Empty : TranslationSource.T($"Users.Role.{user.Role}");
        UserBranchName = string.IsNullOrWhiteSpace(session.BranchName)
            ? TranslationSource.T("Profile.NoBranch")
            : session.BranchName!;
    }

    [RelayCommand]
    private void TogglePalette()
    {
        if (IsPaletteOpen) { IsPaletteOpen = false; return; }
        PaletteQuery = string.Empty;
        PaletteSelectedIndex = PaletteResults.Count > 0 ? 0 : -1;
        IsPaletteOpen = true;
    }

    [RelayCommand] private void ClosePalette() => IsPaletteOpen = false;

    [RelayCommand]
    private void ExecutePalette()
    {
        if (PaletteSelectedIndex < 0 || PaletteSelectedIndex >= PaletteResults.Count) return;
        var item = PaletteResults[PaletteSelectedIndex];
        IsPaletteOpen = false;
        item.Invoke();
    }

    public void MovePalette(int delta)
    {
        if (PaletteResults.Count == 0) return;
        var i = (PaletteSelectedIndex + delta + PaletteResults.Count) % PaletteResults.Count;
        PaletteSelectedIndex = i;
    }

    partial void OnPaletteQueryChanged(string value) => FilterPalette();

    private void FilterPalette()
    {
        var q = PaletteQuery?.Trim() ?? string.Empty;
        PaletteResults.Clear();
        foreach (var item in paletteItems)
            if (q.Length == 0 || item.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
                PaletteResults.Add(item);
        PaletteSelectedIndex = PaletteResults.Count > 0 ? 0 : -1;
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
    [RelayCommand] private void ShowDraftsView() => NavigateTo<SaleDraftsPage>("Nav.Drafts");
    [RelayCommand] private void ShowSalesJournalView() => NavigateTo<SalesJournalPage>("Nav.SalesJournal");
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
        ActivePageKey = titleKey;
        CurrentPageTitle = TranslationSource.T(titleKey);
    }
}