namespace PlayMobic;

using System.Diagnostics;
using System.Text;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

public class FfmpegConverter : IConverter<NodeContainerFormat, BinaryFormat>
{
    private readonly FfmpegConverterParameters parameters;

    public FfmpegConverter(FfmpegConverterParameters parameters)
    {
        this.parameters = parameters;
    }

    public BinaryFormat Convert(NodeContainerFormat source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var process = new Process();
        process.StartInfo.FileName = parameters.ExecutablePath;
        process.StartInfo.Arguments = GetArguments();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        _ = process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0) {
            throw new FormatException($"Error running: {parameters.ExecutablePath} {process.StartInfo.Arguments}");
        }

        return new BinaryFormat(DataStreamFactory.FromFile(parameters.OutputPath, FileOpenMode.ReadWrite));
    }

    private string GetArguments()
    {
        var arguments = new StringBuilder();

        var videoInfo = parameters.VideoInfo;
        if (videoInfo.AudioChannelsCount > 0) {
            _ = arguments.AppendFormat(
                "-f s16le -channel_layout {0} -ar {1} -ac {2} -i {3} ",
                videoInfo.AudioChannelsCount > 1 ? "stereo" : "mono",
                videoInfo.AudioFrequency,
                videoInfo.AudioChannelsCount,
                parameters.RawAudioPath);
        }

        _ = arguments.AppendFormat(
            "-f rawvideo -pix_fmt yuv420p -r {0:F1} -s {1}x{2} -i {3} ",
            videoInfo.FramesPerSecond,
            videoInfo.Width,
            videoInfo.Height,
            parameters.RawVideoPath);
        _ = arguments.AppendFormat(
            "-y -hide_banner -ac {0} {1}",
            videoInfo.AudioChannelsCount,
            parameters.OutputPath);

        return arguments.ToString();
    }
}
