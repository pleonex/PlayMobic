namespace PlayMobic.Container;

public class ModsInfo
{
    public string VideoCodecId { get; set; } = string.Empty;

    public int Unknown06 { get; set; }

    public int FramesCount { get; set; }

    public TimeSpan Duration => TimeSpan.FromSeconds(FramesCount / (double)FramesPerSecond);

    public int Width { get; set; }

    public int Height { get; set; }

    public int FramesPerSecond { get; set; }

    public AudioCodecKind AudioCodec { get; set; }

    public int AudioChannelsCount { get; set; }

    public int AudioFrequency { get; set; }
}
