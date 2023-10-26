namespace PlayMobic.Containers;

using Yarhl.IO;

/// <summary>
/// It contains compressed audio, video or other media data inside a video format.
/// This is the output from a demuxer passed into decoders, or from an encoder passed into a muxer.
/// </summary>
/// <param name="StreamIndex">The index of the stream in the container belonging the data.</param>
/// <param name="Data">Compressed data.</param>
/// <param name="IsKeyFrame">Value indicating whether the data belongs to a key frame and it's self-contained.</param>
public record MediaPacket(int StreamIndex, DataStream Data, bool IsKeyFrame)
    : IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Data?.Dispose();
    }
}
