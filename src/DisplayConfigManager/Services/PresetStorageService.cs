using System.IO;
using System.Text.Json;
using DisplayConfigManager.Exceptions;
using DisplayConfigManager.Models;

namespace DisplayConfigManager.Services;

public sealed class PresetStorageService
{
    private static readonly string StorageDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DisplayConfigManager");

    private static readonly string StoragePath = Path.Combine(StorageDir, "presets.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    // ── Read ──────────────────────────────────────────────────────────────────

    public List<Preset> LoadPresets()
    {
        if (!File.Exists(StoragePath))
            return [];

        try
        {
            var json = File.ReadAllText(StoragePath);
            return JsonSerializer.Deserialize<List<Preset>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            BackupCorruptFile();
            return [];
        }
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    public void SavePresets(List<Preset> presets)
    {
        Directory.CreateDirectory(StorageDir);
        File.WriteAllText(StoragePath, JsonSerializer.Serialize(presets, JsonOptions));
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void AddPreset(Preset preset)
    {
        var list = LoadPresets();
        list.Add(preset);
        SavePresets(list);
    }

    public void DeletePreset(Guid id)
    {
        var list = LoadPresets();
        list.RemoveAll(p => p.Id == id);
        SavePresets(list);
    }

    public void RenamePreset(Guid id, string newName)
    {
        var list = LoadPresets();
        var preset = list.FirstOrDefault(p => p.Id == id)
            ?? throw new DisplayConfigException($"Preset {id} not found.");
        preset.Name = newName;
        SavePresets(list);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void BackupCorruptFile()
    {
        try
        {
            var backupPath = Path.ChangeExtension(StoragePath, ".json.bak");
            File.Copy(StoragePath, backupPath, overwrite: true);
            File.Delete(StoragePath);
        }
        catch
        {
            // Best-effort backup; ignore errors.
        }
    }
}
