using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Registry for tracking call sites across a project.
/// Provides fast lookup of callers for a given method and efficient
/// incremental updates when files are modified.
/// </summary>
/// <remarks>
/// Thread Safety: This class is thread-safe. All operations are protected
/// by a lock, ensuring safe concurrent access from multiple threads.
/// </remarks>
public class GDCallSiteRegistry
{
    private readonly object _lock = new object();

    /// <summary>
    /// Map: (declaring class name, method name) → list of call site entries.
    /// Used to quickly find all callers of a method.
    /// </summary>
    private readonly Dictionary<(string ClassName, string MethodName), List<GDCallSiteEntry>> _callSitesByTarget =
        new(CallSiteKeyComparer.Instance);

    /// <summary>
    /// Map: file path → list of call site entries from that file.
    /// Used for fast invalidation when a file changes.
    /// </summary>
    private readonly Dictionary<string, List<GDCallSiteEntry>> _callSitesByFile =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Map: (file path, method name) → list of call site entries from that method.
    /// Used for method-level invalidation.
    /// </summary>
    private readonly Dictionary<(string FilePath, string MethodName), List<GDCallSiteEntry>> _callSitesBySourceMethod =
        new(SourceMethodKeyComparer.Instance);

    /// <summary>
    /// Gets the total number of registered call sites.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _callSitesByFile.Values.Sum(list => list.Count);
            }
        }
    }

    /// <summary>
    /// Registers a call site entry.
    /// </summary>
    /// <param name="entry">The call site entry to register.</param>
    public void Register(GDCallSiteEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        lock (_lock)
        {
            // Add to target method index
            var targetKey = (entry.TargetClassName, entry.TargetMethodName);
            if (!_callSitesByTarget.TryGetValue(targetKey, out var targetList))
            {
                targetList = new List<GDCallSiteEntry>();
                _callSitesByTarget[targetKey] = targetList;
            }
            targetList.Add(entry);

            // Add to file index
            if (!_callSitesByFile.TryGetValue(entry.SourceFilePath, out var fileList))
            {
                fileList = new List<GDCallSiteEntry>();
                _callSitesByFile[entry.SourceFilePath] = fileList;
            }
            fileList.Add(entry);

            // Add to source method index (only if method name is known)
            if (!string.IsNullOrEmpty(entry.SourceMethodName))
            {
                var sourceKey = (entry.SourceFilePath, entry.SourceMethodName);
                if (!_callSitesBySourceMethod.TryGetValue(sourceKey, out var sourceMethodList))
                {
                    sourceMethodList = new List<GDCallSiteEntry>();
                    _callSitesBySourceMethod[sourceKey] = sourceMethodList;
                }
                sourceMethodList.Add(entry);
            }
        }
    }

    /// <summary>
    /// Registers multiple call site entries.
    /// </summary>
    /// <param name="entries">The entries to register.</param>
    public void RegisterRange(IEnumerable<GDCallSiteEntry> entries)
    {
        if (entries == null)
            throw new ArgumentNullException(nameof(entries));

        foreach (var entry in entries)
        {
            Register(entry);
        }
    }

    /// <summary>
    /// Unregisters all call sites originating from a specific file.
    /// Call this when a file is deleted or needs full re-analysis.
    /// </summary>
    /// <param name="filePath">The full path of the file.</param>
    /// <returns>The number of call sites that were removed.</returns>
    public int UnregisterFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return 0;

        lock (_lock)
        {
            if (!_callSitesByFile.TryGetValue(filePath, out var fileCallSites))
                return 0;

            int removedCount = fileCallSites.Count;

            // Remove from target method index
            foreach (var entry in fileCallSites)
            {
                var targetKey = (entry.TargetClassName, entry.TargetMethodName);
                if (_callSitesByTarget.TryGetValue(targetKey, out var targetList))
                {
                    targetList.Remove(entry);
                    if (targetList.Count == 0)
                        _callSitesByTarget.Remove(targetKey);
                }
            }

            // Remove from source method index
            var keysToRemove = _callSitesBySourceMethod.Keys
                .Where(k => string.Equals(k.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var key in keysToRemove)
            {
                _callSitesBySourceMethod.Remove(key);
            }

            // Remove from file index
            _callSitesByFile.Remove(filePath);

            return removedCount;
        }
    }

    /// <summary>
    /// Unregisters all call sites originating from a specific method within a file.
    /// Call this when a method is modified or removed.
    /// </summary>
    /// <param name="filePath">The full path of the file.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <returns>The number of call sites that were removed.</returns>
    public int UnregisterMethod(string filePath, string? methodName)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(methodName))
            return 0;

        lock (_lock)
        {
            var sourceKey = (filePath, methodName);
            if (!_callSitesBySourceMethod.TryGetValue(sourceKey, out var methodCallSites))
                return 0;

            int removedCount = methodCallSites.Count;

            // Remove from target method index
            foreach (var entry in methodCallSites)
            {
                var targetKey = (entry.TargetClassName, entry.TargetMethodName);
                if (_callSitesByTarget.TryGetValue(targetKey, out var targetList))
                {
                    targetList.Remove(entry);
                    if (targetList.Count == 0)
                        _callSitesByTarget.Remove(targetKey);
                }
            }

            // Remove from file index
            if (_callSitesByFile.TryGetValue(filePath, out var fileList))
            {
                foreach (var entry in methodCallSites)
                {
                    fileList.Remove(entry);
                }
                if (fileList.Count == 0)
                    _callSitesByFile.Remove(filePath);
            }

            // Remove from source method index
            _callSitesBySourceMethod.Remove(sourceKey);

            return removedCount;
        }
    }

    /// <summary>
    /// Gets all call sites targeting a specific method.
    /// </summary>
    /// <param name="targetClassName">The class name where the method is declared.</param>
    /// <param name="targetMethodName">The method name.</param>
    /// <returns>Read-only list of call site entries. Empty if none found.</returns>
    public IReadOnlyList<GDCallSiteEntry> GetCallersOf(string targetClassName, string targetMethodName)
    {
        if (string.IsNullOrEmpty(targetClassName) || string.IsNullOrEmpty(targetMethodName))
            return Array.Empty<GDCallSiteEntry>();

        lock (_lock)
        {
            var key = (targetClassName, targetMethodName);
            if (_callSitesByTarget.TryGetValue(key, out var list))
                return list.ToList(); // Return a copy for thread safety

            return Array.Empty<GDCallSiteEntry>();
        }
    }

    /// <summary>
    /// Gets all call sites originating from a specific file.
    /// </summary>
    /// <param name="filePath">The full path of the file.</param>
    /// <returns>Read-only list of call site entries. Empty if none found.</returns>
    public IReadOnlyList<GDCallSiteEntry> GetCallSitesInFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return Array.Empty<GDCallSiteEntry>();

        lock (_lock)
        {
            if (_callSitesByFile.TryGetValue(filePath, out var list))
                return list.ToList(); // Return a copy for thread safety

            return Array.Empty<GDCallSiteEntry>();
        }
    }

    /// <summary>
    /// Gets all call sites originating from a specific method within a file.
    /// </summary>
    /// <param name="filePath">The full path of the file.</param>
    /// <param name="methodName">The method name.</param>
    /// <returns>Read-only list of call site entries. Empty if none found.</returns>
    public IReadOnlyList<GDCallSiteEntry> GetCallSitesInMethod(string filePath, string? methodName)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(methodName))
            return Array.Empty<GDCallSiteEntry>();

        lock (_lock)
        {
            var key = (filePath, methodName);
            if (_callSitesBySourceMethod.TryGetValue(key, out var list))
                return list.ToList(); // Return a copy for thread safety

            return Array.Empty<GDCallSiteEntry>();
        }
    }

    /// <summary>
    /// Gets all files that contain calls to methods in the specified class.
    /// Useful for determining which files need reanalysis when a class changes.
    /// </summary>
    /// <param name="className">The class name.</param>
    /// <returns>Set of file paths that contain calls to the class.</returns>
    public IReadOnlySet<string> GetFilesCallingClass(string className)
    {
        if (string.IsNullOrEmpty(className))
            return new HashSet<string>();

        lock (_lock)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in _callSitesByTarget.Keys)
            {
                if (string.Equals(key.ClassName, className, StringComparison.OrdinalIgnoreCase))
                {
                    if (_callSitesByTarget.TryGetValue(key, out var entries))
                    {
                        foreach (var entry in entries)
                        {
                            result.Add(entry.SourceFilePath);
                        }
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Gets all unique target class/method pairs in the registry.
    /// </summary>
    /// <returns>Set of (className, methodName) tuples.</returns>
    public IReadOnlySet<(string ClassName, string MethodName)> GetAllTargets()
    {
        lock (_lock)
        {
            return new HashSet<(string, string)>(_callSitesByTarget.Keys);
        }
    }

    /// <summary>
    /// Clears all registered call sites.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _callSitesByTarget.Clear();
            _callSitesByFile.Clear();
            _callSitesBySourceMethod.Clear();
        }
    }

    /// <summary>
    /// Checks if the registry contains any call sites from a file.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file has registered call sites.</returns>
    public bool HasCallSitesFromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        lock (_lock)
        {
            return _callSitesByFile.ContainsKey(filePath);
        }
    }

    /// <summary>
    /// Checks if there are any callers of a specific method.
    /// </summary>
    /// <param name="targetClassName">The class name.</param>
    /// <param name="targetMethodName">The method name.</param>
    /// <returns>True if there are any registered callers.</returns>
    public bool HasCallersOf(string targetClassName, string targetMethodName)
    {
        if (string.IsNullOrEmpty(targetClassName) || string.IsNullOrEmpty(targetMethodName))
            return false;

        lock (_lock)
        {
            var key = (targetClassName, targetMethodName);
            return _callSitesByTarget.TryGetValue(key, out var list) && list.Count > 0;
        }
    }

    #region Key Comparers

    private class CallSiteKeyComparer : IEqualityComparer<(string ClassName, string MethodName)>
    {
        public static readonly CallSiteKeyComparer Instance = new();

        public bool Equals((string ClassName, string MethodName) x, (string ClassName, string MethodName) y)
        {
            return string.Equals(x.ClassName, y.ClassName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.MethodName, y.MethodName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string ClassName, string MethodName) obj)
        {
            unchecked
            {
                var h1 = obj.ClassName?.ToUpperInvariant().GetHashCode() ?? 0;
                var h2 = obj.MethodName?.ToUpperInvariant().GetHashCode() ?? 0;
                return h1 * 397 ^ h2;
            }
        }
    }

    private class SourceMethodKeyComparer : IEqualityComparer<(string FilePath, string MethodName)>
    {
        public static readonly SourceMethodKeyComparer Instance = new();

        public bool Equals((string FilePath, string MethodName) x, (string FilePath, string MethodName) y)
        {
            return string.Equals(x.FilePath, y.FilePath, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.MethodName, y.MethodName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string FilePath, string MethodName) obj)
        {
            unchecked
            {
                var h1 = obj.FilePath?.ToUpperInvariant().GetHashCode() ?? 0;
                var h2 = obj.MethodName?.ToUpperInvariant().GetHashCode() ?? 0;
                return h1 * 397 ^ h2;
            }
        }
    }

    #endregion
}
