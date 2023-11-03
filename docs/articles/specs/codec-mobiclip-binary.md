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

### Intra macroBlock

| Bits | Type         | Description                                                         |
| ---- | ------------ | ------------------------------------------------------------------- |
| 1    | bool         | (**Only for I macroblocks**) Has macroblock level _prediction mode_ |
| ...  | Exp-Golomb   | Block 8x8 residual information table index                          |
| 3    | int          | **Only** if it has _macroblock level mode_: prediction mode         |
| ...  | Exp-Golomb   | **Only** if _macroblock level mode_ is delta plane: value           |
| ...  | IntraBlock[] | 8x8 blocks in Luma macroblock                                       |
| 3    | int          | Prediction mode for chroma blocks U and V                           |
| ...  | IntraBlock   | U block data                                                        |
| ...  | IntraBlock   | V block data                                                        |

### Intra block

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

### Intra mode prediction

| Bits | Type | Description                             |
| ---- | ---- | --------------------------------------- |
| 1    | bool | Indicates if predicted value is correct |
| 3    | int  | **if above is 0**, intra mode           |

If the encoded intra mode is equal or bigger than the predicted value, add 1.
This allows to encoded 9 values (0-8) with 3 bits instead of 4.

## P frame prediction

| Bits | Type              | Description                  |
| ---- | ----------------- | ---------------------------- |
| ...  | Signed exp-golomb | Quantization delta parameter |
| ...  | PBlock[]          | P macroblocks data           |

### P blocks

| Bits | Type           | Description                                     |
| ---- | -------------- | ----------------------------------------------- |
| ...  | int            | Huffman encoded prediction mode                 |
| ...  | ...            | Block prediction data dependant on mode         |
| ...  | Exp-Golomb     | Luma block 8x8 residual information table index |
| ...  | PBlockResidual | Luma 8x8 blocks residual                        |
| ...  | PBlockResidual | Chroma U residual                               |
| ...  | PBlockResidual | Chroma V residual                               |

The prediction modes are:

- 0: motion compensation from current frame
- 1-5: motion compensation with delta vector from past frame
- 6: [intra macroblock](#intra-macroblock) data with mode per macroblock
- 7: [intra macroblock](#intra-macroblock) data with mode per block (prediction)
- 8: block partitioning by height, repeat for each.
- 9: block partitioning by width, repeat for each.

Modes 1-5 have additional data:

| Bits | Type              | Description         |
| ---- | ----------------- | ------------------- |
| ...  | Signed exp-Golomb | Delta X destination |
| ...  | Signed exp-Golomb | Delta Y destination |

### P block residual

| Bits | Type       | Description                                          |
| ---- | ---------- | ---------------------------------------------------- |
| ...  | Exp-Golomb | Block 4x4 residual information table index           |
| ...  | Residual[] | Residual for each 4x4 block or full block if index 0 |

> [!NOTE]  
> Different to the intra block residual, we don't subtract 1 to the index of the
> 4x4 block table.

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
