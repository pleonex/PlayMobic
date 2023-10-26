namespace PlayMobic.Container;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Yarhl.IO;

/// <summary>
/// Move to the next stream packet on each iteration, video or audio blocks at a time.
/// When all the blocks/stream for a frame packets are consumed, it reads the next.
/// </summary>
public sealed class ModsPacketReader : IEnumerator<MediaPacket>
{
    private readonly ModsVideo container;
    private readonly int startFrame;

    private readonly DataStream containerData;
    private readonly DataReader reader;

    private int currentFrame;
    private DataStream? packetStream;
    private int currentPacketStream;
    private int numStreamsPerFramePacket;
    private bool currentIsKeyFrame;

    public ModsPacketReader(ModsVideo container, int startFrame)
    {
        ArgumentNullException.ThrowIfNull(container);

        this.container = container;
        this.startFrame = startFrame;

        uint startOffset = container.KeyFramesInfo.FirstOrDefault(f => f.FrameNumber == startFrame)
            ?.DataOffset
            ?? throw new InvalidOperationException("Can only start from a key frame");

        // We keep our own datastream so working with the stream outside won't affect position for us.
        containerData = new DataStream(container.Data, startOffset, container.Data.Length - startOffset);
        reader = new DataReader(containerData);

        currentFrame = startFrame - 1;

        // Current is not defined before MoveNext call
        Current = null!;
        currentPacketStream = -1;
    }

    public MediaPacket Current { get; private set; }

    object? IEnumerator.Current => Current;

    public bool MoveNext()
    {
        long packetOffset;
        if (currentPacketStream == -1 || currentPacketStream >= numStreamsPerFramePacket) {
            if (currentFrame >= container.Info.FramesCount) {
                return false;
            }

            currentFrame++;
            packetStream?.Dispose();

            ReadNextPacket();
            packetOffset = 0;
        } else {
            packetOffset = Current.Data.Position;
        }

        Current?.Dispose();

        var streamData = new DataStream(packetStream!, packetOffset, packetStream!.Length - packetOffset);
        Current = new MediaPacket(currentPacketStream, streamData, currentIsKeyFrame);

        currentPacketStream++;
        return true;
    }

    public void Reset()
    {
        containerData.Position = 0; // relative to start frame already
        currentFrame = startFrame;
        Current = null!;
    }

    public void Dispose()
    {
        Current?.Dispose();
        packetStream?.Dispose();
        containerData?.Dispose();

        GC.SuppressFinalize(this);
    }

    private void ReadNextPacket()
    {
        // Read the packet header (NAL unit header?)
        uint packetInfo = reader.ReadUInt32();
        uint packetSize = packetInfo >> 14;
        int audioBlocksCount = (int)(packetInfo & 0x3FFF);

        // Peek video data to know if it's key frame.
        // The first bit indicates I-Frame (key frame) or we could iterate
        // the key frame table, but that would be slower.
        ushort frameKind = reader.ReadUInt16();
        containerData.Position -= 2;
        currentIsKeyFrame = (frameKind >> 31) == 1;

        currentPacketStream = 0;
        numStreamsPerFramePacket = 1 + (audioBlocksCount * container.Info.AudioChannelsCount);
        packetStream = new DataStream(containerData, containerData.Position, packetSize);
    }
}
