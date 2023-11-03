namespace PlayMobic.Video.Mobiclip;

/// <summary>
/// Approximated integer based DCT transformation.
/// </summary>
/// <remarks>
/// This is the same implementation as H.264.
/// </remarks>
internal class DiscreteCosineTransformer
{
    public int[] InverseTransformation(int[] matrix, int size)
    {
        int[] output = new int[matrix.Length];

        // hard-coded DC
        matrix[0] += 32;

        // multiply matrix * IDCT
        for (int y = 0; y < size; y++) {
            InverseRow(matrix.AsSpan(y * size, size));
        }

        for (int y = 0; y < size; y++) {
            // transpose matrix
            for (int x = y + 1; x < size; x++) {
                int idx1 = (y * size) + x;
                int idx2 = (x * size) + y;
                (matrix[idx1], matrix[idx2]) = (matrix[idx2], matrix[idx1]);
            }

            // multiply by IDCT transpose
            InverseRow(matrix.AsSpan(y * size, size));

            for (int x = 0; x < size; x++) {
                // copy and de-scale factor 2^6 (scaled quantization)
                output[(y * size) + x] = matrix[(y * size) + x] >> 6;
            }
        }

        return output;
    }

    private static void InverseRow(Span<int> row)
    {
        if (row.Length == 4) {
            InverseRow4(row);
            return;
        }

        Span<int> tmp = stackalloc int[4] { row[0], row[2], row[4], row[6] };
        InverseRow4(tmp);

        int e = row[7] + row[1] - row[3] - (row[3] >> 1);
        int f = row[7] - row[1] + row[5] + (row[5] >> 1);
        int g = row[5] - row[3] - row[7] - (row[7] >> 1);
        int h = row[5] + row[3] + row[1] + (row[1] >> 1);
        int x3 = g + (h >> 2);
        int x2 = e + (f >> 2);
        int x1 = (e >> 2) - f;
        int x0 = h - (g >> 2);

        row[0] = tmp[0] + x0;
        row[1] = tmp[1] + x1;
        row[2] = tmp[2] + x2;
        row[3] = tmp[3] + x3;
        row[4] = tmp[3] - x3;
        row[5] = tmp[2] - x2;
        row[6] = tmp[1] - x1;
        row[7] = tmp[0] - x0;
    }

    private static void InverseRow4(Span<int> row)
    {
        // multiply matrix row by IDCT 4x4
        int a = row[0] + row[2];
        int b = row[0] - row[2];
        int c = row[1] + (row[3] >> 1);
        int d = (row[1] >> 1) - row[3];

        row[0] = a + c;
        row[1] = b + d;
        row[2] = b - d;
        row[3] = a - c;
    }
}
