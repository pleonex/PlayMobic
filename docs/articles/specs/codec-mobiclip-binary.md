# Mobiclip encoding - binary format overview

> [!NOTE]  
> The binary format of the codec based on H.264 is not easy to describe with
> tables. As an example the official H.264 specification explains the format
> (_syntax in tabular form_) with pseudo-code on a table (section 7.3). I prefer
> to try to keep it simple using some words instead of code.

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

| Bits | Type | Description                           |
| ---- | ---- | ------------------------------------- |
| ...  | int  | CAVLC encoded motion prediction index |

- If index is 6: [macroblock](#macroblock) data without coefficient
- If index is 7: [macroblock](#macroblock) data with coefficients
- Otherwise: [motion predicted macroblock](#p-macroblock)

## MacroBlock

| Bits | Type       | Description                                                         |
| ---- | ---------- | ------------------------------------------------------------------- |
| 1    | bool       | (**Only for I macroblocks**) Has macroblock level _prediction mode_ |
| ...  | Exp-Golomb | Block 8x8 residual information table index                          |
| (3)  | int        | **Only** if it has _macroblock level pMode_: pMode                  |
| ...  | Exp-Golomb | **Only** if _macroblock level pMode_ is 2: mode 2 average           |
| ...  | Block[]    | 8x8 blocks in Luma macroblock                                       |
| 3    | int        | P mode for UV                                                       |
| ...  | Block      | U block data                                                        |
| ...  | Block      | V block data                                                        |

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

| Bits | Type   | Description                |
| ---- | ------ | -------------------------- |
| ...  | int[2] | CAVLC motion encoded index |

## Block

If the 8x8 block does **not** have residual:

| Bits | Type            | Description                                                  |
| ---- | --------------- | ------------------------------------------------------------ |
| ...  | pModePrediction | **Only** if it does not have _macroblock level pMode_: pMode |
| ...  | Exp-Golomb      | **Only** if _macroblock level pMode_ is 2: mode 2 average    |

Otherwise, **there is 1 _exp-golomb_ value** with the index to the _block 4x4
residual info table_. If this index is 0, then we process as a single 8x8 block.
Otherwise we split further into 4x4 pixel blocks. For each of them (single or
multiple):

| Bits | Type            | Description                                               |
| ---- | --------------- | --------------------------------------------------------- |
| ...  | pModePrediction | **Only** it does NOT have _macroblock level pMode_: pMode |
| ...  | Exp-Golomb      | **Only** previous pMode is 2: mode 2 average              |
| ...  | Residual        | Residual information                                      |

## P block

| Bits | Type       | Description                                              |
| ---- | ---------- | -------------------------------------------------------- |
| ...  | Exp-Golomb | P frame coefficient 4x4 table index                      |
| ...  | Residual[] | Coefficients for each 4x4 block or full block if index 0 |

## pMode prediction

| Bits | Type | Description            |
| ---- | ---- | ---------------------- |
| 1    | bool | Has encoded value      |
| 3    | int  | if above is 0, X value |

## Residual

| Bits | Type  | Description                            |
| ---- | ----- | -------------------------------------- |
| ...  | int[] | CAVLC encoded quantization value table |
