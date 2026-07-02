namespace VoltStream.WPF.Commons.Localization;

using System.ComponentModel;
using System.Windows.Data;

public sealed class TranslationSource : INotifyPropertyChanged
{
    public static TranslationSource Instance { get; } = new();

    private IReadOnlyDictionary<string, string> current = new Dictionary<string, string>();
    private IReadOnlyDictionary<string, string> fallback = new Dictionary<string, string>();

    public string this[string key]
    {
        get
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            if (current.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                return value;
            if (fallback.TryGetValue(key, out var fall))
                return fall;
            return key;
        }
    }

    public static string T(string key) => Instance[key];

    public void SetDictionaries(IReadOnlyDictionary<string, string> current, IReadOnlyDictionary<string, string> fallback)
    {
        this.current = current;
        this.fallback = fallback;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
