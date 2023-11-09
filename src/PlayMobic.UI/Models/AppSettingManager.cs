namespace PlayMobic.UI.Models;
using System;
using System.IO;
using System.Text.Json;

internal static class AppSettingManager
{
    private static string? SettingsPath =>
        Environment.ProcessPath is null
            ? null
            : Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "settings.json");

    public static AppSettings? LoadSettingFile()
    {
        if (!File.Exists(SettingsPath)) {
            return null;
        }

        string json = File.ReadAllText(SettingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json);
    }

    public static void SaveSettingFile(AppSettings settings)
    {
        if (SettingsPath is null) {
            throw new InvalidOperationException();
        }

        string json = JsonSerializer.Serialize(settings);
        File.WriteAllText(SettingsPath, json);
    }
}
