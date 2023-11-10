namespace PlayMobic.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using PlayMobic.Containers;
using PlayMobic.Containers.Mods;
using PlayMobic.Video;
using PlayMobic.Video.Mobiclip;
using Texim.Colors;
using Texim.Formats;
using Texim.Images;
using Yarhl.IO;

internal sealed class VideoFrameDecoder : IDisposable
{
    private readonly ModsVideo video;
    private readonly MobiclipDecoder videoDecoder;
    private readonly ModsDemuxer demuxer;
    private readonly byte[] rgbFrame;

    private int currentFrame;
    private IEnumerator<MediaPacket> videoPackets;

    public VideoFrameDecoder(string modsPath)
    {
        using DataStream modsStream = DataStreamFactory.FromFile(modsPath, FileOpenMode.Read);
        using var binaryMods = new BinaryFormat(modsStream);
        video = new Binary2Mods().Convert(binaryMods);

        demuxer = new ModsDemuxer(video);
        videoDecoder = new MobiclipDecoder(video.Info.Width, video.Info.Height, isStereo: false);
        rgbFrame = new byte[video.Info.Width * video.Info.Height * 4];

        videoPackets = demuxer.ReadFrames(0).GetEnumerator();
    }

    public ModsInfo VideoInfo => video.Info;

    public int KeyFramesCount => video.KeyFramesInfo.Count;

    public Bitmap? FrameImage { get; private set; }

    public void Dispose()
    {
        video.Dispose();
        videoPackets?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void DecodeFrame(int frameIndex)
    {
        if (videoPackets is IDisposable disposable) {
            disposable.Dispose();
        }

        currentFrame = frameIndex;

        // P-frames needs a buffer of last 5 frames starting from an I-frame.
        // Find it and start decoding from there.
        int nearKeyFrame = video.KeyFramesInfo.Last(i => i.FrameNumber <= currentFrame).FrameNumber;
        videoPackets = demuxer.ReadFrames(nearKeyFrame).GetEnumerator();

        bool isTargetFrame = false;
        do {
            if (!videoPackets.MoveNext()) {
                return;
            }

            isTargetFrame = videoPackets.Current is VideoPacket videoPacket &&
                videoPacket.FrameCount == currentFrame;
            if (!isTargetFrame && videoPackets.Current is VideoPacket) {
                // decode frame so if it's P-frame it's added to the buffer
                _ = videoDecoder!.DecodeFrame(videoPackets.Current.Data);
            }
        } while (!isTargetFrame);

        DecodeCurrentFrame();
    }

    public void DecodeNextFrame()
    {
        do {
            if (!videoPackets.MoveNext()) {
                return;
            }
        } while (videoPackets.Current is not VideoPacket);

        DecodeCurrentFrame();
    }

    private void DecodeCurrentFrame()
    {
        if (videoPackets.Current is not VideoPacket packet) {
            return;
        }

        FrameYuv420 frame = videoDecoder.DecodeFrame(packet.Data);

        if (frame.ColorSpace is YuvColorSpace.YCoCg) {
            ColorSpaceConverter.YCoCg2Rgb32(frame, rgbFrame);
        } else {
            return; // not supported
        }

        var frameImage = new FullImage(frame.Width, frame.Height) {
            Pixels = Rgb32.Instance.Decode(rgbFrame),
        };
        using BinaryFormat bitmapStream = new FullImage2Bitmap().Convert(frameImage);

        bitmapStream.Stream.Position = 0;
        FrameImage = new Bitmap(bitmapStream.Stream);
    }
}
