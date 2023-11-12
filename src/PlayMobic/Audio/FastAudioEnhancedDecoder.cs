namespace PlayMobic.Audio;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using PlayMobic.IO;
using Yarhl.IO;

/// <summary>
/// Decoder for the revision of FastAudio that does not requires a custom codebook.
/// It's similar to mode 0 from original FastAudio.
/// </summary>
/// <remarks>
/// Refactor from the implementation of Mobius.
/// https://github.com/AdibSurani/Mobius
/// </remarks>
public class FastAudioEnhancedDecoder : IAudioDecoder
{
    private const int SegmentsCount = 4;
    private const int SegmentsSize = 64;
    private const int SamplesPerBlock = SegmentsSize * SegmentsCount;

    private static readonly double[] Codebook0_1 =
        Enumerable.Range(0, 8).Select(i => (i - 159.5) / 160)
        .Concat(Enumerable.Range(0, 11).Select(i => (i - 37.5) / 40))
        .Concat(Enumerable.Range(0, 27).Select(i => (i - 13.0) / 20))
        .Concat(Enumerable.Range(0, 11).Select(i => (i + 27.5) / 40))
        .Concat(Enumerable.Range(0, 7).Select(i => (i + 152.5) / 160))
        .ToArray();

    private static readonly double[] Codebook2_3 =
        Enumerable.Range(0, 7).Select(i => (i - 33.5) / 40)
        .Concat(Enumerable.Range(0, 25).Select(i => (i - 13.0) / 20))
        .ToArray();

    private static readonly double[][] Codebooks = new[] {
        Codebook0_1,
        Codebook0_1,
        Codebook2_3,
        Codebook2_3.Select(x => -x).Reverse().ToArray(),
        Enumerable.Range(0, 16).Select(i => (i * 0.22 / 3) - 0.6).ToArray(),
        Enumerable.Range(0, 16).Select(i => (i * 0.20 / 3) - 0.3).ToArray(),
        Enumerable.Range(0, 8).Select(i => (i * 0.36 / 3) - 0.4).ToArray(),
        Enumerable.Range(0, 8).Select(i => (i * 0.34 / 3) - 0.2).ToArray()
    };

    private static readonly int[] IndexSizes = new[] {
        // 5 is 0, because is table pos 2 which is overwritten from values later
        6, 6, 5, 5, 4, 0, 3, 3,
    };

    private readonly double[] filtersBuffer = new double[Codebooks.Length];
    private double lastSample;

    public byte[] Decode(Stream data, bool isCompleteBlock)
    {
        var reader = new BitReader(data, EndiannessMode.LittleEndian, 32);

        // First: get coefficients from each table - Requires in total 32-bits
        double[] codebookFilters = new double[Codebooks.Length];
        for (int i = 0; i < codebookFilters.Length; i++) {
            int valueSize = IndexSizes[i];
            int coefficientIndex = reader.Read(valueSize);

            codebookFilters[^(i + 1)] = Codebooks[i][coefficientIndex];
        }

        // Read inds and pads - Requires in total 32-bits
        double[] scales = new double[SegmentsCount];
        Span<byte> tmpBuffer = stackalloc byte[4];
        for (int i = 0; i < scales.Length; i++) {
            // Read a float value into C# IEE754 standard
            int binValue = reader.Read(6);
            binValue = (binValue + 1) << 20;
            BinaryPrimitives.WriteInt32LittleEndian(tmpBuffer, binValue);
            double value = BinaryPrimitives.ReadSingleLittleEndian(tmpBuffer);

            value *= Math.Pow(2, 116);
            scales[^(i + 1)] = value;
        }

        int[] paddings = new int[SegmentsCount];
        for (int i = 0; i < paddings.Length; i++) {
            // 2 bits gives 4 positions of padding.
            // This shifts all the positions in each segment,
            // needing to iterate 20 times later to reach the 64 positions.
            paddings[^(i + 1)] = reader.Read(2);
        }

        // There are 4 segments of 64 bits (2 pairs of 32-bits each)
        int indexCodebook5 = 0;
        double[] results = new double[SegmentsSize * SegmentsCount];
        for (int i = 0; i < SegmentsCount; i++) {
            double scale = scales[i];
            int resultIdxBase = (i * SegmentsSize) + paddings[i];

            int sample60 = 0;
            for (int j = 0; j < 20; j++) {
                int value = reader.Read(3);
                results[resultIdxBase + (j * 3)] = scale * ((2 * value) - 7);

                // every 10 values, the last 2-bits (10*3=30) of that 32-bits value
                // are used to for the last sample in the block.
                if (j % 10 == 9) {
                    sample60 = (sample60 << 2) | reader.Read(2);
                }
            }

            // Set sample at 60 + padding.
            // No need to multiply by 2 as we did it while getting the value
            results[resultIdxBase + (20 * 3)] = scale * (sample60 - 7);

            // its last bit indicates the coefficient to move.
            indexCodebook5 = (indexCodebook5 << 1) | (sample60 % 2);
        }

        // At the beggining table[2] from codebook[5] was not set (read 0 bits)
        // we set it now from the values we read in the previous blocks.
        codebookFilters[2] = Codebooks[5][indexCodebook5];

        // Now let's apply the filter
        byte[] output = new byte[SamplesPerBlock * 2];
        var writer = new DataWriter(DataStreamFactory.FromArray(output));
        for (int i = 0; i < results.Length; i++) {
            double sampleDiff = results[i];

            // For each value in the buffer, multiply for its coefficient and substract:
            // sample = diff - sum(codebook * buffer)
            // and update the buffer so that
            // buffer = buffer*(1-codebook^2) + codebook*diff_i
            for (int j = 0; j < filtersBuffer.Length; j++) {
                sampleDiff -= codebookFilters[j] * filtersBuffer[j];
                filtersBuffer[j] += codebookFilters[j] * sampleDiff;
            }

            // Append diff to the end of the circular buffer
            AppendToBuffer(sampleDiff);

            // Diff from previous sample
            lastSample = sampleDiff + (lastSample * 0.86);

            // Samples are in range 0-1, re-scale to 16-bits
            short pcm16 = (short)Math.Clamp(lastSample * 65536, short.MinValue, short.MaxValue);

            writer.Write(pcm16);
        }

        return output;
    }

    private void AppendToBuffer(double sample)
    {
        // Skip/Overwrite first and move everything one back
        for (int i = 0; i < filtersBuffer.Length - 1; i++) {
            filtersBuffer[i] = filtersBuffer[i + 1];
        }

        // Write new last
        filtersBuffer[^1] = sample;
    }
}
