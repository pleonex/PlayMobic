namespace PlayMobic.Video.Mobiclip;

internal interface IIntraDecoderBlockPrediction
{
    void PerformBlockPrediction(ComponentBlock block, IntraPredictionBlockMode mode);
}
