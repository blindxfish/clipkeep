namespace ClipForge.App.Services;

/// <summary>Abstraction over modal prompts so view models stay testable.</summary>
public interface IDialogService
{
    /// <summary>Show a yes/no confirmation; returns true if the user confirmed.</summary>
    bool Confirm(string message, string title);
}
