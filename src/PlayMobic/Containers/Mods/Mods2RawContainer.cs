namespace PlayMobic.Containers.Mods;
using System;
using PlayMobic.Audio;
using PlayMobic.Video;
using PlayMobic.Video.Mobiclip;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

public class Mods2RawContainer : IConverter<ModsVideo, NodeContainerFormat>
{
    private readonly bool convertYCbCr;

    public Mods2RawContainer()
        : this(true)
    {
    }

    public Mods2RawContainer(bool convertYCbCr)
    {
        this.convertYCbCr = convertYCbCr;
    }

    public event EventHandler<int>? ProgressUpdate;

    public NodeContainerFormat Convert(ModsVideo source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var container = new NodeContainerFormat();
        var rawVideo = new BinaryFormat();
        container.Root.Add(new Node("video", rawVideo));

        var rawAudio = new BinaryFormat();
        container.Root.Add(new Node("audio", rawAudio));

        Decode(source, rawVideo.Stream, rawAudio.Stream);

        return container;
    }

    private void Decode(ModsVideo video, DataStream videoStream, DataStream audioStream)
    {
        ModsInfo info = video.Info;
        var videoDecoder = new MobiclipDecoder(info.Width, info.Height, isStereo: false);
        var audioDecoders = new IAudioDecoder[info.AudioChannelsCount];
        for (int i = 0; i < audioDecoders.Length; i++) {
            audioDecoders[i] = CreateAudioDecoder(info.AudioCodec);
        }

        var colorConvertedFrame = new FrameYuv420(info.Width, info.Height);
        using var audioInterleaveBuffer = new DataStream();
        var demuxer = new ModsDemuxer(video);
        foreach (MediaPacket framePacket in demuxer.ReadFrames()) {
            if (framePacket is VideoPacket) {
                FrameYuv420 frame = videoDecoder.DecodeFrame(framePacket.Data);

                if (convertYCbCr && frame.ColorSpace is YuvColorSpace.YCoCg) {
                    // ffmpeg YCoCg is bugged, transform to YCbCr
                    ColorSpaceConverter.YCoCg2YCbCr(frame, colorConvertedFrame);
                    frame = colorConvertedFrame;
                }

                videoStream.Write(frame.PackedData);
                ProgressUpdate?.Invoke(this, framePacket.FrameCount);
            } else if (framePacket is AudioPacket audioPacket) {
                byte[] channelData = audioDecoders[audioPacket.TrackIndex].Decode(audioPacket.Data, audioPacket.IsKeyFrame);
                audioInterleaveBuffer.Write(channelData);

                if (audioPacket.TrackIndex + 1 == info.AudioChannelsCount) {
                    audioStream.WriteInterleavedPCM16(audioInterleaveBuffer, info.AudioChannelsCount);
                    audioInterleaveBuffer.Position = 0;
                }
            }
        }
    }

    private static IAudioDecoder CreateAudioDecoder(AudioCodecKind codecKind)
    {
        return codecKind switch {
            AudioCodecKind.ImaAdPcm => new ImaAdpcmDecoder(),
            _ => throw new NotImplementedException("Unsupported audio codec"),
        };
    }
}
