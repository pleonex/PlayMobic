namespace PlayMobic.Tests.Video;

using PlayMobic.Video;

[TestFixture]
internal class FrameYuv420Tests
{
    [Test]
    public void DataIsInitialized()
    {
        var frame = new FrameYuv420(256, 192);
        Assert.That(frame.PackedData.IsEmpty, Is.False);
        Assert.That(frame.Width, Is.EqualTo(256));
        Assert.That(frame.Height, Is.EqualTo(192));
    }

    [Test]
    public void UnpackedDataForLumaAtCorrectIndexes()
    {
        var frame = new FrameYuv420(256, 192);

        frame.Luma.Data.Span[0] = 0x01;
        frame.Luma.Data.Span[^1] = 0x02;

        int fullFirstIdx = 0;
        int fullLastIdx = (256 * 192) - 1;
        Assert.Multiple(() => {
            Assert.That(frame.PackedData[fullFirstIdx], Is.EqualTo(0x01));
            Assert.That(frame.PackedData[fullLastIdx], Is.EqualTo(0x02));
        });
    }

    [Test]
    public void UnpackedDataForChromaUAtCorrectIndexes()
    {
        var frame = new FrameYuv420(256, 192);

        frame.U.Data.Span[0] = 0x01;
        frame.U.Data.Span[^1] = 0x02;

        int fullFirstIdx = 256 * 192;
        int fullLastIdx = fullFirstIdx + (256 * 192 / 2) - 1;
        Assert.Multiple(() => {
            Assert.That(frame.PackedData[fullFirstIdx], Is.EqualTo(0x01));
            Assert.That(frame.PackedData[fullLastIdx], Is.EqualTo(0x02));
        });
    }

    [Test]
    public void UnpackedDataForChromaVAtCorrectIndexes()
    {
        var frame = new FrameYuv420(256, 192);

        frame.V.Data.Span[0] = 0x01;
        frame.V.Data.Span[^1] = 0x02;

        int fullFirstIdx = (256 * 192) + (256 * 192 / 2);
        int fullLastIdx = fullFirstIdx + (256 * 192 / 2) - 1;
        Assert.Multiple(() => {
            Assert.That(frame.PackedData[fullFirstIdx], Is.EqualTo(0x01));
            Assert.That(frame.PackedData[fullLastIdx], Is.EqualTo(0x02));
        });
    }

    [Test]
    public void CleanData()
    {
        var frame = new FrameYuv420(256, 192);
        var cleanBuffer = new byte[frame.PackedData.Length];

        frame.Luma.Data.Span[3] = 0xCA;
        frame.U.Data.Span[5] = 0xFE;
        frame.V.Data.Span[8] = 0xC0;
        frame.V.Data.Span[42] = 0xC0;
        Assert.That(frame.PackedData.ToArray(), Is.Not.EquivalentTo(cleanBuffer));

        frame.CleanData();
        Assert.That(frame.PackedData.ToArray(), Is.EquivalentTo(cleanBuffer));
    }

    [Test]
    public void ModifyDataFromBlocks()
    {
        var frame = new FrameYuv420(256, 192);
        int startU = 256 * 192;
        int startV = (256 * 192) + (256 * 192 / 2);

        int lumaIndex = (1 * 256) + 16 + (2 * 2) + 1;
        int uvIndex = (1 * 256 / 2) + 16 + (2 * 2) + 1;

        PixelBlock lumaBlock = frame.Luma.Partition(16, 16)[1].Partition(2, 2)[2];
        lumaBlock[1, 1] = 42;
        Assert.That(frame.PackedData[lumaIndex], Is.EqualTo(42));

        PixelBlock uBlock = frame.U.Partition(16, 16)[1].Partition(2, 2)[2];
        uBlock[1, 1] = 0xCA;
        Assert.That(frame.PackedData[startU + uvIndex], Is.EqualTo(0xCA));

        PixelBlock vBlock = frame.V.Partition(16, 16)[1].Partition(2, 2)[2];
        vBlock[1, 1] = 0xFE;
        Assert.That(frame.PackedData[startV + uvIndex], Is.EqualTo(0xFE));
    }
}
