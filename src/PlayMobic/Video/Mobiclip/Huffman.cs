namespace PlayMobic.Video.Mobiclip;

using System;
using System.Collections.Generic;
using PlayMobic.IO;

/// <summary>
/// Huffman implementation similar to the original decoder.
/// </summary>
internal class Huffman
{
    private readonly int codewordMaxLength;
    private readonly Node root;
    private readonly Dictionary<int, HuffmanCodeword> codewords;

    public Huffman(int codewordMaxLength)
    {
        this.codewordMaxLength = codewordMaxLength;
        root = Node.CreateNode();
        codewords = new Dictionary<int, HuffmanCodeword>();
    }

    public int ReadCodeword(BitReader reader)
    {
        int length = 0;
        Node? current = root;

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

    public HuffmanCodeword GetCodeword(int value)
    {
        return codewords[value];
    }

    public void InsertCodeword(int codeword, int bitCount, int value)
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

        codewords[value] = new HuffmanCodeword(codeword, bitCount, value);
    }

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
