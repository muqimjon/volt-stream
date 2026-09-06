namespace VoltStream.WPF.Commons.Utils;

using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VoltStream.WPF.Commons.Dialogs;

public static class ComboBoxHelper
{
    public static bool BeforeUpdate(object sender, KeyboardFocusChangedEventArgs e, string? strInfo, bool allow = false)
    {
        if (sender is not ComboBox cb) return false;

        var input = cb.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(input)) return false;

        if (cb.ItemsSource is null) return false;

        var items = cb.ItemsSource.Cast<object>();
        var prop = string.IsNullOrEmpty(cb.DisplayMemberPath) ? null : items.FirstOrDefault()?.GetType().GetProperty(cb.DisplayMemberPath);

        bool inputIsNumber = decimal.TryParse(NumericInput.Normalize(input), NumberStyles.Any, CultureInfo.InvariantCulture, out var inputNumber);

        bool exists = items.Any(item =>
        {
            var val = prop != null ? prop.GetValue(item)?.ToString() : item?.ToString();
            if (inputIsNumber && decimal.TryParse(NumericInput.Normalize(val ?? ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var valNumber))
                return valNumber == inputNumber;
            return string.Equals(val?.Trim(), input, StringComparison.OrdinalIgnoreCase);
        });

        if (exists) return false;

        if (allow && AppDialog.Confirm($"{input} - {strInfo}: ro'yxatda yo'q. Yangi qo'shilsinmi?", "Tekshiruv", Window.GetWindow(cb)))
            return true;

        cb.Text = "";
        cb.SelectedItem = cb.SelectedValue = null;
        e.Handled = true;

        if (!allow) AppDialog.Warning($"{input} - {strInfo}: ro'yxatda yo'q.", "Tekshiruv", Window.GetWindow(cb));

        Application.Current.Dispatcher.BeginInvoke(() => { try { cb.IsDropDownOpen = true; } catch { } });
        return false;
    }
}