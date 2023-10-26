namespace PlayMobic.Container;
using System;
using System.Collections;
using System.Collections.Generic;

public class FramePacketsCollection : IEnumerable<MediaPacket>
{
    private readonly ModsVideo video;
    private readonly int startFrame;

    public FramePacketsCollection(ModsVideo video, int startFrame)
    {
        this.video = video ?? throw new ArgumentNullException(nameof(video));
        this.startFrame = startFrame;
    }

    public IEnumerator<MediaPacket> GetEnumerator()
    {
        return new ModsPacketReader(video, startFrame);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
