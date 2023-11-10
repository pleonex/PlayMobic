namespace PlayMobic.UI.Pages;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class VideoInfoField : ObservableObject
{
    [ObservableProperty]
    private string value = string.Empty;

    public VideoInfoField(string name, string group)
    {
        Name = name;
        Group = group;
    }

    public string Name { get; }

    public string Group { get; }
}
