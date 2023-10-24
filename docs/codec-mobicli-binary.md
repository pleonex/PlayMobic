# Mobiclip encoding - binary format overview

Binary frames are saved independently in the _MODS_ container. Data is saved on
a _bit-stream_ where we are reading bit by bit, not on a byte boundary. Frames
ends on a full byte. Each frame consists on:

| Bits | Type      | Description                                |
| ---- | --------- | ------------------------------------------ |
| 1    | int       | Frame kind: 0 = P, 1 = I                   |
| ...  | FrameData | Data of the frame that depends on the kind |

## I frames

| Bits | Type         | Description                                       |
| ---- | ------------ | ------------------------------------------------- |
| 1    | int          | Encoding format: 0 = MODS (NDS), 1 = Moflex (3DS) |
| 1    | int          | DCT table index                                   |
| 6    | int          | Quantization index                                |
| ...  | MacroBlock[] | Macroblock data                                   |

## P frame

| Bits | Type              | Description               |
| ---- | ----------------- | ------------------------- |
| ...  | Signed exp-golomb | Quantization delta index  |
| ...  | PMacroBlock[]     | P frames macroblocks data |

### P MacroBlocks

| Bits | Type | Description                             |
| ---- | ---- | --------------------------------------- |
| ...  | int  | Huffman encoded motion prediction index |

- If index is 6: [macroblock](#macroblock) data without coefficient
- If index is 7: [macroblock](#macroblock) data with coefficients
- Otherwise: [motion predicted macroblock](#motion-prediction-macroblock)

## MacroBlock

| Bits | Type       | Description                                            |
| ---- | ---------- | ------------------------------------------------------ |
| 1    | bool       | Has _P_ (I also??) mode prediction                     |
| ...  | Exp-Golomb | Block 8x8 coefficient table index                      |
| (3)  | int        | **Only** if it does not have P mode prediction: P mode |
| ...  | Block[]    | For each 4x4 block in Luma macroblock: data            |
| 3    | int        | P mode for UV                                          |
| ...  | Block      | U block data                                           |
| ...  | Block      | V block data                                           |

## P MacroBlock

| Bits | Type                | Description                               |
| ---- | ------------------- | ----------------------------------------- |
| ...  | MotionPredicition[] | Motion prediction data for the macroblock |
| ...  | PBlock[]            | Luma coefficients                         |
| ...  | PBlock[]            | U block data                              |
| ...  | PBlock[]            | V block data                              |

### Motion prediction

- If index is less or equal to 5:

| Bits | Type              | Description         |
| ---- | ----------------- | ------------------- |
| ...  | Signed exp-Golomb | Delta X destination |
| ...  | Signed exp-Golomb | Delta Y destination |

- Otherwise

| Bits | Type   | Description                  |
| ---- | ------ | ---------------------------- |
| ...  | int[2] | Huffman motion encoded index |

## Block

If it doesn't have coefficient: no more data for the block, fully predicted.

| Bits | Type       | Description                                              |
| ---- | ---------- | -------------------------------------------------------- |
| ...  | Exp-Golomb | Block 4x4 coefficient table index                        |
| ...  | Intra[]    | For each block (or only one), data from intra prediction |

If coefficient table index is 0: run predict intra for block. Otherwise split in
4x4 blocks

## P block

| Bits | Type           | Description                                              |
| ---- | -------------- | -------------------------------------------------------- |
| ...  | Exp-Golomb     | P frame coefficient 4x4 table index                      |
| ...  | Coefficients[] | Coefficients for each 4x4 block or full block if index 0 |

## Intra prediction

First, if _P mode prediction_, read P mode:

| Bits | Type | Description       |
| ---- | ---- | ----------------- |
| 1    | bool | Has encoded value |
| 3    | int  | X value           |

Then if we need to add coefficient, read them.

## Coefficients

| Bits | Type  | Description                                                  |
| ---- | ----- | ------------------------------------------------------------ |
| ...  | int[] | Quantization value table encoded with run-length and huffman |
