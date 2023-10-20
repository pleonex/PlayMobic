namespace PlayMobic.Container;

using System;
using Yarhl.FileFormat;
using Yarhl.IO;

public class Binary2Mods : IConverter<IBinary, ModsVideo>
{
    public ModsVideo Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);

        source.Stream.Position = 0;
        var reader = new DataReader(source.Stream);

        ModsHeader header = ReadHeader(reader);

        var video = new ModsVideo {
            Info = header.Info,
        };

        return video;
    }

    private static ModsHeader ReadHeader(DataReader reader)
    {
        if (reader.ReadString(4) != "MODS") {
            throw new FormatException("Invalid file stamp");
        }

        var header = new ModsHeader();

        header.Info.TagId = reader.ReadUInt16();
        header.Info.TagIdSize = reader.ReadUInt16();
        header.Info.FramesCount = reader.ReadInt32();
        header.Info.Width = reader.ReadInt32();
        header.Info.Height = reader.ReadInt32();
        _ = reader.ReadInt24(); // unknown - scale?
        header.Info.FramesPerSecond = reader.ReadByte();
        header.Info.AudioCodecKind = reader.ReadUInt16();
        header.Info.AudioChannelsCount = reader.ReadUInt16();
        header.Info.AudioFrequency = reader.ReadInt32();

        header.LargeFrameIdx = reader.ReadInt32();
        header.AudioCodecInfoOffset = reader.ReadUInt32();
        header.KeyFramesTableOffset = reader.ReadUInt32();
        header.KeyFramesCount = reader.ReadUInt32();

        return header;
    }

    private sealed record ModsHeader
    {
        public ModsInfo Info { get; } = new ModsInfo();

        public int LargeFrameIdx { get; set; }

        public uint AudioCodecInfoOffset { get; set; }

        public uint KeyFramesTableOffset { get; set; }

        public uint KeyFramesCount { get; set; }
    }
}
