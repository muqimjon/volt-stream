namespace VoltStream.WPF.Commons.Converters;

using System.Globalization;
using System.Windows.Data;
using VoltStream.WPF.Commons.Localization;

public class LocalizedEnumConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is null ? string.Empty : TranslationSource.T($"{parameter}.{value}");

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
