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
- Frame packets
- [Key frame info table](#key-frames-table)
  - Because it's at the end of the file, it prevents to support jumping in time
    when _download streaming_. You need to get the full file in order to be able
    to know where are they key frames. You can start streaming from start
    without jump support.

The location of the audio codec info (codebook) is not yet clarified.

### MODS header

| Offset | Type    | Description                                     |
| ------ | ------- | ----------------------------------------------- |
| 0x00   | char[4] | Format identifier: `MODS`                       |
| 0x04   | ushort  | Tag ID (unknown)                                |
| 0x06   | ushort  | Tag ID size (unknown)                           |
| 0x08   | int     | Frames count                                    |
| 0x0C   | int     | Video width resolution                          |
| 0x10   | int     | Video height resolution                         |
| 0x14   | byte[3] | Unknown - fps scale or single big endian value? |
| 0x17   | byte    | Frames per second                               |
| 0x18   | ushort  | Audio codec ID                                  |
| 0x1A   | ushort  | Audio channels count                            |
| 0x1C   | uint    | Audio frequency in hertz                        |
| 0x20   | uint    | Index of the largest frame                      |
| 0x24   | uint    | Offset to the audio codec info section          |
| 0x28   | uint    | Offset to the key frames table                  |
| 0x2C   | uint    | Number of key frames                            |

### Key frames table

This is a collection of 8-byte entries, one for each _key frame_. This allows to
jump in time in the video by finding the nearest complete frame to start
processing.

| Offset | Type | Description         |
| ------ | ---- | ------------------- |
| 0x00   | uint | Frame number        |
| 0x04   | uint | Frame packet offset |
