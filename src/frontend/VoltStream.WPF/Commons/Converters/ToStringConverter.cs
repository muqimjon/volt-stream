namespace VoltStream.WPF.Commons.Converters;

using System.Globalization;
using System.Windows.Data;

public class ToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int number16)
            return number16.ToString();
        else if (value is decimal number32)
            return number32.ToString("N2");
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return default!;
    }
}