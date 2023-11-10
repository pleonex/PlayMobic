namespace PlayMobic.UI.Views;

using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using PlayMobic.UI.ViewModels;

public partial class AnalyzeVideoView : UserControl
{
    public AnalyzeVideoView()
    {
        InitializeComponent();

        var viewModel = new AnalyzeVideoViewModel();
        DataContext = viewModel;

        viewModel.SelectModsFile.RegisterHandler(SelectModsFile);

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
}
