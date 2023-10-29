namespace PlayMobic.IO;

using System;
using Yarhl.IO;

public class BitReader
{
    private const int MaxLength = 32 + 8;

    private ulong buffer;
    private int bufferLength;

    public BitReader(Stream stream, EndiannessMode endianness)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Stream = stream as DataStream ?? new DataStream(stream, 0, stream.Length, false);
        Endianness = endianness;
    }

    public DataStream Stream { get; }

    public EndiannessMode Endianness { get; }

    public int Read(int length)
    {
        EnsureEnoughBuffer(length);

        int value;
        if (Endianness == EndiannessMode.BigEndian) {
            uint mask = (1u << length) - 1u;
            value = (int)(buffer & mask);

            buffer >>= length;
        } else if (Endianness == EndiannessMode.LittleEndian) {
            value = (int)(buffer >> (bufferLength - length));

            uint mask = (1u << length) - 1u;
            uint inverseMask = ~(mask << (bufferLength - length));
            buffer &= inverseMask;
        } else {
            throw new NotSupportedException();
        }

        bufferLength -= length;

        return value;
    }

    public int ReadSigned(int length)
    {
        int sign = (Read(1) == 1) ? -1 : 1;
        return Read(length - 1) * sign;
    }

    public bool ReadBoolean(int length = 1)
    {
        return Read(length) != 0;
    }

    public int ReadEliasGammaCode()
    {
        int n = 0;

        // 1. Count number of consecutives 0's until first 1.
        while (Read(1) != 1) {
            n++;
        }

        // 2. Result is (1 << n) + number from the next 'n' bits.
        return (1 << n) + Read(n);
    }

    public int ReadExpGolomb()
    {
        return ReadEliasGammaCode() - 1;
    }

    public int ReadExpGolombSigned()
    {
        int value = ReadExpGolomb();
        return (value % 2) == 0
            ? -(value / 2)
            : (value / 2) + 1;
    }

    private void EnsureEnoughBuffer(int length)
    {
        if (bufferLength >= length) {
            return;
        }

        int missingBits = length - bufferLength;
        while (missingBits > 0) {
            int newData = Stream.ReadByte();
            if (newData == -1) {
                throw new EndOfStreamException();
            }

            if (Endianness == EndiannessMode.LittleEndian) {
                buffer = (buffer << 8) | (uint)newData;
            } else if (Endianness == EndiannessMode.BigEndian) {
                int shift = MaxLength - bufferLength - 8;
                buffer |= (uint)(newData << shift);
            } else {
                throw new NotSupportedException();
            }

            bufferLength += 8;
            missingBits -= 8;
        }
    }
}
