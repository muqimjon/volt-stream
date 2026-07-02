namespace VoltStream.WPF.Settings.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.ViewModels;

public partial class ConnectionSettingsViewModel : ViewModelBase
{
    private Action? closeAction;

    public ConnectionSettingsViewModel(IServiceProvider services)
    {
        ApiConnection = services.GetRequiredService<ApiConnectionViewModel>();
    }

    [ObservableProperty] private ApiConnectionViewModel apiConnection;

    #region Commands

    [RelayCommand]
    private async Task TestConnection()
    {
        IsLoading = true;
        var ok = await ApiConnection.CheckAsync();
        IsLoading = false;

        if (ok)
            Success = TranslationSource.T("Settings.ConnectionSuccess");
        else Error = TranslationSource.T("Settings.ConnectionFailed");
    }

    [RelayCommand]
    private void SaveAndClose()
    {
        ApiConnection.Save();
        closeAction?.Invoke();
    }

    #endregion Commands

    public void SetCloseAction(Action closeAction)
    {
        this.closeAction = closeAction;
    }
}