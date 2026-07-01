namespace VoltStream.WPF.Commons.Services;

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

public enum AppTheme { Light, Dark }

public static class ThemeManager
{
    private const string ConfigPath = "config/app-settings.json";

    private static readonly Uri LightUri = new("pack://application:,,,/VoltStream.WPF;component/Commons/Styles/Theme/Light.xaml");
    private static readonly Uri DarkUri = new("pack://application:,,,/VoltStream.WPF;component/Commons/Styles/Theme/Dark.xaml");

    public static AppTheme Current { get; private set; } = AppTheme.Light;

    public static event Action<AppTheme>? ThemeChanged;

    public static void Initialize() => Apply(Load(), save: false);

    public static void Toggle() => Apply(Current == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);

    public static void Apply(AppTheme theme, bool save = true)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var existing = dicts.FirstOrDefault(d => d.Source == LightUri || d.Source == DarkUri);
        var next = new ResourceDictionary { Source = theme == AppTheme.Dark ? DarkUri : LightUri };

        if (existing is not null)
            dicts[dicts.IndexOf(existing)] = next;
        else
            dicts.Insert(0, next);

        Current = theme;
        if (save)
            Save(theme);

        ThemeChanged?.Invoke(theme);
    }

    private static AppTheme Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var dto = JsonSerializer.Deserialize<Settings>(File.ReadAllText(ConfigPath));
                if (dto is not null && Enum.TryParse<AppTheme>(dto.Theme, out var t))
                    return t;
            }
        }
        catch { }

        return AppTheme.Light;
    }

    private static void Save(AppTheme theme)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(new Settings { Theme = theme.ToString() }));
        }
        catch { }
    }

    private sealed class Settings
    {
        public string Theme { get; set; } = "Light";
    }
}
