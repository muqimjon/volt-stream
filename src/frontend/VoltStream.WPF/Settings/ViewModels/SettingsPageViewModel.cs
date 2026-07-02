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

        CategorySettings = services.GetRequiredService<CategorySettingsViewModel>();
        ProductSettings = services.GetRequiredService<ProductSettingsViewModel>();
        CustomerSettings = services.GetRequiredService<CustomerSettingsViewModel>();

        _ = LoadData();
    }

    [ObservableProperty] private ApiConnectionViewModel apiConnection;
    [ObservableProperty] private ObservableCollection<CurrencyViewModel> currencies = [];

    [ObservableProperty] private CategorySettingsViewModel categorySettings;
    [ObservableProperty] private ProductSettingsViewModel productSettings;
    [ObservableProperty] private CustomerSettingsViewModel customerSettings;

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
            .Handle(isLoading => IsSelected = isLoading);

        if (response.IsSuccess && response.Data) Success = TranslationSource.T("Settings.ChangesSaved");
        else Error = response.Message ?? TranslationSource.T("Settings.SaveCurrenciesError");
    }

    #endregion Commands

    #region Load Data

    private async Task LoadData() => await Task.WhenAll(LoadCurrencies(), CategorySettings.LoadCategories(), ProductSettings.LoadData(), CustomerSettings.LoadCustomers());

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
