using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ClipForge.Core.Models;
using ClipForge.Core.Storage;

namespace ClipForge.App.ViewModels;

/// <summary>
/// Backs the Quick Paste popup: a short, filterable list of recent clips with
/// favorites floated to the top, plus keyboard-driven selection.
/// </summary>
public sealed partial class QuickPasteViewModel : ObservableObject
{
    private const int MaxResults = 50;
    private readonly IClipRepository _repository;

    public ObservableCollection<ClipEntry> Entries { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ClipEntry? _selectedEntry;

    public QuickPasteViewModel(IClipRepository repository) => _repository = repository;

    partial void OnSearchTextChanged(string value) => Reload();

    /// <summary>Refresh the list and reset the selection to the first item.</summary>
    public void Reload()
    {
        var results = _repository.Query(new ClipQuery { Search = SearchText, Limit = MaxResults });

        // Favorites first, preserving the query's existing order within each group.
        var ordered = results
            .Select((e, i) => (e, i))
            .OrderByDescending(x => x.e.Favorite)
            .ThenBy(x => x.i)
            .Select(x => x.e);

        Entries.Clear();
        foreach (var entry in ordered)
            Entries.Add(entry);

        SelectedEntry = Entries.FirstOrDefault();
    }

    public void MoveSelection(int delta)
    {
        if (Entries.Count == 0) return;
        var index = SelectedEntry is null ? 0 : Entries.IndexOf(SelectedEntry);
        index = Math.Clamp(index + delta, 0, Entries.Count - 1);
        SelectedEntry = Entries[index];
    }
}
