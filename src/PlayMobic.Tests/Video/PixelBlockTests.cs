namespace PlayMobic.Tests.Video;

using System.Drawing;
using PlayMobic.Video;

[TestFixture]
public class PixelBlockTests
{
#pragma warning disable SA1137 // Elements should have the same indentation
    // I know we could generate it, but it helps as a visual guide.
    private static readonly byte[] Block4x4 = new byte[4 * 4] {
         0,  1,  2,  3,
         4,  5,  6,  7,
         8,  9, 10, 11,
        12, 13, 14, 15,
    };
    private static readonly byte[] Block8x8 = new byte[8 * 8] {
         0,  1,  2,  3,  4,  5,  6,  7,
         8,  9, 10, 11, 12, 13, 14, 15,
        16, 17, 18, 19, 20, 21, 22, 23,
        24, 25, 26, 27, 28, 29, 30, 31,
        32, 33, 34, 35, 36, 37, 38, 39,
        40, 41, 42, 43, 44, 45, 46, 47,
        48, 49, 50, 51, 52, 53, 54, 55,
        56, 57, 58, 59, 60, 61, 62, 63,
    };
#pragma warning restore SA1137

    [Test]
    public void ConstructorSetProperties()
    {
        byte[] data = new byte[8];
        var block = new PixelBlock(data, 2, new Rectangle(4, 8, 2, 4), 42);

        Assert.Multiple(() => {
            Assert.That(block.Data, Has.Length.EqualTo(8));
            Assert.That(block.Stride, Is.EqualTo(2));
            Assert.That(block.X, Is.EqualTo(4));
            Assert.That(block.Y, Is.EqualTo(8));
            Assert.That(block.Width, Is.EqualTo(2));
            Assert.That(block.Height, Is.EqualTo(4));
            Assert.That(block.Index, Is.EqualTo(42));
        });
    }

    [Test]
    public void IndexerTopBlock()
    {
        var block = new PixelBlock(Block4x4.ToArray(), 4, new Rectangle(0, 0, 4, 4), 0);

        block[1, 2] = 42;
        Assert.Multiple(() => {
            Assert.That(block[2, 1], Is.EqualTo(6));
            Assert.That(block[0, 3], Is.EqualTo(12));
            Assert.That(block[3, 0], Is.EqualTo(3));

            Assert.That(block[1, 2], Is.EqualTo(42));
        });
    }

    [Test]
    public void IndexerChildBlock()
    {
        var block = new PixelBlock(Block4x4.ToArray(), 4, new Rectangle(2, 2, 2, 2), 3);

        block[1, 1] = 42;
        Assert.Multiple(() => {
            Assert.That(block[0, 0], Is.EqualTo(10));
            Assert.That(block[1, 0], Is.EqualTo(11));
            Assert.That(block[0, 1], Is.EqualTo(14));
            Assert.That(block[1, 1], Is.EqualTo(42));
        });
    }

    [Test]
    public void IndexerChildNeighbors()
    {
        var block = new PixelBlock(Block4x4.ToArray(), 4, new Rectangle(1, 1, 2, 2), 0);

        Assert.Multiple(() => {
            Assert.That(block[-1, 0], Is.EqualTo(4));
            Assert.That(block[-1, 1], Is.EqualTo(8));
            Assert.That(block[1, -1], Is.EqualTo(2));
            Assert.That(block[0, -1], Is.EqualTo(1));
            Assert.That(block[-1, -1], Is.EqualTo(0));
            Assert.That(block[2, -1], Is.EqualTo(3));
        });

        // Right and bottom neightbors are not allowed
        Assert.Multiple(() => {
            Assert.That(() => block[-1, 2], Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => block[0, 2], Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => block[1, 2], Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => block[2, 2], Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => block[2, 1], Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => block[2, 0], Throws.InstanceOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void PartitionOnceSameSides()
    {
        var blockRect = new Rectangle(0, 0, 8, 8);
        var block = new PixelBlock(Block8x8.ToArray(), 8, blockRect, 3);

        PixelBlock[] subBlocks = block.Partition(4, 4);
        Assert.That(subBlocks, Has.Length.EqualTo(4));

        Assert.Multiple(() => {
            Assert.That(subBlocks[3].Width, Is.EqualTo(4));
            Assert.That(subBlocks[3].Height, Is.EqualTo(4));
            Assert.That(subBlocks[3].Stride, Is.EqualTo(8));

            Assert.That(subBlocks[3].X, Is.EqualTo(4));
            Assert.That(subBlocks[3].Y, Is.EqualTo(4));
            Assert.That(subBlocks[3].Index, Is.EqualTo(3));

            Assert.That(subBlocks[3][1, 1], Is.EqualTo(45));
        });
    }

    [Test]
    public void PartitionTwice()
    {
        var block1Rect = new Rectangle(0, 0, 8, 8);
        var block1 = new PixelBlock(Block8x8.ToArray(), 8, block1Rect, 0);

        PixelBlock[] blocks2 = block1.Partition(4, 4);
        PixelBlock[] blocks3 = blocks2[3].Partition(2, 2);

        Assert.That(blocks3, Has.Length.EqualTo(4));

        Assert.Multiple(() => {
            Assert.That(blocks3[3].Width, Is.EqualTo(2));
            Assert.That(blocks3[3].Height, Is.EqualTo(2));
            Assert.That(blocks3[3].Stride, Is.EqualTo(8));

            Assert.That(blocks3[3].X, Is.EqualTo(6));
            Assert.That(blocks3[3].Y, Is.EqualTo(6));
            Assert.That(blocks3[3].Index, Is.EqualTo(3));

            Assert.That(blocks3[3][1, 1], Is.EqualTo(63));
        });
    }

    [Test]
    public void PartitionDirectionRightDown()
    {
        var block = new PixelBlock(Block4x4.ToArray(), 4, new Rectangle(0, 0, 4, 4), 0);

        PixelBlock[] blocks2 = block.Partition(2, 2);
        Assert.Multiple(() => {
            Assert.That(blocks2[0].X, Is.EqualTo(0));
            Assert.That(blocks2[0].Y, Is.EqualTo(0));
            Assert.That(blocks2[0].Index, Is.EqualTo(0));

            Assert.That(blocks2[1].X, Is.EqualTo(2));
            Assert.That(blocks2[1].Y, Is.EqualTo(0));
            Assert.That(blocks2[1].Index, Is.EqualTo(1));

            Assert.That(blocks2[2].X, Is.EqualTo(0));
            Assert.That(blocks2[2].Y, Is.EqualTo(2));
            Assert.That(blocks2[2].Index, Is.EqualTo(2));

            Assert.That(blocks2[3].X, Is.EqualTo(2));
            Assert.That(blocks2[3].Y, Is.EqualTo(2));
            Assert.That(blocks2[3].Index, Is.EqualTo(3));
        });
    }

    [Test]
    public void PartitionNotMultipleThrows()
    {
        var block = new PixelBlock(Block4x4.ToArray(), 4, new Rectangle(0, 0, 4, 4), 0);
        Assert.Multiple(() => {
            Assert.That(() => block.Partition(2, 3), Throws.ArgumentException);
            Assert.That(() => block.Partition(3, 2), Throws.ArgumentException);
        });
    }
}
