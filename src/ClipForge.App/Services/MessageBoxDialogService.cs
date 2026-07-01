using System.Windows;

namespace ClipForge.App.Services;

/// <summary>WPF MessageBox-backed implementation of <see cref="IDialogService"/>.</summary>
public sealed class MessageBoxDialogService : IDialogService
{
    public bool Confirm(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes;
}
