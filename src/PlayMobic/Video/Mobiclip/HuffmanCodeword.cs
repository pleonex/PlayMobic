namespace PlayMobic.Video.Mobiclip;

public readonly struct HuffmanCodeword
{
    public HuffmanCodeword(int code, int bitCount, int value)
    {
        Code = code;
        BitCount = bitCount;
        Value = value;
    }

    public int Code { get; }

    public int BitCount { get; }

    public int Value { get; }
}
