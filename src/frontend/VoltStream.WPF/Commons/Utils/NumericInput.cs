namespace VoltStream.WPF.Commons.Utils;

using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public static class NumericInput
{
    public static readonly DependencyProperty IsNumericProperty =
        DependencyProperty.RegisterAttached("IsNumeric", typeof(bool), typeof(NumericInput), new PropertyMetadata(false, OnIsNumericChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.RegisterAttached("Value", typeof(object), typeof(NumericInput), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty PrecisionProperty =
        DependencyProperty.RegisterAttached("Precision", typeof(int?), typeof(NumericInput), new PropertyMetadata(null));

    private static readonly DependencyProperty IsSubscribedProperty =
        DependencyProperty.RegisterAttached("IsSubscribed", typeof(bool), typeof(NumericInput), new PropertyMetadata(false));

    private static readonly DependencyProperty IsInternalChangeProperty =
        DependencyProperty.RegisterAttached("IsInternalChange", typeof(bool), typeof(NumericInput), new PropertyMetadata(false));

    public static bool GetIsNumeric(DependencyObject obj) => obj != null && (bool)obj.GetValue(IsNumericProperty);
    public static void SetIsNumeric(DependencyObject obj, bool value) => obj?.SetValue(IsNumericProperty, value);

    public static object GetValue(DependencyObject obj) => obj?.GetValue(ValueProperty)!;
    public static void SetValue(DependencyObject obj, object value) => obj?.SetValue(ValueProperty, value);

    public static int? GetPrecision(DependencyObject obj) => obj != null ? (int?)obj.GetValue(PrecisionProperty) : null;
    public static void SetPrecision(DependencyObject obj, int? value) => obj?.SetValue(PrecisionProperty, value);

    private static void OnIsNumericChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            ManageSubscription(textBox, (bool)e.NewValue);
        }
        else if (d is ComboBox comboBox)
        {
            if (comboBox.IsLoaded) SetupComboBox(comboBox);
            else comboBox.Loaded += (s, _) => SetupComboBox(comboBox);
        }
    }

    private static void SetupComboBox(ComboBox cb)
    {
        if (cb.Template.FindName("PART_EditableTextBox", cb) is TextBox textBox)
        {
            ManageSubscription(textBox, GetIsNumeric(cb));
        }
    }

    private static void ManageSubscription(TextBox textBox, bool subscribe)
    {
        if (subscribe)
        {
            if ((bool)textBox.GetValue(IsSubscribedProperty)) return;
            textBox.TextAlignment = TextAlignment.Right;
            textBox.GotFocus += OnGotFocus;
            textBox.PreviewTextInput += OnPreviewTextInput;
            textBox.TextChanged += OnTextChanged;
            textBox.LostFocus += OnLostFocus;
            DataObject.AddPastingHandler(textBox, OnPaste);
            textBox.SetValue(IsSubscribedProperty, true);
            FormatValue(textBox);
        }
        else
        {
            textBox.GotFocus -= OnGotFocus;
            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.TextChanged -= OnTextChanged;
            textBox.LostFocus -= OnLostFocus;
            DataObject.RemovePastingHandler(textBox, OnPaste);
            textBox.SetValue(IsSubscribedProperty, false);
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || (bool)d.GetValue(IsInternalChangeProperty)) return;

        TextBox target = (d as TextBox)!;
        if (d is ComboBox cb) target = (cb.Template?.FindName("PART_EditableTextBox", cb) as TextBox)!;

        if (target != null && !target.IsFocused) FormatValue(target);
    }

    private static void FormatValue(TextBox textBox)
    {
        textBox.SetValue(IsInternalChangeProperty, true);

        var parent = textBox.TemplatedParent as DependencyObject;
        var value = GetValue(textBox) ?? (parent != null ? GetValue(parent) : null);
        var precision = GetPrecision(textBox) ?? (parent != null ? GetPrecision(parent) : null);

        if (value == null)
        {
            textBox.Text = string.Empty;
        }
        else
        {
            decimal decimalValue = ConvertToDecimal(value);
            string format = precision.HasValue ? $"N{precision.Value}" : "N" + GetOptimalDecimalPlaces(decimalValue);
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberGroupSeparator = " ";
            textBox.Text = decimalValue.ToString(format, culture);
        }

        textBox.SetValue(IsInternalChangeProperty, false);
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox || (bool)textBox.GetValue(IsInternalChangeProperty)) return;

        textBox.SetValue(IsInternalChangeProperty, true);
        string clean = textBox.Text.Replace(" ", "");
        string original = textBox.Text;
        int caret = textBox.CaretIndex;
        textBox.SetValue(IsInternalChangeProperty, false);

        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
            UpdateSource(textBox, val);
        else if (string.IsNullOrEmpty(clean))
            UpdateSource(textBox, null);

        textBox.SetValue(IsInternalChangeProperty, true);
        string formatted = ApplyFormatting(clean);
        if (formatted != original)
        {
            textBox.Text = formatted;
            RestoreCaretPosition(textBox, original, formatted, caret);
        }
        textBox.SetValue(IsInternalChangeProperty, false);
    }

    private static void UpdateSource(TextBox textBox, decimal? val)
    {
        DependencyObject targetObj = (textBox.TemplatedParent is ComboBox cb) ? cb : textBox;

        if (val == null)
        {
            targetObj.SetCurrentValue(ValueProperty, null);
            return;
        }

        var binding = (targetObj as FrameworkElement)?.GetBindingExpression(ValueProperty);
        if (binding?.ResolvedSourcePropertyName != null)
        {
            var prop = binding.ResolvedSource?.GetType().GetProperty(binding.ResolvedSourcePropertyName);
            if (prop != null)
            {
                var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                object finalVal = type switch
                {
                    var t when t == typeof(int) => (int)Math.Round(val.Value),
                    var t when t == typeof(double) => (double)val.Value,
                    _ => val.Value
                };
                targetObj.SetCurrentValue(ValueProperty, finalVal);
                return;
            }
        }
        targetObj.SetCurrentValue(ValueProperty, val.Value);
    }

    private static void OnGotFocus(object sender, RoutedEventArgs e) => (sender as TextBox)?.SelectAll();
    private static void OnLostFocus(object sender, RoutedEventArgs e) => FormatValue((TextBox)sender);
    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var tb = (TextBox)sender;
        string nextText = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength).Insert(tb.SelectionStart, e.Text).Replace(" ", "");
        if (!Regex.IsMatch(e.Text, @"^[\d\.,]+$") || nextText.Count(c => c == '.') > 1) e.Handled = true;
    }
    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            if (!Regex.IsMatch((string)e.DataObject.GetData(DataFormats.Text), @"^[\d\s\.]+$")) e.CancelCommand();
        }
    }
    private static string ApplyFormatting(string clean)
    {
        if (string.IsNullOrEmpty(clean) || clean == ".") return clean;
        string[] p = clean.Split('.');
        if (decimal.TryParse(p[0], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal v))
        {
            var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            c.NumberFormat.NumberGroupSeparator = " ";
            string f = v.ToString("N0", c);
            return p.Length > 1 ? f + "." + p[1] : (clean.EndsWith(".") ? f + "." : f);
        }
        return clean;
    }
    private static void RestoreCaretPosition(TextBox tb, string old, string n, int oldC)
    {
        int d = old.Substring(0, Math.Min(oldC, old.Length)).Count(c => c != ' ');
        int newC = 0, f = 0;
        while (newC < n.Length && f < d) { if (n[newC] != ' ') f++; newC++; }
        tb.CaretIndex = newC;
    }
    private static int GetOptimalDecimalPlaces(decimal v) { string s = v.ToString(CultureInfo.InvariantCulture); int i = s.IndexOf('.'); return i == -1 ? 0 : s.Length - i - 1; }
    private static decimal ConvertToDecimal(object v) { try { return Convert.ToDecimal(v, CultureInfo.InvariantCulture); } catch { return 0; } }
}