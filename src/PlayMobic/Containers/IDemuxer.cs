namespace PlayMobic.Containers;

public interface IDemuxer<T>
{
    MediaPacketCollection<T> ReadFrames();

    MediaPacketCollection<T> ReadFrames(int startFrame);
}
