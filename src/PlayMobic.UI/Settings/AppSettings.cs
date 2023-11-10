namespace PlayMobic.UI.Settings;

internal record AppSettings(string FfmpegPath)
{
    public static string Filename => "settings.json";
}
