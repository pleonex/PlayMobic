namespace PlayMobic.UI.Pages;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;

public partial class ConvertVideoView : UserControl
{
    private readonly ConvertVideoViewModel viewModel;
    private readonly TaskDialog convertingDialog;
    private readonly TaskDialogButton convertingCancelButton;
    private readonly TaskDialogButton convertingOkButton;

    public ConvertVideoView()
    {
        InitializeComponent();

        viewModel = new ConvertVideoViewModel();
        DataContext = viewModel;

        viewModel.AskOutputFolder.RegisterHandler(SelectOutputFolder);
        viewModel.AskInputFiles.RegisterHandler(SelectInputFiles);
        viewModel.AskInputFolder.RegisterHandler(SelectInputFolder);

        convertingCancelButton = new TaskDialogButton("Cancel", TaskDialogStandardResult.Cancel);
        convertingOkButton = new TaskDialogButton("OK", TaskDialogStandardResult.OK);
        convertingDialog = new TaskDialog {
            Title = "PlayMobic",
            SubHeader = "Converting videos",
            IconSource = new SymbolIconSource { Symbol = Symbol.Sync },
            Content = string.Empty,
            ShowProgressBar = true,
            Buttons = {
                convertingCancelButton,
                convertingOkButton,
            },
        };

        viewModel.ShowConvertDialog.RegisterHandler(ShowConvertDialog);
        convertingDialog.Opened += OnConvertingDialogOpened;

        viewModel.ConversionProgressed += OnConversionProgressed;
    }

    private async Task<object> ShowConvertDialog()
    {
        convertingCancelButton.IsEnabled = true;
        convertingOkButton.IsEnabled = false;
        convertingDialog.XamlRoot = VisualRoot as Visual;
        return await convertingDialog.ShowAsync().ConfigureAwait(false);
    }

    private async void OnConvertingDialogOpened(TaskDialog sender, EventArgs args)
    {
        await viewModel.ConvertAsync();
    }

    private void OnConversionProgressed(object? _, ConversionProgressEventArgs e)
    {
        if (e.HasError) {
            Dispatcher.UIThread.Post(() => 
                convertingDialog.Content = e.FilePath + Environment.NewLine + "Error: " + e.ErrorDescription);
            convertingDialog.SetProgressBarState(e.Progress, TaskDialogProgressState.Error);
        } else {
            Dispatcher.UIThread.Post(() => convertingDialog.Content = e.FilePath);
            convertingDialog.SetProgressBarState(e.Progress, TaskDialogProgressState.Normal);
        }

        if (e.HasFinished) {
            Dispatcher.UIThread.Post(() => {
                convertingOkButton.IsEnabled = true;
                convertingCancelButton.IsEnabled = false;
            });
        }
    }

    private async Task<IStorageFolder?> SelectOutputFolder()
    {
        var options = new FolderPickerOpenOptions {
            AllowMultiple = false,
            Title = "Select the output folder"
        };

        var results = await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .OpenFolderPickerAsync(options)
            .ConfigureAwait(false);
        return results.FirstOrDefault();
    }

    private async Task<IEnumerable<IStorageFile>> SelectInputFiles()
    {
        var options = new FilePickerOpenOptions {
            AllowMultiple = true,
            Title = "Select the input MODS files",
            FileTypeFilter = new FilePickerFileType[] {
                new FilePickerFileType("MODS videos") {
                    Patterns = new[] { "*.mods" }
                },
                FilePickerFileTypes.All,
            },
        };

        return await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .OpenFilePickerAsync(options)
            .ConfigureAwait(false);
    }

    private async Task<IStorageFolder?> SelectInputFolder()
    {
        var options = new FolderPickerOpenOptions {
            AllowMultiple = false,
            Title = "Select the folder to search for MODS files"
        };

        var results = await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .OpenFolderPickerAsync(options)
            .ConfigureAwait(false);
        return results.FirstOrDefault();
    }
}
