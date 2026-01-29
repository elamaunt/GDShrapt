using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Tracks dependencies between files for incremental invalidation.
/// Thread-safe for concurrent access during parallel analysis.
/// </summary>
public class GDTypeDependencyGraph
{
    // file → files that depend on it (reverse dependencies)
    private readonly ConcurrentDictionary<string, HashSet<string>> _dependents = new(StringComparer.OrdinalIgnoreCase);
    // file → files it depends on
    private readonly ConcurrentDictionary<string, HashSet<string>> _dependencies = new(StringComparer.OrdinalIgnoreCase);
    // Lock for thread-safe HashSet modifications
    private readonly object _lock = new();

    /// <summary>
    /// Adds a dependency: fromFile depends on toFile.
    /// </summary>
    /// <param name="fromFile">The file that has the dependency.</param>
    /// <param name="toFile">The file being depended on.</param>
    public void AddDependency(string fromFile, string toFile)
    {
        if (string.IsNullOrEmpty(fromFile) || string.IsNullOrEmpty(toFile))
            return;

        // Normalize paths
        fromFile = NormalizePath(fromFile);
        toFile = NormalizePath(toFile);

        // Don't add self-dependencies
        if (string.Equals(fromFile, toFile, StringComparison.OrdinalIgnoreCase))
            return;

        lock (_lock)
        {
            // Add forward dependency: fromFile depends on toFile
            var deps = _dependencies.GetOrAdd(fromFile, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            deps.Add(toFile);

            // Add reverse dependency: toFile has dependent fromFile
            var revDeps = _dependents.GetOrAdd(toFile, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            revDeps.Add(fromFile);
        }
    }

    /// <summary>
    /// Removes all dependencies for a file (when file is deleted or modified).
    /// </summary>
    /// <param name="filePath">Path of the file to remove.</param>
    public void RemoveFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        filePath = NormalizePath(filePath);

        lock (_lock)
        {
            // Remove forward dependencies
            if (_dependencies.TryRemove(filePath, out var deps))
            {
                // Remove this file from the dependents list of each file it depended on
                foreach (var dep in deps)
                {
                    if (_dependents.TryGetValue(dep, out var revDeps))
                    {
                        revDeps.Remove(filePath);
                    }
                }
            }

            // Remove reverse dependencies
            if (_dependents.TryRemove(filePath, out var dependents))
            {
                // Remove this file from the dependencies list of each file that depended on it
                foreach (var dependent in dependents)
                {
                    if (_dependencies.TryGetValue(dependent, out var depDeps))
                    {
                        depDeps.Remove(filePath);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets all files that directly depend on the given file.
    /// </summary>
    /// <param name="filePath">Path of the file.</param>
    /// <returns>Set of files that depend on this file.</returns>
    public IReadOnlySet<string> GetDependents(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return new HashSet<string>();

        filePath = NormalizePath(filePath);

        lock (_lock)
        {
            if (_dependents.TryGetValue(filePath, out var deps))
            {
                return new HashSet<string>(deps, StringComparer.OrdinalIgnoreCase);
            }
        }

        return new HashSet<string>();
    }

    /// <summary>
    /// Gets all files that directly depend on the given file.
    /// </summary>
    public IReadOnlySet<string> GetDependencies(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return new HashSet<string>();

        filePath = NormalizePath(filePath);

        lock (_lock)
        {
            if (_dependencies.TryGetValue(filePath, out var deps))
            {
                return new HashSet<string>(deps, StringComparer.OrdinalIgnoreCase);
            }
        }

        return new HashSet<string>();
    }

    /// <summary>
    /// Gets all files that transitively depend on the given file.
    /// Uses BFS to avoid stack overflow on deep dependencies.
    /// </summary>
    /// <param name="filePath">Path of the file.</param>
    /// <returns>Set of all files that transitively depend on this file.</returns>
    public IReadOnlySet<string> GetTransitiveDependents(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return new HashSet<string>();

        filePath = NormalizePath(filePath);

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(filePath);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var directDeps = GetDependents(current);

            foreach (var dep in directDeps)
            {
                if (result.Add(dep))
                {
                    queue.Enqueue(dep);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all files that this file transitively depends on.
    /// </summary>
    public IReadOnlySet<string> GetTransitiveDependencies(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return new HashSet<string>();

        filePath = NormalizePath(filePath);

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(filePath);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var directDeps = GetDependencies(current);

            foreach (var dep in directDeps)
            {
                if (result.Add(dep))
                {
                    queue.Enqueue(dep);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Clears all dependencies for a file and rebuilds them.
    /// Used when a file is modified.
    /// </summary>
    /// <param name="filePath">Path of the file.</param>
    /// <param name="newDependencies">New dependencies for this file.</param>
    public void UpdateDependencies(string filePath, IEnumerable<string> newDependencies)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        filePath = NormalizePath(filePath);

        lock (_lock)
        {
            // Remove old dependencies
            if (_dependencies.TryGetValue(filePath, out var oldDeps))
            {
                foreach (var dep in oldDeps)
                {
                    if (_dependents.TryGetValue(dep, out var revDeps))
                    {
                        revDeps.Remove(filePath);
                    }
                }
                oldDeps.Clear();
            }
            else
            {
                _dependencies[filePath] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            // Add new dependencies
            foreach (var dep in newDependencies)
            {
                var normalizedDep = NormalizePath(dep);
                if (string.Equals(filePath, normalizedDep, StringComparison.OrdinalIgnoreCase))
                    continue;

                _dependencies[filePath].Add(normalizedDep);

                var revDeps = _dependents.GetOrAdd(normalizedDep, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                revDeps.Add(filePath);
            }
        }
    }

    /// <summary>
    /// Gets all files in the graph.
    /// </summary>
    public IReadOnlySet<string> GetAllFiles()
    {
        lock (_lock)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in _dependencies.Keys)
                result.Add(key);
            foreach (var key in _dependents.Keys)
                result.Add(key);
            return result;
        }
    }

    /// <summary>
    /// Gets the total number of dependency relationships.
    /// </summary>
    public int DependencyCount
    {
        get
        {
            lock (_lock)
            {
                return _dependencies.Values.Sum(s => s.Count);
            }
        }
    }

    /// <summary>
    /// Clears all dependencies.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _dependencies.Clear();
            _dependents.Clear();
        }
    }

    /// <summary>
    /// Checks if the graph has any dependencies.
    /// </summary>
    public bool IsEmpty => _dependencies.IsEmpty && _dependents.IsEmpty;

    private static string NormalizePath(string path)
    {
        // Don't convert separators - just trim trailing separators
        // This preserves consistency with paths used elsewhere in the codebase
        return path.TrimEnd('/', '\\');
    }
}
