namespace PlayMobic.UI.Pages;

using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

public partial class AnalyzeVideoView : UserControl
{
    public AnalyzeVideoView()
    {
        InitializeComponent();

        var viewModel = new AnalyzeVideoViewModel();
        DataContext = viewModel;

        viewModel.SelectModsFile.RegisterHandler(SelectModsFile);
        viewModel.AskFrameOutputPath.RegisterHandler(AskExportFramePath);

        videoInfoGrid.ItemsSource = new DataGridCollectionView(viewModel.VideoInfo) {
            GroupDescriptions = {
                new DataGridPathGroupDescription("Group"),
            },
        };
    }

    private async Task<IStorageFile?> SelectModsFile()
    {
        var options = new FilePickerOpenOptions {
            AllowMultiple = false,
            Title = "Select the MDOS video file"
        };

        var results = await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .OpenFilePickerAsync(options)
            .ConfigureAwait(false);
        return results.FirstOrDefault();
    }

    private async Task<IStorageFile?> AskExportFramePath()
    {
        var options = new FilePickerSaveOptions {
            DefaultExtension = ".png",
            FileTypeChoices = new[] { FilePickerFileTypes.ImagePng },
            ShowOverwritePrompt = true,
            SuggestedFileName = "frame.png",
            Title = "Select where to save the frame"
        };

        return await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .SaveFilePickerAsync(options)
            .ConfigureAwait(false);
    }
}
