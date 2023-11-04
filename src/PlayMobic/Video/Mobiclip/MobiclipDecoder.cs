namespace PlayMobic.Video.Mobiclip;

using System;
using System.IO;
using PlayMobic.IO;
using PlayMobic.Video;
using Yarhl.IO;

/// <summary>
/// Video decoder for the Mobiclip format.
/// </summary>
public class MobiclipDecoder : IVideoDecoder
{
    private const int FrameBufferLength = 6;

    private readonly FramesBuffer<FrameYuv420> frames;
    private readonly bool isVideoStereo;

    // I-frame data to use in following P-frames
    private YuvColorSpace colorSpace;
    private int quantizationIdx;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobiclipDecoder"/> class.
    /// </summary>
    /// <param name="width">Video width resolution.</param>
    /// <param name="height">Video height resolution.</param>
    public MobiclipDecoder(int width, int height)
        : this(width, height, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MobiclipDecoder"/> class.
    /// </summary>
    /// <param name="width">Video width resolution.</param>
    /// <param name="height">Video height resolution.</param>
    /// <param name="isStereo">Indicates if the video is stereo (3D).</param>
    public MobiclipDecoder(int width, int height, bool isStereo)
    {
        isVideoStereo = isStereo;
        frames = new FramesBuffer<FrameYuv420>(
            FrameBufferLength,
            () => new FrameYuv420(width, height));
    }

    /// <inheritdoc />
    public FrameYuv420 DecodeFrame(Stream data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var reader = new BitReader(data, EndiannessMode.LittleEndian, 16);

        // There is a buffer of 6 frames (0 is the current to decode),
        // so P-prediction decode using previous 5 frames.
        // At the beginning we rotate the buffer so the last decoded frame
        // becomes the first on the buffer.
        frames.Rotate();
        frames.Current.CleanData();

        int frameKind = reader.Read(1);
        if (frameKind == 1) {
            DecodeIFrame(reader);
        } else {
            DecodePFrame(reader);
        }

        frames.Current.ColorSpace = colorSpace;
        return frames.Current;
    }

    private void DecodeIFrame(BitReader reader)
    {
        // Color space for I (and following P) frames.
        int colorSpaceKind = reader.Read(1);
        colorSpace = (colorSpaceKind == 0) ? YuvColorSpace.YCoCg : YuvColorSpace.YCbCr;

        int vlcTableIndex = reader.Read(1);
        quantizationIdx = reader.Read(6);

        var intraDecoder = new IntraDecoder(reader, vlcTableIndex, quantizationIdx);

        // Create the macroblocks: luma 16x16, chroma 8x8 and decode each of them.
        YuvBlock[] macroBlocks = frames.Current.GetMacroBlocks();
        foreach (YuvBlock macroBlock in macroBlocks) {
            bool modePerBlock = reader.ReadBoolean();
            intraDecoder.DecodeMacroBlock(macroBlock, modePerBlock);
        }
    }

    private void DecodePFrame(BitReader reader)
    {
        int quantizationDeltaIdx = reader.ReadExpGolombSigned();
        int pQuantIndex = quantizationIdx + quantizationDeltaIdx;

        var interDecoder = new InterDecoder(reader, frames, pQuantIndex, isVideoStereo);

        YuvBlock[] macroBlocks = frames.Current.GetMacroBlocks();
        foreach (YuvBlock macroBlock in macroBlocks) {
            interDecoder.DecodeMacroBlock(macroBlock);
        }
    }
}
