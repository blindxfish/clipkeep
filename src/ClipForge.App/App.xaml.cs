using System.Windows;
using ClipForge.App.Clipboard;
using ClipForge.App.ViewModels;
using ClipForge.Core.Classification;
using ClipForge.Core.Security;
using ClipForge.Core.Services;
using ClipForge.Core.Storage;
using ClipForge.Infrastructure.Database;
using ClipForge.Infrastructure.Repositories;
using ClipForge.Infrastructure.Storage;
using ClipForge.Infrastructure.WindowsApi;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;

namespace ClipForge.App;

/// <summary>
/// Composition root: builds the DI container, initializes the database, starts
/// clipboard capture, installs the tray icon, and shows the main window.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;
    private ClipboardCaptureCoordinator? _coordinator;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Closing the main window hides to tray; only the tray "Exit" quits.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _services = BuildServices();

        // Ensure schema/FTS/triggers exist before anything reads or writes.
        _services.GetRequiredService<ClipDatabase>().Initialize();

        _coordinator = _services.GetRequiredService<ClipboardCaptureCoordinator>();
        _coordinator.Start();

        _mainWindow = _services.GetRequiredService<MainWindow>();
        _trayIcon = BuildTrayIcon();

        _mainWindow.Show();
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton(new AppPaths());
        services.AddSingleton<ClipDatabase>();
        services.AddSingleton<IClipRepository, ClipRepository>();
        services.AddSingleton<IClassificationService, ClassificationService>();
        services.AddSingleton<ISensitiveContentDetector, SensitiveContentDetector>();
        services.AddSingleton<ClipboardStorageService>();
        services.AddSingleton<ForegroundWindowTracker>();
        services.AddSingleton<ClipboardListener>();
        services.AddSingleton<ClipboardCaptureCoordinator>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    private TaskbarIcon BuildTrayIcon()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var open = new System.Windows.Controls.MenuItem { Header = "Open ClipForge" };
        open.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(open);

        var pause = new System.Windows.Controls.MenuItem { Header = "Pause Monitoring", IsCheckable = true };
        pause.Click += (_, _) =>
        {
            if (_coordinator is not null) _coordinator.IsPaused = pause.IsChecked;
        };
        menu.Items.Add(pause);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exit = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exit.Click += (_, _) => Shutdown();
        menu.Items.Add(exit);

        var icon = new TaskbarIcon
        {
            ToolTipText = "ClipForge",
            IconSource = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Assets/ClipForge.ico")),
            ContextMenu = menu
        };
        icon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();
        return icon;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _coordinator?.Dispose();
        _services?.Dispose();
        base.OnExit(e);
    }
}
