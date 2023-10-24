# Mobiclip codec

The Mobiclip video encoding was developed by
[Actimagine (now NERD)](https://en.wikipedia.org/wiki/Nintendo_European_Research_%26_Development)
for Nintendo DS. This encoding is also used on Nintendo 3DS with some variants.

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

YUV 4:2:0

MODS have a different color space.

## Prediction

TODO

## Transformation

TODO

## Quantization

TODO

## Encoding

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

## Differences with H.264

TODO
