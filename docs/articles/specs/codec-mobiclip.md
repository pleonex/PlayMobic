# Mobiclip codec

The Mobiclip video encoding was developed by
[Actimagine (now NERD)](https://en.wikipedia.org/wiki/Nintendo_European_Research_%26_Development)
for Nintendo DS. This encoding is also used on Nintendo 3DS with some variants.
Related formats are also used for GBA and Wii.

The encoding is a variant of H.264.

## NAL

This encoding does not have any _network access layer_ (NAL). It's pure _video
codec layer_ (VCL). Data is encoded directly for each frame and passed to the
muxer.

## Components

As with H.264 the main building blocks are:

- Prediction
- Transformation
- Quantization
- Encoding

TODO: flow diagram.

## Color space

The encoding uses the YUV color space with 4:2:0 down-sampling. Each key frame
(I slice) has information about the actual color space that it provides. There
are two possible values:

- 0: YCoCg: first versions of the codec, mostly in the MODS container.
- 1: YCbCr: following versions of the codec, like in Moflex containers.

## Frames

Just like H.264, each frame is divided in _macroblocks_ of 16x16 pixels for the
luma component and 8x8 pixels for the chroma U and V components. As it uses
4:2:0, it generates the same number of _macroblocks_ for each component.

Following the terminology of H.264, a frame contains only one _slide_, meaning
all the _macroblocks_ of the frame are encoded together at once.

## Prediction

There are two methods to encode a frame:

- _I frame_ / [Intra-frame prediction](#intra-frame-prediction): each
  _macroblock_ is encoded / predicted approximating the values of its neighbors.
  It does not require any other frame to get the output. This is also known as
  _key frame_.
- _P frame_ / [Inter-frame prediction](#inter-frame-prediction): each
  _macroblock_ is predicted either using vectorized _motion compensation_ or
  _intra-frame prediction_.

### Intra-frame prediction

This encoding predicts the values of a block from its neighbors pixels. The
difference between the prediction and the actual frame pixel is named _residue_.
The _residue_ along with the _prediction information_ (mode, has residual info
and partitioning) is what we encode in the format. More information about the
residual in [transformation](#transformation).

There are **nine** different prediction modes (0 to 8). They may happen on
blocks of 16x16, 8x8 or 4x4 size. Note that a block of 16x16 only supports mode
2 (DC / average) and its residue is anyway encoded in 8x8 or 4x4 blocks.

Additionally, if every sub-block of 4x4 or 8x8 in a _macroblock_ uses the same
prediction mode, this mode is only encoded once. The same as it were mode 2 for
full _macroblock_. On the other hand, if blocks have different modes, then the
mode value for each block is also _"predicted"_, in some cases saving 2 out 3
bits, in others wasting 1 bit.

For each block we encoded also a value indicating whether it has _residue_ data
or not (all predicted values matches final image). Instead of encoding a bit per
block, it encodes a variable-size integer used as an index in a pre-known table.

For 8x8 blocks, there is one value block at the _macroblock_ level with 6-bits:
4-bits for each luma 8x8 block and 2-bits for each chroma component. There is
another table for 4x4 blocks with 4-bits values (one for each block). These
tables contains all possible values (0-63 for 8x8 and 0-15 for 4x4) and are
sorted in a way where most-frequent values has a smaller index.

For each frame _macroblock_, first we have the information for its luma
component, then chroma U and finally chroma V. Then it follows for the next
frame _macroblock_.

### Inter-frame prediction

TODO

## Transformation

TODO

## Quantization

TODO

## Entropy encoding

TODO

## Binary serialization

The compressed frame data, like a structure, needs to be encoded into bytes. In
this encoding, this is done at the bit level creating a _bit stream_ with **big
endianness**

The format supports encoding the following types:

- Boolean: 1 bit. `0` for `false`, `1` for `true`.
- Fixed size integer
- Variable size integer:
  [Exponential Golomb coding](https://en.wikipedia.org/wiki/Exponential-Golomb_coding).

_Exponential Golomb is identical to the
[Elias gamma code](https://en.wikipedia.org/wiki/Elias_gamma_coding) of x+1,
allowing it to encode 0. [source: wikipedia]_
