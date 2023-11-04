namespace PlayMobic.Tests.Video;
using PlayMobic.Video.Mobiclip;

[TestFixture]
public class HuffmanFactoryTests
{
    [Test]
    [TestCase(0, 0x000, 0x03, 7)]
    [TestCase(0, 0x001, 0x02, 2)]
    [TestCase(0, 0x002, 0x0F, 4)]
    [TestCase(0, 0x021, 0x06, 3)]
    [TestCase(0, 0x081, 0x0C, 5)]
    [TestCase(0, 0x801, 0x07, 4)]
    [TestCase(0, 0x802, 0x19, 9)]
    [TestCase(0, 0x821, 0x0F, 6)]
    [TestCase(0, 0x841, 0x0E, 6)]
    [TestCase(0, 0x8E1, 0x11, 7)]
    [TestCase(1, 0x000, 0x03, 7)]
    [TestCase(1, 0x801, 0x07, 4)]
    public void TreeFromFullIndexTable(int tableIdx, int value, int expectedCode, int expectedBitCount)
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

    [Test]
    public void TreeFromSymbolAndCountLists32()
    {
        byte[] symbols = {
            1, 1, 1, 1, 1, 1, 1, 1, 8, 8, 8, 8, 9, 9, 9, 9,
            4, 3, 2, 2, 7, 7, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0
        };
        byte[] counts = { 2, 2, 4, 5, 5, 5, 5, 4, 3, 3 };

        Huffman huffman = HuffmanFactory.CreateFromSymbolsAndCountLists(symbols, counts);

        Assert.Multiple(() => {
            var codeword = huffman.GetCodeword(0);
            Assert.That(codeword.Code, Is.EqualTo(3));
            Assert.That(codeword.BitCount, Is.EqualTo(2));
        });

        Assert.Multiple(() => {
            var codeword = huffman.GetCodeword(1);
            Assert.That(codeword.Code, Is.EqualTo(0));
            Assert.That(codeword.BitCount, Is.EqualTo(2));
        });
    }
}
