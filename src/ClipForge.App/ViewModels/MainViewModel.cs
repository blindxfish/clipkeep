using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipForge.App.Clipboard;
using ClipForge.Core.Models;
using ClipForge.Core.Services;
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

    public ObservableCollection<ClipEntry> Entries { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ClipEntry? _selectedEntry;

    [ObservableProperty]
    private bool _isPaused;

    public MainViewModel(IClipRepository repository, ClipboardCaptureCoordinator coordinator)
    {
        _repository = repository;
        _coordinator = coordinator;
        _coordinator.EntryStored += OnEntryStored;
    }

    partial void OnSearchTextChanged(string value) => Refresh();

    partial void OnIsPausedChanged(bool value) => _coordinator.IsPaused = value;

    [RelayCommand]
    public void Refresh()
    {
        var query = new ClipQuery { Search = SearchText, Limit = 200 };
        var results = _repository.Query(query);

        Entries.Clear();
        foreach (var entry in results)
            Entries.Add(entry);
    }

    [RelayCommand]
    private void CopySelected()
    {
        if (SelectedEntry?.Content is not { } content) return;
        _coordinator.SuppressNextChange();
        WpfClipboard.SetText(content);
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
