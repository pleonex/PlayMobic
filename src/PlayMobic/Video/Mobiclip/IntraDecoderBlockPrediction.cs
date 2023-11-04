namespace PlayMobic.Video.Mobiclip;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using PlayMobic.IO;

internal class IntraDecoderBlockPrediction : IIntraDecoderBlockPrediction
{
    private const int BitDepth = 8;

    private readonly BitReader reader;
    private readonly int[] prevBlockModes;

    public IntraDecoderBlockPrediction(BitReader reader)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));

        // We store the mode of each 8x8 or 4x4 block to use it to predict others.
        // We set the array to the max capacity (16x16 macroblock has 16 4x4 blocks).
        // If we are processing a 8x8 block, we will have some unused items.
        prevBlockModes = new int[4 * 4];
    }

    public void PerformBlockPrediction(ComponentBlock block, IntraPredictionBlockMode mode)
    {
        if (mode == IntraPredictionBlockMode.Predicted) {
            mode = DecodeBlockMode(block);
        }

        switch (mode) {
            case IntraPredictionBlockMode.Vertical:
                PredictionVertical(block);
                break;

            case IntraPredictionBlockMode.Horizontal:
                PredictionHorizontal(block);
                break;

            case IntraPredictionBlockMode.DeltaPlane:
                PredictionDeltaPlane(block, reader);
                break;

            case IntraPredictionBlockMode.DC:
                PredictionDC(block);
                break;

            case IntraPredictionBlockMode.HorizontalUp:
                PredictionHorizontalUp(block);
                break;

            case IntraPredictionBlockMode.HorizontalDown:
                PredictionHorizontalDown(block);
                break;

            case IntraPredictionBlockMode.VerticalRight:
                PredictionVerticalRight(block);
                break;

            case IntraPredictionBlockMode.DiagonalDownRight:
                PredictionDiagonalDownRight(block);
                break;

            case IntraPredictionBlockMode.VerticalLeft:
                PredictionVerticalLeft(block);
                break;

            case IntraPredictionBlockMode.Nothing:
                // already predicted (e.g. 16x16 DeltaPlane)
                break;

            default:
                throw new NotImplementedException();
        }
    }

    [SuppressMessage("Style", "IDE0047:Remove unnecessary parentheses", Justification = "Readibility")]
    internal IntraPredictionBlockMode DecodeBlockMode(ComponentBlock block)
    {
        // Only for luma macroblocks (16, 16):
        // As the mode from neighbor blocks are highly correlated, the encoder
        // saves some bits by calculating the most probable mode: H.264 8.3.1.1
        // This is computed as the minimum value between our neighbor blocks:
        // the mode of the block above and the mode of the block to the left.
        // This is reset for each macroblock (not slice as H.264).
        // If we are in the top row or left row of the macroblock, ignore the
        // non-existing neighbor. If there aren't neighbors (block 0,0) use DC.
        // DC is the only mode that won't require neighbor pixels either.
        // We can't use the block index as it has no meaning as we could have a
        // 4x4 block from 8x8 blocks or from 16x16 blocks.
        const int BlocksPerRow = 16 / 4;

        // Get the position of the block inside the macroblock (16, 16)
        (int macroX, int macroY) = ((block.X % 16) / 4, (block.Y % 16) / 4);
        int blockIdx = (macroY * BlocksPerRow) + macroX;

        int aboveBlockIdx = blockIdx - BlocksPerRow;
        int leftBlockIdx = (macroX == 0) ? -1 : blockIdx - 1;

        int predictedMode;
        if (aboveBlockIdx < 0 && leftBlockIdx < 0) {
            predictedMode = (int)IntraPredictionBlockMode.DC;
        } else if (aboveBlockIdx < 0) {
            predictedMode = prevBlockModes[leftBlockIdx];
        } else if (leftBlockIdx < 0) {
            predictedMode = prevBlockModes[aboveBlockIdx];
        } else {
            predictedMode = Math.Min(prevBlockModes[leftBlockIdx], prevBlockModes[aboveBlockIdx]);
        }

        // Encoder sets a flag to tell us if we got it right
        int useMostProbableMode = reader.Read(1);
        if (useMostProbableMode == 0) {
            int remainingModeSelector = reader.Read(3);

            // H.264 trick to use 8 values (3 bits) to encode 9 values (4 bits) saving 1 bit.
            predictedMode = (remainingModeSelector < predictedMode)
                ? remainingModeSelector
                : remainingModeSelector + 1;
        }

        prevBlockModes[blockIdx] = predictedMode;

        // if the block is 8x8, fill it like it were 4 blocks of 4x4
        if (block.Width == 8) {
            prevBlockModes[blockIdx + 1] = predictedMode; // right
            prevBlockModes[blockIdx + BlocksPerRow] = predictedMode; // down
            prevBlockModes[blockIdx + BlocksPerRow + 1] = predictedMode; // down right
        }

        return (IntraPredictionBlockMode)predictedMode;
    }

    private static void PredictionVertical(ComponentBlock block)
    {
        // Copy top neighbor pixel for each column, like H.264
        foreach ((int x, int y) in block.Iterate()) {
            block[x, y] = block[x, -1];
        }
    }

    private static void PredictionHorizontal(ComponentBlock block)
    {
        // Copy left neighbor pixel for each row, like H.264
        foreach ((int x, int y) in block.Iterate()) {
            block[x, y] = block[-1, y];
        }
    }

    private static void PredictionDeltaPlane(ComponentBlock block, BitReader reader)
    {
        // shift so it works at bit level
        int SizeAdjust(int value) =>
            (block.Width == 16) ? (value + 1) >> 1 : value;

        // Plane prediction with delta (similar to H.264 (8.3.3.4))
        int delta = reader.ReadExpGolombSigned();
        byte bottomMost = block[-1, block.Height - 1];
        byte rightMost = block[block.Width - 1, -1];

        int shift = (block.Width == 4) ? 2 : 3;

        // In h.264: a = 16 * (p[-1, 15] + p[15, -1])
        int average = (bottomMost + rightMost + 1) / 2;
        average += 2 * delta;

        int hDelta = SizeAdjust(average - bottomMost);
        int vDelta = SizeAdjust(average - rightMost);

        Span<int> hRow = stackalloc int[block.Width];
        for (int x = 0; x < hRow.Length; x++) {
            // In h.264 is sum of every: (x + 1) * (block[8 + x, -1] - block[6 - x, -1])
            // then: b = ((5 * H) + 32) / 64
            hRow[x] = (x + 1) * hDelta;
            hRow[x] += (bottomMost - block[x, -1]) << shift;
            hRow[x] = SizeAdjust(hRow[x]);
        }

        Span<int> vColumn = stackalloc int[block.Height];
        for (int y = 0; y < vColumn.Length; y++) {
            // In h.264: is sum of every:(y + 1) * (block[-1, 8 + y] - block[-1, 6 - y])
            // then: c = ((5 * V) + 32) / 64
            vColumn[y] = (y + 1) * vDelta;
            vColumn[y] += (rightMost - block[-1, y]) << shift;
            vColumn[y] = SizeAdjust(vColumn[y]);
        }

        foreach ((int x, int y) in block.Iterate()) {
            // In h.264: clip((a + b * (x - 7) + c * (y - 7) + 16) / 32)
            int z = ((hRow[x] * (y + 1)) + (vColumn[y] * (x + 1))) >> (2 * shift);
            block[x, y] = (byte)((block[x, -1] + block[-1, y] + z + 1) / 2);
        }
    }

    private static void PredictionDC(ComponentBlock block)
    {
        // Average all top and left neighbor pixels like H.264
        byte average;

        if (block.X == 0 && block.Y == 0) {
            // If both neighbors are not available, then average is constant.
            average = 1 << (BitDepth - 1);
        } else {
            int sum = 0;
            int len = 0;

            // If top or left are not available, then only average what it's available.
            if (block.X != 0) {
                len += block.Height;
                for (int y = 0; y < block.Height; y++) {
                    sum += block[-1, y];
                }
            }

            if (block.Y != 0) {
                len += block.Width;
                for (int x = 0; x < block.Width; x++) {
                    sum += block[x, -1];
                }
            }

            average = (byte)((sum + (len / 2)) / len);
        }

        foreach ((int x, int y) in block.Iterate()) {
            block[x, y] = average;
        }
    }

    private static void PredictionHorizontalUp(ComponentBlock block)
    {
        // Horizontal up like H.264
        foreach ((int x, int y) in block.Iterate()) {
            int zHU = x + (2 * y);
            int zHUMax = (2 * block.Height) - 3;

            if (zHU == zHUMax) {
                block[x, y] = Average(
                    block[-1, block.Height - 2],
                    block[-1, block.Height - 1],
                    block[-1, block.Height - 1],
                    block[-1, block.Height - 1]);
            } else if (zHU > zHUMax) {
                block[x, y] = block[-1, block.Height - 1];
            } else if ((zHU % 2) == 0) {
                block[x, y] = Average2LeftNeighbors(block, y + (x / 2));
            } else {
                block[x, y] = Average3LeftNeighbors(block, y + (x / 2) + 1);
            }
        }
    }

    private static void PredictionHorizontalDown(ComponentBlock block)
    {
        // Horizontal down like H.264
        foreach ((int x, int y) in block.Iterate()) {
            int zHD = (2 * y) - x;

            if (zHD < -1) {
                block[x, y] = Average3TopNeighbors(block, x - (2 * y) - 2);
            } else if (zHD == -1) {
                block[x, y] = AverageCorner(block);
            } else if ((zHD % 2) == 0) {
                block[x, y] = Average2LeftNeighbors(block, y - (x / 2) - 1);
            } else {
                block[x, y] = Average3LeftNeighbors(block, y - (x / 2) - 1);
            }
        }
    }

    private static void PredictionVerticalRight(ComponentBlock block)
    {
        // Vertical right like H.264
        foreach ((int x, int y) in block.Iterate()) {
            int zVR = (2 * x) - y;

            if (zVR < -1) {
                block[x, y] = Average3LeftNeighbors(block, y - (2 * x) - 2);
            } else if (zVR == -1) {
                block[x, y] = AverageCorner(block);
            } else if ((zVR % 2) == 0) {
                block[x, y] = Average2TopNeighbors(block, x - (y / 2) - 1);
            } else {
                block[x, y] = Average3TopNeighbors(block, x - (y / 2) - 1);
            }
        }
    }

    private static void PredictionDiagonalDownRight(ComponentBlock block)
    {
        // Diagonal down right like H.264
        foreach ((int x, int y) in block.Iterate()) {
            if (x > y) {
                block[x, y] = Average3TopNeighbors(block, x - y - 1);
            } else if (x < y) {
                block[x, y] = Average3LeftNeighbors(block, y - x - 1);
            } else {
                block[x, y] = AverageCorner(block);
            }
        }
    }

    private static void PredictionVerticalLeft(ComponentBlock block)
    {
        // Vertical left like H.264
        foreach ((int x, int y) in block.Iterate()) {
            if ((y % 2) == 0) {
                block[x, y] = Average2TopNeighbors(block, x + (y / 2));
            } else {
                block[x, y] = Average3TopNeighbors(block, x + (y / 2) + 1);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Average(params byte[] values)
    {
        // Average of values with ceiling rounding (adding half of total).
        return (byte)((values.Sum(x => x) + (values.Length / 2)) / values.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte AverageMiddleWeight(byte a, byte b, byte c) =>
        Average(a, b, b, c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Average2LeftNeighbors(ComponentBlock block, int y) =>
        Average(block[-1, y], block[-1, y + 1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Average3LeftNeighbors(ComponentBlock block, int y) =>
        AverageMiddleWeight(block[-1, y - 1], block[-1, y], block[-1, y + 1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Average2TopNeighbors(ComponentBlock block, int x) =>
    Average(block[x, -1], block[x + 1, -1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Average3TopNeighbors(ComponentBlock block, int x) =>
        AverageMiddleWeight(block[x - 1, -1], block[x, -1], block[x + 1, -1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte AverageCorner(ComponentBlock block) =>
        AverageMiddleWeight(block[-1, 0], block[-1, -1], block[0, -1]);
}
