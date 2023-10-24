# PlayMobic

_Documentation, library and tool to analyze and decode MODS video files._

> [!IMPORTANT]  
> This project is in work-in progress. Code may not work and documents may not
> be finished and inaccurate.

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

This project is possible thanks to previous research on the codec by:

- Gericom: [MobiclipDecoder](https://github.com/Gericom/MobiclipDecoder).
- Adib Surani: [Mobius](https://github.com/AdibSurani/Mobius).
