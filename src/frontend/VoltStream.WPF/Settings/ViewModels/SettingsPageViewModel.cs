namespace VoltStream.WPF.Settings.ViewModels;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models.Requests;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Utils;
using VoltStream.WPF.Commons.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    private readonly IMapper mapper;
    private readonly IServiceProvider services;

    public SettingsPageViewModel(IServiceProvider services)
    {
        this.services = services;
        mapper = services.GetRequiredService<IMapper>();
        apiConnection = services.GetRequiredService<ApiConnectionViewModel>();

        if (Permission.Has("settings.general")) GeneralSettings = services.GetRequiredService<GeneralSettingsViewModel>();
        if (Permission.Has("settings.categories")) CategorySettings = services.GetRequiredService<CategorySettingsViewModel>();
        if (Permission.Has("settings.products")) ProductSettings = services.GetRequiredService<ProductSettingsViewModel>();
        if (Permission.Has("settings.customers")) CustomerSettings = services.GetRequiredService<CustomerSettingsViewModel>();
        if (Permission.Has("settings.branches")) BranchSettings = services.GetRequiredService<BranchSettingsViewModel>();
        if (Permission.Has("settings.branches")) WarehouseSettings = services.GetRequiredService<WarehouseSettingsViewModel>();
        if (Permission.Has("settings.users")) UserSettings = services.GetRequiredService<UserSettingsViewModel>();
        if (Permission.Has("settings.printer")) PrinterSettings = services.GetRequiredService<PrinterSettingsViewModel>();
        if (Permission.Has("settings.audit")) AuditLog = services.GetRequiredService<AuditLogViewModel>();

        if (Permission.Has("settings.currencies")) _ = LoadData();
    }

    [ObservableProperty] private ApiConnectionViewModel apiConnection;
    [ObservableProperty] private ObservableCollection<CurrencyViewModel> currencies = [];

    [ObservableProperty] private GeneralSettingsViewModel? generalSettings;
    [ObservableProperty] private CategorySettingsViewModel? categorySettings;
    [ObservableProperty] private ProductSettingsViewModel? productSettings;
    [ObservableProperty] private CustomerSettingsViewModel? customerSettings;
    [ObservableProperty] private BranchSettingsViewModel? branchSettings;
    [ObservableProperty] private WarehouseSettingsViewModel? warehouseSettings;
    [ObservableProperty] private UserSettingsViewModel? userSettings;
    [ObservableProperty] private PrinterSettingsViewModel? printerSettings;
    [ObservableProperty] private AuditLogViewModel? auditLog;

    #region Commands

    [RelayCommand]
    private void AddCurrency()
    {
        Currencies.Add(new CurrencyViewModel());
    }

    [RelayCommand]
    private void RemoveCurrency(CurrencyViewModel currency)
    {
        Currencies.Remove(currency);
    }

    [RelayCommand]
    private async Task SaveCurrencies()
    {
        if (Currencies is null || Currencies.Count == 0)
        {
            Warning = TranslationSource.T("Settings.NoCurrencyToSave");
            return;
        }

        var client = services.GetRequiredService<ICurrenciesApi>();
        var dtoList = mapper.Map<List<CurrencyRequest>>(Currencies);

        var response = await client.SaveAllAsync(dtoList)
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess && response.Data) Success = TranslationSource.T("Settings.ChangesSaved");
        else Error = response.Message ?? TranslationSource.T("Settings.SaveCurrenciesError");
    }

    #endregion Commands

    #region Load Data

    private async Task LoadData() => await LoadCurrencies();

    private async Task LoadCurrencies()
    {
        var client = services.GetRequiredService<ICurrenciesApi>();
        var response = await client.GetAllAsync().Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess)
            Currencies = mapper.Map<ObservableCollection<CurrencyViewModel>>(response.Data);
        else Error = response.Message ?? TranslationSource.T("Settings.LoadCurrenciesError");
    }

    #endregion Load Data
}
