namespace PlayMobic.Video.Mobiclip;

internal static class HuffmanMotionModeTables
{
    public static readonly Dictionary<(int Width, int Height), Huffman> Tables = new() {
        [(16, 16)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] {
                1, 1, 1, 1, 1, 1, 1, 1, 8, 8, 8, 8, 9, 9, 9, 9,
                4, 3, 2, 2, 7, 7, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0,
            },
            bitCounts: new byte[] { 2, 2, 4, 5, 5, 5, 5, 4, 3, 3 }),

        [(8, 16)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 9, 9, 5, 4, 2, 2, 3, 8, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 4, 4, 4, 0, 0, 4, 3 }),

        [(4, 16)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 3, 3, 9, 5, 0, 0, 0, 0, 4, 8, 2, 2, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 3, 4, 4, 0, 0, 4, 4 }),

        [(2, 16)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                3, 3, 3, 3, 4, 4, 8, 5, 2, 2, 2, 2, 0, 0, 0, 0,
            },
            bitCounts: new byte[] { 3, 1, 3, 3, 4, 5, 0, 0, 5 }),

        [(16, 8)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 5, 4, 8, 8, 2, 2, 3, 9, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 4, 4, 4, 0, 0, 3, 4 }),

        [(16, 4)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 3, 3, 8, 4, 2, 2, 5, 9, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 3, 4, 4, 0, 0, 4, 4 }),

        [(16, 2)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 9, 4, 2, 2, 0, 0, 5, 3 },
            bitCounts: new byte[] { 3, 1, 3, 4, 4, 4, 0, 0, 0, 4 }),

        [(8, 8)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 3, 3, 5, 9, 4, 8, 2, 2, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 3, 4, 4, 0, 0, 4, 4 }),

        [(8, 4)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 2, 2, 2, 2, 8, 9, 3, 3, 5, 4, 0, 0, 1, 1, 1, 1 },
            bitCounts: new byte[] { 3, 2, 2, 3, 4, 4, 0, 0, 4, 4 }),

        [(8, 2)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 2, 2, 2, 2, 4, 4, 9, 5, 3, 3, 0, 0, 1, 1, 1, 1 },
            bitCounts: new byte[] { 3, 2, 2, 3, 3, 4, 0, 0, 0, 4 }),

        [(4, 8)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 3, 3, 9, 5, 8, 4, 2, 2, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 3, 4, 4, 0, 0, 4, 4 }),

        [(2, 8)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] {
                0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2,
                3, 3, 3, 3, 4, 4, 8, 5, 1, 1, 1, 1, 1, 1, 1, 1,
            },
            bitCounts: new byte[] { 2, 2, 2, 3, 4, 5, 0, 0, 5 }),

        [(4, 4)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] {
                0, 0, 0, 0, 0, 0, 0, 0, 4, 4, 4, 4, 3, 3, 3, 3,
                8, 9, 5, 5, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1,
            },
            bitCounts: new byte[] { 2, 2, 3, 3, 3, 4, 0, 0, 5, 5 }),

        [(4, 2)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 4, 4, 9, 5, 3, 3, 2, 2, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 3, 3, 4, 0, 0, 0, 4 }),

        [(2, 4)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 4, 4, 8, 5, 3, 3, 2, 2, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 3, 3, 4, 0, 0, 4 }),

        [(2, 2)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 5, 4, 1, 1, 0, 0, 3, 2 },
            bitCounts: new byte[] { 2, 2, 3, 3, 3, 3 }),
    };

    public static readonly Dictionary<(int Width, int Height), Huffman> StereoVideoTables = new() {
        [(16, 16)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] {
                8, 8, 8, 8, 8, 8, 8, 8, 2, 2, 2, 2, 3, 3, 6, 6,
                1, 1, 1, 1, 1, 1, 1, 1, 7, 7, 5, 4, 9, 9, 9, 9,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            },
            bitCounts: new byte[] { 1, 3, 4, 5, 6, 6, 5, 5, 3, 4 }),

        [(8, 16)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] {
                9, 9, 9, 9, 9, 9, 9, 9, 2, 2, 2, 2, 3, 3, 5, 4,
                1, 1, 1, 1, 1, 1, 1, 1, 8, 8, 8, 8, 0, 0, 0, 0,
            },
            bitCounts: new byte[] { 3, 2, 3, 4, 5, 5, 0, 0, 3, 2 }),

        [(4, 16)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 5, 4, 2, 2, 9, 9, 3, 8, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 4, 4, 4, 0, 0, 4, 3 }),

        [(2, 16)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 5, 4, 2, 2, 8, 3, 0, 0 },
            bitCounts: new byte[] { 3, 1, 3, 4, 4, 4, 0, 0, 4 }),

        [(16, 8)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] {
                2, 2, 2, 2, 9, 9, 9, 9, 8, 8, 8, 8, 8, 8, 8, 8,
                3, 3, 5, 4, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1,
            },
            bitCounts: new byte[] { 3, 2, 3, 4, 5, 5, 0, 0, 2, 3, 0, 0 }),

        [(16, 4)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 5, 4, 2, 2, 8, 8, 3, 9, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 4, 4, 4, 0, 0, 3, 4 }),

        [(16, 2)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 5, 4, 2, 2, 0, 0, 9, 3 },
            bitCounts: new byte[] { 3, 1, 3, 4, 4, 4, 0, 0, 0, 4 }),

        [(8, 8)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 3, 3, 5, 4, 2, 2, 9, 9, 8, 8, 0, 0, 1, 1, 1, 1 },
            bitCounts: new byte[] { 3, 2, 3, 3, 4, 4, 0, 0, 3, 3 }),

        [(8, 4)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                2, 2, 2, 2, 0, 0, 0, 0, 9, 9, 8, 8, 3, 3, 5, 4,
            },
            bitCounts: new byte[] { 3, 1, 3, 4, 5, 5, 0, 0, 4, 4 }),

        [(8, 2)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 9, 5, 2, 2, 0, 0, 4, 3 },
            bitCounts: new byte[] { 3, 1, 3, 4, 4, 4, 0, 0, 0, 4 }),

        [(4, 8)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                2, 2, 2, 2, 0, 0, 0, 0, 9, 9, 8, 8, 3, 3, 5, 4,
            },
            bitCounts: new byte[] { 3, 1, 3, 4, 5, 5, 0, 0, 4, 4 }),

        [(2, 8)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 8, 5, 2, 2, 0, 0, 4, 3 },
            bitCounts: new byte[] { 3, 1, 3, 4, 4, 4, 0, 0, 4 }),

        [(4, 4)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 3, 3, 9, 8, 5, 4, 2, 2, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 3, 4, 4, 0, 0, 4, 4 }),

        [(4, 2)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 5, 5, 3, 3, 9, 4, 2, 2, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 3, 4, 3, 0, 0, 0, 4 }),

        [(2, 4)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 0, 0, 4, 4, 3, 3, 8, 5, 2, 2, 1, 1, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 3, 3, 4, 0, 0, 4, 0 }),

        [(2, 2)] = HuffmanFactory.CreateFromSymbolsAndCountLists(
            symbols: new byte[] { 0, 0, 4, 5, 3, 2, 1, 1 },
            bitCounts: new byte[] { 2, 2, 3, 3, 3, 3 }),
    };
}
