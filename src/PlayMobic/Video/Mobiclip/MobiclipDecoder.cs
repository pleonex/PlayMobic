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
    private YuvColorSpace colorSpace;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobiclipDecoder"/> class.
    /// </summary>
    /// <param name="width">Video width resolution.</param>
    /// <param name="height">Video height resolution.</param>
    public MobiclipDecoder(int width, int height)
    {
        frames = new FramesBuffer<FrameYuv420>(
            FrameBufferLength,
            () => new FrameYuv420(width, height));
    }

    /// <inheritdoc />
    public FrameYuv420 DecodeFrame(Stream data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var reader = new BitReader(data, EndiannessMode.LittleEndian);

        // Rotate to put last frame the first in the buffer.
        frames.Rotate();
        frames.Current.CleanData();

        int frameKind = reader.Read(1);
        if (frameKind == 0) {
            DecodeIFrame(reader);
        } else {
            DecodePFrame(reader);
        }

        if (colorSpace is YuvColorSpace.YCoCg) {
            // TODO: transform to standard YCbCr colorspace
        }

        return frames.Current;
    }

    private void DecodeIFrame(BitReader reader)
    {
        int colorSpaceKind = reader.Read(1);
        colorSpace = (colorSpaceKind == 0) ? YuvColorSpace.YCoCg : YuvColorSpace.YCbCr;

        int dctTableIndex = reader.Read(1);
        int quantizerIndex = reader.Read(6);

        // Create the macroblocks: luma 16x16, chroma 8x8 and decode each of them.
        MacroBlock[] macroBlocks = frames.Current.GetMacroBlocks();

        var intraDecoder = new IntraDecoder(reader, dctTableIndex, quantizerIndex);
        foreach (MacroBlock macroBlock in macroBlocks) {
            bool modePerBlock = reader.ReadBoolean();
            intraDecoder.DecodeMacroBlock(macroBlock, modePerBlock);
        }
    }

    private void DecodePFrame(BitReader reader)
    {
        throw new NotImplementedException();
    }
}
