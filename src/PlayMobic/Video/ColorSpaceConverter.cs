namespace PlayMobic.Video;
using System;
using System.Runtime.CompilerServices;

public static class ColorSpaceConverter
{
    public static byte[] YCoCg2Rgb32(FrameYuv420 source)
    {
        byte[] rgb = new byte[source.Width * source.Height * 4];

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
                rgb[index + 0] = ClampByte(r);
                rgb[index + 1] = ClampByte(g);
                rgb[index + 2] = ClampByte(b);
            }
        }

        return rgb;
    }

    public static FrameYuv420 YCoCg2YCbCr(FrameYuv420 source)
    {
        var ycbcr = new FrameYuv420(source.Width, source.Height);
        ycbcr.ColorSpace = YuvColorSpace.YCbCr;

        ComponentBlock dstLuma = ycbcr.Luma;
        ComponentBlock dstChromaU = ycbcr.ChromaU;
        ComponentBlock dstChromaV = ycbcr.ChromaV;

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

        return ycbcr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ClampByte(double value) =>
            (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);
}
