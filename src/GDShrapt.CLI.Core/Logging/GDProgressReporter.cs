using System;
using System.IO;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Reports file processing progress to stderr (keeping stdout clean for output).
/// Overwrites the line in interactive terminals using \r.
/// </summary>
public sealed class GDProgressReporter : IDisposable
{
    private readonly TextWriter _stderr;
    private readonly bool _isInteractive;
    private int _totalFiles;
    private int _processedFiles;
    private bool _disposed;

    public bool Enabled { get; }

    public GDProgressReporter(string? mode = "auto")
    {
        _stderr = Console.Error;
        _isInteractive = !Console.IsErrorRedirected;

        Enabled = mode?.ToLowerInvariant() switch
        {
            "always" => true,
            "never" => false,
            "auto" or null => _isInteractive,
            _ => _isInteractive
        };
    }

    public void Start(int totalFiles)
    {
        _totalFiles = totalFiles;
        _processedFiles = 0;

        if (!Enabled) return;
        _stderr.Write($"\rProcessing 0/{_totalFiles} files...");
    }

    public void Report(string fileName)
    {
        _processedFiles++;

        if (!Enabled) return;

        if (_isInteractive)
        {
            _stderr.Write($"\rProcessing {_processedFiles}/{_totalFiles}: {fileName}                    ");
        }
    }

    public void Complete()
    {
        if (!Enabled) return;

        if (_isInteractive)
        {
            _stderr.Write("\r");
            _stderr.Write(new string(' ', 80));
            _stderr.Write("\r");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Complete();
    }
}
