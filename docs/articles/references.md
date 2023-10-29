# References

## Video codec theory

If you are like me, with zero knowledge about video codec, I can recommend you
the following resources to get you started.

- Getting the concepts of video codecs
  - [High level non-tech introduction to H.264](https://www.youtube.com/watch?v=PmoEsPWEdOA)
  - Decoder components:
    [demuxer](<https://en.wikipedia.org/wiki/Demultiplexer_(media_file)>)
  - ffmpeg API docs describing its architecture and concepts:
    [doxygen docs](http://ffmpeg.org/doxygen/trunk/group__libavf.html)
- Learning of referenced codec H.264
  - [Introduction to H.264 encoding](https://www.gumlet.com/learn/what-is-h264/)
  - [High-level info with quantization and decoder](https://www.vcodex.com/an-overview-of-h264-advanced-video-coding/)
  - [Intra-frame prediction modes](https://www.vcodex.com/h264avc-intra-precition/)
  - [Video detailing encoding process of H.264](https://www.youtube.com/watch?v=ZXXDXZfEcAQ)
  - [Additional high-level info on the layers: VCL and NAL](https://membrane.stream/learn/h264)
  - [Detailed overview of the H.264 codec](http://ip.hhi.de/imagecom_G1/assets/pdfs/csvt_overview_0305.pdf)
  - [Thesis on making an H.264 encoder](https://research.sabanciuniv.edu/id/eprint/8308/)
  - [H.264 specification](https://www.itu.int/rec/T-REC-H.264-202108-I/en)
    - Section 7.3 explains the binary format
  - [JM reference implementation](https://vcgit.hhi.fraunhofer.de/jvet/JM)
- Video related concepts:
  - [YUV color format](https://learn.microsoft.com/en-us/windows/win32/medfound/about-yuv-video)
  - [YUV downsampling](https://learn.microsoft.com/en-us/windows/win32/medfound/recommended-8-bit-yuv-formats-for-video-rendering)

## Mobiclip video codec

- Amazing research and reference code by Gericom in
  [MobiclipDecoder](https://github.com/Gericom/MobiclipDecoder)
- Amazing refactor of the video decoder by Adib Surani in
  [Mobius](https://github.com/AdibSurani/Mobius)
- High-level description from
  [multimedia wiki](https://wiki.multimedia.cx/index.php/Mobiclip_Video_Codec)
- [Review of Actimagine codecs](https://codecs.multimedia.cx/2020/08/a-quick-review-of-actimagine-video-codecs/)
  by Kostya
- [Glance at Mobiclip HD codec](https://codecs.multimedia.cx/2014/01/a-glance-at-mobiclip-hd/)
  by Kostya
- [VxDS codec](https://wiki.multimedia.cx/index.php/Actimagine_Video_Codec)
- Pokémon Conquest assembly code (overlay 6) 😉
