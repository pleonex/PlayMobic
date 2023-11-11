namespace PlayMobic.UI.Pages;
using System;

public class ConversionProgressEventArgs : EventArgs
{
    public ConversionProgressEventArgs(string path, double progress, bool hasFinished)
    {
        FilePath = path;
        Progress = progress;
        HasError = false;
        ErrorDescription = string.Empty;
        HasFinished = hasFinished;
    }

    public ConversionProgressEventArgs(string path, double progress, string error)
    {
        FilePath = path;
        Progress = progress;
        HasError = true;
        ErrorDescription = error;
        HasFinished = true;
    }

    public bool HasFinished { get; }

    public string FilePath { get; }

    public bool HasError { get; }

    public string ErrorDescription { get; }

    public double Progress { get; }
}
