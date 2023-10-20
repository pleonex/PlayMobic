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

    ModsInfo info = videoNode.GetFormatAs<ModsVideo>()!.Info;
    double durationSec = info.FramesCount / (double)info.FramesPerSecond;
    var duration = TimeSpan.FromSeconds(durationSec);

    Console.WriteLine("  Tag: {0:X2} - {1:X}", info.TagId, info.TagIdSize);
    Console.WriteLine("  Resolution: {0}x{1}", info.Width, info.Height);
    Console.WriteLine("  Duration: {0} frames, {1}", info.FramesCount, duration);
    Console.WriteLine("  Frames per second: {0}", info.FramesPerSecond);
    Console.WriteLine("  Audio codec: {0}", info.AudioCodecKind);
    Console.WriteLine("  Audio channels: {0}", info.AudioChannelsCount);
    Console.WriteLine("  Audio frequency: {0} Hz", info.AudioFrequency);
}
