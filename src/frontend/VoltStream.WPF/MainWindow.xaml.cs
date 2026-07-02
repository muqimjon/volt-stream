namespace VoltStream.WPF;

using FontAwesome.Sharp;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VoltStream.WPF.Commons.Animations;
using VoltStream.WPF.Commons.Enums;
using VoltStream.WPF.Commons.Localization;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.ViewModels;

public partial class MainWindow : Window
{
    private readonly MainViewModel vm;

    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        Commons.Utils.MonitorMaximizeHelper.Enable(this);
        vm = serviceProvider.GetRequiredService<MainViewModel>();
        DataContext = vm;
        NotificationService.Init(this);
        UpdateThemeIcon();
        Loaded += async (_, _) => await vm.LoadNamozTimesAsync();
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        ThemeManager.Toggle();
        UpdateThemeIcon();
    }

    private void LanguageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }
    }

    private void Lang_UzLatn_Click(object sender, RoutedEventArgs e) => LocalizationManager.Apply(AppLanguage.UzLatn);

    private void Lang_UzCyrl_Click(object sender, RoutedEventArgs e) => LocalizationManager.Apply(AppLanguage.UzCyrl);

    private void Lang_Ru_Click(object sender, RoutedEventArgs e) => LocalizationManager.Apply(AppLanguage.Ru);

    private void Lang_En_Click(object sender, RoutedEventArgs e) => LocalizationManager.Apply(AppLanguage.En);

    private void UpdateThemeIcon()
        => ThemeToggleIcon.Icon = ThemeManager.Current == AppTheme.Dark ? IconChar.Sun : IconChar.Moon;

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (vm is not null)
            vm.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void Header_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsSidebarCollapsed))
        {
            if (vm.IsSidebarCollapsed)
                CollapseSidebar();
            else
                ExpandSidebar();
        }
    }

    private void CollapseSidebar()
    {
        var animation = new GridLengthAnimation
        {
            From = new GridLength(SidebarColumn.ActualWidth, GridUnitType.Pixel),
            To = new GridLength(60, GridUnitType.Pixel),
            Duration = new Duration(TimeSpan.FromMilliseconds(300))
        };
        SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, animation);

        foreach (var tb in FindVisualChildren<TextBlock>(Sidebar))
        {
            if (tb.Name != "LogoText")
                tb.Visibility = Visibility.Collapsed;
        }

        foreach (var sp in FindVisualChildren<StackPanel>(Sidebar))
        {
            if (sp.Orientation == Orientation.Horizontal)
                sp.HorizontalAlignment = HorizontalAlignment.Center;
        }
    }

    private void ExpandSidebar()
    {
        var animation = new GridLengthAnimation
        {
            From = new GridLength(SidebarColumn.ActualWidth, GridUnitType.Pixel),
            To = new GridLength(250, GridUnitType.Pixel),
            Duration = new Duration(TimeSpan.FromMilliseconds(300))
        };
        SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, animation);

        foreach (var tb in FindVisualChildren<TextBlock>(Sidebar))
        {
            tb.Visibility = Visibility.Visible;
        }

        foreach (var sp in FindVisualChildren<StackPanel>(Sidebar))
        {
            if (sp.Orientation == Orientation.Horizontal)
                sp.HorizontalAlignment = HorizontalAlignment.Left;
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj == null) yield break;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            var child = VisualTreeHelper.GetChild(depObj, i);
            if (child is T t)
                yield return t;

            foreach (var childOfChild in FindVisualChildren<T>(child))
                yield return childOfChild;
        }
    }
}
