namespace VoltStream.WPF.Commons.Converters;

using System.Globalization;
using System.Text.Json;
using System.Windows.Data;

public class JsonPrettyConverter : IValueConverter
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string json || string.IsNullOrWhiteSpace(json))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, Options);
        }
        catch
        {
            return json;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
