namespace PlayMobic.Video.Mobiclip;

using PlayMobic.IO;

/// <summary>
/// Variable length coding based on JPEG that encodes residual sample matrix
/// with RLE and Huffman with zig-zag ordering.
/// </summary>
/// <remarks>
/// The residual DC coefficients are encoded in zig-zag order using RLE.
/// The information of RLE assumes the array is already initialized to 0, then
/// it provides number of consecutive zeroes (positions to skip) and the next
/// non-zero amplitude / value. This RLE information is furthermore encoded
/// with HUFFMAN in pre-known tables.
/// The difference with actual JPEG VLC is that the category is encoded for the
/// full frame (tableIdx), giving more bits for zero-run and magnitude.
/// </remarks>
internal class EntropyVlcEncoding
{
#pragma warning disable SA1137 // Elements should have the same indentation
    private static readonly int[][] ResidueTables = {
        new[] {
            12,  6,  4,  3,  3,  3,  3,  2,  2,  2,  2,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  0,  0,  0,  0,  0,
             0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
             3,  2,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,
             1,  1,  1,  1,  1,  1,  1,  1,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
             1, 27, 11,  7,  3,  2,  2,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,
             1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,
             1, 41,  2,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,
             1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,
        },
        new[] {
            27, 10,  5,  4,  3,  3,  3,  3,  2,  2,  1,  1,  1,  1,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
             0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
             8,  3,  2,  2,  2,  2,  2,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
             0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
             1, 15, 10,  8,  4,  3,  2,  2,  2,  2,  2,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,
             1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,
             1, 21,  7,  2,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,
             1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,
        },
    };

    private static readonly int[] ZigZag4x4BlockScan = new[] {
        0,  1,  5,  6,
        2,  4,  7, 12,
        3,  8, 11, 13,
        9, 10, 14, 15,
    };
    private static readonly int[] DeZigZag4x4BlockScan = Enumerable.Range(0, 4 * 4)
        .Select(i => Array.IndexOf(ZigZag4x4BlockScan, i))
        .ToArray();

    private static readonly int[] ZigZag8x8BlockScan = new[] {
         0,  1,  5,  6, 14, 15, 27, 28,
         2,  4,  7, 13, 16, 26, 29, 42,
         3,  8, 12, 17, 25, 30, 41, 43,
         9, 11, 18, 24, 31, 40, 44, 53,
        10, 19, 23, 32, 39, 45, 52, 54,
        20, 22, 33, 38, 46, 51, 55, 60,
        21, 34, 37, 47, 50, 56, 59, 61,
        35, 36, 48, 49, 57, 58, 62, 63,
    };
    private static readonly int[] DeZigZag8x8BlockScan = Enumerable.Range(0, 8 * 8)
        .Select(i => Array.IndexOf(ZigZag8x8BlockScan, i))
        .ToArray();
#pragma warning restore SA1137

    private static readonly Huffman[] HuffmanTables = new[] {
        Huffman.LoadFromFullIndexTable(0),
        Huffman.LoadFromFullIndexTable(1),
    };

    private readonly Huffman huffman;
    private readonly int[] residueTable;

    public EntropyVlcEncoding(int tableIdx)
    {
        huffman = HuffmanTables[tableIdx];
        residueTable = ResidueTables[tableIdx];
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "",
        "S1862:Related \"if/else if\" statements should not have the same condition",
        Justification = "False positive, it reads another bit")]
    public int[] DecodeResidual(BitReader reader, int blockSize)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (blockSize is not(16 or 64)) {
            throw new ArgumentException("Unsupported block size");
        }

        int[] zizagTable = (blockSize == 16) ? DeZigZag4x4BlockScan : DeZigZag8x8BlockScan;

        // C# initializes with 0
        int[] residual = new int[blockSize];

        int matrixPos = 0;
        bool isBlockEnd;
        do {
            (isBlockEnd, int run, int amplitude) = ReadRleInfo(reader);

            // Format:
            // if amplitude is provided in first block, then use it that run and amplitude
            // otherwise, if bit is 1, then we increment amplitude from table
            // otherwise, if bit is 1, then we increment run from table
            // otherwise, info not huffman/rle encoded, read directly
            // in the first 3 cases there is a bit for the sign of the amplitude
            if (amplitude != 0) {
                // First block info contains the actual value, just get sign
                amplitude *= reader.ReadBoolean() ? -1 : 1;
            } else if (reader.ReadBoolean()) {
                // Bigger amplitude: increment reading another RLE info block
                (isBlockEnd, run, amplitude) = ReadRleInfo(reader);

                int residueIdx = run + (isBlockEnd ? 64 : 0);
                amplitude += residueTable[residueIdx];
                amplitude *= reader.ReadBoolean() ? -1 : 1;
            } else if (reader.ReadBoolean()) {
                // Bigger run: increment reading another RLE info block
                (isBlockEnd, run, amplitude) = ReadRleInfo(reader);

                int residueIdx = amplitude + (isBlockEnd ? 64 : 0);
                run += residueTable[residueIdx];
                amplitude *= reader.ReadBoolean() ? -1 : 1;
            } else {
                // Cannot encoded amplitude/run in RLE info blocks, we need 17 bits
                isBlockEnd = reader.ReadBoolean();
                run = reader.Read(6);
                amplitude = reader.ReadSigned(12);
            }

            // skip consecutive zeroes
            matrixPos += run;

            // get the actual index after "de-zigzag"
            int targetIdx = zizagTable[matrixPos];

            residual[targetIdx] = amplitude;
        } while (!isBlockEnd);

        return residual;
    }

    private (bool IsBlockEnd, int ZeroRun, int Amplitude) ReadRleInfo(BitReader reader)
    {
        int value = huffman.ReadCodeword(reader);

        // Our huffman implementation already removes the lower 4 bits with the codeword bit count.
        bool isBlockEnd = (value & 0x800) == 1;
        int zeroRun = (value >> 5) & 0x3F;
        int amplitude = value & 0x1F;

        return (isBlockEnd, zeroRun, amplitude);
    }
}
