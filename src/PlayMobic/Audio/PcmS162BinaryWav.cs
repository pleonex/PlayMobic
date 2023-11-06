namespace PlayMobic.Audio;

using Yarhl.FileFormat;
using Yarhl.IO;

public class PcmS162BinaryWav : IConverter<IBinary, BinaryFormat>
{
    private readonly int channels;
    private readonly int sampleRate;
    private readonly int bitsPerSample;

    public PcmS162BinaryWav(int channels, int sampleRate, int bitsPerSample)
    {
        this.channels = channels;
        this.sampleRate = sampleRate;
        this.bitsPerSample = bitsPerSample;
    }

    public BinaryFormat Convert(IBinary source)
    {
        var output = new BinaryFormat();

        int byteRate = channels * sampleRate * bitsPerSample / 8;
        int fullSampleSize = channels * bitsPerSample / 8;

        var writer = new DataWriter(output.Stream);
        writer.Write("RIFF", nullTerminator: false);
        writer.Write((uint)(36 + source.Stream.Length));
        writer.Write("WAVE", nullTerminator: false);

        // Sub-chunk 'fmt'
        writer.Write("fmt ", nullTerminator: false);
        writer.Write(16);           // Sub-chunk size
        writer.Write((ushort)1);    // Audio format
        writer.Write((ushort)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((ushort)fullSampleSize);
        writer.Write((ushort)bitsPerSample);

        // Sub-chunk 'data'
        writer.Write("data", nullTerminator: false);
        writer.Write((uint)source.Stream.Length);
        source.Stream.WriteTo(output.Stream);

        return output;
    }
}
