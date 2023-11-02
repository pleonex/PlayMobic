namespace PlayMobic.Tests.Video;

using System.Buffers;
using System.Drawing;
using PlayMobic.IO;
using PlayMobic.Video;
using PlayMobic.Video.Mobiclip;
using Yarhl.FileSystem;
using Yarhl.IO;

[TestFixture]
public class IntraDecoderBlockPredictionTests
{
    // Included for visual aid.
    // string.Join(", ", Enumerable.Range(0, 16 * 16).Select(x => $"0x{x:X2}"))
    private static readonly byte[] Block16x16 = new byte[16 * 16] {
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
        0x40, 0x41, 0x42, 0x43,  0x44, 0x45, 0x46, 0x47,  0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
        0x50, 0x51, 0x52, 0x53,  0x54, 0x55, 0x56, 0x57,  0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
        0x60, 0x61, 0x62, 0x63,  0x64, 0x65, 0x66, 0x67,  0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
        0x70, 0x71, 0x72, 0x73,  0x74, 0x75, 0x76, 0x77,  0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
        0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
        0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
        0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
        0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
        0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
        0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
        0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
        0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
    };

    [Test]
    public void ValidatePredictionVertical()
    {
        byte[] expectedData = new byte[4 * 4] {
            0x34, 0x35, 0x36, 0x37,
            0x34, 0x35, 0x36, 0x37,
            0x34, 0x35, 0x36, 0x37,
            0x34, 0x35, 0x36, 0x37,
        };

        AssertPredictionMode(expectedData, IntraPredictionBlockMode.Vertical);
    }

    [Test]
    public void ValidatePredictionHorizontal()
    {
        byte[] expectedData = new byte[4 * 4] {
            0x43, 0x43, 0x43, 0x43,
            0x53, 0x53, 0x53, 0x53,
            0x63, 0x63, 0x63, 0x63,
            0x73, 0x73, 0x73, 0x73,
        };

        AssertPredictionMode(expectedData, IntraPredictionBlockMode.Horizontal);
    }

    [Test]
    [TestCase(0x80, new byte[] { 0x42, 0x41, 0x40, 0x3F, 0x50, 0x4D, 0x49, 0x46, 0x5E, 0x58, 0x53, 0x4E, 0x6C, 0x64, 0x5D, 0x55 })]
    [TestCase(0x10, new byte[] { 0x42, 0x42, 0x41, 0x41, 0x51, 0x4F, 0x4C, 0x4A, 0x5F, 0x5B, 0x57, 0x54, 0x6E, 0x68, 0x63, 0x5D })]
    [TestCase(0x12, new byte[] { 0x41, 0x40, 0x3E, 0x3D, 0x4F, 0x4B, 0x46, 0x42, 0x5C, 0x55, 0x4E, 0x48, 0x6A, 0x60, 0x57, 0x4D })]
    public void ValidatePredictionPlane4x4Delta(byte encodedDelta, byte[] expected)
    {
        // Tested with 0, 4 and -4
        AssertPredictionMode(expected, IntraPredictionBlockMode.DeltaPlane, new byte[] { 0x00, encodedDelta });
    }

    [Test]
    public void ValidatePredictionPlane8x8Delta()
    {
        byte[] expected = new byte[8 * 8] {
            0x95, 0x85, 0x85, 0xB9, 0x99, 0x6D, 0x2E, 0x2A,
            0x4B, 0x46, 0x4F, 0x85, 0x72, 0x55, 0x27, 0x2D,
            0x5C, 0x55, 0x5A, 0x84, 0x71, 0x56, 0x2D, 0x2F,
            0x45, 0x42, 0x48, 0x6C, 0x60, 0x4D, 0x2E, 0x32,
            0x63, 0x5C, 0x5C, 0x72, 0x64, 0x52, 0x36, 0x35,
            0x96, 0x88, 0x7F, 0x85, 0x73, 0x5D, 0x42, 0x38,
            0x6E, 0x67, 0x62, 0x64, 0x5A, 0x4E, 0x40, 0x3A,
            0x57, 0x54, 0x50, 0x4C, 0x48, 0x45, 0x41, 0x3D,
        };

        // Tested with delta -2
        IntraDecoderBlockPrediction decoder = CreateDecoder(0x00, 0x28);
        var expectedBlock = new PixelBlock(expected, 8, new Rectangle(0, 0, 8, 8), 0);

        byte[] frame = new byte[256 * 192];
        new Random(42).NextBytes(frame);
        var block = new PixelBlock(frame, 256, new Rectangle(8, 8, 8, 8), 3);
        decoder.PerformBlockPrediction(block, IntraPredictionBlockMode.DeltaPlane);

        Assert.Multiple(() => {
            foreach ((int x, int y) in block.Iterate()) {
                Assert.That(block[x, y], Is.EqualTo(expectedBlock[x, y]));
            }
        });
    }

    [Test]
    public void ValidatePredictionPlane16x16Delta()
    {
        byte[] expected = new byte[16 * 16] {
            0xDD, 0x95, 0x8A, 0x9B, 0x69, 0x70, 0x71, 0x9A, 0x93, 0xAE, 0x72, 0xA2, 0x8D, 0x66, 0x73, 0x3C,
            0x91, 0x52, 0x4D, 0x61, 0x37, 0x43, 0x48, 0x73, 0x71, 0x90, 0x5D, 0x8E, 0x7F, 0x60, 0x70, 0x42,
            0xB2, 0x75, 0x6E, 0x7F, 0x56, 0x5E, 0x61, 0x87, 0x83, 0x9D, 0x6C, 0x97, 0x87, 0x68, 0x75, 0x48,
            0xD4, 0x9A, 0x91, 0x9E, 0x75, 0x7B, 0x7B, 0x9C, 0x96, 0xAB, 0x7B, 0xA1, 0x90, 0x70, 0x7A, 0x4E,
            0x72, 0x42, 0x40, 0x53, 0x34, 0x3F, 0x45, 0x6A, 0x6A, 0x84, 0x5E, 0x87, 0x7E, 0x67, 0x76, 0x54,
            0x98, 0x6A, 0x66, 0x74, 0x56, 0x5E, 0x61, 0x80, 0x7E, 0x94, 0x6F, 0x92, 0x87, 0x70, 0x7B, 0x5A,
            0xB8, 0x8D, 0x87, 0x92, 0x74, 0x79, 0x7A, 0x94, 0x90, 0xA1, 0x7E, 0x9B, 0x8F, 0x78, 0x80, 0x60,
            0xB7, 0x90, 0x8B, 0x94, 0x7A, 0x7E, 0x7F, 0x95, 0x92, 0xA1, 0x81, 0x9B, 0x90, 0x7C, 0x83, 0x66,
            0xB6, 0x94, 0x8F, 0x97, 0x80, 0x83, 0x84, 0x97, 0x94, 0xA1, 0x85, 0x9B, 0x91, 0x7F, 0x85, 0x6C,
            0x8D, 0x73, 0x71, 0x7A, 0x69, 0x6E, 0x71, 0x84, 0x83, 0x91, 0x7B, 0x91, 0x8B, 0x7E, 0x85, 0x72,
            0x7B, 0x67, 0x66, 0x6F, 0x62, 0x68, 0x6B, 0x7C, 0x7D, 0x8A, 0x79, 0x8C, 0x89, 0x7F, 0x87, 0x78,
            0xC8, 0xB2, 0xAD, 0xB0, 0xA0, 0xA0, 0x9E, 0xA7, 0xA3, 0xA9, 0x97, 0xA1, 0x9A, 0x8D, 0x8F, 0x7E,
            0xA1, 0x93, 0x91, 0x94, 0x8B, 0x8C, 0x8D, 0x95, 0x94, 0x9A, 0x8E, 0x98, 0x94, 0x8C, 0x8F, 0x84,
            0x8B, 0x82, 0x82, 0x86, 0x81, 0x83, 0x84, 0x8B, 0x8C, 0x91, 0x8A, 0x92, 0x91, 0x8D, 0x90, 0x8A,
            0x84, 0x81, 0x82, 0x84, 0x83, 0x84, 0x86, 0x8A, 0x8B, 0x8F, 0x8C, 0x91, 0x91, 0x90, 0x92, 0x90,
            0xE7, 0xE1, 0xDC, 0xD7, 0xD1, 0xCC, 0xC6, 0xC1, 0xBC, 0xB6, 0xB1, 0xAC, 0xA6, 0xA1, 0x9B, 0x96,
        };

        // Tested with delta +2
        IntraDecoderBlockPrediction decoder = CreateDecoder(0x00, 0x20);
        var expectedBlock = new PixelBlock(expected, 16, new Rectangle(0, 0, 16, 16), 0);

        byte[] frame = new byte[256 * 192];
        new Random(42).NextBytes(frame);
        var block = new PixelBlock(frame, 256, new Rectangle(16, 16, 16, 16), 17);
        decoder.PerformBlockPrediction(block, IntraPredictionBlockMode.DeltaPlane);

        Assert.Multiple(() => {
            foreach ((int x, int y) in block.Iterate()) {
                Assert.That(block[x, y], Is.EqualTo(expectedBlock[x, y]));
            }
        });
    }

    [Test]
    public void ValidatePredictionDcFullAverage()
    {
        byte[] expected = new byte[4 * 4] {
            0x48, 0x48, 0x48, 0x48,
            0x48, 0x48, 0x48, 0x48,
            0x48, 0x48, 0x48, 0x48,
            0x48, 0x48, 0x48, 0x48,
        };
        AssertPredictionMode(expected, IntraPredictionBlockMode.DC);
    }

    [Test]
    public void ValidatePredictionDcLeftAndTopAverage()
    {
        byte[] expectedLeft = new byte[4 * 4] {
            0x1B, 0x1B, 0x1B, 0x1B,
            0x1B, 0x1B, 0x1B, 0x1B,
            0x1B, 0x1B, 0x1B, 0x1B,
            0x1B, 0x1B, 0x1B, 0x1B,
        };
        AssertPredictionMode(expectedLeft, IntraPredictionBlockMode.DC, 4, 0);

        byte[] expectedTop = new byte[4 * 4] {
            0x32, 0x32, 0x32, 0x32,
            0x32, 0x32, 0x32, 0x32,
            0x32, 0x32, 0x32, 0x32,
            0x32, 0x32, 0x32, 0x32,
        };
        AssertPredictionMode(expectedTop, IntraPredictionBlockMode.DC, 0, 4);
    }

    [Test]
    public void ValidatePredictionDcNoAverage()
    {
        byte[] expected = new byte[4 * 4] {
            0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80,
        };
        AssertPredictionMode(expected, IntraPredictionBlockMode.DC, 0, 0);
    }

    [Test]
    public void ValidatePredictionHorizontalUp()
    {
        byte[] expected = new byte[4 * 4] {
            0x4B, 0x53, 0x5B, 0x63,
            0x5B, 0x63, 0x6B, 0x6F,
            0x6B, 0x6F, 0x73, 0x73,
            0x73, 0x73, 0x73, 0x73,
        };
        AssertPredictionMode(expected, IntraPredictionBlockMode.HorizontalUp);
    }

    [Test]
    public void ValidatePredictionHorizontalDown()
    {
        byte[] expected = new byte[4 * 4] {
            0x3B, 0x37, 0x34, 0x35,
            0x4B, 0x43, 0x3B, 0x37,
            0x5B, 0x53, 0x4B, 0x43,
            0x6B, 0x63, 0x5B, 0x53,
        };
        AssertPredictionMode(expected, IntraPredictionBlockMode.HorizontalDown);
    }

    [Test]
    public void ValidatePredictionVerticalRight()
    {
        byte[] expected = new byte[4 * 4] {
            0x34, 0x35, 0x36, 0x37,
            0x37, 0x34, 0x35, 0x36,
            0x43, 0x34, 0x35, 0x36,
            0x53, 0x37, 0x34, 0x35,
        };
        AssertPredictionMode(expected, IntraPredictionBlockMode.VerticalRight);
    }

    [Test]
    public void ValidatePredictionDiagonalDownRight()
    {
        byte[] expected = new byte[4 * 4] {
            0x37, 0x34, 0x35, 0x36,
            0x43, 0x37, 0x34, 0x35,
            0x53, 0x43, 0x37, 0x34,
            0x63, 0x53, 0x43, 0x37,
        };
        AssertPredictionMode(expected, IntraPredictionBlockMode.DiagonalDownRight);
    }

    [Test]
    public void ValidatePredictionVerticalLeft()
    {
        byte[] expected = new byte[4 * 4] {
            0x35, 0x36, 0x37, 0x38,
            0x35, 0x36, 0x37, 0x38,
            0x36, 0x37, 0x38, 0x39,
            0x36, 0x37, 0x38, 0x39,
        };
        AssertPredictionMode(expected, IntraPredictionBlockMode.VerticalLeft);
    }

    [Test]
    public void ValidateSkipPrediction()
    {
        byte[] expected = new byte[4 * 4];
        AssertPredictionMode(expected, IntraPredictionBlockMode.Nothing);
    }

    [Test]
    public void ValidateModePrediction4x4InsideBlock()
    {
        // note: bytes are reversed due to LE.
        var decoder = CreateDecoder(0b101_1_1_1_1_0, 0b1_1_0010_1_0, 0x00, 0b011_00000);

        // start with DC
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(0, 0, 0)),
            Is.EqualTo(IntraPredictionBlockMode.DC));

        // to the right copies corner
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(4, 0, 1)),
            Is.EqualTo(IntraPredictionBlockMode.DC));

        // to the right again copies left but we correct (smaller)
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(8, 0, 2)),
            Is.EqualTo(IntraPredictionBlockMode.DeltaPlane));

        // to the right again copies corrected value
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(12, 0, 3)),
            Is.EqualTo(IntraPredictionBlockMode.DeltaPlane));

        // first bottom copies top but correct to bigger
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(0, 4, 4)),
            Is.EqualTo(IntraPredictionBlockMode.VerticalRight));

        // second bottom does min top (DC) and left (VerticalRight)
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(4, 4, 5)),
            Is.EqualTo(IntraPredictionBlockMode.DC));

        // third bottom does min top (Delta) and left (DC) -> delta
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(8, 4, 6)),
            Is.EqualTo(IntraPredictionBlockMode.DeltaPlane));

        // forth min top (delta) and left (delta)
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(12, 0, 7)),
            Is.EqualTo(IntraPredictionBlockMode.DeltaPlane));

        // second bottom copies top (vertical right)
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(0, 8, 8)),
            Is.EqualTo(IntraPredictionBlockMode.VerticalRight));

        // next min left (vertical right) and top (dc) correct with equal -> +1
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(4, 8, 9)),
            Is.EqualTo(IntraPredictionBlockMode.HorizontalUp));
    }

    [Test]
    public void ValidateModePrediction8x8()
    {
        // note: bytes are reversed due to LE.
        var decoder = CreateDecoder(0b10_1_1_1_000, 0b1_0011_1_00);

        // (0, 0) start with DC
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(0, 0, 0)),
            Is.EqualTo(IntraPredictionBlockMode.DC));

        // (0, 0) again but changing bigger
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(0, 0, 0)),
            Is.EqualTo(IntraPredictionBlockMode.HorizontalUp));

        // (8, 0) top blocks copies left (horizontal up)
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(8, 0, 1)),
            Is.EqualTo(IntraPredictionBlockMode.HorizontalUp));

        // (8, 0) again but changing (smaller)
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(8, 0, 1)),
            Is.EqualTo(IntraPredictionBlockMode.DeltaPlane));

        // (0, 8) bottom copies top (horizontal up)
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(0, 8, 2)),
            Is.EqualTo(IntraPredictionBlockMode.HorizontalUp));

        // (8, 8) does min left (horizontal up) and top (delta plane)
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(8, 8, 3)),
            Is.EqualTo(IntraPredictionBlockMode.DeltaPlane));

        // (0,0) for next block restart
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(0, 0, 0)),
            Is.EqualTo(IntraPredictionBlockMode.DC));
    }

    [Test]
    public void ModePrediction4x4TakesAbove8x8FromMacroblock()
    {
        // note: bytes are reversed due to LE.
        var decoder = CreateDecoder(0b11_1_00000, 0b0110_1_1_01);

        // Fill mode predictions from first two 8x8 blocks (first row (0,0) -> (8, 0))
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(0, 0, 0)),
            Is.EqualTo(IntraPredictionBlockMode.DiagonalDownRight));
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(8, 0, 1)),
            Is.EqualTo(IntraPredictionBlockMode.DiagonalDownRight));

        // Second row uses blocks 4x4 and it takes mode prediction from above block 8x8
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(0, 8, 0)),
            Is.EqualTo(IntraPredictionBlockMode.DiagonalDownRight));

        // again but changing to check min later
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(0, 8, 0)),
            Is.EqualTo(IntraPredictionBlockMode.VerticalLeft));

        // min left (8 vertical left) and top (7 diagonal down rigt)
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(4, 8, 1)),
            Is.EqualTo(IntraPredictionBlockMode.DiagonalDownRight));
    }

    [Test]
    public void ModePrediction4x4TakesAboveLeft4x4FromMacroblock()
    {
        // note: bytes are reversed due to LE.
        var decoder = CreateDecoder(0b1_0000_111, 0b0001_1_010, 0xFF, 0xFF);

        // Quick fill left and top side of target block (8, 8)
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(4, 8, 1)),
            Is.EqualTo(IntraPredictionBlockMode.DeltaPlane));
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(4, 12, 3)),
            Is.EqualTo(IntraPredictionBlockMode.Vertical));
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(8, 4, 2)),
            Is.EqualTo(IntraPredictionBlockMode.VerticalRight));
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(12, 4, 3)),
            Is.EqualTo(IntraPredictionBlockMode.Horizontal));

        // From block 8x8 @ (8, 8)
        // Block 4x4 (0,0) = (8,8) -> min left(4,8)=2_DeltaPlane & top(8,4)=6_VerticalRight
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(8, 8, 0)),
            Is.EqualTo(IntraPredictionBlockMode.DeltaPlane));

        // Block 4x4 (4,0) = (12,8) -> min left(8,8)=2_DeltaPlane & top(12, 4)=1_Horizontal
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(12, 8, 1)),
            Is.EqualTo(IntraPredictionBlockMode.Horizontal));

        // Block 4x4 (0,4) = (8,12) -> min left(4, 12)=0_Vertical & top(8,8)=2_DeltaPlane
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(8, 12, 2)),
            Is.EqualTo(IntraPredictionBlockMode.Vertical));

        // Block 4x4 (4,4) = (12,12)
        Assert.That(
            decoder.DecodeBlockMode(Create4x4EmptyBlock(12, 12, 3)),
            Is.EqualTo(IntraPredictionBlockMode.Vertical));
    }

    [Test]
    public void ModePredictionResetPerMacroblock()
    {
        // note: bytes are reversed due to LE.
        var decoder = CreateDecoder(0x00, 0b0110_1_1_1_1);

        // Fill mode predictions
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(0, 0, 0)),
            Is.EqualTo(IntraPredictionBlockMode.DiagonalDownRight));
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(8, 0, 1)),
            Is.EqualTo(IntraPredictionBlockMode.DiagonalDownRight));
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(0, 8, 2)),
            Is.EqualTo(IntraPredictionBlockMode.DiagonalDownRight));
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(8, 8, 3)),
            Is.EqualTo(IntraPredictionBlockMode.DiagonalDownRight));

        // below macroblock resets
        Assert.That(
            decoder.DecodeBlockMode(Create8x8EmptyBlock(0, 16, 0)),
            Is.EqualTo(IntraPredictionBlockMode.DC));
    }

    [Test]
    public void PredictAndRun()
    {
        byte[] expected = new byte[4 * 4] {
            0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80,
        };

        IntraDecoderBlockPrediction decoder = CreateDecoder(0x00, 0x80);
        var expectedBlock = new PixelBlock(expected, 4, new Rectangle(0, 0, 4, 4), 0);

        var block = new PixelBlock(Block16x16.ToArray(), 16, new Rectangle(0, 0, 4, 4), 0);
        decoder.PerformBlockPrediction(block, IntraPredictionBlockMode.Predicted);

        Assert.Multiple(() => {
            foreach ((int x, int y) in block.Iterate()) {
                Assert.That(block[x, y], Is.EqualTo(expectedBlock[x, y]));
            }
        });
    }

    [Test]
    public void InvalidConstructorArgsThrow()
    {
        Assert.That(() => new IntraDecoderBlockPrediction(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void InvalidModeThrow()
    {
        var decoder = CreateDecoder();
        var block = Create4x4EmptyBlock(0, 0, 0);
        Assert.That(
            () => decoder.PerformBlockPrediction(block, (IntraPredictionBlockMode)100),
            Throws.InstanceOf<NotImplementedException>());
    }

    private static void AssertPredictionMode(byte[] expected, IntraPredictionBlockMode mode)
    {
        AssertPredictionMode(expected, mode, Array.Empty<byte>());
    }

    private static void AssertPredictionMode(byte[] expected, IntraPredictionBlockMode mode, byte[] data)
    {
        PixelBlock block = Create4x4EmptyBlock(4, 4, 5);
        IntraDecoderBlockPrediction decoder = CreateDecoder(data);
        var expectedBlock = new PixelBlock(expected, 4, new Rectangle(0, 0, 4, 4), 0);

        decoder.PerformBlockPrediction(block, mode);

        Assert.Multiple(() => {
            foreach ((int x, int y) in block.Iterate()) {
                Assert.That(block[x, y], Is.EqualTo(expectedBlock[x, y]));
            }
        });
    }

    private static void AssertPredictionMode(byte[] expected, IntraPredictionBlockMode mode, int sx, int sy)
    {
        PixelBlock block = Create4x4EmptyBlock(sx, sy, 0);
        IntraDecoderBlockPrediction decoder = CreateDecoder();
        var expectedBlock = new PixelBlock(expected, 4, new Rectangle(0, 0, 4, 4), 0);

        decoder.PerformBlockPrediction(block, mode);

        Assert.Multiple(() => {
            foreach ((int x, int y) in block.Iterate()) {
                Assert.That(block[x, y], Is.EqualTo(expectedBlock[x, y]));
            }
        });
    }

    private static IntraDecoderBlockPrediction CreateDecoder(params byte[] data)
    {
        var stream = new MemoryStream(data);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian, 16);
        return new IntraDecoderBlockPrediction(reader);
    }

    private static PixelBlock Create4x4EmptyBlock(int startX, int startY, int index)
    {
        // Clone to have the neighbors and then clean it.
        var block = new PixelBlock(Block16x16.ToArray(), 16, new Rectangle(startX, startY, 4, 4), index);

        foreach ((int x, int y) in block.Iterate()) {
            block[x, y] = 0;
        }

        return block;
    }

    private static PixelBlock Create8x8EmptyBlock(int startX, int startY, int index)
    {
        // Clone to have the neighbors and then clean it.
        var block = new PixelBlock(new byte[4 * 16 * 16], 16, new Rectangle(startX, startY, 8, 8), index);

        foreach ((int x, int y) in block.Iterate()) {
            block[x, y] = 0;
        }

        return block;
    }
}
