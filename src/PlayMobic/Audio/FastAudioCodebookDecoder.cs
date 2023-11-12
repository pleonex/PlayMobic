namespace PlayMobic.Audio;
using System;
using System.IO;

/// <summary>
/// Decoder for the first version of FastAudio that requires a custom codebook to decode.
/// </summary>
public class FastAudioCodebookDecoder : IAudioDecoder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("", "S4487", Justification = "TODO")]
    private readonly Stream codebook;

    public FastAudioCodebookDecoder(Stream codebook)
    {
        this.codebook = codebook;
    }

    public byte[] Decode(Stream data, bool isCompleteBlock)
    {
        // https://wiki.multimedia.cx/index.php/Actimagine_Video_Codec
        // https://github.com/Gericom/MobiclipDecoder/blob/newer_stuff/LibMobiclip/Codec/Sx/SxDecoder.cs
        throw new NotImplementedException("FastAudioCodebook codec is not supported");
    }
}
