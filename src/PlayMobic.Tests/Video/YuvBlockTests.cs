namespace PlayMobic.Tests.Video;

using System.Drawing;
using PlayMobic.Video;

[TestFixture]
internal class YuvBlockTests
{
    [Test]
    public void ConstructorSetProperties()
    {
        var expectedY = new ComponentBlock(new byte[0], 0, Rectangle.Empty, 1);
        var expectedU = new ComponentBlock(new byte[0], 0, Rectangle.Empty, 2);
        var expectedV = new ComponentBlock(new byte[0], 0, Rectangle.Empty, 3);

        var macroblock = new YuvBlock(expectedY, expectedU, expectedV);

        Assert.Multiple(() => {
            Assert.That(macroblock.Luma, Is.EqualTo(expectedY));
            Assert.That(macroblock.ChromaU, Is.EqualTo(expectedU));
            Assert.That(macroblock.ChromaV, Is.EqualTo(expectedV));
        });
    }
}
