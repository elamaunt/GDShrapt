namespace GDShrapt.Semantics.Incremental.Tracking;

/// <summary>
/// Tracks dependencies between files for cascade invalidation.
/// </summary>
public class GDDependencyGraph
{
    // File -> Files it depends on
    private readonly Dictionary<string, HashSet<string>> _dependencies = new(StringComparer.OrdinalIgnoreCase);

    // File -> Files that depend on it
    private readonly Dictionary<string, HashSet<string>> _dependents = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    /// <summary>
    /// Sets the dependencies for a file.
    /// </summary>
    public void SetDependencies(string filePath, IEnumerable<string> dependencies)
    {
        lock (_lock)
        {
            // Remove old reverse mappings
            if (_dependencies.TryGetValue(filePath, out var oldDeps))
            {
                foreach (var dep in oldDeps)
                {
                    if (_dependents.TryGetValue(dep, out var depSet))
                    {
                        depSet.Remove(filePath);
                    }
                }
            }

            // Set new dependencies
            var newDeps = new HashSet<string>(dependencies, StringComparer.OrdinalIgnoreCase);
            _dependencies[filePath] = newDeps;

            // Add reverse mappings
            foreach (var dep in newDeps)
            {
                if (!_dependents.TryGetValue(dep, out var depSet))
                {
                    depSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _dependents[dep] = depSet;
                }
                depSet.Add(filePath);
            }
        }
    }

    /// <summary>
    /// Gets files that depend on the specified file.
    /// </summary>
    public IReadOnlySet<string> GetDependents(string filePath)
    {
        lock (_lock)
        {
            if (_dependents.TryGetValue(filePath, out var dependents))
            {
                return new HashSet<string>(dependents, StringComparer.OrdinalIgnoreCase);
            }
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Gets files that the specified file depends on.
    /// </summary>
    public IReadOnlySet<string> GetDependencies(string filePath)
    {
        lock (_lock)
        {
            if (_dependencies.TryGetValue(filePath, out var dependencies))
            {
                return new HashSet<string>(dependencies, StringComparer.OrdinalIgnoreCase);
            }
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Gets all files that transitively depend on the specified file.
    /// </summary>
    public HashSet<string> GetTransitiveDependents(string filePath)
    {
        lock (_lock)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            queue.Enqueue(filePath);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (_dependents.TryGetValue(current, out var dependents))
                {
                    foreach (var dep in dependents)
                    {
                        if (result.Add(dep))
                        {
                            queue.Enqueue(dep);
                        }
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Removes a file from the dependency graph.
    /// </summary>
    public void RemoveFile(string filePath)
    {
        lock (_lock)
        {
            // Remove from dependencies
            if (_dependencies.TryRemove(filePath, out var deps) && deps != null)
            {
                foreach (var dep in deps)
                {
                    if (_dependents.TryGetValue(dep, out var depSet))
                    {
                        depSet.Remove(filePath);
                    }
                }
            }

            // Remove from dependents
            if (_dependents.TryRemove(filePath, out var dependents) && dependents != null)
            {
                foreach (var dependent in dependents)
                {
                    if (_dependencies.TryGetValue(dependent, out var depSet))
                    {
                        depSet.Remove(filePath);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Clears the entire graph.
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
    /// Gets the number of files in the graph.
    /// </summary>
    public int FileCount
    {
        get
        {
            lock (_lock)
            {
                return _dependencies.Count;
            }
        }
    }
}

internal static class DictionaryExtensions
{
    public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, out TValue? value)
        where TKey : notnull
    {
        if (dict.TryGetValue(key, out value))
        {
            dict.Remove(key);
            return true;
        }
        value = default;
        return false;
    }
}
