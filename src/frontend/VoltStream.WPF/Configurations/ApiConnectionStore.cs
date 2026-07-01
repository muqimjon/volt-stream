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

    private sealed record ApiConnectionDto(string Url);

    public ApiConnectionViewModel Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new();

            var json = File.ReadAllText(ConfigPath);
            var dto = JsonSerializer.Deserialize<ApiConnectionDto>(json, jsonOptions);
            if (dto is null)
                return new();

            return new ApiConnectionViewModel { Url = dto.Url };
        }
        catch
        {
            return new();
        }
    }

    public void Save(ApiConnectionViewModel model)
    {
        try
        {
            var dto = new ApiConnectionDto(model.Url);
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
            if (e.PropertyName is nameof(model.Url))
                Save(model);
        };
    }
}
