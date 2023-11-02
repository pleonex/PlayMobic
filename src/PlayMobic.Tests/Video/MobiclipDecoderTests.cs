﻿namespace PlayMobic.Tests.Video;

using PlayMobic.Video;
using PlayMobic.Video.Mobiclip;

[TestFixture]
public class MobiclipDecoderTests
{
    private static readonly byte[] EncodedBlackIFrame = new byte[] {
        0x07, 0xAC, 0x3E, 0x38, 0xCD, 0x07, 0x7B, 0x5F, 0xFE, 0xFC, 0x3F, 0x7F,
        0xCF, 0x9F, 0xF3, 0xE7, 0xFC, 0xF9, 0x7F, 0xFE, 0x9F, 0x3F, 0xE7, 0xCF,
        0xF8, 0xF3, 0x7E, 0xFC, 0x1F, 0x3F, 0xC7, 0x8F, 0xF1, 0xE3, 0xFC, 0xF8,
        0x3F, 0x7E, 0x8F, 0x1F, 0xE3, 0xC7, 0xF8, 0xF1, 0x7E, 0xFC, 0x1F, 0x3F,
        0xC7, 0x8F, 0xF1, 0xE3, 0xFC, 0xF8, 0x3F, 0x7E, 0x8F, 0x1F, 0xE3, 0xC7,
        0xF8, 0xF1, 0x7E, 0xFC, 0x1F, 0x3F, 0xC7, 0x8F, 0xF1, 0xE3, 0xFC, 0xF8,
        0x3F, 0x7E, 0x8F, 0x1F, 0xE3, 0xC7, 0xF8, 0xF1, 0x7E, 0xFC, 0x1F, 0x3F,
        0xC7, 0x8F, 0xF1, 0xE3, 0xFC, 0xF8, 0x3F, 0x7E, 0x8F, 0x1F, 0xE3, 0xC7,
        0xF8, 0xF1, 0x7E, 0xFC, 0x1F, 0x3F, 0xC7, 0x8F, 0xF1, 0xE3, 0xFC, 0xF8,
        0x3F, 0x7E, 0x8F, 0x1F, 0xE3, 0xC7, 0xF8, 0xF1, 0x7E, 0xFC, 0x1F, 0x3F,
        0xC7, 0x8F, 0xF1, 0xE3, 0xFC, 0xF8, 0x3F, 0x7E, 0x8F, 0x1F, 0xE3, 0xC7,
        0xF8, 0xF1, 0x7E, 0xFC, 0x1F, 0x3F, 0xC7, 0x8F, 0xF1, 0xE3, 0xFC, 0xF8,
        0x3F, 0x7E, 0x8F, 0x1F, 0xE3, 0xC7, 0xF8, 0xF1, 0x7E, 0xFC, 0x1F, 0x3F,
        0xC7, 0x8F, 0xF1, 0xE3, 0xFC, 0xF8, 0x3F, 0x7E, 0x8F, 0x1F, 0xE3, 0xC7,
        0xF8, 0xF1, 0x7E, 0xFC, 0x1F, 0x3F, 0xC7, 0x8F, 0xF1, 0xE3, 0xFC, 0xF8,
        0x3F, 0x7E, 0x8F, 0x1F, 0xE3, 0xC7, 0xF8, 0xF1, 0x7E, 0xFC, 0x1F, 0x3F,
        0xC7, 0x8F, 0xF1, 0xE3, 0xFC, 0xF8, 0x3F, 0x7E, 0x8F, 0x1F, 0xE3, 0xC7,
        0xF8, 0xF1, 0x7E, 0xFC, 0x1F, 0x3F, 0xC7, 0x8F, 0xF1, 0xE3, 0xFC, 0xF8,
        0x3F, 0x7E, 0x8F, 0x1F, 0xE3, 0xC7, 0x00, 0xF0
    };

    [Test]
    public void DecoderCreatesFrameExpectedLength()
    {
        // Data doesn't matter as frame length depends on size
        using var dataStream = new MemoryStream(EncodedBlackIFrame);
        var decoder = new MobiclipDecoder(256, 192);

        FrameYuv420 frame = decoder.DecodeFrame(dataStream);

        Assert.That(frame.PackedData.Length, Is.EqualTo(0x12000));
    }

    [Test]
    public void DecodingIFrameMovesEndData()
    {
        using var dataStream = new MemoryStream(EncodedBlackIFrame);
        var decoder = new MobiclipDecoder(256, 192);

        _ = decoder.DecodeFrame(dataStream);

        Assert.That(dataStream.Position, Is.EqualTo(dataStream.Length));
    }

    [Test]
    public void DecodeBlackIFrameHasCorrectChannels()
    {
        using var dataStream = new MemoryStream(EncodedBlackIFrame);
        var decoder = new MobiclipDecoder(256, 192);

        FrameYuv420 frame = decoder.DecodeFrame(dataStream);

        Assert.That(frame.ColorSpace, Is.EqualTo(YuvColorSpace.YCoCg));

        Assert.Multiple(() => {
            Assert.That(frame.PackedData[..0xC000].ToArray(), Has.All.InRange(0, 1));
            Assert.That(frame.PackedData[0xC000..0xF000].ToArray(), Has.All.EqualTo(0x80));
            Assert.That(frame.PackedData[0xF000..].ToArray(), Has.All.InRange(0x7F, 0x80));
        });
    }
}