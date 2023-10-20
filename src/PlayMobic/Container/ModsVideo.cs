namespace PlayMobic.Container;

using Yarhl.FileFormat;

public class ModsVideo : IFormat
{
    public ModsVideo()
    {
        Info = new ModsInfo();
    }

    public ModsInfo Info { get; init; }
}
