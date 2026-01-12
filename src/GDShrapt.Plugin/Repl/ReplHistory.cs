using System.Collections.Generic;

namespace GDShrapt.Plugin;

/// <summary>
/// Stores REPL command history in memory for the current session.
/// Supports navigation through history with up/down arrows.
/// </summary>
internal class ReplHistory
{
    private readonly List<string> _history = new();
    private int _currentIndex = -1;
    private const int MaxHistorySize = 100;

    /// <summary>
    /// Adds a command to the history.
    /// Resets navigation index.
    /// </summary>
    public void Add(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        // Don't add duplicates of the last command
        if (_history.Count > 0 && _history[_history.Count - 1] == command)
        {
            ResetNavigation();
            return;
        }

        _history.Add(command);

        // Trim history if too large
        if (_history.Count > MaxHistorySize)
        {
            _history.RemoveAt(0);
        }

        ResetNavigation();
    }

    /// <summary>
    /// Gets the previous command in history (up arrow).
    /// Returns null if at the beginning or history is empty.
    /// </summary>
    public string? GetPrevious()
    {
        if (_history.Count == 0)
            return null;

        if (_currentIndex == -1)
        {
            // Start from the end
            _currentIndex = _history.Count - 1;
        }
        else if (_currentIndex > 0)
        {
            _currentIndex--;
        }

        return _history[_currentIndex];
    }

    /// <summary>
    /// Gets the next command in history (down arrow).
    /// Returns null if at the end.
    /// </summary>
    public string? GetNext()
    {
        if (_history.Count == 0 || _currentIndex == -1)
            return null;

        if (_currentIndex < _history.Count - 1)
        {
            _currentIndex++;
            return _history[_currentIndex];
        }

        // At the end, reset and return null (empty input)
        ResetNavigation();
        return null;
    }

    /// <summary>
    /// Resets the navigation index.
    /// Called after executing a command or when user types new input.
    /// </summary>
    public void ResetNavigation()
    {
        _currentIndex = -1;
    }

    /// <summary>
    /// Gets all commands in history.
    /// </summary>
    public IReadOnlyList<string> GetAll() => _history;

    /// <summary>
    /// Clears the history.
    /// </summary>
    public void Clear()
    {
        _history.Clear();
        ResetNavigation();
    }

    /// <summary>
    /// Gets the number of commands in history.
    /// </summary>
    public int Count => _history.Count;
}
