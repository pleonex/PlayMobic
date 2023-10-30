namespace PlayMobic.Video.Mobiclip;

/// <summary>
/// Approximated integer based DCT transformation.
/// </summary>
internal class DiscreteCosineTransformer
{
    private readonly int tableIndex;

    public DiscreteCosineTransformer(int tableIndex)
    {
        this.tableIndex = tableIndex;
    }

    public void InverseTransformation(int[] coefficients)
    {
        throw new NotImplementedException();
    }
}
