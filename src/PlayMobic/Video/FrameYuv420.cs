namespace PlayMobic.Video;

using System;

public class FrameYuv420
{
    private const int BitsPerPixel = 8;

    private readonly byte[] data;
    private readonly PixelBlock componentLuma;
    private readonly PixelBlock componentU;
    private readonly PixelBlock componentV;

    public FrameYuv420(int width, int height)
    {
        Width = width;
        Height = height;

        // IMC4 (yuv420p): 8 bpp YUV 4:2:0, stride = width (for now)
        int bytesPerPixelChannel = Width * Height * (BitsPerPixel / 8);
        int lumaLength = bytesPerPixelChannel;
        int uvLength = bytesPerPixelChannel / 2;

        data = new byte[lumaLength + (2 * uvLength)];
        Memory<byte> lumaData = data.AsMemory(0, lumaLength);
        Memory<byte> uData = data.AsMemory(lumaLength, uvLength);
        Memory<byte> vData = data.AsMemory(lumaLength + uvLength, uvLength);

        componentLuma = new PixelBlock(lumaData, width, new(0, 0, width, height), 0);
        componentU = new PixelBlock(uData, width / 2, new(0, 0, width / 2, height / 2), 0);
        componentV = new PixelBlock(vData, width / 2, new(0, 0, width / 2, height / 2), 0);
    }

    public int Width { get; init; }

    public int Height { get; init; }

    public ReadOnlySpan<byte> PackedData => data;

    internal PixelBlock Luma => componentLuma;

    internal PixelBlock U => componentU;

    internal PixelBlock V => componentV;

    internal void CleanData()
    {
        Array.Fill<byte>(data, 0);
    }
}
