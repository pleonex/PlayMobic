namespace PlayMobic.UI.Views;

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;

public partial class ConvertVideoView : UserControl
{
    private readonly TaskDialog downloadDialog;

    public ConvertVideoView()
    {
        InitializeComponent();

        downloadDialog = new TaskDialog {
            Title = "PlayMobic",
            SubHeader = "Converting video",
            IconSource = new SymbolIconSource { Symbol = Symbol.Download },
            Content = "Please wait while the video converts",
            ShowProgressBar = true,
            Buttons = {
                TaskDialogButton.CancelButton,
            },
        };
        downloadDialog.Opened += async (_, _) => await Task.Delay(10_000);

        convertBtn.Click += async (_, _) => {
            downloadDialog.XamlRoot = VisualRoot as Visual;
            await downloadDialog.ShowAsync();
        };
    }

    private void UpdateDialogProgress(double progress)
    {
        downloadDialog.SetProgressBarState(progress, TaskDialogProgressState.Normal);
    }

    private void CloseDialog()
    {
        Dispatcher.UIThread.Post(() =>  downloadDialog.Hide(TaskDialogStandardResult.OK));
    }
}
