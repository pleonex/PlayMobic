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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ClampByte(double value) =>
            (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);
}
