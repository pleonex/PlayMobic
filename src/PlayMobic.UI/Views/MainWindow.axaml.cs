namespace PlayMobic.UI.Views;

using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Windowing;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
