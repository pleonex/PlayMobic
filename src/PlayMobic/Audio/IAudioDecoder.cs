namespace PlayMobic.Audio;

public interface IAudioDecoder
{
    byte[] Decode(Stream data, bool isCompleteBlock);
}
