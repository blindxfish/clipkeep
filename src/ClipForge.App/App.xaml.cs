using System.Windows;
using System.Windows.Threading;
using ClipForge.App.Clipboard;
using ClipForge.App.Hotkeys;
using ClipForge.App.Interop;
using ClipForge.App.Services;
using ClipForge.App.ViewModels;
using ClipForge.Core.Models;
using ClipForge.Core.Classification;
using ClipForge.Core.Security;
using ClipForge.Core.Services;
using ClipForge.Core.Settings;
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
    private HotkeyService? _hotkeys;
    private QuickPasteWindow? _quickPaste;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private DispatcherTimer? _retentionTimer;

    // Window that had focus when Quick Paste opened; the paste target.
    private IntPtr _pasteTarget;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Closing the main window hides to tray; only the tray "Exit" quits.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _services = BuildServices();

        // Ensure schema/FTS/triggers exist before anything reads or writes.
        _services.GetRequiredService<ClipDatabase>().Initialize();

        // Apply retention on launch, then re-run daily while the app is open.
        RunRetention();
        _retentionTimer = new DispatcherTimer { Interval = TimeSpan.FromHours(24) };
        _retentionTimer.Tick += (_, _) => RunRetention();
        _retentionTimer.Start();

        _coordinator = _services.GetRequiredService<ClipboardCaptureCoordinator>();
        _coordinator.EntryStored += OnEntryStoredNotify;
        _coordinator.Start();

        _quickPaste = _services.GetRequiredService<QuickPasteWindow>();
        _quickPaste.EntryChosen += OnQuickPasteEntryChosen;

        _hotkeys = _services.GetRequiredService<HotkeyService>();
        _hotkeys.QuickPasteRequested += (_, _) => OpenQuickPaste();
        _hotkeys.Start();

        _services.GetRequiredService<MainViewModel>().SettingsRequested += (_, _) => OpenSettings();

        _mainWindow = _services.GetRequiredService<MainWindow>();
        _trayIcon = BuildTrayIcon();

        // Respect "Start minimized to tray": launch straight to the tray.
        if (!_services.GetRequiredService<ISettingsService>().Current.StartMinimized)
            _mainWindow.Show();
    }

    private void RunRetention()
    {
        var removed = _services?.GetRequiredService<RetentionService>().RunCleanup() ?? 0;
        if (removed > 0)
            _services?.GetRequiredService<MainViewModel>().Refresh();
    }

    private void OnEntryStoredNotify(object? sender, StoreResult e)
    {
        if (!e.IsNew) return;
        if (_services?.GetRequiredService<ISettingsService>().Current.ShowNotifications != true) return;

        var preview = e.Entry.Content is { Length: > 60 } c ? c[..60] + "…" : e.Entry.Content;
        _trayIcon?.ShowBalloonTip("Clip saved", preview ?? e.Entry.Type.ToString(), BalloonIcon.Info);
    }

    private void OpenQuickPaste()
    {
        // Remember the app the user was in so we can paste back into it.
        _pasteTarget = ForegroundPaster.CaptureForeground();
        _quickPaste?.ShowAtCursor();
    }

    private void OnQuickPasteEntryChosen(object? sender, ClipEntry entry)
    {
        if (entry.Content is not { } content) return;

        // Put the clip on the clipboard without re-capturing our own write.
        _coordinator?.SuppressNextChange();
        System.Windows.Clipboard.SetText(content);

        // Let the popup fully close and focus settle before restoring + pasting.
        var target = _pasteTarget;
        Dispatcher.BeginInvoke(DispatcherPriority.Background,
            new Action(() => ForegroundPaster.PasteInto(target)));
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton(new AppPaths());
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<ClipDatabase>();
        services.AddSingleton<IClipRepository, ClipRepository>();
        services.AddSingleton<IClassificationService, ClassificationService>();
        services.AddSingleton<ISensitiveContentDetector, SensitiveContentDetector>();
        services.AddSingleton<ClipboardStorageService>();
        services.AddSingleton<RetentionService>();
        services.AddSingleton<IDialogService, MessageBoxDialogService>();
        services.AddSingleton<ForegroundWindowTracker>();
        services.AddSingleton<ClipboardListener>();
        services.AddSingleton<ClipboardCaptureCoordinator>();
        services.AddSingleton<HotkeyService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<QuickPasteViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<QuickPasteWindow>();

        // Settings dialog is transient so each open starts from a fresh working copy.
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();

        return services.BuildServiceProvider();
    }

    private TaskbarIcon BuildTrayIcon()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var open = new System.Windows.Controls.MenuItem { Header = "Open ClipForge" };
        open.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(open);

        var quickPaste = new System.Windows.Controls.MenuItem { Header = "Quick Paste  (Ctrl+Shift+V)" };
        quickPaste.Click += (_, _) => OpenQuickPaste();
        menu.Items.Add(quickPaste);

        var pause = new System.Windows.Controls.MenuItem { Header = "Pause Monitoring", IsCheckable = true };
        pause.Click += (_, _) =>
        {
            if (_coordinator is not null) _coordinator.IsPaused = pause.IsChecked;
        };
        menu.Items.Add(pause);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var settings = new System.Windows.Controls.MenuItem { Header = "Settings" };
        settings.Click += (_, _) => OpenSettings();
        menu.Items.Add(settings);

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

    private void OpenSettings()
    {
        if (_services is null) return;
        var window = _services.GetRequiredService<SettingsWindow>();
        if (_mainWindow is { IsVisible: true })
            window.Owner = _mainWindow;
        window.ShowDialog();
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
        _retentionTimer?.Stop();
        _trayIcon?.Dispose();
        _hotkeys?.Dispose();
        if (_coordinator is not null)
        {
            _coordinator.EntryStored -= OnEntryStoredNotify;
            _coordinator.Dispose();
        }
        _services?.Dispose();
        base.OnExit(e);
    }
}
