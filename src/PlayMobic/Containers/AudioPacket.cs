namespace PlayMobic.Containers;
public record AudioPacket(int StreamIndex, int TrackIndex, Stream Data, bool IsKeyFrame)
    : MediaPacket(StreamIndex, Data, IsKeyFrame);
