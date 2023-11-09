namespace PlayMobic.UI.ViewModels;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using PlayMobic.UI.Models;

public class AnalyzeVideoViewModel : ObservableObject
{
    public AnalyzeVideoViewModel()
    {
        VideoInfo = new List<VideoInfoField> {
            new VideoInfoField("Format", "Container") { Value = "N3" },
            new VideoInfoField("Codec", "Video") { Value = "MobiclipV1" },
            new VideoInfoField("Resolution", "Video") { Value = "256x192" },
            new VideoInfoField("Frames", "Video") { Value = "1371" },
            new VideoInfoField("Duration", "Video") { Value = "00:00:57.125" },
            new VideoInfoField("Frames per second", "Video") { Value = "24" },
            new VideoInfoField("Key frames", "Video") { Value = "70" },
            new VideoInfoField("Codec", "Audio") { Value = "ImaAdPcm" },
            new VideoInfoField("Channels", "Audio") { Value = "2" },
            new VideoInfoField("Frequency", "Audio") { Value = "48000 Hz" },
            new VideoInfoField("Count", "Metadata") { Value = "0" },
        };
    }

    public IEnumerable<VideoInfoField> VideoInfo { get; }
}
