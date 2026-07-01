using System.ComponentModel;
using System.Windows;
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
}
