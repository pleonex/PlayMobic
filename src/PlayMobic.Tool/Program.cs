#pragma warning disable SA1200 // False positive on top-level without namespace
using System.CommandLine;
using PlayMobic.Containers;
using PlayMobic.Containers.Mods;
using PlayMobic.Video;
using PlayMobic.Video.Mobiclip;
using Yarhl.FileSystem;
using Yarhl.IO;
#pragma warning restore SA1200

return await new RootCommand("Tool for MODS videos") {
    SetupInfoCommand(),
    SetupDemuxCommand(),
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

Command SetupDemuxCommand()
{
    var inputArg = new Option<FileInfo>("--input", "Path to the .mods file") { IsRequired = true };
    var outputArg = new Option<string>("--output", "Path to the folder to write the streams") { IsRequired = true };
    var command = new Command("demux", "Extract each video and audio streams") {
        inputArg,
        outputArg,
    };
    command.SetHandler(Demux, inputArg, outputArg);

    return command;
}

void PrintInfo(FileInfo videoFile)
{
    Console.WriteLine("Video: {0}", videoFile.FullName);

    using Node videoNode = NodeFactory.FromFile(videoFile.FullName, FileOpenMode.Read)
        .TransformWith<Binary2Mods>();

    ModsVideo video = videoNode.GetFormatAs<ModsVideo>()!;
    ModsInfo info = video.Info;

    Console.WriteLine("  Container format: {0}", info.ContainerFormatId);
    Console.WriteLine("  Video codec: {0}", info.VideoCodec);
    Console.WriteLine("    Resolution: {0}x{1}", info.Width, info.Height);
    Console.WriteLine("    Duration: {0} frames, {1}", info.FramesCount, info.Duration);
    Console.WriteLine("    Frames per second: {0}", info.FramesPerSecond);
    Console.WriteLine("    Key frames: {0}", video.KeyFramesInfo.Count);
    Console.WriteLine("  Audio codec: {0}", info.AudioCodec);
    Console.WriteLine("    Audio channels: {0}", info.AudioChannelsCount);
    Console.WriteLine("    Audio frequency: {0} Hz", info.AudioFrequency);
    Console.WriteLine("  Extra parameters: {0}", info.AdditionalParameters.Count);
    foreach (VideoParameter param in info.AdditionalParameters) {
        Console.WriteLine("    {0}: {1}", param.Id, string.Join(", ", param.Parameters.Select(p => $"{p:X4}")));
    }
}

void Demux(FileInfo videoFile, string outputPath)
{
    string videoPath = Path.Combine(outputPath, videoFile.Name + ".rawvideo");

    Console.WriteLine("Video: {0}", videoFile.FullName);
    Console.WriteLine("Output video: {0}", videoPath);

    using Node videoNode = NodeFactory.FromFile(videoFile.FullName, FileOpenMode.Read)
        .TransformWith<Binary2Mods>();

    ModsVideo video = videoNode.GetFormatAs<ModsVideo>()!;
    ModsInfo info = video.Info;

    var videoDecoder = new MobiclipDecoder(info.Width, info.Height);
    using DataStream videoStream = DataStreamFactory.FromFile(videoPath, FileOpenMode.Write);

    var demuxer = new ModsDemuxer(video);
    foreach (MediaPacket framePacket in demuxer.ReadFrames()) {
        if (framePacket.IsKeyFrame && framePacket is VideoPacket) {
            FrameYuv420 frame = videoDecoder.DecodeFrame(framePacket.Data);
            videoStream.Write(frame.PackedData);

            Console.Write('+');
        } else {
            Console.Write('.');
        }
    }

    Console.WriteLine();
    Console.WriteLine("Done");
}
