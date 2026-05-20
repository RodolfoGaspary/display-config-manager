using System.IO;
using System.Text.Json;
using DisplayConfigManager.Models;

namespace DisplayConfigManager.Services;

public sealed class SettingsStorageService
{
    private static readonly string StorageDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DisplayConfigManager");

    private static readonly string StoragePath = Path.Combine(StorageDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public Settings Load()
    {
        if (!File.Exists(StoragePath))
            return new Settings();

        try
        {
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(StoragePath), JsonOptions)
                ?? new Settings();
        }
        catch (JsonException)
        {
            return new Settings();
        }
    }

    public void Save(Settings settings)
    {
        Directory.CreateDirectory(StorageDir);
        File.WriteAllText(StoragePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
