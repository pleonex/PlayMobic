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
    var image2BinaryBitmap = new FullImage2Bitmap();

    // This work because video is always the first stream in the packets
    // and we don't need to decode audio to advance to next frame.
    foreach (MediaPacket framePacket in demuxer.ReadFrames().OfType<VideoPacket>()) {
        FrameYuv420 frame = videoDecoder.DecodeFrame(framePacket.Data);

        byte[] rgbFrame = ColorSpaceConverter.YCoCg2Rgb32(frame);
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

void Demux(FileInfo videoFile, string outputPath)
{
    string videoPath = Path.Combine(outputPath, videoFile.Name + ".rawvideo");
    string audioPath = Path.Combine(outputPath, videoFile.Name + ".rawaudio");

    Console.WriteLine("Video: {0}", videoFile.FullName);
    Console.WriteLine("Output video: {0}", videoPath);
    PrintInfo(videoFile);
    var watch = Stopwatch.StartNew();

    using Node videoNode = NodeFactory.FromFile(videoFile.FullName, FileOpenMode.Read)
        .TransformWith<Binary2Mods>();

    ModsVideo video = videoNode.GetFormatAs<ModsVideo>()!;
    ModsInfo info = video.Info;

    int framesCount = info.FramesCount;
    int frame5Percentage = 5 * framesCount / 100;

    var videoDecoder = new MobiclipDecoder(info.Width, info.Height);
    using DataStream videoStream = DataStreamFactory.FromFile(videoPath, FileOpenMode.Write);

    ImaAdpcmDecoder[] audioDecoders = Enumerable.Range(0, info.AudioChannelsCount).Select(_ => new ImaAdpcmDecoder()).ToArray();
    using var audioInterleaveBuffer = new DataStream();
    using DataStream audioStream = DataStreamFactory.FromFile(audioPath, FileOpenMode.Write);

    var demuxer = new ModsDemuxer(video);
    foreach (MediaPacket framePacket in demuxer.ReadFrames()) {
        if (framePacket is VideoPacket) {
            FrameYuv420 frame = videoDecoder.DecodeFrame(framePacket.Data);

            // ffmpeg YCoCg is bugged, transform to YCbCr
            if (frame.ColorSpace is YuvColorSpace.YCoCg) {
                frame = ColorSpaceConverter.YCoCg2YCbCr(frame);
            }

            videoStream.Write(frame.PackedData);
        } else if (framePacket is AudioPacket audioPacket) {
            byte[] channelData = audioDecoders[audioPacket.TrackIndex].Decode(audioPacket.Data, audioPacket.IsKeyFrame);
            audioInterleaveBuffer.Write(channelData);

            if (audioPacket.TrackIndex + 1 == info.AudioChannelsCount) {
                audioStream.WriteInterleavedPCM16(audioInterleaveBuffer, info.AudioChannelsCount);
                audioInterleaveBuffer.Position = 0;
            }
        }

        if (framePacket.FrameCount % frame5Percentage == 0) {
            Console.Write("\rDecoding... {0}%   ", 100 * framePacket.FrameCount / framesCount);
        }
    }

    watch.Stop();
    Console.WriteLine("\rDecoding... 100%");
    Console.WriteLine("Done in {0} ({1} fps)", watch.Elapsed, framesCount / watch.Elapsed.TotalSeconds);
}
