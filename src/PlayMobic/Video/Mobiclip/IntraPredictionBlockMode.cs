namespace PlayMobic.Video.Mobiclip;

internal enum IntraPredictionBlockMode
{
    Predicted = -1,

    // sorted as most probably neighbor mode for prediction
    Vertical = 0,
    Horizontal = 1,
    DeltaPlane = 2,
    DC = 3,
    HorizontalUp = 4,
    HorizontalDown = 5,
    VerticalRight = 6,
    DiagonalDownRight = 7,
    VerticalLeft = 8,

    Nothing = 9,
}
