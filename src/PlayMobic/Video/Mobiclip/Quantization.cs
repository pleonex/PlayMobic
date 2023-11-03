namespace PlayMobic.Video.Mobiclip;

using System;

/// <summary>
/// Quantizates the coefficients of DCT to a smaller scale to have less bits.
/// </summary>
/// <remarks>
/// Like H.264 <see href="https://www.vcodex.com/h264avc-4x4-transform-and-quantization/"/>.
/// </remarks>
internal class Quantization
{
    // These tables comes from the DCT and QStep used in H.264.
    // We apply by the scale factor 2^15 or 2^6 in the DCT transform.
    // More info: https://www.vcodex.com/h264avc-4x4-transform-and-quantization/
    private static readonly int[][] Block4x4Table = {
        new[] {
            10, 13, 13, 10,
            16, 10, 13, 13,
            13, 13, 16, 10,
            16, 13, 13, 16
        },
        new[] {
            11, 14, 14, 11,
            18, 11, 14, 14,
            14, 14, 18, 11,
            18, 14, 14, 18,
        },
        new[] {
            13, 16, 16, 13,
            20, 13, 16, 16,
            16, 16, 20, 13,
            20, 16, 16, 20,
        },
        new[] {
            14, 18, 18, 14,
            23, 14, 18, 18,
            18, 18, 23, 14,
            23, 18, 18, 23,
        },
        new[] {
            16, 20, 20, 16,
            25, 16, 20, 20,
            20, 20, 25, 16,
            25, 20, 20, 25,
        },
        new[] {
            18, 23, 23, 18,
            29, 18, 23, 23,
            23, 23, 29, 18,
            29, 23, 23, 29,
        },
    };

    private static readonly int[][] Block8x8Table = {
        new[] {
            20, 19, 19, 25, 18, 25, 19, 24,
            24, 19, 20, 18, 32, 18, 20, 19,
            19, 24, 24, 19, 19, 25, 18, 25,
            18, 25, 18, 25, 19, 24, 24, 19,
            19, 24, 24, 19, 18, 32, 18, 20,
            18, 32, 18, 24, 24, 19, 19, 24,
            24, 18, 25, 18, 25, 18, 19, 24,
            24, 19, 18, 32, 18, 24, 24, 18,
        },
        new[] {
            22, 21, 21, 28, 19, 28, 21, 26,
            26, 21, 22, 19, 35, 19, 22, 21,
            21, 26, 26, 21, 21, 28, 19, 28,
            19, 28, 19, 28, 21, 26, 26, 21,
            21, 26, 26, 21, 19, 35, 19, 22,
            19, 35, 19, 26, 26, 21, 21, 26,
            26, 19, 28, 19, 28, 19, 21, 26,
            26, 21, 19, 35, 19, 26, 26, 19,
        },
        new[] {
            26, 24, 24, 33, 23, 33, 24, 31,
            31, 24, 26, 23, 42, 23, 26, 24,
            24, 31, 31, 24, 24, 33, 23, 33,
            23, 33, 23, 33, 24, 31, 31, 24,
            24, 31, 31, 24, 23, 42, 23, 26,
            23, 42, 23, 31, 31, 24, 24, 31,
            31, 23, 33, 23, 33, 23, 24, 31,
            31, 24, 23, 42, 23, 31, 31, 23,
        },
        new[] {
            28, 26, 26, 35, 25, 35, 26, 33,
            33, 26, 28, 25, 45, 25, 28, 26,
            26, 33, 33, 26, 26, 35, 25, 35,
            25, 35, 25, 35, 26, 33, 33, 26,
            26, 33, 33, 26, 25, 45, 25, 28,
            25, 45, 25, 33, 33, 26, 26, 33,
            33, 25, 35, 25, 35, 25, 26, 33,
            33, 26, 25, 45, 25, 33, 33, 25,
        },
        new[] {
            32, 30, 30, 40, 28, 40, 30, 38,
            38, 30, 32, 28, 51, 28, 32, 30,
            30, 38, 38, 30, 30, 40, 28, 40,
            28, 40, 28, 40, 30, 38, 38, 30,
            30, 38, 38, 30, 28, 51, 28, 32,
            28, 51, 28, 38, 38, 30, 30, 38,
            38, 28, 40, 28, 40, 28, 30, 38,
            38, 30, 28, 51, 28, 38, 38, 28,
        },
        new[] {
            36, 34, 34, 46, 32, 46, 34, 43,
            43, 34, 36, 32, 58, 32, 36, 34,
            34, 43, 43, 34, 34, 46, 32, 46,
            32, 46, 32, 46, 34, 43, 43, 34,
            34, 43, 43, 34, 32, 58, 32, 36,
            32, 58, 32, 43, 43, 34, 34, 43,
            43, 32, 46, 32, 46, 32, 34, 43,
            43, 34, 32, 58, 32, 43, 43, 32,
        },
    };

    private readonly int tableIdx;
    private readonly int scale;

    public Quantization(int qp)
    {
        tableIdx = qp % 6;
        scale = qp / 6;
    }

    public void Quantize(int[] block)
    {
        if (block.Length is not(16 or 64)) {
            throw new ArgumentException("Unsupported block size");
        }

        int[] table;
        int blockScale;
        if (block.Length == 16) {
            table = Block4x4Table[tableIdx];
            blockScale = scale;
        } else {
            table = Block8x8Table[tableIdx];
            blockScale = scale - 2;
        }

        for (int i = 0; i < block.Length; i++) {
            block[i] /= table[i] << blockScale;
        }
    }

    public void Dequantize(int[] block)
    {
        if (block.Length is not(16 or 64)) {
            throw new ArgumentException("Unsupported block size");
        }

        int[] table;
        int blockScale;
        if (block.Length == 16) {
            table = Block4x4Table[tableIdx];
            blockScale = scale;
        } else {
            table = Block8x8Table[tableIdx];
            blockScale = scale - 2;
        }

        for (int i = 0; i < block.Length; i++) {
            block[i] *= table[i] << blockScale;
        }
    }
}
