namespace PlayMobic.Tests.Video;

using PlayMobic.Video;

[TestFixture]
internal class FramesBufferTest
{
    [Test]
    public void CurrentIsFirst()
    {
        var buffer = new FramesBuffer<FrameYuv420>(6, () => new FrameYuv420(256, 192));

        Assert.That(buffer.Current, Is.SameAs(buffer.Buffer[0]));
    }

    [Test]
    public void RotateShiftOne()
    {
        var buffer = new FramesBuffer<FrameYuv420>(3, () => new FrameYuv420(256, 192));
        var frame0 = buffer.Buffer[0];
        var frame1 = buffer.Buffer[1];

        buffer.Rotate();

        Assert.Multiple(() => {
            Assert.That(buffer.Buffer.Count, Is.EqualTo(3));
            Assert.That(buffer.Buffer[1], Is.SameAs(frame0));
            Assert.That(buffer.Buffer[2], Is.SameAs(frame1));
        });
    }

    [Test]
    public void RotateReuseLastAsCurrent()
    {
        var buffer = new FramesBuffer<FrameYuv420>(3, () => new FrameYuv420(256, 192));
        var current = buffer.Current;
        var last = buffer.Buffer[^1];

        buffer.Rotate();

        Assert.Multiple(() => {
            Assert.That(buffer.Buffer.Count, Is.EqualTo(3));
            Assert.That(buffer.Current, Is.SameAs(last));
            Assert.That(buffer.Buffer[0], Is.SameAs(last));

            Assert.That(buffer.Buffer[1], Is.SameAs(current));
        });
    }
}
