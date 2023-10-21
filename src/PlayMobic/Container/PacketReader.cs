namespace PlayMobic.Container;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Yarhl.IO;

public sealed class PacketReader : IEnumerator<FramePacket>
{
    private const int AudioBlockSize = 128;
    private const int CompleteAudioBlockSize = 4 + AudioBlockSize;

    private readonly ModsVideo container;
    private readonly int startFrame;

    private readonly DataStream containerData;
    private readonly DataReader reader;

    private int currentFrame;

    public PacketReader(ModsVideo container, int startFrame)
    {
        ArgumentNullException.ThrowIfNull(container);

        this.container = container;
        this.startFrame = startFrame;

        uint startOffset = container.KeyFramesInfo.FirstOrDefault(f => f.FrameNumber == startFrame)
            ?.DataOffset
            ?? throw new InvalidOperationException("Can only start from a key frame");

        // We keep our own datastream so the position doesn't change for us.
        containerData = new DataStream(container.Data, startOffset, container.Data.Length - startOffset);
        reader = new DataReader(containerData);

        currentFrame = startFrame;

        // Current is not defined before MoveNext call
        Current = null!;
    }

    public FramePacket Current { get; set; }

    object? IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (currentFrame >= container.Info.FramesCount) {
            return false;
        }

        Current?.Dispose();
        Current = ReadNextFramePacket();

        return true;
    }

    public void Reset()
    {
        containerData.Position = 0;
        currentFrame = startFrame;
        Current = null!;
    }

    public void Dispose()
    {
        Current?.Dispose();
        containerData?.Dispose();
        GC.SuppressFinalize(this);
    }

    private FramePacket ReadNextFramePacket()
    {
        uint packetInfo = reader.ReadUInt32();
        uint packetSize = packetInfo >> 14;
        int audioBlocksCount = (int)(packetInfo & 0x3FFF);

        // Peek video data to know if it's key frame
        ushort frameKind = reader.ReadUInt16();
        containerData.Position -= 2;
        bool isKeyFrame = (frameKind >> 31) == 1;

        // NOTE: THIS DOES NOT WORK. WE CANNOT GET THE VIDEO WITHOUT RUNNING
        // THE VIDEO DECODER FIRST!!
        // We can get the video packet data from the number of audio blocks.
        int audioDataSize = GetAudioDataSize(isKeyFrame, audioBlocksCount) * container.Info.AudioChannelsCount;
        int videoDataSize = (int)(packetSize - audioDataSize);

        long packetStart = containerData.Position;
        var streamPackets = new List<StreamPacket>();

        // Video is first
        var videoData = new DataStream(containerData, packetStart, videoDataSize);
        streamPackets.Add(new StreamPacket(0, videoData));

        // Followed by audio, which is divided in blocks for each channel.
        // For simplicity we join all the blocks for the same channel / stream
        containerData.Position = packetStart + videoDataSize;
        DataStream[] audioData = GetAudioPackets(audioBlocksCount, isKeyFrame);
        var audioPackets = audioData.Select((p, idx) => new StreamPacket(1 + idx, p));
        streamPackets.AddRange(audioPackets);

        reader.SkipPadding(4);
        if (packetSize != (containerData.Position - packetStart)) {
            throw new FormatException("Invalid packet size");
        }

        currentFrame++;
        return new FramePacket(new ReadOnlyCollection<StreamPacket>(streamPackets), isKeyFrame);
    }

    private int GetAudioDataSize(bool isKeyFrame, int blocks)
    {
        int completeAudioBlocks = isKeyFrame ? 1 : 0;
        int regularAudioBlocks = blocks - completeAudioBlocks;

        int audioDataSize = (completeAudioBlocks * CompleteAudioBlockSize)
            + (regularAudioBlocks * AudioBlockSize);

        return audioDataSize;
    }

    private DataStream[] GetAudioPackets(int audioBlocksCount, bool isKeyFrame)
    {
        int channels = container.Info.AudioChannelsCount;
        var audioChannelsData = new DataStream[channels];
        for (int i = 0; i < audioChannelsData.Length; i++) {
            audioChannelsData[i] = new DataStream();
        }

        // It could be that there aren't enough blocks for every channel.
        for (int i = 0; i < audioBlocksCount; i++) {
            // If the frame is key, then the first block of each channel has an additional 4 bytes.
            int dataSize = (isKeyFrame && i == 0) ? CompleteAudioBlockSize : AudioBlockSize;

            for (int c = 0; c < channels; c++) {
                containerData.WriteSegmentTo(containerData.Position, dataSize, audioChannelsData[c]);
                containerData.Position += dataSize;
            }
        }

        return audioChannelsData;
    }
}
