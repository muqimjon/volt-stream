namespace VoltStream.WPF.Configurations;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoltStream.WPF.Commons.Services;
using VoltStream.WPF.Commons.ViewModels;

public class PrinterSettingsStore
{
    private static readonly string ConfigPath = AppStorage.PathFor("printer.json");
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed record PrinterSettingsDto(bool Enabled, string? PrinterName, int Copies, string Paper, ReceiptLayout Layout = ReceiptLayout.Expanded, int Priority = 1, long? BranchId = null, string? BranchName = null);

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
                Paper = string.IsNullOrWhiteSpace(dto.Paper) ? "A4" : dto.Paper,
                Layout = dto.Layout,
                Priority = dto.Priority < 1 ? 1 : Math.Min(dto.Priority, 5),
                BranchId = dto.BranchId,
                BranchName = dto.BranchName
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
            var dto = new PrinterSettingsDto(model.Enabled, model.PrinterName, model.Copies, model.Paper, model.Layout, model.Priority, model.BranchId, model.BranchName);
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(dto, jsonOptions));
        }
        catch { }
    }

    public void BindAutoSave(PrinterSettings model)
        => model.PropertyChanged += (_, _) => Save(model);
}
