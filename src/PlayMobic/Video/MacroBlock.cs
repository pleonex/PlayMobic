namespace PlayMobic.Video;

internal readonly struct MacroBlock
{
    public MacroBlock(PixelBlock luma, PixelBlock chromaU, PixelBlock chromaV)
    {
        Luma = luma;
        ChromaU = chromaU;
        ChromaV = chromaV;
    }

    public PixelBlock Luma { get; }

    public PixelBlock ChromaU { get; }

    public PixelBlock ChromaV { get; }
}
