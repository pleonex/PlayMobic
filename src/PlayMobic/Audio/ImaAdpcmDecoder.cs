namespace PlayMobic.Audio;

using Yarhl.IO;

/// <summary>
/// Decode 4-bits IMA-ADPCM binary data into PCM 16-bits wave samples.
/// </summary>
public class ImaAdpcmDecoder
{
    private const int SamplesPerBlock = 256;

    private static readonly int[] StepIndexTable = new int[8] {
        -1, -1, -1, -1, 2, 4, 6, 8,
    };

    // step(n+1) = step(n) * 1.1M(L(n))
    private static readonly short[] StepTable = new short[89] {
        7, 8, 9, 10, 11, 12, 13, 14,
        16, 17, 19, 21, 23, 25, 28,
        31, 34, 37, 41, 45, 50, 55,
        60, 66, 73, 80, 88, 97, 107,
        118, 130, 143, 157, 173, 190, 209,
        230, 253, 279, 307, 337, 371, 408,
        449, 494, 544, 598, 658, 724, 796,
        876, 963, 1060, 1166, 1282, 1411, 1552,
        1707, 1878, 2066, 2272, 2499, 2749, 3024, 3327, 3660, 4026,
        4428, 4871, 5358, 5894, 6484, 7132, 7845, 8630,
        9493, 10442, 11487, 12635, 13899, 15289, 16818,
        18500, 20350, 22385, 24623, 27086, 29794, 32767
    };

    private readonly byte[] output = new byte[SamplesPerBlock * 2];  // 16-bits PCM
    private int stepIndex;
    private int predictor;

    public byte[] Decode(Stream data, bool isCompleteBlock)
    {
        data.Position = 0;

        using DataStream outputStream = DataStreamFactory.FromArray(output);
        var writer = new DataWriter(outputStream);

        if (isCompleteBlock) {
            var headerReader = new DataReader(data);
            stepIndex = headerReader.ReadUInt16();
            predictor = headerReader.ReadInt16();
        }

        for (int i = 0; i < SamplesPerBlock; i += 2) {
            int value = data.ReadByte();

            short sample0 = DecodeSample(value & 0x0F);
            writer.Write(sample0);

            short sample1 = DecodeSample(value >> 4);
            writer.Write(sample1);
        }

        return output;
    }

    private short DecodeSample(int data)
    {
        short step = StepTable[stepIndex];

        int diff = step >> 3;
        if ((data & 4) != 0) {
            diff += step;
        }

        if ((data & 2) != 0) {
            diff += step >> 1;
        }

        if ((data & 1) != 0) {
            diff += step >> 2;
        }

        if ((data & 8) != 0) {
            diff = -diff;
        }

        predictor += diff;

        stepIndex += StepIndexTable[data & 7];
        stepIndex = Math.Clamp(stepIndex, 0, StepTable.Length - 1);

        return (short)Math.Clamp(predictor, short.MinValue, short.MaxValue);
    }
}
