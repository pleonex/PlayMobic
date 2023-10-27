namespace PlayMobic.Video;

using System;
using System.Drawing;

internal readonly struct PixelBlock
{
    public PixelBlock(Memory<byte> data, int stride, Rectangle rect, int index)
    {
        Data = data;
        Stride = stride;
        Index = index;

        X = rect.X;
        Y = rect.Y;
        Width = rect.Width;
        Height = rect.Height;
    }

    public Memory<byte> Data { get; }

    public int Stride { get; }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public int Index { get; }

    public byte this[int x, int y] {
        get {
            int fullIdx = ((Y + y) * Stride) + X + x;
            return Data.Span[fullIdx];
        }
        set {
            int fullIdx = ((Y + y) * Stride) + X + x;
            Data.Span[fullIdx] = value;
        }
    }

    public readonly PixelBlock[] Partition(int blockWidth, int blockHeight)
    {
        if ((Width % blockWidth) != 0 || (Height % blockHeight) != 0) {
            throw new ArgumentException("Sides must be multiple of current block");
        }

        int columnsCount = Width / blockWidth;
        int rowsCount = Height / blockHeight;
        var blocks = new PixelBlock[columnsCount * rowsCount];

        // Origin: top-left, starting to the right then down.
        int idx = 0;
        for (int r = 0; r < rowsCount; r++) {
            for (int c = 0; c < columnsCount; c++) {
                var rect = new Rectangle(
                    X + (c * blockWidth),
                    Y + (r * blockHeight),
                    blockWidth,
                    blockHeight);
                blocks[idx] = new PixelBlock(Data, Stride, rect, idx);
                idx++;
            }
        }

        return blocks;
    }
}
