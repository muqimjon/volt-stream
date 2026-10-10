namespace VoltStream.WPF.Settings.ViewModels;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models.Requests;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;

public partial class GeneralSettingsViewModel : ViewModelBase
{
    private readonly ISettingsApi settingsApi;

    public GeneralSettingsViewModel(IServiceProvider services)
    {
        settingsApi = services.GetRequiredService<ISettingsApi>();
        _ = Load();
    }

    [ObservableProperty] private bool allowNegativeStock = true;

    private async Task Load()
    {
        var response = await settingsApi.GetGeneralAsync().Handle(isLoading => IsLoading = isLoading);
        if (response.IsSuccess) AllowNegativeStock = response.Data.AllowNegativeStock;
        else Error = response.Message ?? TranslationSource.T("Settings.GeneralLoadError");
    }

    [RelayCommand]
    private async Task Save()
    {
        var response = await settingsApi.UpdateGeneralAsync(new GeneralSettingsRequest { AllowNegativeStock = AllowNegativeStock })
            .Handle(isLoading => IsLoading = isLoading);

        if (response.IsSuccess) Success = TranslationSource.T("Settings.ChangesSaved");
        else Error = response.Message ?? TranslationSource.T("Settings.GeneralSaveError");
    }
}
