namespace PlayMobic.Video.Mobiclip;

using System;

/// <summary>
/// Quantizates the coefficients of DCT to a smaller scale to have less bits.
/// </summary>
internal class Quantization
{
    private readonly int qp;

    public Quantization(int qp)
    {
        this.qp = qp;
    }

    public void Dequantize(int[] block)
    {
        throw new NotImplementedException();
    }
}
