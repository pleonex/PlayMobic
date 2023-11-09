namespace PlayMobic.UI.ViewModels;

using System;
using System.IO;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.Styling;
using PlayMobic.UI.Models;

public partial class SettingsViewModel : ObservableObject
{
    private readonly FluentAvaloniaTheme themeManager;

    [ObservableProperty]
    private string? applicationVersion;

    [ObservableProperty]
    private string license;

    [ObservableProperty]
    private ApplicationThemes currentTheme;

    public SettingsViewModel()
    {
        AvailableThemes = Enum.GetValues<ApplicationThemes>();
        currentTheme = ApplicationThemes.System;
        themeManager = Application.Current?.Styles[0] as FluentAvaloniaTheme
            ?? throw new InvalidOperationException("Cannot get theme manager");

        ApplicationVersion = typeof(Program).Assembly.GetName().Version?.ToString();

        string appUri = "avares://" + typeof(Program).Namespace;
        using Stream licenseStream = AssetLoader.Open(new Uri(appUri + "/Assets/LICENSE"));
        using var licenseStreamReader = new StreamReader(licenseStream);
        License = licenseStreamReader.ReadToEnd();
    }

    public ApplicationThemes[] AvailableThemes { get; }

    partial void OnCurrentThemeChanged(ApplicationThemes value)
    {
        switch (value) {
            case ApplicationThemes.System:
                themeManager.PreferSystemTheme = true;
                break;

            case ApplicationThemes.Light:
                themeManager.PreferSystemTheme = false;
                Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
                break;

            case ApplicationThemes.Dark:
                themeManager.PreferSystemTheme = false;
                Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
                break;
        }
    }
}
