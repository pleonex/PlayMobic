﻿namespace PlayMobic.Video;
using System;
using System.Runtime.CompilerServices;

public static class ColorSpaceConverter
{
    public static void YCoCg2Rgb32(FrameYuv420 source, Span<byte> output)
    {
        if (output.Length != source.Width * source.Height * 4) {
            throw new ArgumentException("Invalid output size");
        }

        for (int y = 0; y < source.Height; y++) {
            for (int x = 0; x < source.Width; x++) {
                // luma is in range 0-255 but chroma is centered at 128, center at 0
                byte luma = source.Luma[x, y];
                int co = source.ChromaU[x / 2, y / 2] - 128;
                int cg = source.ChromaV[x / 2, y / 2] - 128;

                int tmp = luma - cg;
                int g = luma + cg;
                int b = tmp - co;
                int r = tmp + co;

                int index = ((y * source.Width) + x) * 4;
                output[index + 0] = ClampByte(r);
                output[index + 1] = ClampByte(g);
                output[index + 2] = ClampByte(b);
                output[index + 3] = 0; // not used
            }
        }
    }

    public static void YCoCg2Bgr32(FrameYuv420 source, Span<byte> output)
    {
        if (output.Length != source.Width * source.Height * 4) {
            throw new ArgumentException("Invalid output size");
        }

        for (int y = 0; y < source.Height; y++) {
            for (int x = 0; x < source.Width; x++) {
                // luma is in range 0-255 but chroma is centered at 128, center at 0
                byte luma = source.Luma[x, y];
                int co = source.ChromaU[x / 2, y / 2] - 128;
                int cg = source.ChromaV[x / 2, y / 2] - 128;

                int tmp = luma - cg;
                int g = luma + cg;
                int b = tmp - co;
                int r = tmp + co;

                int index = ((y * source.Width) + x) * 4;
                output[index + 0] = ClampByte(b);
                output[index + 1] = ClampByte(g);
                output[index + 2] = ClampByte(r);
                output[index + 3] = 0; // not used
            }
        }
    }

    public static void YCoCg2YCbCr(FrameYuv420 source, FrameYuv420 output)
    {
        if (source.Width != output.Width || source.Height != output.Height) {
            throw new ArgumentException("Invalid output size");
        }

        output.ColorSpace = YuvColorSpace.YCbCr;

        ComponentBlock dstLuma = output.Luma;
        ComponentBlock dstChromaU = output.ChromaU;
        ComponentBlock dstChromaV = output.ChromaV;

        // From Mobius:
        // https://github.com/AdibSurani/Mobius/blob/f71ddc69f374a8ff6d899efd4dcaf3858af63bf7/Mobius/MobiDecoder.cs#L132
        for (int y = 0; y < source.Height; y++) {
            for (int x = 0; x < source.Width; x++) {
                dstLuma[x, y] = ClampByte(source.Luma[x, y] * 0.859);

                if ((x % 2) == 0 && (y % 2) == 0) {
                    byte chromaU = source.ChromaU[x / 2, y / 2];
                    byte chromaV = source.ChromaV[x / 2, y / 2];
                    dstChromaU[x / 2, y / 2] = ClampByte((-0.587 * chromaU) - (0.582 * chromaV) + 277.691);
                    dstChromaV[x / 2, y / 2] = ClampByte((0.511 * chromaU) - (0.736 * chromaV) + 156.776);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ClampByte(double value) =>
            (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);
}
