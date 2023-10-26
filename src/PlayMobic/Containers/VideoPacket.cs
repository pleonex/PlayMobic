namespace PlayMobic.Containers;

public record VideoPacket(int StreamIndex, Stream Data, bool IsKeyFrame)
    : MediaPacket(StreamIndex, Data, IsKeyFrame);
