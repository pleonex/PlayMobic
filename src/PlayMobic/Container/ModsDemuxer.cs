namespace PlayMobic.Container;

using System;

public class ModsDemuxer
{
    private readonly ModsVideo container;

    public ModsDemuxer(ModsVideo container)
    {
        this.container = container ?? throw new ArgumentNullException(nameof(container));
    }

    public FramePacketsCollection ReadFrames(int startFrame = 0)
    {
        return new FramePacketsCollection(container, startFrame);
    }
}
