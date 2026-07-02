using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using ClipForge.App.Startup;
using ClipForge.App.ViewModels;

namespace ClipForge.App;

/// <summary>
/// Settings dialog. Saving persists via the view model and applies side effects
/// that live outside settings storage (e.g. the Windows startup registration).
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.Saved += (_, _) =>
        {
            StartupManager.Apply(viewModel.LaunchOnStartup);
            Close();
        };
        CancelButton.Click += (_, _) => Close();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version is not null)
            VersionRun.Text = $"  v{version.Major}.{version.Minor}.{version.Build}";
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();

    // Open About links in the user's default browser.
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
