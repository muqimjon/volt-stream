namespace VoltStream.WPF.Commons.UserControls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FontAwesome.Sharp;

public class FilterBar : Control
{
    static FilterBar()
        => DefaultStyleKeyProperty.OverrideMetadata(typeof(FilterBar), new FrameworkPropertyMetadata(typeof(FilterBar)));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(FilterBar), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(IconChar), typeof(FilterBar), new PropertyMetadata(IconChar.Filter));

    public static readonly DependencyProperty FiltersProperty =
        DependencyProperty.Register(nameof(Filters), typeof(object), typeof(FilterBar), new PropertyMetadata(null));

    public static readonly DependencyProperty ActionsEnabledProperty =
        DependencyProperty.Register(nameof(ActionsEnabled), typeof(bool), typeof(FilterBar), new PropertyMetadata(true));

    public static readonly DependencyProperty ClearCommandProperty =
        DependencyProperty.Register(nameof(ClearCommand), typeof(ICommand), typeof(FilterBar), new PropertyMetadata(null));

    public static readonly DependencyProperty PreviewCommandProperty =
        DependencyProperty.Register(nameof(PreviewCommand), typeof(ICommand), typeof(FilterBar), new PropertyMetadata(null));

    public static readonly DependencyProperty PrintCommandProperty =
        DependencyProperty.Register(nameof(PrintCommand), typeof(ICommand), typeof(FilterBar), new PropertyMetadata(null));

    public static readonly DependencyProperty ExcelCommandProperty =
        DependencyProperty.Register(nameof(ExcelCommand), typeof(ICommand), typeof(FilterBar), new PropertyMetadata(null));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public IconChar Icon { get => (IconChar)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public object Filters { get => GetValue(FiltersProperty); set => SetValue(FiltersProperty, value); }
    public bool ActionsEnabled { get => (bool)GetValue(ActionsEnabledProperty); set => SetValue(ActionsEnabledProperty, value); }
    public ICommand ClearCommand { get => (ICommand)GetValue(ClearCommandProperty); set => SetValue(ClearCommandProperty, value); }
    public ICommand PreviewCommand { get => (ICommand)GetValue(PreviewCommandProperty); set => SetValue(PreviewCommandProperty, value); }
    public ICommand PrintCommand { get => (ICommand)GetValue(PrintCommandProperty); set => SetValue(PrintCommandProperty, value); }
    public ICommand ExcelCommand { get => (ICommand)GetValue(ExcelCommandProperty); set => SetValue(ExcelCommandProperty, value); }
}
