namespace PlayMobic.Containers;

public record VideoPacket(int StreamIndex, Stream Data, bool IsKeyFrame, int FrameCount)
    : MediaPacket(StreamIndex, Data, IsKeyFrame, FrameCount);
