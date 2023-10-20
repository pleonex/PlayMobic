namespace PlayMobic.Container;

using System.Collections.ObjectModel;

public record FramePacket(ReadOnlyCollection<StreamPacket> StreamPackets, bool IsKeyFrame)
    : IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var packet in StreamPackets) {
            packet?.Data?.Dispose();
        }
    }
}
