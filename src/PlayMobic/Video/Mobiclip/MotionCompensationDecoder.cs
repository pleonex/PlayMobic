namespace PlayMobic.Video.Mobiclip;

using System;
using PlayMobic.IO;

internal class MotionCompensationDecoder
{
    private readonly BitReader reader;
    private readonly Dictionary<(int Width, int Height), Huffman> huffmanTables;

    public MotionCompensationDecoder(BitReader reader, Dictionary<(int Width, int Height), Huffman> huffmanTables)
    {
        this.reader = reader;
        this.huffmanTables = huffmanTables;
    }

    public void DecodeMacroBlock(YuvBlock block, int mode)
    {
        // TODO: vector buffer

        // mode 0: motion compensation from current frame (P_Skip?)
        // mode 1-5: motion compensation with delta from past frame
        // mode 6-7: intra - forbidden at this stage
        // mode 8: partition by height and decode each block
        // mode 9: partition by width and decode each block
        throw new NotImplementedException();
    }
}
