namespace PlayMobic.Video.Mobiclip;

using System;
using PlayMobic.IO;

internal class MotionCompensationDecoder
{
    private readonly BitReader reader;
    private readonly FramesBuffer<FrameYuv420> previousFrames;
    private readonly Dictionary<(int Width, int Height), Huffman> huffmanTables;
    private readonly Vector2D[] vectorsCache;
    private Vector2D predictedVector;

    public MotionCompensationDecoder(
        BitReader reader,
        FramesBuffer<FrameYuv420> previousFrames,
        Dictionary<(int Width, int Height), Huffman> huffmanTables)
    {
        this.reader = reader;
        this.previousFrames = previousFrames;
        this.huffmanTables = huffmanTables;

        // Vector cache for each macroblock on a row
        // + 2, so we can have a 0 for the outbound left and right sides when
        // calculating the median without additional logic.
        vectorsCache = new Vector2D[(previousFrames.Current.Width / 16) + 2];
    }

    public void DecodeMacroBlock(YuvBlock block, int mode)
    {
        int macroBlockIndex = UpdateVectorCache(block);
        DecodeBlock(block, mode, macroBlockIndex);
    }

    public void SkipMacroBlock(YuvBlock block)
    {
        // For modes 6 and 7 (use IntraPrediction) we still need to update the
        // vector cache as (0, 0)
        int macroBlockIndex = UpdateVectorCache(block);
        vectorsCache[macroBlockIndex] = new Vector2D(0, 0);
    }

    private int UpdateVectorCache(YuvBlock block)
    {
        // We predict the vector by doing the median between the last vector
        // used in the macroblock to the left and to the right
        // It's not resetted for each row, so next row will take our last right value.
        int macroBlockIndex = (block.Luma.X / 16) + 1;
        predictedVector = Median3(
            vectorsCache[macroBlockIndex - 1],
            vectorsCache[macroBlockIndex],
            vectorsCache[macroBlockIndex + 1]);

        return macroBlockIndex;
    }

    private static Vector2D Median3(Vector2D a, Vector2D b, Vector2D c)
    {
        // https://stackoverflow.com/a/23392433
        static int Median3(int a, int b, int c) =>
            Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));

        return new Vector2D(Median3(a.X, b.X, c.X), Median3(a.Y, b.Y, c.Y));
    }

    private void DecodeBlock(YuvBlock block, int mode, int macroBlockIndex)
    {
        // mode 0: motion compensation from current frame (P_Skip?)
        // mode 1-5: motion compensation with delta from past frame
        // mode 6-7: intra - forbidden at this stage
        // mode 8: partition by height and decode each block
        // mode 9: partition by width and decode each block
        if (mode is >= 0 and <= 5) {
            DecodeBlockMotion(block, mode, macroBlockIndex);
        } else if (mode is 6 or 7) {
            throw new InvalidOperationException("Invalid inter block");
        } else if (mode is 8 or 9) {
            DecodeBlockPartioning(block, mode, macroBlockIndex);
        } else {
            throw new NotSupportedException("Invalid inter mode");
        }
    }

    private void DecodeBlockPartioning(YuvBlock block, int mode, int macroBlockIndex)
    {
        // Depending on the mode, partition by height or width
        YuvBlock[] partitions = (mode == 8)
            ? block.PartitionBy(1, 2)  // by height
            : block.PartitionBy(2, 1); // by width

        // The new partition size gives the Huffman table
        int newLumaWidth = partitions[0].Luma.Width;
        int newLumaHeight = partitions[0].Luma.Height;
        Huffman huffmanTable = huffmanTables[(newLumaWidth, newLumaHeight)];

        // Re-run motion compensation algorithm on each partition.
        foreach (YuvBlock partition in partitions) {
            int partitionsMode = huffmanTable.ReadCodeword(reader);
            DecodeBlock(partition, partitionsMode, macroBlockIndex);
        }
    }

    private void DecodeBlockMotion(YuvBlock block, int mode, int macroBlockIdx)
    {
        Vector2D vector = predictedVector;
        if (mode != 0) {
            int deltaX = reader.ReadExpGolombSigned();
            int deltaY = reader.ReadExpGolombSigned();
            vector = new Vector2D(vector.X + deltaX, vector.Y + deltaY);
        }

        vectorsCache[macroBlockIdx] = vector;

        int frameIndex = (mode == 0) ? 1 : mode;
        YuvBlock src = previousFrames.Buffer[frameIndex].GetFrameBlock();
        CopyMovingYuvBlock(block, src, vector);
    }

    private void CopyMovingYuvBlock(YuvBlock dstBlock, YuvBlock srcFrame, Vector2D delta)
    {
        ComponentBlock dstLuma = dstBlock.Luma;
        ComponentBlock dstChromaU = dstBlock.ChromaU;
        ComponentBlock dstChromaV = dstBlock.ChromaV;

        for (int y = 0; y < dstBlock.Luma.Height; y++) {
            for (int x = 0; x < dstBlock.Luma.Width; x++) {
                int srcLumaX = dstLuma.X + x;
                int srcLumaY = dstLuma.Y + y;
                dstLuma[x, y] = GetHalfPelComponent(srcFrame.Luma, srcLumaX, srcLumaY, delta);

                // Because of YUV 4:2:0 downsampling we divide by 2 and do it half of time
                if ((x % 2) == 0 && (y % 2) == 0) {
                    // shifting instead of regular division is important for rounding.
                    var chromaDelta = new Vector2D(delta.X >> 1, delta.Y >> 1);
                    int dstChromaX = dstChromaU.X + (x / 2);
                    int dstChromaY = dstChromaU.Y + (y / 2);
                    dstChromaU[x / 2, y / 2] = GetHalfPelComponent(srcFrame.ChromaU, dstChromaX, dstChromaY, chromaDelta);
                    dstChromaV[x / 2, y / 2] = GetHalfPelComponent(srcFrame.ChromaV, dstChromaX, dstChromaY, chromaDelta);
                }
            }
        }
    }

    private byte GetHalfPelComponent(ComponentBlock block, int x, int y, Vector2D delta)
    {
        // Actual positions are multiplied by two, so we can have half-pixel positions
        // without storing decimals.
        bool isExactX = (delta.X % 2) == 0;
        bool isExactY = (delta.Y % 2) == 0;

        // We divide with bitwise shifting (same as / 2) because of
        // how rounding works. Same for the operations below.
        x += delta.X >> 1;
        y += delta.Y >> 1;

        if (isExactX && isExactY) {
            return block[x, y];
        } else if (isExactX) {
            return (byte)((block[x, y] >> 1) + (block[x, y + 1] >> 1));
        } else if (isExactY) {
            return (byte)((block[x, y] >> 1) + (block[x + 1, y] >> 1));
        } else {
            int a = (block[x, y] >> 1) + (block[x + 1, y] >> 1);
            int b = (block[x, y + 1] >> 1) + (block[x + 1, y + 1] >> 1);
            return (byte)((a >> 1) + (b >> 1));
        }
    }

    private readonly struct Vector2D
    {
        public Vector2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }
    }
}
