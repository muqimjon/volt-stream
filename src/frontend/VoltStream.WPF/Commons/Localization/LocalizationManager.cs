namespace VoltStream.WPF.Commons.Localization;

using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

public enum AppLanguage { UzLatn, UzCyrl, Ru, En }

public static class LocalizationManager
{
    private const string ConfigPath = "config/language.json";
    private const AppLanguage Fallback = AppLanguage.UzLatn;

    private static readonly Dictionary<AppLanguage, string> Codes = new()
    {
        [AppLanguage.UzLatn] = "uz-Latn",
        [AppLanguage.UzCyrl] = "uz-Cyrl",
        [AppLanguage.Ru] = "ru",
        [AppLanguage.En] = "en",
    };

    private static readonly Dictionary<AppLanguage, IReadOnlyDictionary<string, string>> maps = new();

    public static AppLanguage Current { get; private set; } = Fallback;

    public static event Action<AppLanguage>? LanguageChanged;

    public static void Initialize() => Apply(Load(), save: false);

    public static void Apply(AppLanguage language, bool save = true)
    {
        EnsureLoaded();
        var current = maps.TryGetValue(language, out var c) ? c : maps[Fallback];
        var fallback = maps.TryGetValue(Fallback, out var f) ? f : new Dictionary<string, string>();

        TranslationSource.Instance.SetDictionaries(current, fallback);
        Current = language;

        if (save)
            Save(language);

        LanguageChanged?.Invoke(language);
    }

    private static void EnsureLoaded()
    {
        if (maps.Count > 0)
            return;

        foreach (var (lang, code) in Codes)
            maps[lang] = LoadJson(code);
    }

    private static IReadOnlyDictionary<string, string> LoadJson(string code)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith($".{code}.json", StringComparison.OrdinalIgnoreCase));

            if (name is null)
                return new Dictionary<string, string>();

            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd())
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static AppLanguage Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var dto = JsonSerializer.Deserialize<Settings>(File.ReadAllText(ConfigPath));
                if (dto is not null && Enum.TryParse<AppLanguage>(dto.Language, out var lang))
                    return lang;
            }
        }
        catch { }

        return Fallback;
    }

    private static void Save(AppLanguage language)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(new Settings { Language = language.ToString() }));
        }
        catch { }
    }

    private sealed class Settings
    {
        public string Language { get; set; } = nameof(AppLanguage.UzLatn);
    }
}
