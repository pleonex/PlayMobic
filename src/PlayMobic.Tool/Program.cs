using System.CommandLine;
using PlayMobic.Container;
using Yarhl.FileSystem;
using Yarhl.IO;

return await new RootCommand("Tool for MODS videos") {
    SetupInfoCommand(),
}.InvokeAsync(args);

Command SetupInfoCommand()
{
    var fileArg = new Option<FileInfo>("--path", "Path to the .mods file") { IsRequired = true };
    var infoCommand = new Command("info", "Show codec information of a video") {
        fileArg,
    };
    infoCommand.SetHandler(PrintInfo, fileArg);

    return infoCommand;
}

void PrintInfo(FileInfo videoFile)
{
    Console.WriteLine("Video: {0}", videoFile.FullName);

    Node videoNode = NodeFactory.FromFile(videoFile.FullName, FileOpenMode.Read)
        .TransformWith<Binary2Mods>();

    ModsVideo video = videoNode.GetFormatAs<ModsVideo>()!;
    ModsInfo info = video.Info;

    Console.WriteLine("  Video codec ID: {0} ({1:X2})", info.VideoCodecId, info.Unknown06);
    Console.WriteLine("  Resolution: {0}x{1}", info.Width, info.Height);
    Console.WriteLine("  Duration: {0} frames, {1}", info.FramesCount, info.Duration);
    Console.WriteLine("  Frames per second: {0}", info.FramesPerSecond);
    Console.WriteLine("  Key frames: {0}", video.KeyFramesInfo.Count);
    Console.WriteLine("  Audio codec: {0}", info.AudioCodec);
    Console.WriteLine("  Audio channels: {0}", info.AudioChannelsCount);
    Console.WriteLine("  Audio frequency: {0} Hz", info.AudioFrequency);
}
