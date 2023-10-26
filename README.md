# PlayMobic

_Documentation, library and tool to analyze and decode MODS video files._

> [!IMPORTANT]  
> This project **only supports the MODS format (NDS)**. Related formats such us
> Moflex or MO for GBA, 3DS or Wii are out of scope of this project. Please do
> **not** open issues or pull request for them.

## Features

- 📃 Documentation of the video format.
  - [MODS container](docs/MODS.md)
  - [Mobiclip codec](docs/codec-mobicli-binary.md)
- 📚 .NET library that supports the video format:
  - Deserialization of the MODS container
  - MODS demuxer
- 🔧 Tool to perform some operations on files
  - Display codec information of the MODS container

## Credits

_Standing on the shoulders of giants._

This project wouldn't be possible without the reverse engineering and
implementation work of the following projects:

- Gericom: [MobiclipDecoder](https://github.com/Gericom/MobiclipDecoder).
- Adib Surani: [Mobius](https://github.com/AdibSurani/Mobius).

## Roadmap

- [x] Document container format
- [x] Implement container format and demuxer
- [ ] Document video codec ([#1](https://github.com/pleonex/PlayMobic/issues/1))
- [ ] Implement video decoder
      ([#2](https://github.com/pleonex/PlayMobic/issues/2))
- [ ] Implement audio decoders
      ([#4](https://github.com/pleonex/PlayMobic/issues/4))
  - [ ] IMA-ADPCM
  - [ ] DSP-ADPCM
  - [ ] FastAudio
  - [ ] Raw PCM16
- [ ] Implement IMA-ADPCM audio encoder
- [ ] Implement video encoder
      ([#3](https://github.com/pleonex/PlayMobic/issues/3)) 😮 😕
