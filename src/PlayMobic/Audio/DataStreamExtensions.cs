namespace PlayMobic.Audio;
using System;
using Yarhl.IO;

public static class DataStreamExtensions
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
}
