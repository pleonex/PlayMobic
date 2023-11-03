namespace PlayMobic.Video.Mobiclip;
using System;
using System.Reflection;
using Yarhl.IO;

/// <summary>
/// Factory of Huffman trees following the original binary tree definitions.
/// </summary>
internal static class HuffmanFactory
{
    public static Huffman CreateFromResidualTable(string resourceName)
    {
        using Stream tableStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException("Missing huffman table");

        // Format is for each 16-bits item:
        // index (13 bits max): codeword
        // bit0-3: number of codeword bits (to clean up the index)
        // bit4-15: value
        // this is a trick to decode huffman via a hash table lookup (fast reads)
        // it repeats the same value for all the variant of short codewords
        // so for a codeword of 10 bits, it will repeat the value for all combinations
        // of that codeword and its 3 remaining bits.
        int numItems = (int)(tableStream.Length / 2);
        var reader = new DataReader(tableStream);

        const int MaxCodewordLength = 13;
        var huffman = new Huffman(MaxCodewordLength);

        for (int i = 0; i < numItems; i++) {
            ushort item = reader.ReadUInt16();

            int bitCount = item & 0xF;
            int value = item >> 4;
            int codeword = i >> (MaxCodewordLength - bitCount);

            if (bitCount == 1) {
                // padding
                continue;
            }

            huffman.InsertCodeword(codeword, bitCount, value);
        }

        // the codeword for 0 is "hard-coded" in code
        huffman.InsertCodeword(0b00000011, 8, 0);

        return huffman;
    }

    public static Huffman CreateFromSymbolsAndCountLists(byte[] symbols, byte[] bitCounts)
    {
        // similar to above, the index gives the codeword for each symbol.
        // the value points also to the bits count of the codeword
        int maxCodewordLength = (int)Math.Log2(symbols.Length);

        var huffman = new Huffman(maxCodewordLength);
        for (int i = 0; i < symbols.Length; i++) {
            int symbol = symbols[i];
            int bitCount = bitCounts[symbol];
            int codeword = i >> (maxCodewordLength - bitCount);

            huffman.InsertCodeword(codeword, bitCount, symbol);
        }

        return huffman;
    }
}
