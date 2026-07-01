using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows.Media.Imaging;
using ClipForge.App.Clipboard;
using ClipForge.App.Services;
using ClipForge.App.Storage;
using ClipForge.Core.Models;
using ClipForge.Core.Services;
using ClipForge.Core.Settings;
using ClipForge.Core.Storage;
using WpfClipboard = System.Windows.Clipboard;

namespace ClipForge.App.ViewModels;

/// <summary>
/// Drives the main window: live list of entries, search, and per-item actions.
/// Listens to the capture coordinator so newly copied clips appear immediately.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IClipRepository _repository;
    private readonly ClipboardCaptureCoordinator _coordinator;
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogs;
    private readonly ImageStore _imageStore;

    public ObservableCollection<ClipEntry> Entries { get; } = new();

    public IReadOnlyList<ClipFilter> Filters => ClipFilter.Sidebar;

    [ObservableProperty]
    private ClipFilter _selectedFilter = ClipFilter.Sidebar[0];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ClipEntry? _selectedEntry;

    [ObservableProperty]
    private bool _isPaused;

    /// <summary>Raised when the user asks to open Settings (handled by the app shell).</summary>
    public event EventHandler? SettingsRequested;

    public MainViewModel(
        IClipRepository repository,
        ClipboardCaptureCoordinator coordinator,
        ISettingsService settings,
        IDialogService dialogs,
        ImageStore imageStore)
    {
        _repository = repository;
        _coordinator = coordinator;
        _settings = settings;
        _dialogs = dialogs;
        _imageStore = imageStore;
        _coordinator.EntryStored += OnEntryStored;
    }

    partial void OnSearchTextChanged(string value) => Refresh();

    partial void OnSelectedFilterChanged(ClipFilter value) => Refresh();

    partial void OnIsPausedChanged(bool value) => _coordinator.IsPaused = value;

    [RelayCommand]
    public void Refresh()
    {
        var query = new ClipQuery
        {
            Search = SearchText,
            Type = SelectedFilter.Type,
            FavoritesOnly = SelectedFilter.FavoritesOnly,
            Limit = 200
        };
        var results = _repository.Query(query);

        Entries.Clear();
        foreach (var entry in results)
            Entries.Add(entry);
    }

    [RelayCommand]
    private void OpenSettings() => SettingsRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void CopySelected()
    {
        if (SelectedEntry is not { } entry) return;

        if (entry.Type == ClipType.Image)
        {
            CopyImageToClipboard(entry);
            return;
        }

        if (entry.Content is not { } content) return;
        _coordinator.SuppressNextChange();
        WpfClipboard.SetText(content);
    }

    private void CopyImageToClipboard(ClipEntry entry)
    {
        var image = _repository.GetImage(entry.Id);
        if (image is null || !File.Exists(image.FilePath)) return;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(image.FilePath);
        bitmap.EndInit();

        _coordinator.SuppressNextChange();
        WpfClipboard.SetImage(bitmap);
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        if (SelectedEntry is not { } entry) return;
        var newValue = !entry.Favorite;
        _repository.SetFavorite(entry.Id, newValue);
        entry.Favorite = newValue;
        Refresh();
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedEntry is not { } entry) return;

        if (_settings.Current.ConfirmBeforeDelete &&
            !_dialogs.Confirm("Delete this clipboard entry?", "ClipForge"))
            return;

        // Remove image files before the row (cascade drops the images record).
        if (entry.Type == ClipType.Image && _repository.GetImage(entry.Id) is { } image)
            _imageStore.DeleteFiles(image);

        _repository.Delete(entry.Id);
        Entries.Remove(entry);
    }

    private void OnEntryStored(object? sender, StoreResult e)
    {
        // Raised off the UI thread by the capture pipeline; marshal back.
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                Refresh();
        });
    }

    public void Dispose() => _coordinator.EntryStored -= OnEntryStored;
}
