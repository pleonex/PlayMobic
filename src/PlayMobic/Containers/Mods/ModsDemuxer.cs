namespace PlayMobic.Containers.Mods;

using System;

public class ModsDemuxer : IDemuxer<MediaPacket>
{
    private readonly ModsVideo container;

    public ModsDemuxer(ModsVideo container)
    {
        this.container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public MediaPacketCollection<MediaPacket> ReadFrames()
    {
        return ReadFrames(0);
    }

    public MediaPacketCollection<MediaPacket> ReadFrames(int startFrame)
    {
        return new MediaPacketCollection<MediaPacket>(() => new ModsPacketReader(container, startFrame));
    }
}
