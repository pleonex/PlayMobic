namespace PlayMobic.Tests.IO;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayMobic.IO;
using Yarhl.IO;

[TestFixture]
internal class BitReaderTests
{
    [Test]
    public void TestDefaultConstructorValues()
    {
        using Stream stream = CreateStream(0xCa, 0xfe);

        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        Assert.Multiple(() => {
            Assert.That(reader.Stream, Is.SameAs(stream));
            Assert.That(reader.Endianness, Is.EqualTo(EndiannessMode.LittleEndian));
        });
    }

    [Test]
    public void ConstructorCreatesDataStream()
    {
        using var stream = new MemoryStream();

        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        Assert.That(reader.Stream.BaseStream, Is.SameAs(stream));
    }

    [Test]
    public void ConstructorWithInvalidArguments()
    {
        Assert.That(
            () => new BitReader(null!, EndiannessMode.LittleEndian),
            Throws.ArgumentNullException);
    }

    [Test]
    public void ReadConsumesExpectedByte()
    {
        using Stream stream = CreateStream(0xCA, 0xFE);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        _ = reader.Read(2);
        Assert.That(stream.Position, Is.EqualTo(1));
    }

    [Test]
    public void ReadConsumesExpectedByte16()
    {
        using Stream stream = CreateStream(0xFE, 0xCA, 0xC0, 0xC0);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian, 16);

        _ = reader.Read(2);
        Assert.That(stream.Position, Is.EqualTo(2));
    }

    [Test]
    public void ReadWithSmallerBufferConsumesMoreBytes()
    {
        using Stream stream = CreateStream(0xCA, 0xFE);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        _ = reader.Read(2);
        Assert.That(stream.Position, Is.EqualTo(1));

        _ = reader.Read(10);
        Assert.That(stream.Position, Is.EqualTo(2));
    }

    [Test]
    public void ReadWithSmallerBufferConsumesMoreBytes16()
    {
        using Stream stream = CreateStream(0xCA, 0xFE, 0xC0, 0xC0, 0xA0);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian, 16);

        _ = reader.Read(10);
        Assert.That(stream.Position, Is.EqualTo(2));

        _ = reader.Read(10);
        Assert.That(stream.Position, Is.EqualTo(4));
    }

    [Test]
    public void ReadWithBiggerBufferDoNotConsume()
    {
        using Stream stream = CreateStream(0xCA, 0xFE);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        _ = reader.Read(2);
        Assert.That(stream.Position, Is.EqualTo(1));

        _ = reader.Read(2);
        Assert.That(stream.Position, Is.EqualTo(1));
    }

    [Test]
    public void ReadIncrementsPosition()
    {
        using Stream stream = CreateStream(0xCA, 0xFE, 0xC0, 0xC0);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian, 16);
        Assert.That(reader.BitPosition, Is.EqualTo(0));

        _ = reader.Read(10);
        Assert.That(reader.BitPosition, Is.EqualTo(10));

        _ = reader.Read(15);
        Assert.That(reader.BitPosition, Is.EqualTo(25));
    }

    [Test]
    public void ReadEndOfStreamThrows()
    {
        using Stream stream = CreateStream();
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        Assert.That(() => reader.Read(1), Throws.InstanceOf<EndOfStreamException>());
    }

    [Test(Description = "Testing if it reads well values in buffer")]
    public void ReadInside8BitsBlockIsCorrectLE()
    {
        using Stream stream = CreateStream(0xCA, 0xFE);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        int actual1 = reader.Read(3);
        int actual2 = reader.Read(2);
        int actual3 = reader.Read(3);

        Assert.Multiple(() => {
            Assert.That(actual1, Is.EqualTo(6), "First");
            Assert.That(actual2, Is.EqualTo(1), "Second");
            Assert.That(actual3, Is.EqualTo(2), "Third");
            Assert.That(stream.Position, Is.EqualTo(1), "Position");
        });
    }

    [Test]
    public void ReadInside16BitsBlockIsCorrectLE()
    {
        using Stream stream = CreateStream(0xFB, 0xCA);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian, 16);

        int actual1 = reader.Read(3);
        int actual2 = reader.Read(10);
        int actual3 = reader.Read(3);

        Assert.Multiple(() => {
            Assert.That(actual1, Is.EqualTo(6), "First");
            Assert.That(actual2, Is.EqualTo(0x15F), "Second");
            Assert.That(actual3, Is.EqualTo(3), "Third");
            Assert.That(stream.Position, Is.EqualTo(2), "Position");
        });
    }

    [Test(Description = "Testing if it mixes well bytes from stream")]
    public void ReadCrossBlockSizeBoundaryIsCorrectLE()
    {
        using Stream stream = CreateStream(0xCA, 0xFB, 0xFF);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        int actual1 = reader.Read(3);
        int actual2 = reader.Read(10);
        int actual3 = reader.Read(3);

        Assert.Multiple(() => {
            Assert.That(actual1, Is.EqualTo(6), "First");
            Assert.That(actual2, Is.EqualTo(0x15F), "Second");
            Assert.That(actual3, Is.EqualTo(3), "Third");
            Assert.That(stream.Position, Is.EqualTo(2), "Position");
        });
    }

    [Test]
    public void ReadCross16BlockSizeBoundaryIsCorrectLE()
    {
        using Stream stream = CreateStream(0xC1, 0xC0, 0x00, 0xA0);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian, 16);

        int actual1 = reader.Read(15);
        int actual2 = reader.Read(5);

        Assert.Multiple(() => {
            Assert.That(actual1, Is.EqualTo(0x6060), "First");
            Assert.That(actual2, Is.EqualTo(0x1A), "Second");
            Assert.That(stream.Position, Is.EqualTo(4), "Position");
        });
    }

    [Test]
    public void ReadSigned()
    {
        using Stream stream = CreateStream(0xF9, 0xA7, 0x90);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        int actual1 = reader.ReadSigned(12);
        int actual2 = reader.ReadSigned(4);
        int actual3 = reader.ReadSigned(4);

        Assert.Multiple(() => {
            Assert.That(actual1, Is.EqualTo(-102), "First");
            Assert.That(actual2, Is.EqualTo(7), "Second");
            Assert.That(actual3, Is.EqualTo(-7), "Third");
        });
    }

    [Test]
    public void ReadBooleanIsCorrectAnd1Bit()
    {
        using Stream stream = CreateStream(0x02);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        _ = reader.Read(6);
        bool actual1 = reader.ReadBoolean();
        bool actual2 = reader.ReadBoolean();

        Assert.Multiple(() => {
            Assert.That(actual1, Is.True, "First");
            Assert.That(actual2, Is.False, "Second");
            Assert.That(stream.Position, Is.EqualTo(1), "Position");
        });
    }

    [Test]
    public void ReadEliasGammaCode()
    {
        using Stream stream = CreateStream(0b1_010_011_0, 0b00100_000, 0b00101_000, 0b00001000, 0b1_0000000);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        long actual1 = reader.ReadEliasGammaCode();
        long actual2 = reader.ReadEliasGammaCode();
        long actual3 = reader.ReadEliasGammaCode();
        _ = reader.Read(1);

        long actual4 = reader.ReadEliasGammaCode();
        _ = reader.Read(3);

        long actual5 = reader.ReadEliasGammaCode();
        _ = reader.Read(3);

        long actual17 = reader.ReadEliasGammaCode();

        Assert.Multiple(() => {
            Assert.That(actual1, Is.EqualTo(1));
            Assert.That(actual2, Is.EqualTo(2));
            Assert.That(actual3, Is.EqualTo(3));
            Assert.That(actual4, Is.EqualTo(4));
            Assert.That(actual5, Is.EqualTo(5));
            Assert.That(actual17, Is.EqualTo(17));
        });
    }

    [Test]
    public void ReadExpGolomb()
    {
        using Stream stream = CreateStream(0b1_010_011_0, 0b00111_000, 0b0001001_0);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        long actual0 = reader.ReadExpGolomb();
        long actual1 = reader.ReadExpGolomb();
        long actual2 = reader.ReadExpGolomb();
        _ = reader.Read(1);

        long actual6 = reader.ReadExpGolomb();
        _ = reader.Read(3);

        long actual8 = reader.ReadExpGolomb();

        Assert.Multiple(() => {
            Assert.That(actual0, Is.EqualTo(0));
            Assert.That(actual1, Is.EqualTo(1));
            Assert.That(actual2, Is.EqualTo(2));
            Assert.That(actual6, Is.EqualTo(6));
            Assert.That(actual8, Is.EqualTo(8));
        });
    }

    [Test]
    public void ReadExpGolombSigned()
    {
        using Stream stream = CreateStream(0b1_010_011_0, 0b0001000_0, 0b0001001_0);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);

        long actual0 = reader.ReadExpGolombSigned();
        long actual1 = reader.ReadExpGolombSigned();
        long actualM1 = reader.ReadExpGolombSigned();
        _ = reader.Read(1);

        long actual4 = reader.ReadExpGolombSigned();
        _ = reader.Read(1);

        long actualM4 = reader.ReadExpGolombSigned();

        Assert.Multiple(() => {
            Assert.That(actual0, Is.EqualTo(0));
            Assert.That(actual1, Is.EqualTo(1));
            Assert.That(actualM1, Is.EqualTo(-1));
            Assert.That(actual4, Is.EqualTo(4));
            Assert.That(actualM4, Is.EqualTo(-4));
        });
    }

    private static Stream CreateStream(params byte[] data) =>
        DataStreamFactory.FromArray(data);
}
