﻿namespace PlayMobic.Containers.Mods;
using System;
using PlayMobic.Audio;
using PlayMobic.Video;
using PlayMobic.Video.Mobiclip;
using SharpAvi.Codecs;
using SharpAvi.Output;
using Yarhl.FileFormat;
using Yarhl.IO;

public class Mods2BinaryAvi : IConverter<ModsVideo, BinaryFormat>
{
    private const int MaxBlocksPerFrame = 0x3FFF + 1;
    private const int DecodedBlockSize = 0x200; // 256 samples * 16-bits
    private const int MaxBlockPerChannelSize = MaxBlocksPerFrame * DecodedBlockSize;

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
        IAviAudioStream? audioStream = source.Info.AudioChannelsCount > 0
            ? writer.AddAudioStream(source.Info.AudioChannelsCount, source.Info.AudioFrequency, 16)
            : null;
        Decode(source, videoStream, audioStream);

        writer.Close();
        return new BinaryFormat(output);
    }

    private void Decode(ModsVideo video, IAviVideoStream videoStream, IAviAudioStream? audioStream)
    {
        ModsInfo info = video.Info;
        var videoDecoder = new MobiclipDecoder(info.Width, info.Height, isStereo: false);
        var audioDecoders = new IAudioDecoder[info.AudioChannelsCount];
        for (int i = 0; i < audioDecoders.Length; i++) {
            audioDecoders[i] = CreateAudioDecoder(video, i);
        }

        byte[] rgbFrame = new byte[info.Width * info.Height * 4];

        // This is allocating a huge buffer (16 MB) for the interleaved buffer but I can't figure out
        // a better way as the AVI API only accepts byte[] as input, so it's that or create a buffer each time.
        using var audioBlocksBuffer = new DataStream();
        byte[] audioInterleaveBuffer = new byte[info.AudioChannelsCount * MaxBlockPerChannelSize];
        int audioBlockLength = 0;

        var demuxer = new ModsDemuxer(video);
        foreach (MediaPacket framePacket in demuxer.ReadFrames()) {
            if (framePacket is VideoPacket) {
                // Flush previous audio data block
                if (audioBlocksBuffer.Length > 0) {
                    audioBlocksBuffer.ReadInterleavedPCM16(audioBlockLength, audioInterleaveBuffer, info.AudioChannelsCount);
                    audioStream!.WriteBlock(audioInterleaveBuffer, 0, audioBlockLength);
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
            audioStream!.WriteBlock(audioInterleaveBuffer, 0, audioBlockLength);
        }
    }

    private static IAudioDecoder CreateAudioDecoder(ModsVideo video, int channelIdx)
    {
        Stream? codebook = null;
        if (video.Info.AudioCodec is AudioCodecKind.FastAudioCodebook) {
            if (channelIdx >= video.AudioCodebook.Length) {
                throw new InvalidOperationException("Codebook is missing for channel");
            }

            codebook = video.AudioCodebook[channelIdx];
        }

        return video.Info.AudioCodec switch {
            AudioCodecKind.FastAudioCodebook => new FastAudioCodebookDecoder(codebook!),
            AudioCodecKind.FastAudioEnhanced => new FastAudioEnhancedDecoder(),
            AudioCodecKind.ImaAdPcm => new ImaAdpcmDecoder(),
            AudioCodecKind.RawPcm16 => new RawPcm16Decoder(),
            _ => throw new NotImplementedException("Unsupported audio codec"),
        };
    }
}
