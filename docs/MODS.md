# MODS video format

_MObiclip DS_ is a video format for Nintendo DS. Developers could encode videos
with tools from the official SDK. Games with this type of videos show the usage
of the SDK: `Actimagine:Mobiclip SDK Vx.x.x` (e.g. 1.2.1). The video files have
`.mods` extension.

The MODS format is a container with a video and audio streams.

## MODS container

The integers are encoded in little endian.

File structure:

- [Header](#mods-header)
- [Frame packets](#frame-packets)
- [Key frame info table](#key-frames-table)
  - Because it's at the end of the file, it prevents to support jumping in time
    when _download streaming_. You need to get the full file in order to be able
    to know where are they key frames. You can start streaming from start
    without jump support.

The location of the audio codec info (codebook) is not yet clarified.

### MODS header

| Offset | Type    | Description                                          |
| ------ | ------- | ---------------------------------------------------- |
| 0x00   | char[4] | Format identifier: `MODS`                            |
| 0x04   | ushort  | Video codec ID                                       |
| 0x06   | ushort  | Unknown (video codec version?)                       |
| 0x08   | int     | Frames count                                         |
| 0x0C   | int     | Video width resolution                               |
| 0x10   | int     | Video height resolution                              |
| 0x14   | byte[3] | Unknown - fps scale or single big endian value?      |
| 0x17   | byte    | Frames per second                                    |
| 0x18   | ushort  | Audio codec ID                                       |
| 0x1A   | ushort  | Audio channels count                                 |
| 0x1C   | uint    | Audio frequency in hertz                             |
| 0x20   | uint    | Index of the largest frame                           |
| 0x24   | uint    | Offset to the audio codec info section               |
| 0x28   | uint    | Offset to the key frames table                       |
| 0x2C   | uint    | Number of key frames                                 |
| 0x30   | uint    | Unknown (~total number of audio blocks per channel?) |

The video codec ID can be:

- `N2`
- `N3`

The audio codec ID can be:

- 1: FastAudio
- 2: Sx
- 3: IMA-ADPCM (common).

### Key frames table

This is a collection of 8-byte entries, one for each _key frame_. This allows to
jump in time in the video by finding the nearest complete frame to start
processing.

| Offset | Type | Description         |
| ------ | ---- | ------------------- |
| 0x00   | uint | Frame number        |
| 0x04   | uint | Frame packet offset |

### Frame packets

The container data is a set of _frame packets_. Each packet contains data to
decode one video frame and audio data for each channel.

| Offset | Type     | Description          |
| ------ | -------- | -------------------- |
| 0x00   | uint     | Packet info          |
| 0x04   | byte[]   | Video frame data     |
| ....   | byte[][] | Blocks of audio data |

The size of the video data is variable. Each audio block is `128` bytes, with an
additional `4` bytes for the first block _of each channel_ when the frame packet
is for a key frame (a complete block of data). The audio blocks are mixed
between channels, meaning that there will be first a block for channel 0, then a
block for channel 1, then a block for channel 0 again, repeating until reading
all the blocks.

> [!NOTE]  
> It could happen that there aren't enough blocks for each channel. There could
> be a frame packet with 7 blocks for a 2 audio channel, so the last channel
> will get less data.

The packet info contains information about this frame packet:

- Bits 0-13: packet size
- Bits 14-31: number of audio blocks

The _packet size_ includes the video and audio block. To get each data size, you
can use the _number of audio blocks_ as follow:

```csharp
int completeAudioBlocks = isKeyFrame ? channelsCount : 0;
int regularAudioBlocks = blocks - completeAudioBlocks;

int audioDataSize = (completeAudioBlocks * (128 + 4)) + (regularAudioBlocks * 128);
int videoDataSize = (int)(packetSize - audioDataSize);
```

> [!IMPORTANT]  
> This logic works for IMA-ADPCM audio encoding. It remains to be confirmed for
> other codecs.
