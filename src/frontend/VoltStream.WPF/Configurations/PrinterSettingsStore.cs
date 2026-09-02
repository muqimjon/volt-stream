namespace VoltStream.WPF.Configurations;

using System.IO;
using System.Text.Json;
using VoltStream.WPF.Commons.ViewModels;

public class PrinterSettingsStore
{
    private static readonly string ConfigPath = AppStorage.PathFor("printer.json");
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private sealed record PrinterSettingsDto(bool Enabled, string? PrinterName, int Copies, string Paper);

    public PrinterSettings Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new();

            var dto = JsonSerializer.Deserialize<PrinterSettingsDto>(File.ReadAllText(ConfigPath), jsonOptions);
            if (dto is null)
                return new();

            return new PrinterSettings
            {
                Enabled = dto.Enabled,
                PrinterName = dto.PrinterName,
                Copies = dto.Copies < 1 ? 1 : dto.Copies,
                Paper = string.IsNullOrWhiteSpace(dto.Paper) ? "A4" : dto.Paper
            };
        }
        catch
        {
            return new();
        }
    }

    public void Save(PrinterSettings model)
    {
        try
        {
            var dto = new PrinterSettingsDto(model.Enabled, model.PrinterName, model.Copies, model.Paper);
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(dto, jsonOptions));
        }
        catch { }
    }

    public void BindAutoSave(PrinterSettings model)
        => model.PropertyChanged += (_, _) => Save(model);
}
