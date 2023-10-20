namespace PlayMobic.Container;

using System.Collections.ObjectModel;
using Yarhl.FileFormat;

public class ModsVideo : IFormat
{
    public ModsVideo(Stream data)
    {
        ArgumentNullException.ThrowIfNull(data);

        Info = new ModsInfo();
        KeyFramesInfo = new();
        Data = data;
    }

    public ModsInfo Info { get; init; }

    public Collection<KeyFrameInfo> KeyFramesInfo { get; init; }

    public Stream Data { get; init; }
}
