namespace VoltStream.WPF.Commons.Utils;

using System.Windows;
using System.Windows.Controls;

public static class DataGridRowNumbering
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridRowNumbering),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DataGrid obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DataGrid obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGrid dataGrid)
        {
            if ((bool)e.NewValue)
            {
                dataGrid.LoadingRow += DataGrid_LoadingRow;
                dataGrid.UnloadingRow += DataGrid_UnloadingRow;
            }
            else
            {
                dataGrid.LoadingRow -= DataGrid_LoadingRow;
                dataGrid.UnloadingRow -= DataGrid_UnloadingRow;
            }
        }
    }

    private static void DataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }

    private static void DataGrid_UnloadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Header = null;
    }
}