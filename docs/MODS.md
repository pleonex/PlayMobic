# MODS video format

_MObiclip DS_ is a video format for Nintendo DS. Developers could encode videos
with tools from the official SDK. Games with this type of videos show the usage
of the SDK: `Actimagine:Mobiclip SDK Vx.x.x` (e.g. 1.2.1). The video files have
`.mods` extension.

The MODS format is a container with a video and audio streams.

This format supports _random frame access_ but it requires the full file to be
available to get the bottom _key frame info table_.

## MODS container

The integers are encoded in little endian.

File structure:

- [Header](#mods-header)
- [Containers](#containers)
  - [Frame packets](#frame-packets)
- [Key frame info table](#key-frames-table)

The location of the audio codec info (codebook) is not yet clarified.

### MODS header

| Offset | Type    | Description                                     |
| ------ | ------- | ----------------------------------------------- |
| 0x00   | char[4] | Format identifier: `MODS`                       |
| 0x04   | ushort  | Container kind ID                               |
| 0x06   | ushort  | Container kind version                          |
| 0x08   | int     | Frames count                                    |
| 0x0C   | int     | Video width resolution                          |
| 0x10   | int     | Video height resolution                         |
| 0x14   | uint    | Frames per second * 0x01000000                  |
| 0x18   | ushort  | Audio codec ID                                  |
| 0x1A   | ushort  | Audio channels count                            |
| 0x1C   | uint    | Audio frequency in hertz                        |
| 0x20   | uint    | Index of the largest frame                      |
| 0x24   | uint    | Offset to the audio codec info section          |
| 0x28   | uint    | Offset to the key frames table                  |
| 0x2C   | uint    | Number of key frames                            |

The container kind can be:

- `N2`
- `N3` with version `0A`

The audio codec ID can be:

- 0: No audio
- 1: FastAudio
- 2: Sx
- 3: IMA-ADPCM: 4-bits samples with a header of 32-bits (index + last sample)
  per key frame
- 4: unknown

### Containers

The container kind `N3` seems to support multiple sub-containers. After the
header (`0x30`), the collection of sub-containers start. Each sub-container has
a 32-bits value with information followed with the data.

| Offset | Type    | Description                 |
| ------ | ------- | --------------------------- |
| 0x00   | char[2] | ID                          |
| 0x02   | ushort  | Length in blocks of 32-bits |

The only known ID is `HE` that contains video with audio.

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

| Offset | Type     | Description                  |
| ------ | -------- | ---------------------------- |
| 0x00   | uint     | Packet info                  |
| 0x04   | byte[]   | Video frame data             |
| ....   | byte[][] | Blocks of audio data         |
| ....   | byte[]   | 0 padding to 32-bits address |

The packet info contains information about this frame packet:

- Bits 0-13: number of audio blocks **per channel**
- Bits 14-31: packet size including padding but without this header

Additionally, it's possible to know if it's a _key frame_ without the
[key frames table](#key-frames-table) by checking the first 16-bits value of the
video data. If it has the highest bit 15 set to 1, it's a key frame.

The size of the video data is variable for each frame. The size of the audio
depends on the encoding. As we don't know the size of the video data and the
_packet size_ includes padding bytes, it's not possible to access the audio data
without running the video decoder first.

The blocks of audio are mixed between channels. This means that for a two
channels video there will be first a block for channel 0, then a block for
channel 1, then a block for channel 0 again, repeating until reading all the
blocks for each channel.

> [!NOTE]  
> The decoder skips _four_ bytes after reading the video frame date if the video
> codec is `N3` and the first 16-bits value of the video data has the highest
> bit set to 1. It's unknown at this moment if it's some adjustment from the
> internal logic of video decoding or there are four byte to skip.
