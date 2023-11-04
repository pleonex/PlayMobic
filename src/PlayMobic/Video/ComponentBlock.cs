namespace PlayMobic.Video;

using System;
using System.Drawing;

internal readonly struct ComponentBlock
{
    public ComponentBlock(Memory<byte> data, int stride, Rectangle rect, int index)
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
            // 1 neighbor pixel from left and 2 times width top is allowed.
            if ((x < 0 && X == 0) || (x < -1) || (x >= Width && y != -1) || (x >= Width * 2)) {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if ((y < 0 && Y == 0) || (y < -1) || (y >= Height)) {
                throw new ArgumentOutOfRangeException(nameof(y));
            }

            int fullIdx = ((Y + y) * Stride) + X + x;
            return Data.Span[fullIdx];
        }
        set {
            int fullIdx = ((Y + y) * Stride) + X + x;
            Data.Span[fullIdx] = value;
        }
    }

    public IEnumerable<(int x, int y)> Iterate()
    {
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                yield return ValueTuple.Create(x, y);
            }
        }
    }

    public readonly ComponentBlock[] Partition(int blockWidth, int blockHeight)
    {
        if ((Width % blockWidth) != 0 || (Height % blockHeight) != 0) {
            throw new ArgumentException("Sides must be multiple of current block");
        }

        int columnsCount = Width / blockWidth;
        int rowsCount = Height / blockHeight;
        var blocks = new ComponentBlock[columnsCount * rowsCount];

        // Origin: top-left, starting to the right then down.
        int idx = 0;
        for (int r = 0; r < rowsCount; r++) {
            for (int c = 0; c < columnsCount; c++) {
                var rect = new Rectangle(
                    X + (c * blockWidth),
                    Y + (r * blockHeight),
                    blockWidth,
                    blockHeight);
                blocks[idx] = new ComponentBlock(Data, Stride, rect, idx);
                idx++;
            }
        }

        return blocks;
    }
}
