namespace PlayMobic.Video;

using System;
using System.Diagnostics;

public class FrameYuv420
{
    private const int BitsPerPixel = 8;

    private readonly byte[] data;

    public FrameYuv420(int width, int height)
    {
        Width = width;
        Height = height;

        // IMC4 (yuv420p): 8 bpp YUV 4:2:0, stride = width (for now)
        int bytesPerPixelChannel = Width * Height * (BitsPerPixel / 8);
        int lumaLength = bytesPerPixelChannel;
        int uvLength = bytesPerPixelChannel / 4;

        data = new byte[lumaLength + (2 * uvLength)];
        Memory<byte> lumaData = data.AsMemory(0, lumaLength);
        Memory<byte> uData = data.AsMemory(lumaLength, uvLength);
        Memory<byte> vData = data.AsMemory(lumaLength + uvLength, uvLength);

        Luma = new PixelBlock(lumaData, width, new(0, 0, width, height), 0);
        ChromaU = new PixelBlock(uData, width / 2, new(0, 0, width / 2, height / 2), 0);
        ChromaV = new PixelBlock(vData, width / 2, new(0, 0, width / 2, height / 2), 0);
    }

    public int Width { get; init; }

    public int Height { get; init; }

    public ReadOnlySpan<byte> PackedData => data;

    internal PixelBlock Luma { get; }

    internal PixelBlock ChromaU { get; }

    internal PixelBlock ChromaV { get; }

    internal MacroBlock[] GetMacroBlocks()
    {
        PixelBlock[] lumaBlocks = Luma.Partition(16, 16);
        PixelBlock[] chromaUBlocks = ChromaU.Partition(8, 8);
        PixelBlock[] chromaVBlocks = ChromaV.Partition(8, 8);

        Debug.Assert(lumaBlocks.Length == chromaUBlocks.Length, "Mismatch block num");
        Debug.Assert(lumaBlocks.Length == chromaVBlocks.Length, "Mismatch block num");

        var macroblocks = new MacroBlock[lumaBlocks.Length];
        for (int i = 0; i < lumaBlocks.Length; i++) {
            macroblocks[i] = new(lumaBlocks[i], chromaUBlocks[i], chromaVBlocks[i]);
        }

        return macroblocks;
    }

    internal void CleanData()
    {
        Array.Fill<byte>(data, 0);
    }
}
