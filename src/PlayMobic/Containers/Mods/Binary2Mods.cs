namespace PlayMobic.Containers.Mods;

using System;
using System.Collections.ObjectModel;
using Yarhl.FileFormat;
using Yarhl.IO;

public class Binary2Mods : IConverter<IBinary, ModsVideo>
{
    private const uint DataOffset = 0x34; // after header

    public ModsVideo Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);

        source.Stream.Position = 0;
        var reader = new DataReader(source.Stream);

        // Container header
        ModsHeader header = ReadHeader(reader);

        // Packets data
        long endDataOffset = header.KeyFramesTableOffset;
        long dataLength = endDataOffset - DataOffset;
        var dataStream = new DataStream(source.Stream, DataOffset, dataLength);

        // Key frame info table
        reader.Stream.Position = header.KeyFramesTableOffset;
        Collection<KeyFrameInfo> keyFramesInfo = ReadKeyFramesTable(reader, header.KeyFramesCount);

        // TODO: Get stream for the audio codec info (codebook)
        return new ModsVideo(dataStream) {
            Info = header.Info,
            KeyFramesInfo = keyFramesInfo,
        };
    }

    private static ModsHeader ReadHeader(DataReader reader)
    {
        if (reader.ReadString(4) != "MODS") {
            throw new FormatException("Invalid file stamp");
        }

        var header = new ModsHeader();

        header.Info.VideoCodecId = reader.ReadString(2);
        header.Info.Unknown06 = reader.ReadUInt16();
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
        header.Unknown = reader.ReadUInt32();

        return header;
    }

    private static Collection<KeyFrameInfo> ReadKeyFramesTable(DataReader reader, uint count)
    {
        var infos = new Collection<KeyFrameInfo>();
        for (int i = 0; i < count; i++) {
            int number = reader.ReadInt32();
            uint offset = reader.ReadUInt32() - DataOffset; // relative to the data stream
            infos.Add(new KeyFrameInfo(number, offset));
        }

        return infos;
    }

    private static AudioCodecKind GetAudioCodec(int id) =>
        id switch {
            0 => AudioCodecKind.None,
            1 => AudioCodecKind.Sx,
            2 => AudioCodecKind.FastAudio,
            3 => AudioCodecKind.ImaAdpcm,
            4 => AudioCodecKind.Unknown4,
            _ => throw new NotSupportedException("Unsupported audio codec"),
        };

    private sealed record ModsHeader
    {
        public ModsInfo Info { get; } = new ModsInfo();

        public int LargeFrameIdx { get; set; }

        public uint AudioCodecInfoOffset { get; set; }

        public uint KeyFramesTableOffset { get; set; }

        public uint KeyFramesCount { get; set; }

        public uint Unknown { get; set; }
    }
}
