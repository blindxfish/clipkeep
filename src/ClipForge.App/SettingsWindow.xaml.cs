using System.Windows;
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
    }
}
