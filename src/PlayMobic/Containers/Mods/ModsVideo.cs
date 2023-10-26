namespace PlayMobic.Containers.Mods;

using System.Collections.ObjectModel;
using Yarhl.FileFormat;

public sealed class ModsVideo : IFormat, IDisposable
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

    public void Dispose()
    {
        Data?.Dispose();
        GC.SuppressFinalize(this);
    }
}
