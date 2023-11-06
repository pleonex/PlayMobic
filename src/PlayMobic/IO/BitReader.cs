namespace PlayMobic.IO;

using System;
using Yarhl.IO;

public class BitReader
{
    private const int MaxLength = 32 + 8;

    private readonly DataReader reader;
    private readonly int blockSize;
    private ulong buffer;
    private int bufferLength;

    public BitReader(Stream stream, EndiannessMode endianness)
        : this(stream, endianness, 8)
    {
    }

    public BitReader(Stream stream, EndiannessMode endianness, int blockSize)
    {
        ArgumentNullException.ThrowIfNull(stream);

        reader = new DataReader(stream) { Endianness = endianness };
        Endianness = endianness;
        this.blockSize = blockSize;
    }

    public DataStream Stream => reader.Stream;

    public bool EndOfStream => (bufferLength == 0) && Stream.EndOfStream;

    public EndiannessMode Endianness { get; }

    public long BitPosition { get; private set; }

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
        BitPosition += length;

        return value;
    }

    public int ReadSigned(int length)
    {
        int value = Read(length);

        int sign = (value >> (length - 1)) == 1 ? -1 : 0;
        sign <<= length - 1;

        // We can't multiply because it would do two's complement, we just want
        // to set all bits to 1. Shifting a SIGNED int will do that
        int mask = (1 << (length - 1)) - 1;
        value = (value & mask) | sign;

        return value;
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
            uint newData = blockSize switch {
                8 => reader.ReadByte(),
                16 => reader.ReadUInt16(),
                32 => reader.ReadUInt32(),
                _ => throw new NotSupportedException("Unsupported block size"),
            };

            if (Endianness == EndiannessMode.LittleEndian) {
                buffer = (buffer << blockSize) | newData;
            } else if (Endianness == EndiannessMode.BigEndian) {
                int shift = MaxLength - bufferLength - blockSize;
                buffer |= newData << shift;
            } else {
                throw new NotSupportedException();
            }

            bufferLength += blockSize;
            missingBits -= blockSize;
        }
    }
}
