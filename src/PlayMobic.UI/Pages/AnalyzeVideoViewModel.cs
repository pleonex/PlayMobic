namespace PlayMobic.UI.Pages;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlayMobic.Containers.Mods;
using PlayMobic.UI.Decoders;
using PlayMobic.UI.Mvvm;

public partial class AnalyzeVideoViewModel : ObservableObject
{
    private readonly Dictionary<string, VideoInfoField> videoInfo;
    private readonly object decodingSync = new object();
    private CancellationTokenSource? previousDecodingCancellation;
    private VideoFrameDecoder? decoder;

    [ObservableProperty]
    private string modsFilePath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextFrameCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousFrameCommand))]
    private int framesCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextFrameCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousFrameCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportFrameCommand))]
    private int currentFrame;

    [ObservableProperty]
    private string currentTime;

    [ObservableProperty]
    private Bitmap? currentFrameImage;

    [ObservableProperty]
    private bool isFrameLoading;

    public AnalyzeVideoViewModel()
    {
        SelectModsFile = new AsyncInteraction<IStorageFile?>();
        AskFrameOutputPath = new AsyncInteraction<IStorageFile?>();

        ModsFilePath = string.Empty;
        videoInfo = new Dictionary<string, VideoInfoField> {
            { "Container", new VideoInfoField("Format", "Container") },
            { "MetadataCount", new VideoInfoField("Metadata count", "Container") },
            { "VideoCodec", new VideoInfoField("Codec", "Video") },
            { "Resolution", new VideoInfoField("Resolution", "Video") },
            { "Frames", new VideoInfoField("Frames", "Video") },
            { "Duration", new VideoInfoField("Duration", "Video") },
            { "FPS", new VideoInfoField("Frames per second", "Video") },
            { "KeyFrames", new VideoInfoField("Key frames", "Video") },
            { "AudioCodec", new VideoInfoField("Codec", "Audio") },
            { "Channels", new VideoInfoField("Channels", "Audio") },
            { "Frequency", new VideoInfoField("Frequency", "Audio") },
        };

        currentFrame = -1;
        currentTime = TimeSpan.Zero.ToString("g");
    }

    public AsyncInteraction<IStorageFile?> SelectModsFile { get; }

    public AsyncInteraction<IStorageFile?> AskFrameOutputPath { get; }

    public IReadOnlyCollection<VideoInfoField> VideoInfo => videoInfo.Values;

    [RelayCommand]
    private async Task OpenModsFileAsync()
    {
        IStorageFile? selectedFile = await SelectModsFile.HandleAsync().ConfigureAwait(false);
        string path = selectedFile?.TryGetLocalPath() ?? string.Empty;
        if (!File.Exists(path)) {
            return;
        }

        decoder = new VideoFrameDecoder(path);

        await Dispatcher.UIThread.InvokeAsync(() => {
            ModsFilePath = path;
            FramesCount = decoder.VideoInfo.FramesCount;
            UpdateVideoInfo();
            CurrentFrame = -1;
            CurrentFrame = 0;
        });
    }

    [RelayCommand(CanExecute = nameof(CanNextFrame))]
    private void NextFrame()
    {
        CurrentFrame++;
    }

    private bool CanNextFrame()
    {
        if (decoder is null) {
            return false;
        }

        return CurrentFrame + 1 < decoder.VideoInfo.FramesCount;
    }

    [RelayCommand(CanExecute = nameof(CanPreviousFrame))]
    private void PreviousFrame()
    {
        CurrentFrame--;
    }

    private bool CanPreviousFrame()
    {
        if (decoder is null) {
            return false;
        }

        return CurrentFrame > 0;
    }

    [RelayCommand(CanExecute = nameof(CanExportFrame))]
    private async Task ExportFrameAsync()
    {
        IStorageFile? outputFile = await AskFrameOutputPath.HandleAsync().ConfigureAwait(false);
        string? outputPath = outputFile?.TryGetLocalPath();
        if (outputPath is null) {
            return;
        }

        decoder!.FrameImage!.Save(outputPath);
    }

    private bool CanExportFrame()
    {
        return decoder?.FrameImage is not null;
    }

    private void UpdateVideoInfo()
    {
        if (decoder is null) {
            return;
        }

        ModsInfo info = decoder.VideoInfo;
        videoInfo["Container"].Value = info.ContainerFormatId;
        videoInfo["MetadataCount"].Value = info.AdditionalParameters.Count.ToString();
        videoInfo["VideoCodec"].Value = info.VideoCodec.ToString();
        videoInfo["Resolution"].Value = $"{info.Width}x{info.Height}";
        videoInfo["Frames"].Value = info.FramesCount.ToString();
        videoInfo["Duration"].Value = info.Duration.ToString("g");
        videoInfo["FPS"].Value = info.FramesPerSecond.ToString("F1");
        videoInfo["KeyFrames"].Value = decoder.KeyFramesCount.ToString();
        videoInfo["AudioCodec"].Value = info.AudioCodec.ToString();
        videoInfo["Channels"].Value = info.AudioChannelsCount.ToString();
        videoInfo["Frequency"].Value = info.AudioFrequency.ToString();
    }

    partial void OnCurrentFrameChanged(int oldValue, int newValue)
    {
        if (oldValue == newValue || decoder is null || CurrentFrame < 0 || CurrentFrame >= FramesCount) {
            return;
        }

        var time = TimeSpan.FromSeconds(CurrentFrame / decoder.VideoInfo.FramesPerSecond);
        CurrentTime = $"{time:g}";
        CurrentFrameImage = null;
        IsFrameLoading = true;

        previousDecodingCancellation?.Cancel();
        previousDecodingCancellation?.Dispose();
        previousDecodingCancellation = new CancellationTokenSource();
        var currentToken = previousDecodingCancellation.Token;

        Task.Run(() => {
            lock (decodingSync) {
                currentToken.ThrowIfCancellationRequested();
                if (newValue == oldValue + 1) {
                    decoder.DecodeNextFrame();
                } else {
                    decoder.DecodeFrame(newValue);
                }

                Dispatcher.UIThread.Post(() => {
                    IsFrameLoading = false;
                    CurrentFrameImage = decoder.FrameImage;
                });
            }
        }, previousDecodingCancellation.Token);
    }
}
