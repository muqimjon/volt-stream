namespace VoltStream.WPF.Commons.UserControls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VoltStream.WPF.ViewModels;

public partial class CommandPalette : UserControl
{
    public CommandPalette() => InitializeComponent();

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void Root_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (root.Visibility != Visibility.Visible) return;
        Dispatcher.BeginInvoke(() =>
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down: Vm?.MovePalette(1); ScrollToSelected(); e.Handled = true; break;
            case Key.Up: Vm?.MovePalette(-1); ScrollToSelected(); e.Handled = true; break;
            case Key.Enter: Vm?.ExecutePaletteCommand.Execute(null); e.Handled = true; break;
            case Key.Escape: Vm?.ClosePaletteCommand.Execute(null); e.Handled = true; break;
        }
    }

    private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e) => ScrollToSelected();

    private void ResultsList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ItemFromPoint(e.OriginalSource) is not { } container) return;
        var index = ResultsList.ItemContainerGenerator.IndexFromContainer(container);
        if (index < 0) return;
        ResultsList.SelectedIndex = index;
        Vm?.ExecutePaletteCommand.Execute(null);
    }

    private void Scrim_MouseDown(object sender, MouseButtonEventArgs e) => Vm?.ClosePaletteCommand.Execute(null);

    private void ScrollToSelected()
    {
        if (ResultsList.SelectedItem is { } item)
            ResultsList.ScrollIntoView(item);
    }

    private static ListBoxItem? ItemFromPoint(object source)
    {
        var d = source as DependencyObject;
        while (d is not null and not ListBoxItem)
            d = VisualTreeHelper.GetParent(d);
        return d as ListBoxItem;
    }
}
