namespace PlayMobic.Audio;
using System.IO;
using Yarhl.IO;

public class RawPcm16Decoder : IAudioDecoder
{
    private const int SamplesCount = 256;

    public byte[] Decode(Stream data, bool isCompleteBlock)
    {
        var reader = new DataReader(data);
        return reader.ReadBytes(SamplesCount * 2);
    }
}
