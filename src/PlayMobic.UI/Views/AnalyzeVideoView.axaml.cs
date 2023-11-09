namespace PlayMobic.UI.Views;

using Avalonia.Collections;
using Avalonia.Controls;
using PlayMobic.UI.ViewModels;

public partial class AnalyzeVideoView : UserControl
{
    public AnalyzeVideoView()
    {
        InitializeComponent();

        var viewModel = new AnalyzeVideoViewModel();
        DataContext = viewModel;

        videoInfoGrid.ItemsSource = new DataGridCollectionView(viewModel.VideoInfo) {
            GroupDescriptions = {
                new DataGridPathGroupDescription("Group"),
            },
        };
    }
}
