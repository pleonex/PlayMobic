namespace PlayMobic.Tests.Video;

using System.Collections;
using PlayMobic.IO;
using PlayMobic.Video.Mobiclip;
using Yarhl.IO;

[TestFixture]
internal class HuffmanTests
{
    [Test]
    [TestCase(0, 0x000, 0x03, 8)]
    [TestCase(0, 0x001, 0x02, 3)]
    [TestCase(0, 0x002, 0x0F, 5)]
    [TestCase(0, 0x021, 0x06, 4)]
    [TestCase(0, 0x081, 0x0C, 6)]
    [TestCase(0, 0x801, 0x07, 5)]
    [TestCase(0, 0x802, 0x19, 10)]
    [TestCase(0, 0x821, 0x0F, 7)]
    [TestCase(0, 0x841, 0x0E, 7)]
    [TestCase(0, 0x8E1, 0x11, 8)]
    [TestCase(1, 0x000, 0x03, 8)]
    [TestCase(1, 0x801, 0x07, 5)]
    public void TestTreeFromTable(int tableIdx, int value, int expectedCode, int expectedBitCount)
    {
        var huffman = Huffman.LoadFromFullIndexTable(tableIdx);

        (int actualCode, int actualBitCount) = huffman.GetCodeword(value);
        Assert.Multiple(() => {
            Assert.That(actualCode, Is.EqualTo(expectedCode));
            Assert.That(actualBitCount, Is.EqualTo(expectedBitCount));
        });
    }

    [Test]
    [TestCase(new byte[] { 0b0000011_0 }, 0x000)]
    [TestCase(new byte[] { 0b10_000000 }, 0x001)]
    [TestCase(new byte[] { 0b1111_0000 }, 0x002)]
    [TestCase(new byte[] { 0b110_00000 }, 0x021)]
    [TestCase(new byte[] { 0b00001100, 0b1_0000000 }, 0x802)]
    public void ReadCodewordBits(byte[] data, int expectedValue)
    {
        using DataStream stream = DataStreamFactory.FromArray(data);
        var reader = new BitReader(stream, EndiannessMode.LittleEndian);
        var huffman = Huffman.LoadFromFullIndexTable(0);

        int actualValue = huffman.ReadCodeword(reader);

        Assert.That(actualValue, Is.EqualTo(expectedValue));
    }
}
