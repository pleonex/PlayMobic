namespace PlayMobic.UI.Views;
using System;
using Avalonia.Controls;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;

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
        if (e.SelectedItem is NavigationViewItem nvi) {
            string viewTypeName = typeof(MainView).Namespace + "." + nvi.Tag;
            Type viewType = Type.GetType(viewTypeName)
                ?? throw new InvalidOperationException($"Cannot find view Type: {viewTypeName}");

            mainNavigationFrame.Navigate(viewType);
        }
    }
}
