using System.CommandLine;
using PlayMobic.Audio;
using PlayMobic.Container;
using Yarhl.FileSystem;
using Yarhl.IO;

return await new RootCommand("Tool for MODS videos") {
    SetupInfoCommand(),
    SetupExtractAudioCommand(),
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

Command SetupExtractAudioCommand()
{
    var inputArg = new Option<FileInfo>("--input", "Path to the .mods file") { IsRequired = true };
    var outputArg = new Option<string>("--output", "Path to the output .wav file") { IsRequired = true };
    var command = new Command("extract-audio", "Extract the first audio track") {
        inputArg,
        outputArg,
    };
    command.SetHandler(ExtractAudio, inputArg, outputArg);

    return command;
}

void PrintInfo(FileInfo videoFile)
{
    Console.WriteLine("Video: {0}", videoFile.FullName);

    using Node videoNode = NodeFactory.FromFile(videoFile.FullName, FileOpenMode.Read)
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

void ExtractAudio(FileInfo videoFile, string outputPath)
{
    Console.WriteLine("Video: {0}", videoFile.FullName);
    Console.WriteLine("Output: {0}", outputPath);

    using Node videoNode = NodeFactory.FromFile(videoFile.FullName, FileOpenMode.Read)
        .TransformWith<Binary2Mods>();

    ModsVideo video = videoNode.GetFormatAs<ModsVideo>()!;
    ModsInfo info = video.Info;

    var audioStream1 = new DataStream();
    var audioDecoder1 = new ImaAdpcmDecoder();

    var audioStream2 = new DataStream();
    var audioDecoder2 = new ImaAdpcmDecoder();

    var demuxer = new ModsDemuxer(video);
    foreach (FramePacket framePacket in demuxer.ReadFrames()) {
        Console.Write('.');

        Stream stream1 = framePacket.StreamPackets.Where(p => p.StreamIndex == 1).First().Data;
        byte[] output = audioDecoder1.Decode(stream1, framePacket.IsKeyFrame);
        audioStream1.Write(output);

        Stream stream2 = framePacket.StreamPackets.Where(p => p.StreamIndex == 2).First().Data;
        output = audioDecoder2.Decode(stream2, framePacket.IsKeyFrame);
        audioStream2.Write(output);
    }

    Console.WriteLine("Decoded - Saving");

    // TODO: Mix streams to create an stereo file
    audioStream1.Position = 0;
    audioStream2.Position = 0;
    ExportWave(audioStream1, 1, info.AudioFrequency, 16)
        .WriteTo(outputPath);

    Console.WriteLine();
    Console.WriteLine("Done");
}

DataStream ExportWave(DataStream waveData, int channels, int sampleRate, int bitsPerSample)
{
    var output = new DataStream();

    int byteRate = channels * sampleRate * bitsPerSample / 8;
    int fullSampleSize = channels * bitsPerSample / 8;

    var writer = new DataWriter(output);
    writer.Write("RIFF", nullTerminator: false);
    writer.Write((uint)(36 + waveData.Length));
    writer.Write("WAVE", nullTerminator: false);

    // Sub-chunk 'fmt'
    writer.Write("fmt ", nullTerminator: false);
    writer.Write((uint)16);             // Sub-chunk size
    writer.Write((ushort)1);    // Audio format
    writer.Write((ushort)channels);
    writer.Write(sampleRate);
    writer.Write(byteRate);
    writer.Write((ushort)fullSampleSize);
    writer.Write((ushort)bitsPerSample);

    // Sub-chunk 'data'
    writer.Write("data", nullTerminator: false);
    writer.Write((uint)waveData.Length);
    waveData.WriteTo(output);

    return output;
}
