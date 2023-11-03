namespace PlayMobic.Tests.Video;

using System.Collections;
using PlayMobic.IO;
using PlayMobic.Video.Mobiclip;
using Yarhl.IO;

[TestFixture]
internal class HuffmanTests
{
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
        var huffman = HuffmanFactory.CreateFromResidualTable(typeof(Huffman).Namespace + ".huffman_residual_table0.bin");

        int actualValue = huffman.ReadCodeword(reader);

        Assert.That(actualValue, Is.EqualTo(expectedValue));
    }
}
