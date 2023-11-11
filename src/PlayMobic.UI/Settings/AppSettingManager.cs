namespace PlayMobic.UI.Settings;
using System;
using System.IO;
using System.Text.Json;

internal class AppSettingManager
{
    private static AppSettingManager? instance;
    private static string? SettingsPath =>
        Environment.ProcessPath is null
            ? null
            : Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "settings.json");

    public static AppSettingManager Instance => instance ??= new AppSettingManager();

    public event EventHandler<AppSettings>? SettingsChanged;

    public AppSettings? LoadSettingFile()
    {
        if (!File.Exists(SettingsPath)) {
            return null;
        }

        string json = File.ReadAllText(SettingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json);
    }

    public void SaveSettingFile(AppSettings settings)
    {
        if (SettingsPath is null) {
            throw new InvalidOperationException();
        }

        string json = JsonSerializer.Serialize(settings);
        File.WriteAllText(SettingsPath, json);

        SettingsChanged?.Invoke(this, settings);
    }
}
