namespace PlayMobic.Containers;
public record AudioPacket(int StreamIndex, int TrackIndex, Stream Data, bool IsKeyFrame, int FrameCount)
    : MediaPacket(StreamIndex, Data, IsKeyFrame, FrameCount);
