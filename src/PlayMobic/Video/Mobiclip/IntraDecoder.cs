namespace PlayMobic.Video.Mobiclip;

using System.Runtime.CompilerServices;
using PlayMobic.IO;

/// <summary>
/// Intra-frame prediction decoder for Mobiclip video format.
/// This class runs CBP (partitioning) for a macroblock with Luma and chroma
/// components and calls <see cref="IntraDecoderBlockPrediction"/> for decoding.
/// </summary>
/// <remarks>
/// The prediction runs on blocks of 16x16 (only delta-plane), 8x8 and 4x4.
/// The prediction mode may be available for all the block or per-sublock.
/// The prediction mode may be also predicted as it's correlated with its neighbors.
/// The residual data is just after each block prediction data.
/// Pseudo-code:
/// - Residual block 8x8 table indicator index
/// - IF no predicted mode:
///   - mode at 16x16 level applying all sub-blocks
///   - (if mode is DeltaPlane) plane delta value for 16x16
/// - FOR EACH 8x8:
///   - IF not residual for this 8x8:
///     - (if predicted mode) mode prediction info
///     - (if mode is DeltaPlane AND was predicted) plane delta for 8x8
///   - ELSE:
///     - partition mode | resdiaul for block 4x4 table indicator index
///     - IF 8x8:
///       - (if predicted mode) mode prediction info
///       - (if mode is DeltaPlane AND was predicted) plane delta for 8x8
///       - RESIDUAL for 8x8
///     - ELSE FOREACH 4x4:
///       - (if predicted mode) mode prediction info
///       - (if 4x4 block has residual) RESIDUAL.
/// </remarks>
internal class IntraDecoder
{
    // Each bit indicates whether the block ith has residual or not.
    private static readonly byte[] CodedBlockPatterns4x4 = {
        15, 0, 2, 1, 4, 8, 12, 3, 11, 13, 14, 7, 10, 5, 9, 6,
    };

    // Bits 0-3 indicates whether the luma 8x8 block has residual or not.
    // Bit 4 is for chroma U, bit 5 for chroma V.
    private static readonly byte[] CodedBlockPatterns8x8 = {
        0x00, 0x1F, 0x3F, 0x0F, 0x08, 0x04, 0x02, 0x01, 0x0B, 0x0E, 0x1B, 0x0D, 0x03, 0x07, 0x0C, 0x17,
        0x1D, 0x0A, 0x1E, 0x05, 0x10, 0x2F, 0x37, 0x3B, 0x13, 0x3D, 0x3E, 0x09, 0x1C, 0x06, 0x15, 0x1A,
        0x33, 0x11, 0x12, 0x14, 0x18, 0x20, 0x3C, 0x35, 0x19, 0x16, 0x3A, 0x30, 0x31, 0x32, 0x27, 0x34,
        0x2B, 0x2D, 0x39, 0x38, 0x23, 0x36, 0x2E, 0x21, 0x25, 0x22, 0x24, 0x2C, 0x2A, 0x28, 0x29, 0x26,
    };

    private readonly BitReader reader;
    private readonly IIntraDecoderBlockPrediction blockPrediction;
    private readonly DiscreteCosineTransformer dct;
    private readonly Quantization quantization;
    private readonly EntropyVlcEncoding entropyVlc;

    public IntraDecoder(BitReader reader, int vlcTableIndex, int quantizerIndex)
    : this(reader, vlcTableIndex, quantizerIndex, new IntraDecoderBlockPrediction(reader))
    {
    }

    internal IntraDecoder(
        BitReader reader,
        int vlcTableIndex,
        int quantizerIndex,
        IIntraDecoderBlockPrediction blockPrediction)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        this.blockPrediction = blockPrediction ?? throw new ArgumentNullException(nameof(blockPrediction));

        dct = new DiscreteCosineTransformer();
        quantization = new Quantization(quantizerIndex);
        entropyVlc = new EntropyVlcEncoding(vlcTableIndex);
    }

    public void DecodeMacroBlock(MacroBlock macroBlock, bool lumaHasModePerSubBlocks)
    {
        // Data encoded:
        // - block size for prediction
        // - prediction mode for macroblock or per mode
        // - CBP: coded block pattern for each block
        // - block size for residual (different if prediction on 16x16)
        int residualIdx = reader.ReadExpGolomb();
        byte residualFlags = CodedBlockPatterns8x8[residualIdx];

        // First we process the luma macroblock (16x16)
        IntraPredictionBlockMode blockMode = IntraPredictionBlockMode.Predicted;
        if (!lumaHasModePerSubBlocks) {
            // then we use the same mode for all the blocks in the macroblock.
            blockMode = (IntraPredictionBlockMode)reader.Read(3);

            // mode DeltaPlane is the only one that runs at the level of 16x16 block
            // let's do it before we split it in 8x8 blocks.
            // Residual will happen anyways at 8x8 (or 4x4) blocks.
            if (blockMode == IntraPredictionBlockMode.DeltaPlane) {
                blockPrediction.PerformBlockPrediction(macroBlock.Luma, blockMode);
                blockMode = IntraPredictionBlockMode.Nothing; // only do residual later
            }
        }

        // Split the luma component into 8x8 and process each of them
        PixelBlock[] lumaBlocks = macroBlock.Luma.Partition(8, 8);
        for (int i = 0; i < lumaBlocks.Length; i++) {
            bool hasResidual = TestBit(residualFlags, i);
            DecodeBlock(lumaBlocks[i], hasResidual, blockMode);
        }

        // Time for chroma, it's already 8x8 so let's run it.
        // There isn't mode per block option for them, same mode for both blocks.
        var chromaMode = (IntraPredictionBlockMode)reader.Read(3);
        if (chromaMode == IntraPredictionBlockMode.DeltaPlane) {
            // Just like luma, mode 2 happens at the macroblock level before residual decoding.
            blockPrediction.PerformBlockPrediction(macroBlock.ChromaU, chromaMode);
            blockPrediction.PerformBlockPrediction(macroBlock.ChromaV, chromaMode);
            chromaMode = IntraPredictionBlockMode.Nothing; // only do residual later
        }

        bool hasUResidual = TestBit(residualFlags, 4);
        DecodeBlock(macroBlock.ChromaU, hasUResidual, chromaMode);

        bool hasVResidual = TestBit(residualFlags, 5);
        DecodeBlock(macroBlock.ChromaV, hasVResidual, chromaMode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TestBit(byte flags, int idx) => ((flags >> idx) & 1) == 1;

    private void DecodeBlock(PixelBlock block, bool hasResidual, IntraPredictionBlockMode mode)
    {
        // If it doesn't have residual, then just run prediction on the 8x8 block
        if (!hasResidual) {
            blockPrediction.PerformBlockPrediction(block, mode);
            return;
        }

        // Otherwise, check if residual happens at 8x8 level
        int partitionFlag = reader.ReadExpGolomb();
        if (partitionFlag == 0) {
            // Block 8x8 with residual
            blockPrediction.PerformBlockPrediction(block, mode);
            ApplyResidual(block);
            return;
        }

        // Split in blocks 4x4 with or without residual for each of them.
        int residualTableIdx = partitionFlag - 1;
        byte hasResidualFlags = CodedBlockPatterns4x4[residualTableIdx];

        PixelBlock[] blocks4x4 = block.Partition(4, 4);
        for (int i = 0; i < blocks4x4.Length; i++) {
            blockPrediction.PerformBlockPrediction(blocks4x4[i], mode);

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
