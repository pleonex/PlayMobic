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

    public readonly YuvBlock[] PartitionBy(int widthDiv, int heightDiv)
    {
        ComponentBlock[] lumas = Luma.Partition(Luma.Width / widthDiv, Luma.Height / heightDiv);
        ComponentBlock[] chromaUs = ChromaU.Partition(ChromaU.Width / widthDiv, ChromaU.Height / heightDiv);
        ComponentBlock[] chromaVs = ChromaU.Partition(ChromaV.Width / widthDiv, ChromaV.Height / heightDiv);

        var partitions = new YuvBlock[lumas.Length];
        for (int i = 0; i < lumas.Length; i++) {
            partitions[i] = new YuvBlock(lumas[i], chromaUs[i], chromaVs[i]);
        }

        return partitions;
    }
}
