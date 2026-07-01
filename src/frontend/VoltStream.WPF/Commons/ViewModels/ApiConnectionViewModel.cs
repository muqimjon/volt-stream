namespace VoltStream.WPF.Commons.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoltStream.WPF.Commons.Enums;
using VoltStream.WPF.Configurations;

public partial class ApiConnectionViewModel : ViewModelBase
{
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private bool isConnected;
    [ObservableProperty] private bool isHttps;
    [ObservableProperty] private string host = "localhost";
    [ObservableProperty] private int port = 5000;
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private ConnectionStatus status;

    #region Commands

    [RelayCommand]
    public void Save()
    {
        if (TrySetUrl())
            _ = VerifyAsync();
    }

    public async Task<bool> CheckAsync()
        => TrySetUrl() && await VerifyAsync();

    private bool TrySetUrl()
    {
        Error = string.Empty;
        Success = string.Empty;

        var scheme = IsHttps ? "https" : "http";
        if (!Uri.TryCreate($"{scheme}://{Host}:{Port}/", UriKind.Absolute, out var uri))
        {
            Error = "Kiritilgan manzil yaroqsiz";
            return false;
        }

        Url = uri.ToString();
        return true;
    }

    private async Task<bool> VerifyAsync()
    {
        Status = ConnectionStatus.Connecting;
        IsConnected = await ServerHealth.IsAliveAsync(Url);
        Status = IsConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
        return IsConnected;
    }

    #endregion Commands

    #region PropertyChanged

    partial void OnIsConnectedChanged(bool value)
    {
        if (value)
            Success = "Aloqa tiklandi";
        else
            Warning = "Aloqa tiklanmadi";

        UpdateStatus();
    }

    partial void OnStatusChanged(ConnectionStatus value)
    {
        StatusText = Status switch
        {
            ConnectionStatus.Connected => "Ulangan",
            ConnectionStatus.Disconnected => "Uzilgan",
            ConnectionStatus.Connecting => "Ulanmoqda...",
            _ => string.Empty
        };

        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnUrlChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            IsHttps = uri.Scheme == "https";
            Host = uri.Host;
            Port = uri.Port;
        }
    }

    #endregion PropertyChanged

    #region Private helper

    private void UpdateStatus()
    {
        Status = IsConnected
            ? ConnectionStatus.Connected
            : ConnectionStatus.Disconnected;
    }

    #endregion Private helper
}