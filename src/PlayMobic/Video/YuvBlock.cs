namespace PlayMobic.Video;

internal readonly struct YuvBlock
{
    public YuvBlock(ComponentBlock luma, ComponentBlock chromaU, ComponentBlock chromaV)
    {
        Luma = luma;
        ChromaU = chromaU;
        ChromaV = chromaV;
    }

    public ComponentBlock Luma { get; }

    public ComponentBlock ChromaU { get; }

    public ComponentBlock ChromaV { get; }
}
