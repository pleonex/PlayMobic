namespace PlayMobic;

using PlayMobic.Containers.Mods;

public record FfmpegConverterParameters(
    string ExecutablePath,
    string RawVideoPath,
    string RawAudioPath,
    string OutputPath,
    ModsInfo VideoInfo);
