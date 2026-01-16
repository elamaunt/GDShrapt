using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Tracks dependencies between methods for incremental inference updates.
/// When a method changes, this tracker can identify all dependent methods that need re-inference.
/// </summary>
/// <remarks>
/// This class is a preparation for future incremental analysis.
/// Currently, it provides dependency tracking that can be used to minimize re-analysis
/// when only a subset of files change.
/// </remarks>
public class GDInferenceDependencyTracker
{
    // Method -> Methods that depend on it (reverse dependencies)
    private readonly Dictionary<string, HashSet<string>> _dependents = new();

    // Method -> Methods it depends on (forward dependencies)
    private readonly Dictionary<string, HashSet<string>> _dependencies = new();

    // File -> Methods defined in that file
    private readonly Dictionary<string, HashSet<string>> _fileMethods = new();

    // Method -> File where it's defined
    private readonly Dictionary<string, string> _methodFiles = new();

    /// <summary>
    /// Clears all tracked dependencies.
    /// </summary>
    public void Clear()
    {
        _dependents.Clear();
        _dependencies.Clear();
        _fileMethods.Clear();
        _methodFiles.Clear();
    }

    /// <summary>
    /// Registers a method and its file.
    /// </summary>
    /// <param name="methodKey">Full method key (e.g., "Player.attack").</param>
    /// <param name="filePath">Path to the file containing the method.</param>
    public void RegisterMethod(string methodKey, string filePath)
    {
        if (string.IsNullOrEmpty(methodKey) || string.IsNullOrEmpty(filePath))
            return;

        _methodFiles[methodKey] = filePath;

        if (!_fileMethods.TryGetValue(filePath, out var methods))
        {
            methods = new HashSet<string>();
            _fileMethods[filePath] = methods;
        }
        methods.Add(methodKey);
    }

    /// <summary>
    /// Registers a dependency between methods.
    /// </summary>
    /// <param name="fromMethod">Method that has the dependency.</param>
    /// <param name="toMethod">Method that is depended upon.</param>
    public void AddDependency(string fromMethod, string toMethod)
    {
        if (string.IsNullOrEmpty(fromMethod) || string.IsNullOrEmpty(toMethod))
            return;

        // Forward dependency: fromMethod depends on toMethod
        if (!_dependencies.TryGetValue(fromMethod, out var deps))
        {
            deps = new HashSet<string>();
            _dependencies[fromMethod] = deps;
        }
        deps.Add(toMethod);

        // Reverse dependency: toMethod has fromMethod as dependent
        if (!_dependents.TryGetValue(toMethod, out var revDeps))
        {
            revDeps = new HashSet<string>();
            _dependents[toMethod] = revDeps;
        }
        revDeps.Add(fromMethod);
    }

    /// <summary>
    /// Registers multiple dependencies from a list.
    /// </summary>
    public void AddDependencies(IEnumerable<GDInferenceDependency> dependencies)
    {
        foreach (var dep in dependencies)
        {
            AddDependency(dep.FromMethod, dep.ToMethod);
        }
    }

    /// <summary>
    /// Gets all methods that directly depend on the given method.
    /// </summary>
    public IEnumerable<string> GetDirectDependents(string methodKey)
    {
        if (string.IsNullOrEmpty(methodKey))
            return Enumerable.Empty<string>();

        return _dependents.TryGetValue(methodKey, out var deps)
            ? deps
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Gets all methods that the given method directly depends on.
    /// </summary>
    public IEnumerable<string> GetDirectDependencies(string methodKey)
    {
        if (string.IsNullOrEmpty(methodKey))
            return Enumerable.Empty<string>();

        return _dependencies.TryGetValue(methodKey, out var deps)
            ? deps
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Gets all methods that transitively depend on the given method.
    /// Uses BFS to traverse the dependency graph.
    /// </summary>
    public IEnumerable<string> GetAllDependents(string methodKey)
    {
        if (string.IsNullOrEmpty(methodKey))
            return Enumerable.Empty<string>();

        var result = new HashSet<string>();
        var queue = new Queue<string>();

        foreach (var dep in GetDirectDependents(methodKey))
        {
            queue.Enqueue(dep);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!result.Add(current))
                continue;

            foreach (var dep in GetDirectDependents(current))
            {
                if (!result.Contains(dep))
                    queue.Enqueue(dep);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all methods that transitively depend on any of the given methods.
    /// </summary>
    public IEnumerable<string> GetAllDependents(IEnumerable<string> methodKeys)
    {
        var result = new HashSet<string>();
        foreach (var key in methodKeys)
        {
            foreach (var dep in GetAllDependents(key))
            {
                result.Add(dep);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets all methods defined in a file.
    /// </summary>
    public IEnumerable<string> GetMethodsInFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return Enumerable.Empty<string>();

        return _fileMethods.TryGetValue(filePath, out var methods)
            ? methods
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Gets the file path for a method.
    /// </summary>
    public string? GetFileForMethod(string methodKey)
    {
        if (string.IsNullOrEmpty(methodKey))
            return null;

        return _methodFiles.TryGetValue(methodKey, out var file) ? file : null;
    }

    /// <summary>
    /// Gets all methods that need re-inference when a file changes.
    /// This includes methods in the file plus all transitive dependents.
    /// </summary>
    public IEnumerable<string> GetMethodsToRecomputeOnFileChange(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return Enumerable.Empty<string>();

        var methodsInFile = GetMethodsInFile(filePath).ToList();
        var dependents = GetAllDependents(methodsInFile);

        // Return methods in file + all dependents
        return methodsInFile.Concat(dependents).Distinct();
    }

    /// <summary>
    /// Gets all methods that need re-inference when multiple files change.
    /// </summary>
    public IEnumerable<string> GetMethodsToRecomputeOnFilesChange(IEnumerable<string> filePaths)
    {
        var result = new HashSet<string>();
        foreach (var path in filePaths)
        {
            foreach (var method in GetMethodsToRecomputeOnFileChange(path))
            {
                result.Add(method);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets all files that need re-analysis when a file changes.
    /// </summary>
    public IEnumerable<string> GetFilesToReanalyzeOnFileChange(string filePath)
    {
        var methods = GetMethodsToRecomputeOnFileChange(filePath);
        var files = new HashSet<string>();

        foreach (var method in methods)
        {
            var file = GetFileForMethod(method);
            if (file != null)
                files.Add(file);
        }

        return files;
    }

    /// <summary>
    /// Gets statistics about the dependency tracker.
    /// </summary>
    public GDDependencyTrackerStatistics GetStatistics()
    {
        var totalMethods = _methodFiles.Count;
        var totalDependencies = _dependencies.Values.Sum(d => d.Count);
        var methodsWithDependents = _dependents.Count(d => d.Value.Count > 0);
        var maxDependents = _dependents.Values.Any() ? _dependents.Values.Max(d => d.Count) : 0;
        var avgDependents = totalMethods > 0
            ? (double)_dependents.Values.Sum(d => d.Count) / totalMethods
            : 0;

        return new GDDependencyTrackerStatistics
        {
            TotalMethodsTracked = totalMethods,
            TotalFilesTracked = _fileMethods.Count,
            TotalDependencies = totalDependencies,
            MethodsWithDependents = methodsWithDependents,
            MaxDependentsPerMethod = maxDependents,
            AverageDependentsPerMethod = avgDependents
        };
    }

    /// <summary>
    /// Removes a method and all its dependencies.
    /// </summary>
    public void RemoveMethod(string methodKey)
    {
        if (string.IsNullOrEmpty(methodKey))
            return;

        // Remove from file tracking
        if (_methodFiles.TryGetValue(methodKey, out var file))
        {
            _methodFiles.Remove(methodKey);
            if (_fileMethods.TryGetValue(file, out var methods))
            {
                methods.Remove(methodKey);
                if (methods.Count == 0)
                    _fileMethods.Remove(file);
            }
        }

        // Remove forward dependencies
        if (_dependencies.TryGetValue(methodKey, out var deps))
        {
            foreach (var dep in deps)
            {
                if (_dependents.TryGetValue(dep, out var revDeps))
                {
                    revDeps.Remove(methodKey);
                }
            }
            _dependencies.Remove(methodKey);
        }

        // Remove reverse dependencies
        if (_dependents.TryGetValue(methodKey, out var dependents))
        {
            foreach (var dep in dependents)
            {
                if (_dependencies.TryGetValue(dep, out var forwardDeps))
                {
                    forwardDeps.Remove(methodKey);
                }
            }
            _dependents.Remove(methodKey);
        }
    }

    /// <summary>
    /// Removes all methods in a file.
    /// </summary>
    public void RemoveFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        if (!_fileMethods.TryGetValue(filePath, out var methods))
            return;

        // Copy to avoid modification during iteration
        foreach (var method in methods.ToList())
        {
            RemoveMethod(method);
        }

        _fileMethods.Remove(filePath);
    }
}

/// <summary>
/// Statistics about the dependency tracker.
/// </summary>
public class GDDependencyTrackerStatistics
{
    /// <summary>
    /// Total number of methods being tracked.
    /// </summary>
    public int TotalMethodsTracked { get; init; }

    /// <summary>
    /// Total number of files being tracked.
    /// </summary>
    public int TotalFilesTracked { get; init; }

    /// <summary>
    /// Total number of dependencies tracked.
    /// </summary>
    public int TotalDependencies { get; init; }

    /// <summary>
    /// Number of methods that have at least one dependent.
    /// </summary>
    public int MethodsWithDependents { get; init; }

    /// <summary>
    /// Maximum number of dependents for any single method.
    /// </summary>
    public int MaxDependentsPerMethod { get; init; }

    /// <summary>
    /// Average number of dependents per method.
    /// </summary>
    public double AverageDependentsPerMethod { get; init; }

    public override string ToString()
    {
        return $"Methods: {TotalMethodsTracked}, Files: {TotalFilesTracked}, " +
               $"Dependencies: {TotalDependencies}, Max dependents: {MaxDependentsPerMethod}";
    }
}
