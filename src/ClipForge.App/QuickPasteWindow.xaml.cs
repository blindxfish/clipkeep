using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using ClipForge.App.ViewModels;
using ClipForge.Core.Models;

namespace ClipForge.App;

/// <summary>
/// Lightweight Quick Paste popup. Appears at the cursor, focuses the search box,
/// supports arrow-key navigation, and raises <see cref="EntryChosen"/> on Enter.
/// Hides on Escape or when it loses focus.
/// </summary>
public partial class QuickPasteWindow : Window
{
    private readonly QuickPasteViewModel _viewModel;

    /// <summary>Raised when the user commits a selection (Enter or double-click).</summary>
    public event EventHandler<ClipEntry>? EntryChosen;

    public QuickPasteWindow(QuickPasteViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        PreviewKeyDown += OnPreviewKeyDown;
        Deactivated += (_, _) => Hide();
        ResultsList.MouseDoubleClick += (_, _) => Commit();
    }

    /// <summary>Reload results, position at the cursor, show, and focus search.</summary>
    public void ShowAtCursor()
    {
        _viewModel.SearchText = string.Empty;
        _viewModel.Reload();
        PositionAtCursor();

        Show();
        Activate();
        SearchBox.Focus();
    }

    private void PositionAtCursor()
    {
        if (!GetCursorPos(out var p)) return;

        // Cursor is in device pixels; convert to WPF device-independent units.
        var source = PresentationSource.FromVisual(this);
        var dpiX = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;
        var dpiY = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1.0;

        var left = p.X * dpiX;
        var top = p.Y * dpiY;

        // Keep the popup on-screen (virtual screen bounds, in DIPs).
        left = Math.Min(left, SystemParameters.VirtualScreenWidth - Width);
        top = Math.Min(top, SystemParameters.VirtualScreenHeight - Height);
        Left = Math.Max(0, left);
        Top = Math.Max(0, top);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
            case Key.Down:
                _viewModel.MoveSelection(1);
                ResultsList.ScrollIntoView(_viewModel.SelectedEntry);
                e.Handled = true;
                break;
            case Key.Up:
                _viewModel.MoveSelection(-1);
                ResultsList.ScrollIntoView(_viewModel.SelectedEntry);
                e.Handled = true;
                break;
            case Key.Enter:
                Commit();
                e.Handled = true;
                break;
        }
    }

    private void Commit()
    {
        if (_viewModel.SelectedEntry is not { } entry) return;
        Hide();
        EntryChosen?.Invoke(this, entry);
    }

    // Closing the app-owned popup should just hide it, never tear down the window.
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }
}
