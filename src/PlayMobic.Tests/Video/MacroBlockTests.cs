namespace PlayMobic.Tests.Video;

using System.Drawing;
using PlayMobic.Video;

[TestFixture]
internal class MacroBlockTests
{
    [Test]
    public void ConstructorSetProperties()
    {
        var expectedY = new PixelBlock(new byte[0], 0, Rectangle.Empty, 1);
        var expectedU = new PixelBlock(new byte[0], 0, Rectangle.Empty, 2);
        var expectedV = new PixelBlock(new byte[0], 0, Rectangle.Empty, 3);

        var macroblock = new MacroBlock(expectedY, expectedU, expectedV);

        Assert.Multiple(() => {
            Assert.That(macroblock.Luma, Is.EqualTo(expectedY));
            Assert.That(macroblock.ChromaU, Is.EqualTo(expectedU));
            Assert.That(macroblock.ChromaV, Is.EqualTo(expectedV));
        });
    }
}
