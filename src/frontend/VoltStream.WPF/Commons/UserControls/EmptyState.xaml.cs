namespace VoltStream.WPF.Commons.UserControls;

using System.Windows;
using System.Windows.Controls;
using FontAwesome.Sharp;
using VoltStream.WPF.Commons.Localization;

public partial class EmptyState : UserControl
{
    public EmptyState() => InitializeComponent();

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(EmptyState), new PropertyMetadata(TranslationSource.T("Controls.NoDataFound")));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(IconChar), typeof(EmptyState), new PropertyMetadata(IconChar.FolderOpen));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public IconChar Icon
    {
        get => (IconChar)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
