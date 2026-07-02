using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ClipForge.App.ViewModels;

namespace ClipForge.App;

/// <summary>
/// Interaction logic for MainWindow.xaml. Closing hides to tray rather than
/// exiting; the app is shut down explicitly from the tray menu.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Refresh();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Keep running in the tray instead of terminating.
        e.Cancel = true;
        Hide();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    // Open a clip's "…" context menu on left-click (it's normally right-click only).
    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }
    }
}
