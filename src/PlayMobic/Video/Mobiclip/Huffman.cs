namespace PlayMobic.Video.Mobiclip;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PlayMobic.IO;
using Yarhl.IO;

/// <summary>
/// Huffman implementation similar to the original decoder.
/// </summary>
/// <remarks>
/// It builds the tree from the original block of data to showcase its format.
/// </remarks>
internal class Huffman
{
    private static readonly string[] MobiclipTable = new string[] {
        typeof(Huffman).Namespace + ".huffman0.bin",
        typeof(Huffman).Namespace + ".huffman1.bin",
    };

    private readonly int codewordMaxLength;
    private readonly Node root;
    private readonly Dictionary<int, Codeword> codewords;

    public Huffman(int codewordMaxLength)
    {
        this.codewordMaxLength = codewordMaxLength;
        root = Node.CreateNode();
        codewords = new Dictionary<int, Codeword>();
    }

    public static Huffman LoadFromFullIndexTable(int tableIdx)
    {
        string tablePath = MobiclipTable[tableIdx];
        using Stream tableStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(tablePath)
            ?? throw new FileNotFoundException("Missing huffman table");

        // Format is for each item:
        // index: codeword
        int numItems = (int)(tableStream.Length / 2);
        var reader = new DataReader(tableStream);

        var huffman = new Huffman(13);

        for (int i = 0; i < numItems; i++) {
            ushort item = reader.ReadUInt16();

            int bitCount = item & 0xF;
            int value = item >> 4;
            int codeword = i >> (13 - bitCount);

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

    public int ReadCodeword(BitReader reader)
    {
        int length = 0;
        Node? current = root.Left ?? // first bit 0 is skipped, we start directly
            throw new InvalidOperationException("Huffman is not initialized");

        // Naviage the huffman tree bit by bit until we find a node at the end
        // it contains our uncompressed value.
        while (length < codewordMaxLength) {
            int branch = reader.Read(1);
            length++;

            current = (branch == 0) ? current.Left : current.Right;
            if (current is null) {
                throw new InvalidOperationException("Invalid huffman tree search");
            }

            if (current.IsChild) {
                return current.Value;
            }
        }

        throw new FormatException("codeword not found");
    }

    public (int Codeword, int BitCount) GetCodeword(int value)
    {
        Codeword item = codewords[value];
        return (item.Code, item.BitCount);
    }

    private void InsertCodeword(int codeword, int bitCount, int value)
    {
        // Find the parent
        Node current = root;
        for (int i = 0; i < bitCount - 1; i++) {
            int branch = codeword & (1 << (bitCount - 1 - i));

            if (branch == 0) {
                current.Left ??= Node.CreateNode();
                current = current.Left;
            } else {
                current.Right ??= Node.CreateNode();
                current = current.Right;
            }

            if (current.IsChild) {
                throw new InvalidOperationException("Invalid huffman tree");
            }
        }

        // Get the child and insert it if it didn't exist
        int childBranch = codeword & 1;
        if (childBranch == 0) {
            current.Left ??= Node.CreateChild(value);
            current = current.Left;
        } else {
            current.Right ??= Node.CreateChild(value);
            current = current.Right;
        }

        // Verify the child has the expected value
        if (!current.IsChild || current.Value != value) {
            throw new InvalidOperationException("Invalid huffman tree");
        }

        codewords[value] = new Codeword(codeword, bitCount);
    }

    private sealed record Codeword(int Code, int BitCount);
    private sealed record Node
    {
        private Node()
        {
        }

        public bool IsChild { get; init; }

        public int Value { get; init; }

        public Node? Left { get; set; }

        public Node? Right { get; set; }

        public static Node CreateChild(int value)
        {
            return new Node { IsChild = true, Value = value };
        }

        public static Node CreateNode()
        {
            return new Node { IsChild = false };
        }
    }
}
