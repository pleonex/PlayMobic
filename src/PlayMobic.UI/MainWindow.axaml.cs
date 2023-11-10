namespace PlayMobic.UI;

using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Windowing;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
