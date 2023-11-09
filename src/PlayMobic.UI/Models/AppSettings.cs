namespace PlayMobic.UI.Models;

internal record AppSettings(string FfmpegPath)
{
    public static string Filename => "settings.json";
}
