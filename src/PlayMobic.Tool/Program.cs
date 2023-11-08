using System.CommandLine;
using System.Diagnostics;
using PlayMobic.Audio;
using PlayMobic.Containers;
using PlayMobic.Containers.Mods;
using PlayMobic.Video;
using PlayMobic.Video.Mobiclip;
using Texim.Colors;
using Texim.Formats;
using Texim.Images;
using Yarhl.FileSystem;
using Yarhl.IO;

return await new RootCommand("Tool for MODS videos") {
    SetupInfoCommand(),
    SetupExtraFramesCommand(),
    SetupMods2AviCommand(),
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

Command SetupExtraFramesCommand()
{
    var inputArg = new Option<FileInfo>("--input", "Path to the .mods file") { IsRequired = true };
    var outputArg = new Option<string>("--output", "Path to the folder to write the frames") { IsRequired = true };
    var command = new Command("extract-frames", "Extract each video frame into PNG images") {
        inputArg,
        outputArg,
    };
    command.SetHandler(ExtractFrames, inputArg, outputArg);

    return command;
}

Command SetupMods2AviCommand()
{
    var inputArg = new Option<FileInfo>("--input", "Path to the .mods file") { IsRequired = true };
    var outputArg = new Option<string>("--output", "Path to the file output AVI file") { IsRequired = true };
    var command = new Command("mods2avi", "Convert a MODS video into an AVI file") {
        inputArg,
        outputArg,
    };
    command.SetHandler(Mods2Avi, inputArg, outputArg);

    return command;
}

Command SetupDemuxCommand()
{
    var inputArg = new Option<FileInfo>("--input", "Path to the .mods file") { IsRequired = true };
    var outputArg = new Option<string>("--output", "Path to the folder to write the streams") { IsRequired = true };
    var command = new Command("demux", "Extract and decode each video and audio streams") {
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
    PrintVideoInfo(video);
}

void PrintVideoInfo(ModsVideo video)
{
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

void ExtractFrames(FileInfo videoFile, string outputPath)
{
    Console.WriteLine("Video: {0}", videoFile.FullName);
    Console.WriteLine("Output: {0}", outputPath);

    using Node videoNode = NodeFactory.FromFile(videoFile.FullName, FileOpenMode.Read)
        .TransformWith<Binary2Mods>();

    ModsVideo video = videoNode.GetFormatAs<ModsVideo>()!;
    ModsInfo info = video.Info;

    var demuxer = new ModsDemuxer(video);
    var videoDecoder = new MobiclipDecoder(info.Width, info.Height);
    byte[] rgbFrame = new byte[info.Width * info.Height * 4];
    var image2BinaryBitmap = new FullImage2Bitmap();

    // This work because video is always the first stream in the packets
    // and we don't need to decode audio to advance to next frame.
    foreach (MediaPacket framePacket in demuxer.ReadFrames().OfType<VideoPacket>()) {
        FrameYuv420 frame = videoDecoder.DecodeFrame(framePacket.Data);

        if (frame.ColorSpace is not YuvColorSpace.YCoCg) {
            throw new NotSupportedException("Unsupported colorspace");
        }

        ColorSpaceConverter.YCoCg2Rgb32(frame, rgbFrame);
        var frameImage = new FullImage(frame.Width, frame.Height) {
            Pixels = Rgb32.Instance.Decode(rgbFrame),
        };
        image2BinaryBitmap.Convert(frameImage)
            .Stream.WriteTo(Path.Combine(outputPath, $"frame{framePacket.FrameCount}.png"));

        Console.Write('+');
    }

    Console.WriteLine();
    Console.WriteLine("Done");
}

void Mods2Avi(FileInfo videoFile, string outputPath)
{
    Console.WriteLine("Input: {0}", videoFile.FullName);
    Console.WriteLine("Output: {0}", outputPath);

    Console.WriteLine("Decoding MODS video into an AVI file...");
    var watch = Stopwatch.StartNew();

    using DataStream outputStream = DataStreamFactory.FromFile(outputPath, FileOpenMode.Write);
    using Node videoNode = NodeFactory.FromFile(videoFile.FullName, FileOpenMode.Read)
        .TransformWith<Binary2Mods>()
        .TransformWith(new Mods2BinaryAvi(outputStream));

    watch.Stop();
    Console.WriteLine("Done in {0}", watch.Elapsed);
}

void Demux(FileInfo videoFile, string outputPath)
{
    string videoPath = Path.GetFullPath(Path.Combine(outputPath, videoFile.Name + ".rawvideo"));
    string audioPath = Path.GetFullPath(Path.Combine(outputPath, videoFile.Name + ".rawaudio"));

    Console.WriteLine("Input: {0}", videoFile.FullName);
    Console.WriteLine("Output video: {0}", videoPath);
    Console.WriteLine("Output audio: {0}", audioPath);

    using Node videoNode = NodeFactory.FromFile(videoFile.FullName, FileOpenMode.Read)
        .TransformWith<Binary2Mods>();

    ModsVideo video = videoNode.GetFormatAs<ModsVideo>()!;
    PrintVideoInfo(video);

    int framesCount = video.Info.FramesCount;
    int frame5Percentage = 5 * framesCount / 100;

    var mods2RawStreams = new Mods2RawContainer(convertYCbCr: true);
    mods2RawStreams.ProgressUpdate += (_, e) => {
        if ((e % frame5Percentage) == 0) {
            Console.Write("\rDecoding... {0} / {1} ({2:P2})", e, framesCount, (double)e / framesCount);
        }
    };

    var watch = Stopwatch.StartNew();
    using NodeContainerFormat outputs = mods2RawStreams.Convert(video);
    watch.Stop();

    outputs.Root.Children["video"]!.Stream!.WriteTo(videoPath);
    if (video.Info.AudioChannelsCount > 0) {
        outputs.Root.Children["audio"]!.Stream!.WriteTo(audioPath);
    }

    Console.WriteLine("\rDone in {0} ({1:F1} fps)", watch.Elapsed, framesCount / watch.Elapsed.TotalSeconds);

    Console.WriteLine();
    Console.WriteLine("Use the following ffmpeg command to convert to MP4:");

    Console.Write("ffmpeg ");
    if (video.Info.AudioChannelsCount > 0) {
        Console.Write(
            "-f s16le -channel_layout {0} -ar {1} -ac {2} -i {3} ",
            video.Info.AudioChannelsCount > 1 ? "stereo" : "mono",
            video.Info.AudioFrequency,
            video.Info.AudioChannelsCount,
            Path.GetFileName(audioPath));
    }

    Console.Write(
        "-f rawvideo -pix_fmt yuv420p -r {0:F1} -s {1}x{2} -i {3} ",
        video.Info.FramesPerSecond,
        video.Info.Width,
        video.Info.Height,
        Path.GetFileName(videoPath));
    Console.WriteLine(
        "-y -hide_banner -ac {0} {1}.mp4",
        video.Info.AudioChannelsCount,
        Path.GetFileNameWithoutExtension(videoFile.Name));
}
