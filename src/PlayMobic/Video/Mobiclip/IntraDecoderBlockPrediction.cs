namespace PlayMobic.Video.Mobiclip;

using System;

internal class IntraDecoderBlockPrediction : IIntraDecoderBlockPrediction
{
    private const int BitDepth = 8;

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
                PredictionDeltaPlane(block);
                break;

            case IntraPredictionBlockMode.DC:
                PredictionDC(block);
                break;

            case IntraPredictionBlockMode.Nothing:
                // already predicted (e.g. 16x16 mode 2)
                break;

            default: throw new NotImplementedException();
        }
    }

    internal IntraPredictionBlockMode DecodeBlockMode(PixelBlock block)
    {
        // As mode from neighbor blocks are highly correlated, it saves some bits
        // by calculating the most probable mode: H.264 8.3.1.1
        throw new NotImplementedException();
    }

    private static void PredictionVertical(PixelBlock block)
    {
        foreach ((int x, int y) in block.Iterate()) {
            block[x, y] = block[x, -1];
        }
    }

    private static void PredictionHorizontal(PixelBlock block)
    {
        foreach ((int x, int y) in block.Iterate()) {
            block[x, y] = block[-1, y];
        }
    }

    private static void PredictionDeltaPlane(PixelBlock block)
    {
        // Plane prediction with delta (different H.264 (8.3.3.4))
        throw new NotImplementedException();
    }

    private static void PredictionDC(PixelBlock block)
    {
        byte average;
        if (block.X == 0 && block.Y == 0) {
            average = 1 << (BitDepth - 1);
        } else {
            int sum = 0;
            int len = 0;
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
}
