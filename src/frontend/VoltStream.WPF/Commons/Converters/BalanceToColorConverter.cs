namespace VoltStream.WPF.Commons.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

public class BalanceToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value is decimal balance
            ? balance > 0 ? "Success" : balance < 0 ? "Danger" : "TextPrimary"
            : "TextPrimary";
        return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
