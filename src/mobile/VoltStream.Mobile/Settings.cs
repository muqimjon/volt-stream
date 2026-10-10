namespace VoltStream.Mobile;

public static class Settings
{
    public static int Revision { get; private set; }

    public static string PrimaryUrl
    {
        get => Preferences.Get(nameof(PrimaryUrl), string.Empty);
        set => Set(nameof(PrimaryUrl), value);
    }

    public static string BackupUrl
    {
        get => Preferences.Get(nameof(BackupUrl), string.Empty);
        set => Set(nameof(BackupUrl), value);
    }

    public static bool HasAddress
        => !string.IsNullOrWhiteSpace(PrimaryUrl) || !string.IsNullOrWhiteSpace(BackupUrl);

    private static void Set(string key, string value)
    {
        Preferences.Set(key, value);
        Revision++;
    }
}
