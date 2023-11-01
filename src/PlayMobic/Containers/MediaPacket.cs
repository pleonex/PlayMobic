namespace PlayMobic.Containers;

/// <summary>
/// It contains compressed audio, video or other media data inside a video format.
/// This is the output from a demuxer passed into decoders, or from an encoder passed into a muxer.
/// </summary>
/// <param name="StreamIndex">The index of the stream in the container belonging the data.</param>
/// <param name="Data">Compressed data.</param>
/// <param name="IsKeyFrame">Value indicating whether the data belongs to a key frame and it's self-contained.</param>
public abstract record MediaPacket(int StreamIndex, Stream Data, bool IsKeyFrame, int FrameCount)
    : IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing) {
            Data?.Dispose();
        }
    }
}
