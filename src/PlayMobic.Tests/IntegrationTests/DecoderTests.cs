namespace PlayMobic.Tests.IntegrationTests;

using PlayMobic.Containers.Mods;
using PlayMobic.Containers;
using PlayMobic.Video;
using PlayMobic.Video.Mobiclip;
using Yarhl.FileSystem;
using Yarhl.IO;

[TestFixture]
public class DecoderTests
{
    public static IEnumerable<TestCaseData> GetKeyFrameArgs()
    {
        string programDir = AppDomain.CurrentDomain.BaseDirectory;
        string path = Path.GetFullPath(Path.Combine(programDir, "Resources"));

        var vpypA = new DirectoryInfo(Path.Combine(path, "vpyp_a"));
        if (vpypA.Exists) {
            foreach (FileInfo encFrame in vpypA.EnumerateFiles("*.enc")) {
                string frameName = Path.GetFileNameWithoutExtension(encFrame.Name);
                string decFrame = Path.Combine(encFrame.DirectoryName!, frameName + ".dec");
                yield return new TestCaseData(encFrame.FullName, decFrame, 256, 192) {
                    TestName = $"{nameof(DecodeIdenticalKeyFrames)}(vpyp_a/{frameName})",
                };
            }
        }
    }

    public static IEnumerable<TestCaseData> GetVideoStreamArgs()
    {
        string programDir = AppDomain.CurrentDomain.BaseDirectory;
        string path = Path.GetFullPath(Path.Combine(programDir, "Resources"));

        string vpypAVideo = Path.Combine(path, "vpyp_a.rawvideo");
        string vpypAMods = Path.Combine(path, "vpyp_a.mods");
        if (File.Exists(vpypAVideo) && File.Exists(vpypAMods)) {
            yield return new TestCaseData(vpypAMods, vpypAVideo) {
                TestName = $"{nameof(DecodeIdenticalRawVideoStream)}(vpyp_a)",
            };
        }
    }

    [TestCaseSource(nameof(GetKeyFrameArgs))]
    public void DecodeIdenticalKeyFrames(string encodedPath, string decodedPath, int width, int height)
    {
        using var encodedStream = DataStreamFactory.FromFile(encodedPath, FileOpenMode.Read);
        var decoder = new MobiclipDecoder(width, height);

        FrameYuv420 actualFrame = decoder.DecodeFrame(encodedStream);

        Assert.That(encodedStream.Position, Is.EqualTo(encodedStream.Length), "Input stream position should be at the end");

        using var expectedDecoded = DataStreamFactory.FromFile(decodedPath, FileOpenMode.Read);
        using var actualDecoded = DataStreamFactory.FromArray(actualFrame.PackedData.ToArray());
        Assert.That(actualFrame.PackedData.Length, Is.EqualTo(expectedDecoded.Length), "Decoded length should match");
        Assert.That(expectedDecoded.Compare(actualDecoded), Is.True, "Decoded data should be identical");
    }

    [TestCaseSource(nameof(GetVideoStreamArgs))]
    public void DecodeIdenticalRawVideoStream(string containerPath, string expectedVideoPath)
    {
        using DataStream expectedVideoStream = DataStreamFactory.FromFile(expectedVideoPath, FileOpenMode.Read);
        using DataStream actualVideoStream = DataStreamFactory.FromMemory();

        using Node videoNode = NodeFactory.FromFile(containerPath, FileOpenMode.Read)
            .TransformWith<Binary2Mods>();
        ModsVideo video = videoNode.GetFormatAs<ModsVideo>()!;
        ModsInfo info = video.Info;

        var videoDecoder = new MobiclipDecoder(info.Width, info.Height);
        var demuxer = new ModsDemuxer(video);
        foreach (MediaPacket framePacket in demuxer.ReadFrames().OfType<VideoPacket>()) {
            FrameYuv420 frame = videoDecoder.DecodeFrame(framePacket.Data);
            actualVideoStream.Write(frame.PackedData);
        }

        Assert.That(actualVideoStream.Length, Is.EqualTo(expectedVideoStream.Length), "Stream length must match");
        Assert.That(actualVideoStream.Compare(expectedVideoStream), Is.True, "Content must match");
    }
}
