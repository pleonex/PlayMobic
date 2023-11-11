namespace PlayMobic.UI.Pages;

using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.Styling;
using PlayMobic.UI.Mvvm;
using PlayMobic.UI.Settings;

public partial class SettingsViewModel : ObservableObject
{
    private readonly FluentAvaloniaTheme themeManager;

    [ObservableProperty]
    private string? applicationVersion;

    [ObservableProperty]
    private string license;

    [ObservableProperty]
    private ApplicationThemes currentTheme;

    [ObservableProperty]
    private string ffmpegPath;

    [ObservableProperty]
    private bool isValidFfmpegPath;

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

        ffmpegPath = AppSettingManager.Instance.LoadSettingFile()?.FfmpegPath ?? string.Empty;
        IsValidFfmpegPath = File.Exists(ffmpegPath);
        OpenFfmpegBinary = new AsyncInteraction<IStorageFile?>();
    }

    public ApplicationThemes[] AvailableThemes { get; }

    public AsyncInteraction<IStorageFile?> OpenFfmpegBinary { get; }

    [RelayCommand]
    private async Task SelectFfmpegPathAsync()
    {
        IStorageFile? selectedFile = await OpenFfmpegBinary.HandleAsync().ConfigureAwait(false);
        if (selectedFile is null) {
            return;
        }

        FfmpegPath = selectedFile.TryGetLocalPath() ?? string.Empty;
    }

    partial void OnFfmpegPathChanged(string? oldValue, string newValue)
    {
        if (oldValue == newValue) {
            return;
        }

        IsValidFfmpegPath = File.Exists(FfmpegPath);

        // Thread-issue
        var currentSettings = AppSettingManager.Instance.LoadSettingFile();
        var newSettings = (currentSettings is null)
            ? new AppSettings(FfmpegPath)
            : currentSettings with { FfmpegPath = FfmpegPath };
        AppSettingManager.Instance.SaveSettingFile(newSettings);
    }

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
