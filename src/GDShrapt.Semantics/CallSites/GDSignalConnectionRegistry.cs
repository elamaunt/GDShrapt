using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Registry of signal connections across the project.
/// Tracks which callbacks are connected to which signals.
/// Thread-safe for concurrent access.
/// </summary>
public class GDSignalConnectionRegistry
{
    private readonly object _lock = new();

    // Index by callback method: (className, methodName) -> connections
    private readonly Dictionary<(string?, string), List<GDSignalConnectionEntry>> _byCallback = new();

    // Index by signal: (emitterType, signalName) -> connections
    private readonly Dictionary<(string?, string), List<GDSignalConnectionEntry>> _bySignal = new();

    // Index by file: filePath -> connections
    private readonly Dictionary<string, List<GDSignalConnectionEntry>> _byFile = new();

    /// <summary>
    /// Registers a signal connection.
    /// </summary>
    public void Register(GDSignalConnectionEntry entry)
    {
        lock (_lock)
        {
            // Index by callback
            var callbackKey = (entry.CallbackClassName, entry.CallbackMethodName);
            if (!_byCallback.TryGetValue(callbackKey, out var callbackList))
            {
                callbackList = new List<GDSignalConnectionEntry>();
                _byCallback[callbackKey] = callbackList;
            }
            callbackList.Add(entry);

            // Index by signal
            var signalKey = (entry.EmitterType, entry.SignalName);
            if (!_bySignal.TryGetValue(signalKey, out var signalList))
            {
                signalList = new List<GDSignalConnectionEntry>();
                _bySignal[signalKey] = signalList;
            }
            signalList.Add(entry);

            // Index by file
            if (!_byFile.TryGetValue(entry.SourceFilePath, out var fileList))
            {
                fileList = new List<GDSignalConnectionEntry>();
                _byFile[entry.SourceFilePath] = fileList;
            }
            fileList.Add(entry);
        }
    }

    /// <summary>
    /// Gets all signal connections that call a specific callback method.
    /// </summary>
    public IReadOnlyList<GDSignalConnectionEntry> GetSignalsCallingMethod(string? className, string methodName)
    {
        lock (_lock)
        {
            var key = (className, methodName);
            if (_byCallback.TryGetValue(key, out var list))
                return list.ToList();

            // Also try without class name for self references
            if (className != null)
            {
                var selfKey = ((string?)null, methodName);
                if (_byCallback.TryGetValue(selfKey, out var selfList))
                    return selfList.ToList();
            }

            return new List<GDSignalConnectionEntry>();
        }
    }

    /// <summary>
    /// Gets all callbacks connected to a specific signal.
    /// </summary>
    public IReadOnlyList<GDSignalConnectionEntry> GetCallbacksForSignal(string? emitterType, string signalName)
    {
        lock (_lock)
        {
            var key = (emitterType, signalName);
            if (_bySignal.TryGetValue(key, out var list))
                return list.ToList();

            // Also try without emitter type for duck-typed signals
            if (emitterType != null)
            {
                var duckKey = ((string?)null, signalName);
                if (_bySignal.TryGetValue(duckKey, out var duckList))
                    return duckList.ToList();
            }

            return new List<GDSignalConnectionEntry>();
        }
    }

    /// <summary>
    /// Gets all signal connections in a specific file.
    /// </summary>
    public IReadOnlyList<GDSignalConnectionEntry> GetConnectionsInFile(string filePath)
    {
        lock (_lock)
        {
            if (_byFile.TryGetValue(filePath, out var list))
                return list.ToList();
            return new List<GDSignalConnectionEntry>();
        }
    }

    /// <summary>
    /// Unregisters all connections from a specific file.
    /// Used for incremental updates when a file changes.
    /// </summary>
    public void UnregisterFile(string filePath)
    {
        lock (_lock)
        {
            if (!_byFile.TryGetValue(filePath, out var entries))
                return;

            foreach (var entry in entries)
            {
                // Remove from callback index
                var callbackKey = (entry.CallbackClassName, entry.CallbackMethodName);
                if (_byCallback.TryGetValue(callbackKey, out var callbackList))
                {
                    callbackList.RemoveAll(e => e.SourceFilePath == filePath);
                    if (callbackList.Count == 0)
                        _byCallback.Remove(callbackKey);
                }

                // Remove from signal index
                var signalKey = (entry.EmitterType, entry.SignalName);
                if (_bySignal.TryGetValue(signalKey, out var signalList))
                {
                    signalList.RemoveAll(e => e.SourceFilePath == filePath);
                    if (signalList.Count == 0)
                        _bySignal.Remove(signalKey);
                }
            }

            _byFile.Remove(filePath);
        }
    }

    /// <summary>
    /// Gets all registered signal connections.
    /// </summary>
    public IReadOnlyList<GDSignalConnectionEntry> GetAllConnections()
    {
        lock (_lock)
        {
            return _byFile.Values.SelectMany(x => x).ToList();
        }
    }

    /// <summary>
    /// Gets all unique signal names that have connections.
    /// </summary>
    public IReadOnlyList<string> GetAllConnectedSignals()
    {
        lock (_lock)
        {
            return _bySignal.Keys.Select(k => k.Item2).Distinct().ToList();
        }
    }

    /// <summary>
    /// Gets all unique callback methods that are connected to signals.
    /// </summary>
    public IReadOnlyList<(string? className, string methodName)> GetAllCallbackMethods()
    {
        lock (_lock)
        {
            return _byCallback.Keys.ToList();
        }
    }

    /// <summary>
    /// Clears all registered connections.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _byCallback.Clear();
            _bySignal.Clear();
            _byFile.Clear();
        }
    }

    /// <summary>
    /// Gets the total number of registered connections.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _byFile.Values.Sum(x => x.Count);
            }
        }
    }
}
