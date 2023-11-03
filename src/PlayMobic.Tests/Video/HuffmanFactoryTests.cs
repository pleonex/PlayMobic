namespace PlayMobic.Tests.Video;
using PlayMobic.Video.Mobiclip;

[TestFixture]
public class HuffmanFactoryTests
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
        string name = typeof(Huffman).Namespace + $".huffman_residual_table{tableIdx}.bin";
        var huffman = HuffmanFactory.CreateFromResidualTable(name);

        HuffmanCodeword codeword = huffman.GetCodeword(value);
        Assert.Multiple(() => {
            Assert.That(codeword.Value, Is.EqualTo(value));
            Assert.That(codeword.Code, Is.EqualTo(expectedCode));
            Assert.That(codeword.BitCount, Is.EqualTo(expectedBitCount));
        });
    }
}
