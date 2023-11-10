namespace PlayMobic.UI;
using System;
using Avalonia.Controls;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using PlayMobic.UI.Pages;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        mainNavigationView.SelectionChanged += OnMainNavigationItemChange;
        mainNavigationView.SelectedItem = mainNavigationView.MenuItems.ElementAt(0);
    }

    private void OnMainNavigationItemChange(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.IsSettingsSelected) {
            mainNavigationFrame.Navigate(typeof(SettingsView));
        } else if (e.SelectedItem is NavigationViewItem nvi) {
            string viewTypeName = typeof(MainView).Namespace + ".Pages." + nvi.Tag;
            Type viewType = Type.GetType(viewTypeName)
                ?? throw new InvalidOperationException($"Cannot find view Type: {viewTypeName}");

            mainNavigationFrame.Navigate(viewType);
        }
    }
}
