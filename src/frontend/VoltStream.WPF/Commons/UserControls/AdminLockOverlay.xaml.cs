namespace VoltStream.WPF.Commons.UserControls;

using ApiServices.Interfaces;
using ApiServices.Models.Requests;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Services;

public partial class AdminLockOverlay : UserControl
{
    public static readonly DependencyProperty IsLockedProperty =
        DependencyProperty.Register(nameof(IsLocked), typeof(bool), typeof(AdminLockOverlay),
            new PropertyMetadata(true, OnIsLockedChanged));

    public static readonly DependencyProperty LockMessageProperty =
        DependencyProperty.Register(nameof(LockMessage), typeof(string), typeof(AdminLockOverlay),
            new PropertyMetadata(TranslationSource.T("Controls.AdminAccessRequired")));

    public static readonly DependencyProperty InnerContentProperty =
        DependencyProperty.Register(nameof(InnerContent), typeof(object), typeof(AdminLockOverlay),
            new PropertyMetadata(null, OnInnerContentChanged));

    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    public string LockMessage
    {
        get => (string)GetValue(LockMessageProperty);
        set => SetValue(LockMessageProperty, value);
    }

    public object InnerContent
    {
        get => GetValue(InnerContentProperty);
        set => SetValue(InnerContentProperty, value);
    }

    public AdminLockOverlay()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            CheckAuth();
        };
    }

    private static void OnInnerContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdminLockOverlay control)
        {
            if (e.NewValue is FrameworkElement element)
            {
                if (element.DataContext is null)
                {
                    element.Loaded += (s, args) =>
                    {
                        if (element.DataContext is null && control.DataContext is not null)
                        {
                            element.DataContext = control.DataContext;
                        }
                    };
                }
            }
        }
    }

    private static void OnIsLockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdminLockOverlay control && e.NewValue is bool isLocked)
        {
            if (!isLocked)
            {
                if (control.InnerContent is FrameworkElement element)
                {
                    if (element.DataContext is null && control.DataContext is not null)
                    {
                        element.DataContext = control.DataContext;
                    }
                }
            }
        }
    }

    public void CheckAuth()
    {
        var session = App.Services?.GetService<ISessionService>();
        var isAdmin = session?.IsAdmin ?? false;

        if (session is not null && isAdmin)
            IsLocked = false;
        else
            IsLocked = true;
    }

    private async void UnlockButton_Click(object sender, RoutedEventArgs e)
        => await AttemptUnlock();

    private async void AdminPasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await AttemptUnlock();
    }

    private async Task AttemptUnlock()
    {
        var username = AdminUsernameBox.Text;
        var password = AdminPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetViewModelError(TranslationSource.T("Controls.FillLoginPassword"));
            return;
        }

        var loginApi = App.Services?.GetService<ILoginApi>();
        if (loginApi is null) return;

        try
        {
            var response = await loginApi.VerifyAdminAsync(new VerifyAdminRequest(username, password));

            if (response.IsSuccess && response.Data)
            {
                IsLocked = false;
                AdminUsernameBox.Text = string.Empty;
                AdminPasswordBox.Password = string.Empty;
                if (this.DataContext is ViewModelBase vm)
                    vm.Success = TranslationSource.T("Controls.AccessGranted");
            }
            else
            {
                SetViewModelError(TranslationSource.T("Controls.WrongLoginPassword"));
            }
        }
        catch (Exception)
        {
            SetViewModelError(TranslationSource.T("Controls.ConnectionError"));
        }
    }

    private void SetViewModelError(string message)
    {
        if (this.DataContext is ViewModelBase vm)
            vm.Error = message;
    }

    private void AdminPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            PassPlaceholder.Visibility = string.IsNullOrEmpty(pb.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}