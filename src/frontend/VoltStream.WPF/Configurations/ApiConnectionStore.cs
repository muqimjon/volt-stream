namespace VoltStream.WPF.Configurations;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoltStream.WPF.Commons.ViewModels;

public class ApiConnectionStore
{
    private const string ConfigPath = "config/api-connection.json";
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed record ApiConnectionDto(
        string Url,
        bool AutoReconnectEnabled,
        bool CheckUrlEnabled,
        bool ShowIndicator);

    public ApiConnectionViewModel Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new() { AutoReconnectEnabled = true };

            var json = File.ReadAllText(ConfigPath);
            var dto = JsonSerializer.Deserialize<ApiConnectionDto>(json, jsonOptions);
            if (dto is null)
                return new() { AutoReconnectEnabled = true };

            return new ApiConnectionViewModel
            {
                Url = dto.Url,
                AutoReconnectEnabled = dto.AutoReconnectEnabled,
                CheckUrlEnabled = dto.CheckUrlEnabled,
                ShowIndicator = dto.ShowIndicator
            };
        }
        catch
        {
            return new() { AutoReconnectEnabled = true };
        }
    }

    public void Save(ApiConnectionViewModel model)
    {
        try
        {
            var dto = new ApiConnectionDto(model.Url, model.AutoReconnectEnabled, model.CheckUrlEnabled, model.ShowIndicator);
            var json = JsonSerializer.Serialize(dto, jsonOptions);
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }

    public void BindAutoSave(ApiConnectionViewModel model)
    {
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(model.Url) or nameof(model.AutoReconnectEnabled))
                Save(model);
        };
    }
}
