namespace PlayMobic.Containers.Mods;

using PlayMobic;

public class ModsInfo
{
    public static uint FramesPerSecondBase => 0x01000000;

    public string ContainerFormatId { get; set; } = string.Empty;

    public VideoCodecKind VideoCodec { get; set; }

    public int FramesCount { get; set; }

    public TimeSpan Duration => TimeSpan.FromSeconds(FramesCount / FramesPerSecond);

    public int Width { get; set; }

    public int Height { get; set; }

    public double FramesPerSecond { get; set; }

    public AudioCodecKind AudioCodec { get; set; }

    public int AudioChannelsCount { get; set; }

    public int AudioFrequency { get; set; }

    public IReadOnlyList<VideoParameter> AdditionalParameters { get; set; } = Array.Empty<VideoParameter>();
}
