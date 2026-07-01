namespace VoltStream.WPF.LoginPages.Models;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models.Requests;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VoltStream.WPF.Commons;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;
using VoltStream.WPF.Configurations;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IServiceProvider services;
    private readonly CredentialStore credentialStore;

    public LoginViewModel(IServiceProvider services)
    {
        this.services = services;
        credentialStore = services.GetRequiredService<CredentialStore>();

        var saved = credentialStore.Load();
        if (saved is not null)
        {
            Username = saved.Value.username;
            Password = saved.Value.password;
            RememberMe = true;
        }
    }

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool rememberMe;

    public event Action? LoginSucceeded;

    [RelayCommand]
    public async Task Login()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            Warning = "Foydalanuvchi nomini kiriting";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            Warning = "Parolni kiriting";
            return;
        }

        var apiConnection = services.GetRequiredService<ApiConnectionViewModel>();

        IsLoading = true;
        var isConnected = await ServerHealth.IsAliveAsync(apiConnection.Url);
        IsLoading = false;

        if (!isConnected)
        {
            Error = "⚠ Server bilan bog'lanib bo'lmadi";

            if (!OpenConnectionSettings())
            {
                Error = "Aloqa sozlanmadi. Iltimos, qaytadan urinib ko'ring.";
                return;
            }

            IsLoading = true;
            var recheckResult = await ServerHealth.IsAliveAsync(apiConnection.Url);
            IsLoading = false;

            if (!recheckResult)
            {
                Error = "Server bilan bog'lanish hali ham mavjud emas.";
                return;
            }
        }

        LoginRequest credentials = new()
        {
            Username = Username,
            Password = Password
        };

        var loginApi = services.GetRequiredService<ILoginApi>();
        var loginResult = await loginApi.LoginAsync(credentials)
            .Handle(loading => IsLoading = loading);

        if (loginResult.IsSuccess && loginResult.Data != null)
        {
            if (RememberMe) credentialStore.Save(Username, Password);
            else credentialStore.Clear();

            var sessionService = services.GetRequiredService<ISessionService>();
            sessionService.CurrentUser = loginResult.Data;
            LoginSucceeded?.Invoke();
        }
        else Error = loginResult.Message ?? "Noto'g'ri foydalanuvchi nomi yoki parol!";
    }

    public async Task<bool> TryAutoLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            return false;

        LoginRequest credentials = new()
        {
            Username = Username,
            Password = Password
        };

        try
        {
            var loginApi = services.GetRequiredService<ILoginApi>();
            var loginResult = await loginApi.LoginAsync(credentials)
                .Handle(loading => IsLoading = loading);

            if (loginResult.IsSuccess && loginResult.Data != null)
            {
                services.GetRequiredService<ISessionService>().CurrentUser = loginResult.Data;
                LoginSucceeded?.Invoke();
                return true;
            }
        }
        catch { }

        return false;
    }

    private bool OpenConnectionSettings()
    {
        try
        {
            return services.GetRequiredService<ConnectionRecovery>().Prompt();
        }
        catch (Exception ex)
        {
            Error = $"Sozlamalar oynasini ochishda xatolik: {ex.Message}";
            return false;
        }
    }
}