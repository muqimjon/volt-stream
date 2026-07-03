namespace VoltStream.WPF.Commons.Converters;

using System.Globalization;
using System.Windows.Data;

public class UtcToUzbekistanTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            TimeSpan uzbekistanOffset = TimeSpan.FromHours(5);
            DateTimeOffset uzbekistanTime = dateTimeOffset.ToOffset(uzbekistanOffset);
            return uzbekistanTime;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
