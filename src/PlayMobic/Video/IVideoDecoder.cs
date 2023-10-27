namespace PlayMobic.Video;

/// <summary>
/// Interface for video decoders.
/// </summary>
public interface IVideoDecoder
{
    /// <summary>
    /// Decode the next frame of the video into a YUV 4:2:0 format.
    /// </summary>
    /// <param name="data">Video compressed data.</param>
    /// <returns>Next decompressed frame in YUV 4:2:0 binary format.</returns>
    FrameYuv420 DecodeFrame(Stream data);
}
