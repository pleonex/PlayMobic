namespace PlayMobic.UI.Views;

using Avalonia.Controls;
using PlayMobic.UI.ViewModels;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        DataContext = new SettingsViewModel();
    }
}
