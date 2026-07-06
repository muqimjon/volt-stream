namespace VoltStream.WPF.Configurations;

using System.IO;

public static class AppStorage
{
    public static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ovoza", "VoltStream");

    public static string PathFor(string fileName)
    {
        Directory.CreateDirectory(Root);
        return Path.Combine(Root, fileName);
    }
}
