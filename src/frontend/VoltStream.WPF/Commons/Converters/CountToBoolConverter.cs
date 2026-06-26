namespace VoltStream.WPF.Commons.Converters;

using System.Globalization;
using System.Windows.Data;

// Kolleksiya elementlari soni > 0 bo'lsa true (tugmalarni ma'lumot bo'lmasa o'chirish uchun).
public class CountToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count && count > 0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
