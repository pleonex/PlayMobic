namespace PlayMobic.UI.Pages;

using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        var viewModel = new SettingsViewModel();
        DataContext = viewModel;

        viewModel.OpenFfmpegBinary.RegisterHandler(OpenFfmpegBinary);
    }

    private async Task<IStorageFile?> OpenFfmpegBinary()
    {
        var options = new FilePickerOpenOptions {
            AllowMultiple = false,
            Title = "Select ffmpeg binary"
        };

        var results = await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .OpenFilePickerAsync(options)
            .ConfigureAwait(false);
        return results.FirstOrDefault();
    }
}
