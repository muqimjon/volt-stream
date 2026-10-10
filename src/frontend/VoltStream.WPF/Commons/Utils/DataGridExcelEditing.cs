namespace VoltStream.WPF.Commons.Utils;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

public static class DataGridExcelEditing
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridExcelEditing),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DataGrid obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DataGrid obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static readonly DependencyProperty OriginalTextProperty =
        DependencyProperty.RegisterAttached("OriginalText", typeof(string), typeof(DataGridExcelEditing));

    // Shunchaki Tab/Enter bilan o'tilganda ombor/kesish tasdig'i qayta chiqmasin va saqlangan qiymat ko'rinadigan matnga yaxlitlanmasin.
    public static bool IsUnchanged(TextBox box) => (string?)box.GetValue(OriginalTextProperty) == box.Text;

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
            return;

        if ((bool)e.NewValue)
        {
            grid.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            grid.PreviewKeyDown += OnPreviewKeyDown;
            grid.PreparingCellForEdit += OnPreparingCellForEdit;
        }
        else
        {
            grid.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            grid.PreviewKeyDown -= OnPreviewKeyDown;
            grid.PreparingCellForEdit -= OnPreparingCellForEdit;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridCell>(e.OriginalSource as DependencyObject) is not { IsReadOnly: false, IsEditing: false } cell)
            return;

        var grid = (DataGrid)sender;
        if (!grid.CommitEdit(DataGridEditingUnit.Row, true))
            grid.CancelEdit(DataGridEditingUnit.Row);

        BeginEdit(grid, cell);
        e.Handled = true;
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Tab))
            return;

        var grid = (DataGrid)sender;
        if (FindParent<DataGridCell>(Keyboard.FocusedElement as DependencyObject) is not { } cell)
            return;

        bool forward = e.Key == Key.Enter || (Keyboard.Modifiers & ModifierKeys.Shift) == 0;

        if (cell.IsEditing)
        {
            e.Handled = true;
            if (!grid.CommitEdit(DataGridEditingUnit.Row, true))
                return;
        }
        else if (e.Key == Key.Enter && !cell.IsReadOnly)
        {
            e.Handled = true;
            BeginEdit(grid, cell);
            return;
        }

        if (MoveToNext(grid, forward))
            e.Handled = true;
        else if (e.Handled)
            grid.MoveFocus(new TraversalRequest(forward ? FocusNavigationDirection.Next : FocusNavigationDirection.Previous));
    }

    private static void OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
    {
        if (sender is not DataGrid grid || e.EditingElement is not TextBox box)
            return;

        box.SetValue(OriginalTextProperty, box.Text);
        box.LostKeyboardFocus += (_, args) => OnEditorLostFocus(grid, args);
        box.Dispatcher.BeginInvoke(() =>
        {
            box.Focus();
            box.SelectAll();
        }, DispatcherPriority.Input);
    }

    // Bog'lanish bir tomonlama: fokus boshqa boshqaruvga (masalan, Yuborish) o'tsa, tugallanmagan tahrir yo'qolardi.
    private static void OnEditorLostFocus(DataGrid grid, KeyboardFocusChangedEventArgs e)
    {
        if (e.NewFocus is not DependencyObject target
            || FindParent<DataGrid>(target) == grid
            || Window.GetWindow(target) != Window.GetWindow(grid))
            return;

        if (!grid.CommitEdit(DataGridEditingUnit.Row, true))
            grid.CancelEdit(DataGridEditingUnit.Row);
    }

    private static void BeginEdit(DataGrid grid, DataGridCell cell)
    {
        cell.IsSelected = true;
        grid.CurrentCell = new DataGridCellInfo(cell);
        cell.Focus();
        grid.BeginEdit();
    }

    private static bool MoveToNext(DataGrid grid, bool forward)
    {
        var editable = grid.Columns
            .Where(c => !c.IsReadOnly && c.Visibility == Visibility.Visible)
            .OrderBy(c => c.DisplayIndex)
            .ToList();

        var current = grid.CurrentCell;
        int rowIndex = grid.Items.IndexOf(current.Item);
        if (editable.Count == 0 || rowIndex < 0 || current.Column is null)
            return false;

        var next = forward
            ? editable.FirstOrDefault(c => c.DisplayIndex > current.Column.DisplayIndex)
            : editable.LastOrDefault(c => c.DisplayIndex < current.Column.DisplayIndex);

        if (next is null)
        {
            rowIndex += forward ? 1 : -1;
            if (rowIndex < 0 || rowIndex >= grid.Items.Count)
                return false;

            next = forward ? editable[0] : editable[^1];
        }

        grid.ScrollIntoView(grid.Items[rowIndex], next);
        grid.UpdateLayout();

        if (GetCell(grid, rowIndex, next) is not { } cell)
            return false;

        BeginEdit(grid, cell);
        return true;
    }

    private static DataGridCell? GetCell(DataGrid grid, int rowIndex, DataGridColumn column)
    {
        if (grid.ItemContainerGenerator.ContainerFromIndex(rowIndex) is not DataGridRow row)
            return null;

        return FindChild<DataGridCellsPresenter>(row)?.ItemContainerGenerator.ContainerFromIndex(column.DisplayIndex) as DataGridCell;
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null and not T)
            child = child is Visual ? VisualTreeHelper.GetParent(child) : LogicalTreeHelper.GetParent(child);

        return child as T;
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var match = child as T ?? FindChild<T>(child);
            if (match is not null)
                return match;
        }

        return null;
    }
}
