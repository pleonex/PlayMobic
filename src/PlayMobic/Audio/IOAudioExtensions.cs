namespace PlayMobic.Audio;
using System;
using Yarhl.IO;

public static class IOAudioExtensions
{
    public static void WriteInterleavedPCM16(this DataStream stream, Stream audioData, int channels)
    {
        int samplesPerChannel = (int)(audioData.Length / channels / 2);

        Span<byte> tempBuffer = stackalloc byte[2];
        for (int i = 0; i < samplesPerChannel; i++) {
            for (int c = 0; c < channels; c++) {
                audioData.Position = (i * 2) + (c * samplesPerChannel * 2);
                audioData.Read(tempBuffer);
                stream.Write(tempBuffer);
            }
        }
    }

    public static void ReadInterleavedPCM16(this DataStream stream, int audioDataLength, byte[] output, int channels)
    {
        if (output.Length < stream.Length) {
            throw new ArgumentException("Output is too small");
        }

        const int SamplesPerBlock = 256;
        int channelBlockSize = SamplesPerBlock * 2;
        int blockSize = channelBlockSize * channels;
        int blocksCount = audioDataLength / blockSize;

        int outputPos = 0;
        for (int b = 0; b < blocksCount; b++) {
            int blockOffset = b * blockSize;

            for (int i = 0; i < SamplesPerBlock; i++) {
                for (int c = 0; c < channels; c++) {
                    stream.Position = blockOffset + (c * channelBlockSize) + (i * 2);
                    stream.Read(output, outputPos, 2);
                    outputPos += 2;
                }
            }
        }
    }
}
