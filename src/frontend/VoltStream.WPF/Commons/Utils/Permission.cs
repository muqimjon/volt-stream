namespace VoltStream.WPF.Commons.Utils;

using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using VoltStream.WPF.Commons.Services;

public static class Permission
{
    public static readonly DependencyProperty RequireProperty =
        DependencyProperty.RegisterAttached("Require", typeof(string), typeof(Permission), new PropertyMetadata(null, OnRequireChanged));

    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached("Enable", typeof(string), typeof(Permission), new PropertyMetadata(null, OnEnableChanged));

    public static string? GetRequire(DependencyObject obj) => (string?)obj.GetValue(RequireProperty);
    public static void SetRequire(DependencyObject obj, string? value) => obj.SetValue(RequireProperty, value);

    public static string? GetEnable(DependencyObject obj) => (string?)obj.GetValue(EnableProperty);
    public static void SetEnable(DependencyObject obj, string? value) => obj.SetValue(EnableProperty, value);

    public static bool Has(string key)
    {
        var session = App.Services?.GetService<ISessionService>();
        return session?.Has(key) ?? false;
    }

    private static void OnRequireChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not string key) return;
        fe.Loaded += (s, _) => fe.Visibility = Has(key) ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not string key) return;
        fe.Loaded += (s, _) => fe.IsEnabled = Has(key);
    }
}
