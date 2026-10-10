namespace VoltStream.WPF.Settings.Views.Modules;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using VoltStream.WPF.Settings.ViewModels;

public partial class UserSettingsView : UserControl
{
    public UserSettingsView()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is UserSettingsViewModel oldVm) oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is UserSettingsViewModel newVm) newVm.PropertyChanged += Vm_PropertyChanged;
        };
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not UserSettingsViewModel vm) return;
        if (e.PropertyName == nameof(vm.Password) && string.IsNullOrEmpty(vm.Password)) PasswordBox.Password = string.Empty;
        if (e.PropertyName == nameof(vm.NewPassword) && string.IsNullOrEmpty(vm.NewPassword)) NewPasswordBox.Password = string.Empty;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserSettingsViewModel vm) vm.Password = PasswordBox.Password;
    }

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserSettingsViewModel vm) vm.NewPassword = NewPasswordBox.Password;
    }
}
