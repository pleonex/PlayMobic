namespace PlayMobic.Video.Mobiclip;
using System;
using PlayMobic.IO;

internal class ResidualEncoding
{
    private readonly BitReader reader;
    private readonly DiscreteCosineTransformer dct;
    private readonly Quantization quantization;
    private readonly EntropyVlcEncoding entropyVlc;

    public ResidualEncoding(BitReader reader, int vlcTableIndex, int quantizerIndex)
    {
        this.reader = reader;
        dct = new DiscreteCosineTransformer();
        quantization = new Quantization(quantizerIndex);
        entropyVlc = new EntropyVlcEncoding(vlcTableIndex);
    }

    public void DecodeAndAddResidual(PixelBlock block)
    {
        // 1. VLC to get residual DC coefficients matrix
        int[] coefficients = entropyVlc.DecodeResidual(reader, block.Width * block.Height);

        // 2. Dequantize to re-store scale
        quantization.Dequantize(coefficients);

        // 3. Apply inverse DCT to decode
        int[] residual = dct.InverseTransformation(coefficients, block.Width);

        // 4. Sum the residual to the predicted colors in the block.
        for (int y = 0; y < block.Height; y++) {
            for (int x = 0; x < block.Width; x++) {
                int idx = x + (y * block.Width);
                block[x, y] = (byte)Math.Clamp(block[x, y] + residual[idx], byte.MinValue, byte.MaxValue);
            }
        }
    }
}
