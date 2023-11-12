namespace PlayMobic.Containers.Mods;

using System;
using System.Collections.ObjectModel;
using PlayMobic;
using Yarhl.FileFormat;
using Yarhl.IO;

public class Binary2Mods : IConverter<IBinary, ModsVideo>
{
    private const uint AudioCodebookLength = 0xC34; // hard-coded in code

    public ModsVideo Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);

        source.Stream.Position = 0;
        var reader = new DataReader(source.Stream);

        // Container header
        ModsHeader header = ReadHeader(reader);

        // Get the video in a separate stream so we ensure we don't over-read.
        uint dataOffset = (uint)reader.Stream.Position;
        long endDataOffset = (header.AudioCodecInfoOffset != 0 && header.AudioCodecInfoOffset < header.KeyFramesTableOffset)
            ? header.AudioCodecInfoOffset
            : header.KeyFramesTableOffset;
        long dataLength = endDataOffset - dataOffset;

        var dataStream = new DataStream(reader.Stream, reader.Stream.Position, dataLength);

        // Audio codebooks
        var codebookList = new List<Stream>();
        if (header.AudioCodecInfoOffset != 0) {
            long codebookOffset = header.AudioCodecInfoOffset;

            for (int i = 0; i < header.Info.AudioChannelsCount; i++) {
                var channelBook = new DataStream(source.Stream, codebookOffset, AudioCodebookLength);
                codebookList.Add(channelBook);
                codebookOffset += AudioCodebookLength;
            }
        }

        // Key frame info table
        reader.Stream.Position = header.KeyFramesTableOffset;
        Collection<KeyFrameInfo> keyFramesInfo = ReadKeyFramesTable(reader, dataOffset, header.KeyFramesCount);

        return new ModsVideo(dataStream) {
            Info = header.Info,
            KeyFramesInfo = keyFramesInfo,
            AudioCodebook = codebookList.ToArray(),
        };
    }

    private static ModsHeader ReadHeader(DataReader reader)
    {
        if (reader.ReadString(4) != "MODS") {
            throw new FormatException("Invalid file stamp");
        }

        var header = new ModsHeader();

        header.Info.ContainerFormatId = reader.ReadString(2);
        if (header.Info.ContainerFormatId is not ("N2" or "N3")) {
            throw new NotSupportedException("Unsupported container format");
        }

        header.Info.VideoCodec = reader.ReadUInt16() == 0x0A
            ? VideoCodecKind.MobiclipV1
            : throw new NotSupportedException("Unsupported container format");

        header.Info.FramesCount = reader.ReadInt32();
        header.Info.Width = reader.ReadInt32();
        header.Info.Height = reader.ReadInt32();
        header.Info.FramesPerSecond = reader.ReadUInt32() / (double)ModsInfo.FramesPerSecondBase;
        header.Info.AudioCodec = GetAudioCodec(reader.ReadUInt16());
        header.Info.AudioChannelsCount = reader.ReadUInt16();
        header.Info.AudioFrequency = reader.ReadInt32();

        header.LargeFrameIdx = reader.ReadInt32();
        header.AudioCodecInfoOffset = reader.ReadUInt32();
        header.KeyFramesTableOffset = reader.ReadUInt32();
        header.KeyFramesCount = reader.ReadUInt32();

        // Additional parameters
        var additionalParameters = new List<VideoParameter>();
        if (header.Info.ContainerFormatId == "N3") {
            string parameterId;
            do {
                parameterId = reader.ReadString(2);
                int parametersCount = reader.ReadUInt16();

                if (parameterId != "HE") {
                    var parameters = new List<uint>();
                    for (int i = 0; i < parametersCount; i++) {
                        parameters.Add(reader.ReadUInt32());
                    }

                    additionalParameters.Add(new VideoParameter(parameterId, parameters));
                }
            } while (parameterId != "HE" && !reader.Stream.EndOfStream);
        }

        header.Info.AdditionalParameters = additionalParameters;

        return header;
    }

    private static Collection<KeyFrameInfo> ReadKeyFramesTable(DataReader reader, uint dataOffset, uint count)
    {
        var infos = new Collection<KeyFrameInfo>();
        for (int i = 0; i < count; i++) {
            int number = reader.ReadInt32();
            uint offset = reader.ReadUInt32() - dataOffset; // relative to the data stream
            infos.Add(new KeyFrameInfo(number, offset));
        }

        return infos;
    }

    private static AudioCodecKind GetAudioCodec(int id) =>
        id switch {
            0 => AudioCodecKind.None,
            1 => AudioCodecKind.FastAudioCodebook,
            2 => AudioCodecKind.FastAudioEnhanced,
            3 => AudioCodecKind.ImaAdPcm,
            4 => AudioCodecKind.RawPcm16,
            _ => throw new NotSupportedException("Unsupported audio codec"),
        };

    private sealed record ModsHeader
    {
        public ModsInfo Info { get; } = new ModsInfo();

        public int LargeFrameIdx { get; set; }

        public uint AudioCodecInfoOffset { get; set; }

        public uint KeyFramesTableOffset { get; set; }

        public uint KeyFramesCount { get; set; }
    }
}
