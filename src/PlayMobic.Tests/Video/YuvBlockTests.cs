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
#pragma warning disable NUnit2010 // Bug in NUnit with Is.EqualTo
            Assert.That(macroblock.Luma.Equals(expectedY), Is.True);
            Assert.That(macroblock.ChromaU.Equals(expectedU), Is.True);
            Assert.That(macroblock.ChromaV.Equals(expectedV), Is.True);
#pragma warning restore NUnit2010
        });
    }
}
