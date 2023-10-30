﻿namespace PlayMobic.Video.Mobiclip;

using System;
using System.Runtime.CompilerServices;
using PlayMobic.IO;

internal class IntraDecoderBlockPrediction : IIntraDecoderBlockPrediction
{
    private const int BitDepth = 8;

    private readonly BitReader reader;
    private readonly int[] blockModes;

    public IntraDecoderBlockPrediction(BitReader reader)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));

        // We store the mode of each 8x8 or 4x4 block to use it to predict others.
        // We set the array to the max capacity (16x16 macroblock has 16 4x4 blocks).
        // If we are processing a 8x8 block, we will have some unused items.
        blockModes = new int[16];
    }

    public void PerformBlockPrediction(PixelBlock block, IntraPredictionBlockMode mode)
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

    internal IntraPredictionBlockMode DecodeBlockMode(PixelBlock block)
    {
        // As the mode from neighbor blocks are highly correlated, the encoder
        // saves some bits by calculating the most probable mode: H.264 8.3.1.1
        // This is computed as the minimum value between our neighbor blocks:
        // the mode of the block above and the mode of the block to the left.
        // This is reset for each macroblock (not slice as H.264).
        // If we are in the top row or left row of the macroblock, ignore the
        // non-existing neighbor. If there aren't neighbors (block 0,0) use DC.
        // DC is the only mode that won't require neighbor pixels either.
        int blocksPerRow = 16 / block.Width;
        int aboveBlockIdx = block.Index - blocksPerRow;
        int leftBlockIdx = block.Index - 1;

        int predictedMode;
        if (aboveBlockIdx < 0 && leftBlockIdx < 0) {
            predictedMode = (int)IntraPredictionBlockMode.DC;
        } else if (aboveBlockIdx < 0) {
            predictedMode = blockModes[leftBlockIdx];
        } else if (leftBlockIdx < 0) {
            predictedMode = blockModes[aboveBlockIdx];
        } else {
            predictedMode = Math.Min(blockModes[leftBlockIdx], blockModes[aboveBlockIdx]);
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

        blockModes[block.Index] = predictedMode;

        return (IntraPredictionBlockMode)predictedMode;
    }

    private static void PredictionVertical(PixelBlock block)
    {
        // Copy top neighbor pixel for each column, like H.264
        foreach ((int x, int y) in block.Iterate()) {
            block[x, y] = block[x, -1];
        }
    }

    private static void PredictionHorizontal(PixelBlock block)
    {
        // Copy left neighbor pixel for each row, like H.264
        foreach ((int x, int y) in block.Iterate()) {
            block[x, y] = block[-1, y];
        }
    }

    private static void PredictionDeltaPlane(PixelBlock block, BitReader reader)
    {
        // Plane prediction with delta (similar to H.264 (8.3.3.4))
        int delta = reader.ReadExpGolombSigned();
        byte bottomMost = block[-1, block.Height - 1];
        byte rightMost = block[block.Width - 1, -1];

        // TBC if it can be used for 4x4
        int shift = (block.Width == 4) ? 2 : 3;
        int sizeAdjust = (block.Width == 16) ? 2 : 1;

        // In h.264: a = 16 * (p[-1, 15] + p[15, -1])
        int average = (bottomMost + rightMost + 1) / 2;
        average += 2 * delta;

        int hDelta = (average - bottomMost + 1) >> sizeAdjust;
        int vDelta = (average - rightMost + 1) >> sizeAdjust;

        Span<int> hRow = stackalloc int[block.Width];
        for (int x = 0; x < hRow.Length; x++) {
            // In h.264 is sum of every: (x + 1) * (block[8 + x, -1] - block[6 - x, -1])
            // then: b = ((5 * H) + 32) / 64
            hRow[x] = (x + 1) * hDelta;
            hRow[x] += (bottomMost - block[x, -1]) << shift;
            hRow[x] = (hRow[x] + 1) >> sizeAdjust;
        }

        Span<int> vColumn = stackalloc int[block.Height];
        for (int y = 0; y < vColumn.Length; y++) {
            // In h.264: is sum of every:(y + 1) * (block[-1, 8 + y] - block[-1, 6 - y])
            // then: c = ((5 * V) + 32) / 64
            vColumn[y] = (y + 1) * vDelta;
            vColumn[y] += (rightMost - block[-1, y]) << shift;
            vColumn[y] = (vColumn[y] + 1) >> sizeAdjust;
        }

        foreach ((int x, int y) in block.Iterate()) {
            // In h.264: clip((a + b * (x - 7) + c * (y - 7) + 16) / 32)
            int z = ((hRow[x] * (y + 1)) + (vColumn[y] * (x + 1))) >> (2 * shift);
            block[x, y] = (byte)((block[x, -1] + block[-1, y] + z + 1) / 2);
        }
    }

    private static void PredictionDC(PixelBlock block)
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

            average = (byte)(sum / len);
        }

        foreach ((int x, int y) in block.Iterate()) {
            block[x, y] = average;
        }
    }

    private static void PredictionHorizontalUp(PixelBlock block)
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
                block[x, y] = Average3LeftNeighbors(block, y + (x / 2));
            }
        }
    }

    private static void PredictionHorizontalDown(PixelBlock block)
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

    private static void PredictionVerticalRight(PixelBlock block)
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

    private static void PredictionDiagonalDownRight(PixelBlock block)
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

    private static void PredictionVerticalLeft(PixelBlock block)
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
    private static byte Average2LeftNeighbors(PixelBlock block, int y) =>
        Average(block[-1, y], block[-1, y + 1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Average3LeftNeighbors(PixelBlock block, int y) =>
        AverageMiddleWeight(block[-1, y - 1], block[-1, y], block[-1, y + 1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Average2TopNeighbors(PixelBlock block, int x) =>
    Average(block[x, -1], block[x + 1, -1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Average3TopNeighbors(PixelBlock block, int x) =>
        AverageMiddleWeight(block[x - 1, -1], block[x, -1], block[x + 1, -1]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte AverageCorner(PixelBlock block) =>
        AverageMiddleWeight(block[-1, 0], block[-1, -1], block[0, -1]);
}
