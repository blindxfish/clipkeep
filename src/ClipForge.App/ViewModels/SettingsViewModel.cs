using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipForge.Core.Settings;

namespace ClipForge.App.ViewModels;

/// <summary>
/// Editable working copy of <see cref="AppSettings"/>. Changes only take effect
/// when <see cref="SaveCommand"/> runs; closing without saving discards them.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    // General
    [ObservableProperty] private bool _launchOnStartup;
    [ObservableProperty] private bool _startMinimized;
    [ObservableProperty] private bool _showNotifications;
    [ObservableProperty] private bool _confirmBeforeDelete;

    // Monitoring
    [ObservableProperty] private bool _enableMonitoring;
    [ObservableProperty] private bool _monitorText;
    [ObservableProperty] private bool _monitorImages;
    [ObservableProperty] private bool _monitorFiles;
    [ObservableProperty] private bool _monitorHtml;

    // Privacy
    [ObservableProperty] private bool _doNotSaveSensitive;
    [ObservableProperty] private string _newExcludedApp = string.Empty;
    public ObservableCollection<string> ExcludedApps { get; } = new();

    // Storage
    public RetentionPeriod[] RetentionOptions { get; } = Enum.GetValues<RetentionPeriod>();
    [ObservableProperty] private RetentionPeriod _retention;

    /// <summary>Raised after a successful save so the host window can close.</summary>
    public event EventHandler? Saved;

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        LoadFrom(settings.Current);
    }

    private void LoadFrom(AppSettings s)
    {
        LaunchOnStartup = s.LaunchOnStartup;
        StartMinimized = s.StartMinimized;
        ShowNotifications = s.ShowNotifications;
        ConfirmBeforeDelete = s.ConfirmBeforeDelete;

        EnableMonitoring = s.EnableMonitoring;
        MonitorText = s.MonitorText;
        MonitorImages = s.MonitorImages;
        MonitorFiles = s.MonitorFiles;
        MonitorHtml = s.MonitorHtml;

        DoNotSaveSensitive = s.SensitivePolicy == SensitivePolicy.DoNotSave;
        ExcludedApps.Clear();
        foreach (var app in s.ExcludedApps) ExcludedApps.Add(app);

        Retention = s.Retention;
    }

    [RelayCommand]
    private void AddExcludedApp()
    {
        var name = NewExcludedApp.Trim();
        if (name.Length == 0) return;
        if (!ExcludedApps.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)))
            ExcludedApps.Add(name);
        NewExcludedApp = string.Empty;
    }

    [RelayCommand]
    private void RemoveExcludedApp(string? app)
    {
        if (app is not null) ExcludedApps.Remove(app);
    }

    [RelayCommand]
    private void Save()
    {
        var updated = new AppSettings
        {
            LaunchOnStartup = LaunchOnStartup,
            StartMinimized = StartMinimized,
            ShowNotifications = ShowNotifications,
            ConfirmBeforeDelete = ConfirmBeforeDelete,
            EnableMonitoring = EnableMonitoring,
            MonitorText = MonitorText,
            MonitorImages = MonitorImages,
            MonitorFiles = MonitorFiles,
            MonitorHtml = MonitorHtml,
            SensitivePolicy = DoNotSaveSensitive ? SensitivePolicy.DoNotSave : SensitivePolicy.SaveNormally,
            ExcludedApps = ExcludedApps.ToList(),
            Retention = Retention,
            MaxDatabaseSizeMb = _settings.Current.MaxDatabaseSizeMb
        };

        _settings.Save(updated);
        Saved?.Invoke(this, EventArgs.Empty);
    }
}
