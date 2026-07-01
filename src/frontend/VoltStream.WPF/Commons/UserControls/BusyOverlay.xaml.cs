namespace VoltStream.WPF.Commons.UserControls;

using System.Windows;
using System.Windows.Controls;

public partial class BusyOverlay : UserControl
{
    public BusyOverlay() => InitializeComponent();

    public static readonly DependencyProperty IsBusyProperty =
        DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(BusyOverlay), new PropertyMetadata(false));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(BusyOverlay), new PropertyMetadata("Yuklanmoqda..."));

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }
}
