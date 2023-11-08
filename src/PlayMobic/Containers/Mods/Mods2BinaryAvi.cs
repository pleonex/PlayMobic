namespace PlayMobic.Containers.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayMobic.Audio;
using PlayMobic.Video.Mobiclip;
using PlayMobic.Video;
using SharpAvi.Codecs;
using SharpAvi.Output;
using Yarhl.FileFormat;
using Yarhl.IO;

public class Mods2BinaryAvi : IConverter<ModsVideo, BinaryFormat>
{
    private readonly Stream output;

    public Mods2BinaryAvi(Stream output)
    {
        this.output = output;
    }

    public event EventHandler<int>? ProgressUpdate;

    public BinaryFormat Convert(ModsVideo source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var writer = new AviWriter(output, true) {
            FramesPerSecond = (decimal)source.Info.FramesPerSecond,
            EmitIndex1 = true,
        };

        IAviVideoStream videoStream = writer.AddUncompressedVideoStream(source.Info.Width, source.Info.Height);
        IAviAudioStream audioStream = writer.AddAudioStream(source.Info.AudioChannelsCount, source.Info.AudioFrequency, 16);
        Decode(source, videoStream, audioStream);

        writer.Close();
        return new BinaryFormat(output);
    }

    private void Decode(ModsVideo video, IAviVideoStream videoStream, IAviAudioStream audioStream)
    {
        ModsInfo info = video.Info;
        var videoDecoder = new MobiclipDecoder(info.Width, info.Height, isStereo: false);
        var audioDecoders = new IAudioDecoder[info.AudioChannelsCount];
        for (int i = 0; i < audioDecoders.Length; i++) {
            audioDecoders[i] = CreateAudioDecoder(info.AudioCodec);
        }

        byte[] rgbFrame = new byte[info.Width * info.Height * 4];
        using var audioBlocksBuffer = new DataStream();
        byte[] audioInterleaveBuffer = new byte[info.AudioChannelsCount * 6000];
        int audioBlockLength = 0;

        var demuxer = new ModsDemuxer(video);
        foreach (MediaPacket framePacket in demuxer.ReadFrames()) {
            if (framePacket is VideoPacket) {
                // Flush previous audio data block
                if (audioBlocksBuffer.Length > 0) {
                    audioBlocksBuffer.ReadInterleavedPCM16(audioBlockLength, audioInterleaveBuffer, info.AudioChannelsCount);
                    audioStream.WriteBlock(audioInterleaveBuffer, 0, audioBlockLength);
                    audioBlocksBuffer.Position = 0;
                    audioBlockLength = 0;
                }

                FrameYuv420 frame = videoDecoder.DecodeFrame(framePacket.Data);
                if (frame.ColorSpace is not YuvColorSpace.YCoCg) {
                    throw new NotSupportedException("Not supported colorspace");
                }

                ColorSpaceConverter.YCoCg2Bgr32(frame, rgbFrame);
                videoStream.WriteFrame(framePacket.IsKeyFrame, rgbFrame);
                ProgressUpdate?.Invoke(this, framePacket.FrameCount);
            } else if (framePacket is AudioPacket audioPacket) {
                byte[] channelData = audioDecoders[audioPacket.TrackIndex].Decode(audioPacket.Data, audioPacket.IsKeyFrame);
                audioBlocksBuffer.Write(channelData);
                audioBlockLength += channelData.Length;
            }
        }

        // Flush last block
        if (audioBlocksBuffer.Length > 0) {
            audioBlocksBuffer.ReadInterleavedPCM16(audioBlockLength, audioInterleaveBuffer, info.AudioChannelsCount);
            audioStream.WriteBlock(audioInterleaveBuffer, 0, audioBlockLength);
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
