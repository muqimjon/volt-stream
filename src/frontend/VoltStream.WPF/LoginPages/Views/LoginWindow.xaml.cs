namespace VoltStream.WPF.LoginPages.Views;

using System.Windows;
using System.Windows.Input;
using VoltStream.WPF.LoginPages.Models;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        if (!string.IsNullOrEmpty(vm.Password))
            tbxPassword.Password = vm.Password;

        txtUser.Focus();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private void TbxPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Password = tbxPassword.Password;
    }

    private async void TbxPassword_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel vm)
            await vm.Login();
    }
}
