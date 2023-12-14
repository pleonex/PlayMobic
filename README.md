# PlayMobic

<!-- markdownlint-disable MD033 -->
<p align="center">
<!--
  <a href="https://dev.azure.com/SceneGate/SceneGate/_packaging?_a=feed&feed=SceneGate-Preview">
    <img alt="Stable version" src="https://img.shields.io/nuget/v/PleoSoft.PlayMobic?label=Stable" />
  </a>
  &nbsp;
-->
  <a href="https://dev.azure.com/SceneGate/SceneGate/_packaging?_a=feed&feed=SceneGate-Preview">
    <img alt="GitHub commits since latest release (by SemVer)" src="https://img.shields.io/github/commits-since/pleonex/PlayMobic/latest?sort=semver" />
  </a>
  &nbsp;
  <a href="https://github.com/pleonex/PlayMobic/actions/workflows/build-and-release.yml">
    <img alt="Build and release" src="https://github.com/pleonex/PlayMobic/actions/workflows/build-and-release.yml/badge.svg" />
  </a>
  &nbsp;
  <a href="https://choosealicense.com/licenses/mit/">
    <img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue.svg?style=flat" />
  </a>
  &nbsp;
</p>

Documentation, library and tools to analyze and **decode MODS video files**.

- 📃 Documentation of the formats
- 📚 .NET library that supports the video and audio format:
  - Deserialization of the MODS container
  - MODS demuxer
  - Mobiclip video decoder
  - IMA-AD PCM audio decoder
  - FastAudio v2 audio decoder
- 🔧 Command-line tool to show, demux and convert to AVI files
- 📺 Application to convert files to MP4, AVI or raw streams

> [!IMPORTANT]  
> This project **only supports the MODS format (NDS)**. Similar formats like
> VXDS, Moflex or MO are out of scope of this project (at the moment at least).
> Please do **not** open issues or pull request for them.

## Installation

- The applications will be available to download from the
  [GitHub release page](https://github.com/pleonex/PlayMobic/releases).
- The .NET library is available in the
  [SceneGate AzureDevOps feed](https://dev.azure.com/SceneGate/SceneGate/_artifacts/feed/SceneGate-Preview).

## Get started

Check out the [documentation site](https://www.pleonex.dev/PlayMobic/).

Feel free to ask any question in the
[project discussions](https://github.com/pleonex/PlayMobic/discussions).

## Build

The project requires
[.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

To build, test and generate artifacts run:

```sh
dotnet tool restore
dotnet script build.csx --isolated-load-context -- --target=Default
```

To build and run the tests uses the `Default` target or skip the `target`
argument. To create distributable bundles run the target `Bundle`.

## Release

Create a new GitHub release with a tag `v{Version}` (e.g. `v2.4`) and that's it!
This triggers a pipeline that builds and deploy the project.

## Credits

_Standing on the shoulders of giants._

This project wouldn't be possible without the reverse engineering and
implementation work of the following projects:

- Gericom: [MobiclipDecoder](https://github.com/Gericom/MobiclipDecoder).
- Adib Surani: [Mobius](https://github.com/AdibSurani/Mobius).
