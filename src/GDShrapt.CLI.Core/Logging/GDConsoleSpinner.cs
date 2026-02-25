using System;
using System.IO;
using System.Threading;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Displays a spinning indicator on stderr while a long-running operation is in progress.
/// Uses \r to overwrite the line in interactive terminals.
/// </summary>
public sealed class GDConsoleSpinner : IDisposable
{
    private static readonly char[] SpinnerChars = { '|', '/', '-', '\\' };

    private readonly TextWriter _stderr;
    private readonly string _message;
    private readonly bool _isInteractive;
    private readonly Timer? _timer;
    private int _frame;
    private bool _disposed;

    public GDConsoleSpinner(string message)
    {
        _stderr = Console.Error;
        _message = message;
        _isInteractive = !Console.IsErrorRedirected;

        if (!_isInteractive)
            return;

        _stderr.Write($"\r{SpinnerChars[0]} {_message}");
        _timer = new Timer(Tick, null, 100, 100);
    }

    private void Tick(object? state)
    {
        if (_disposed)
            return;

        _frame++;
        var ch = SpinnerChars[_frame % SpinnerChars.Length];
        _stderr.Write($"\r{ch} {_message}");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _timer?.Dispose();

        if (!_isInteractive)
            return;

        _stderr.Write("\r");
        _stderr.Write(new string(' ', _message.Length + 3));
        _stderr.Write("\r");
    }
}
