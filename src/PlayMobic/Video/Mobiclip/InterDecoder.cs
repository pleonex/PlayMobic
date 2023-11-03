namespace PlayMobic.Video.Mobiclip;

using System.Runtime.CompilerServices;
using PlayMobic.IO;

internal class InterDecoder
{
    private static readonly byte[] CodedBlockPatterns4x4 = {
        0, 4, 1, 8, 2, 12, 3, 5, 10, 15, 7, 13, 14, 11, 9, 6,
    };

    private static readonly byte[] CodedBlockPatterns8x8 = {
        0x00, 0x0F, 0x04, 0x01, 0x08, 0x02, 0x0C, 0x03, 0x05, 0x0A, 0x0D, 0x07, 0x0E, 0x0B, 0x1F, 0x09,
        0x06, 0x10, 0x3F, 0x1E, 0x17, 0x1D, 0x1B, 0x1C, 0x13, 0x18, 0x1A, 0x12, 0x11, 0x14, 0x15, 0x20,
        0x2F, 0x16, 0x19, 0x37, 0x3D, 0x3E, 0x3B, 0x3C, 0x33, 0x35, 0x21, 0x24, 0x22, 0x28, 0x23, 0x2C,
        0x30, 0x27, 0x2D, 0x25, 0x3A, 0x2B, 0x2E, 0x2A, 0x31, 0x34, 0x38, 0x32, 0x29, 0x26, 0x39, 0x36,
    };

    private readonly BitReader reader;
    private readonly IntraDecoder intraDecoder;
    private readonly DiscreteCosineTransformer dct;
    private readonly Quantization quantization;
    private readonly EntropyVlcEncoding entropyVlc;

    public InterDecoder(BitReader reader, int quantizerIndex)
    {
        this.reader = reader;

        intraDecoder = new IntraDecoder(reader, 0, quantizerIndex);
        dct = new DiscreteCosineTransformer();
        quantization = new Quantization(quantizerIndex);
        entropyVlc = new EntropyVlcEncoding(0);
    }

    public void DecodeMacroBlock(MacroBlock macroBlock)
    {
        // TODO: vector buffer
        // TODO: get mode from huffman
        int mode = -1;
        if (mode == 6) {
            intraDecoder.DecodeMacroBlock(macroBlock, false);
        } else if (mode == 7) {
            intraDecoder.DecodeMacroBlock(macroBlock, true);
        } else {
            DecodeMotionCompensation(macroBlock, mode);
        }

        DecodeMacroBlockResidual(macroBlock);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TestBit(byte flags, int idx) => ((flags >> idx) & 1) == 1;

    private void DecodeMotionCompensation(MacroBlock block, int mode)
    {
        // mode 0: motion compensation from current frame
        // mode 1-5: motion compensation with delta from past frame
        // mode 6-7: forbidden at this stage
        // mode 8: partition by height and decode each block
        // mode 9: partition by width and decode each block
        throw new NotImplementedException();
    }

    private void DecodeMacroBlockResidual(MacroBlock macroBlock)
    {
        // Add residual to each block similar to intra
        int residualIdx = reader.ReadExpGolomb();
        byte residualFlags = CodedBlockPatterns8x8[residualIdx];

        PixelBlock[] lumaBlocks = macroBlock.Luma.Partition(8, 8);
        for (int i = 0; i < lumaBlocks.Length; i++) {
            if (TestBit(residualFlags, i)) {
                DecodePartitionBlockResidual(lumaBlocks[i]);
            }
        }

        if (TestBit(residualFlags, 4)) {
            DecodePartitionBlockResidual(macroBlock.ChromaU);
        }

        if (TestBit(residualFlags, 5)) {
            DecodePartitionBlockResidual(macroBlock.ChromaV);
        }
    }

    private void DecodePartitionBlockResidual(PixelBlock block)
    {
        int partitionFlag = reader.ReadExpGolomb();
        if (partitionFlag == 0) {
            // Block 8x8 with residual
            ApplyResidual(block);
            return;
        }

        // Split in blocks 4x4 with or without residual for each of them.
        // Note: difference to Intra is that we don't substract 1 to index
        int residualTableIdx = partitionFlag;
        byte hasResidualFlags = CodedBlockPatterns4x4[residualTableIdx];

        PixelBlock[] blocks4x4 = block.Partition(4, 4);
        for (int i = 0; i < blocks4x4.Length; i++) {
            if (TestBit(hasResidualFlags, i)) {
                ApplyResidual(blocks4x4[i]);
            }
        }
    }

    private void ApplyResidual(PixelBlock block)
    {
        // 1. VLC to get residual DC coefficients matrix
        int[] coefficients = entropyVlc.DecodeResidual(reader, block.Width * block.Height);

        // 2. Dequantize to re-store scale
        quantization.Dequantize(coefficients);

        // 3. Apply inverse DCT to decode
        int[] residual = dct.InverseTransformation(coefficients, block.Width);

        // 4. Sum the residual to the predicted colors in the block.
        for (int y = 0; y < block.Height; y++) {
            for (int x = 0; x < block.Width; x++) {
                int idx = x + (y * block.Width);
                block[x, y] = (byte)Math.Clamp(block[x, y] + residual[idx], byte.MinValue, byte.MaxValue);
            }
        }
    }
}
