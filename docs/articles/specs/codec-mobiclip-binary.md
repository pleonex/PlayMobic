# Mobiclip encoding - binary format overview

> [!NOTE]  
> The binary format of the codec based on H.264 is not easy to describe with
> tables. As an example the official H.264 specification explains the format
> (_syntax in tabular form_) with pseudo-code on a table (section 7.3). I prefer
> to try to keep it simple using some words instead of code.

Binary frames are saved independently in the _MODS_ container. Data is saved on
a _bit-stream_ where we are reading bit by bit, not on a byte boundary. Frames
ends on a full byte. Each frame consists on:

| Bits | Type      | Description                                         |
| ---- | --------- | --------------------------------------------------- |
| 1    | int       | Frame prediction kind: 0 = P (inter), 1 = I (intra) |
| ...  | FrameData | Prediction frame data                               |

## I frames prediction

| Bits | Type         | Description                         |
| ---- | ------------ | ----------------------------------- |
| 1    | int          | Color space: 0 = YCoCg, 1 = YCbCr   |
| 1    | int          | Entropy VLC table (huffman and RLE) |
| 6    | int          | Quantization parameter              |
| ...  | MacroBlock[] | Macroblock data                     |

## P frame prediction

| Bits | Type              | Description                  |
| ---- | ----------------- | ---------------------------- |
| ...  | Signed exp-golomb | Quantization delta parameter |
| ...  | PMacroBlock[]     | P frames macroblocks data    |

### P MacroBlocks

| Bits | Type | Description                         |
| ---- | ---- | ----------------------------------- |
| ...  | int  | VLC encoded motion prediction index |

- If index is 6: [intra macroblock](#intra-macroblock) data without residual
- If index is 7: [intra macroblock](#intra-macroblock) data with residual
- Otherwise: [motion predicted macroblock](#p-macroblock)

## Intra macroBlock

| Bits | Type       | Description                                                         |
| ---- | ---------- | ------------------------------------------------------------------- |
| 1    | bool       | (**Only for I macroblocks**) Has macroblock level _prediction mode_ |
| ...  | Exp-Golomb | Block 8x8 residual information table index                          |
| 3    | int        | **Only** if it has _macroblock level mode_: prediction mode         |
| ...  | Exp-Golomb | **Only** if _macroblock level mode_ is delta plane: value           |
| ...  | Block[]    | 8x8 blocks in Luma macroblock                                       |
| 3    | int        | Prediction mode for chroma blocks U and V                           |
| ...  | Block      | U block data                                                        |
| ...  | Block      | V block data                                                        |

## P MacroBlock

| Bits | Type                | Description                               |
| ---- | ------------------- | ----------------------------------------- |
| ...  | MotionPredicition[] | Motion prediction data for the macroblock |
| ...  | PBlock[]            | Luma residual                             |
| ...  | PBlock[]            | U block data                              |
| ...  | PBlock[]            | V block data                              |

### Motion prediction

- If index is less or equal to 5:

| Bits | Type              | Description         |
| ---- | ----------------- | ------------------- |
| ...  | Signed exp-Golomb | Delta X destination |
| ...  | Signed exp-Golomb | Delta Y destination |

- Otherwise

| Bits | Type   | Description              |
| ---- | ------ | ------------------------ |
| ...  | int[2] | VLC motion encoded index |

## Block

If the 8x8 block does **not** have residual:

| Bits | Type            | Description                                                |
| ---- | --------------- | ---------------------------------------------------------- |
| ...  | pModePrediction | **Only** if it does not have _macroblock level mode_: mode |
| ...  | Exp-Golomb      | **Only** if _macroblock level mode_ is delta plane: value  |

Otherwise, **there is one _exp-golomb_ value** with the index to the _block 4x4
residual info table_. If this index is 0, then we process it as a single 8x8
block. Otherwise we split further into 4x4 pixel blocks. For each of them
(single or multiple):

| Bits | Type            | Description                                             |
| ---- | --------------- | ------------------------------------------------------- |
| ...  | pModePrediction | **Only** it does NOT have _macroblock level mode_: mode |
| ...  | Exp-Golomb      | **Only** previous mode is delta plane: value            |
| ...  | Residual        | Residual information encoded with VLC                   |

## P block

| Bits | Type       | Description                                              |
| ---- | ---------- | -------------------------------------------------------- |
| ...  | Exp-Golomb | P frame coefficient 4x4 table index                      |
| ...  | Residual[] | Coefficients for each 4x4 block or full block if index 0 |

## Intra mode prediction

| Bits | Type | Description                             |
| ---- | ---- | --------------------------------------- |
| 1    | bool | Indicates if predicted value is correct |
| 3    | int  | **if above is 0**, intra mode           |

If the encoded intra mode is equal or bigger than the predicted value, add 1.
This allows to encoded 9 values (0-8) with 3 bits instead of 4.

## Residual

Residual coefficient transformation matrix is encoded in blocks of 12-bits.
These blocks are encoded with Huffman with pre-established trees (two to choose
per frame).

The decoded huffman value has:

- 0-4: amplitude / value
- 5-10: zeroes run
- 11: boolean indicating if this is the last value for the matrix (rest zeroes)

If amplitude is zero, then a bit indicates if there is another block used to
increase amplitude. Otherwise if the next bit is zero, then another block to
increase run. Otherwise it reads:

- end of block: 1 bit
- zeroes run: 6 bits
- amplitude: 12 bits

It's repeated until end of block is `1`.
