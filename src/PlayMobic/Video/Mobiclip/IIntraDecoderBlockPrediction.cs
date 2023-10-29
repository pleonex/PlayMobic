namespace PlayMobic.Video.Mobiclip;

internal interface IIntraDecoderBlockPrediction
{
    void PerformBlockPrediction(PixelBlock block, IntraPredictionBlockMode mode);
}
