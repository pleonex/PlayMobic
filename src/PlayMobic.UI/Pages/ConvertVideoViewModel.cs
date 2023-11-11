namespace PlayMobic.UI.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using PlayMobic.Containers.Mods;
using PlayMobic.UI.Mvvm;
using Yarhl.FileSystem;
using Yarhl.IO;

public partial class ConvertVideoViewModel : ObservableObject
{
    private CancellationTokenSource? convertCancellation;

    [ObservableProperty]
    private ObservableCollection<string> inputFiles;

    [ObservableProperty]
    private string outputPath;

    [ObservableProperty]
    private OutputFormatKind selectedOutputFormat;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveInputFileCommand))]
    private string? selectedInputFile;

    [ObservableProperty]
    private string ffmpegCommand;

    [ObservableProperty]
    private bool hasFfmpegCommand;

    public ConvertVideoViewModel()
    {
        inputFiles = new ObservableCollection<string>();
        outputPath = string.Empty;
        selectedOutputFormat = OutputFormatKind.MP4;

        hasFfmpegCommand = false;
        ffmpegCommand = string.Empty;

        AskOutputFolder = new AsyncInteraction<IStorageFolder?>();
        AskInputFiles = new AsyncInteraction<IEnumerable<IStorageFile>>();
        AskInputFolder = new AsyncInteraction<IStorageFolder?>();
        ShowConvertDialog = new AsyncInteraction<object>();
    }

    public OutputFormatKind[] AvailableOutputFormats => Enum.GetValues<OutputFormatKind>();

    public AsyncInteraction<IStorageFolder?> AskOutputFolder { get; }

    public AsyncInteraction<IEnumerable<IStorageFile>> AskInputFiles { get; }

    public AsyncInteraction<IStorageFolder?> AskInputFolder { get; }

    public AsyncInteraction<object> ShowConvertDialog { get; }

    public event EventHandler<ConversionProgressEventArgs>? ConversionProgressed;

    [RelayCommand]
    private async Task SelectOutputPathAsync()
    {
        IStorageFolder? selectedFolder = await AskOutputFolder.HandleAsync().ConfigureAwait(false);
        string? selectedPath = selectedFolder?.TryGetLocalPath();
        if (selectedPath is null) {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => {
            OutputPath = selectedPath;
            StartConvertCommand.NotifyCanExecuteChanged();
        });
    }

    [RelayCommand]
    private async Task AddInputFileAsync()
    {
        IEnumerable<IStorageFile> selectedFiles = await AskInputFiles.HandleAsync().ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() => {
            foreach (IStorageFile file in selectedFiles) {
                string? localPath = file.TryGetLocalPath();
                if (localPath is not null) {
                    InputFiles.Add(localPath);
                }
            }

            RemoveInputFileCommand.NotifyCanExecuteChanged();
            ClearInputFilesCommand.NotifyCanExecuteChanged();
            StartConvertCommand.NotifyCanExecuteChanged();
        });
    }

    [RelayCommand]
    private async Task AddInputFolderAsync()
    {
        IStorageFolder? selectedFolder = await AskInputFolder.HandleAsync().ConfigureAwait(false);
        string? selectedPath = selectedFolder?.TryGetLocalPath();
        if (selectedPath is null) {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => {
            foreach (string path in Directory.EnumerateFiles(selectedPath, "*.mods")) {
                InputFiles.Add(path);
            }

            RemoveInputFileCommand.NotifyCanExecuteChanged();
            ClearInputFilesCommand.NotifyCanExecuteChanged();
            StartConvertCommand.NotifyCanExecuteChanged();
        });
    }

    [RelayCommand(CanExecute = nameof(CanRemoveInputFile))]
    private void RemoveInputFile()
    {
        if (SelectedInputFile is not null) {
            InputFiles.Remove(SelectedInputFile);
        }

        ClearInputFilesCommand.NotifyCanExecuteChanged();
        StartConvertCommand.NotifyCanExecuteChanged();
    }

    private bool CanRemoveInputFile()
    {
        return SelectedInputFile is not null;
    }

    [RelayCommand(CanExecute = nameof(CanClearInputFiles))]
    private void ClearInputFiles()
    {
        InputFiles.Clear();

        RemoveInputFileCommand.NotifyCanExecuteChanged();
        StartConvertCommand.NotifyCanExecuteChanged();
    }

    private bool CanClearInputFiles()
    {
        return InputFiles.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanConvert))]
    private async Task StartConvert()
    {
        var result = await ShowConvertDialog.HandleAsync();
        if (result is TaskDialogStandardResult dialogResult && dialogResult == TaskDialogStandardResult.Cancel) {
            convertCancellation?.Cancel();
        }
    }

    private bool CanConvert()
    {
        return InputFiles.Count > 0 && !string.IsNullOrEmpty(OutputPath);
    }

    public async Task ConvertAsync()
    {
        convertCancellation = new CancellationTokenSource();

        bool success = true;
        for (int i = 0; i < InputFiles.Count && success; i++) {
            success = await Task.Run(() => ConvertFile(i, convertCancellation.Token));
        }

        if (success) {
            ConversionProgressed?.Invoke(this, new ConversionProgressEventArgs("Completed!", 100, true));
        }
    }

    private bool ConvertFile(int fileIndex, CancellationToken token)
    {
        string inputPath = InputFiles[fileIndex];
        using Node input = NodeFactory.FromFile(inputPath, FileOpenMode.Read)
            .TransformWith(new Binary2Mods());

        token.ThrowIfCancellationRequested();
        switch (SelectedOutputFormat) {
            case OutputFormatKind.Raw:
                return ConvertToRaw(input, fileIndex);

            case OutputFormatKind.AVI:
                return ConvertToAvi(input, fileIndex);

            case OutputFormatKind.MP4:
                break;
        }

        return true;
    }

    private bool ConvertToAvi(Node input, int index)
    {
        string name = Path.GetFileNameWithoutExtension(input.Name);
        string outputAviPath = Path.Combine(OutputPath, name + ".avi");

        DataStream outputAvi = DataStreamFactory.FromFile(outputAviPath, FileOpenMode.Write);
        var converter = new Mods2BinaryAvi(outputAvi);

        int framesCount = input.GetFormatAs<ModsVideo>()!.Info.FramesCount;
        converter.ProgressUpdate += (_, e) => {
            convertCancellation?.Token.ThrowIfCancellationRequested();
            ConversionProgressed?.Invoke(
                this,
                new ConversionProgressEventArgs(input.Name, GetProgress(e, framesCount, index), false));
        };

        try {
            _ = input.TransformWith(converter);
            outputAvi.Dispose();
            return true;
        } catch (Exception ex) {
            outputAvi.Dispose();
            File.Delete(outputAviPath);

            ConversionProgressed?.Invoke(this, new ConversionProgressEventArgs(input.Name, 100, ex.Message));
            return false;
        }
    }

    private bool ConvertToRaw(Node input, int index)
    {
        string name = Path.GetFileNameWithoutExtension(input.Name);
        string outputVideo = Path.Combine(OutputPath, name + ".rawvideo");
        DataStream outputVideoStream = DataStreamFactory.FromFile(outputVideo, FileOpenMode.Write);

        string outputAudio = Path.Combine(OutputPath, name + ".rawaudio");
        DataStream outputAudioStream = DataStreamFactory.FromFile(outputAudio, FileOpenMode.Write);

        var converter = new Mods2RawContainer(outputVideoStream, outputAudioStream);

        int framesCount = input.GetFormatAs<ModsVideo>()!.Info.FramesCount;
        converter.ProgressUpdate += (_, e) => {
            convertCancellation?.Token.ThrowIfCancellationRequested();
            ConversionProgressed?.Invoke(
                this,
                new ConversionProgressEventArgs(input.Name, GetProgress(e, framesCount, index), false));
        };

        try {
            _ = input.TransformWith(converter);
            outputVideoStream.Dispose();
            outputAudioStream.Dispose();
            return true;
        } catch (Exception ex) {
#pragma warning disable S3966
            outputVideoStream.Dispose();
            outputAudioStream.Dispose();
            File.Delete(outputVideo);
            File.Delete(outputAudio);
#pragma warning restore S3966

            ConversionProgressed?.Invoke(this, new ConversionProgressEventArgs(input.Name, 100, ex.Message));
            return false;
        }
    }

    private double GetProgress(int frame, int framesCount, int fileIndex)
    {
        double currentFileProgress = 100d * frame / framesCount / InputFiles.Count;
        double baseProgress = 100d * fileIndex / InputFiles.Count;
        return baseProgress + currentFileProgress;
    }
}
