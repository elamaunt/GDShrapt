using GDShrapt.Semantics.Incremental.Cache;

namespace GDShrapt.Semantics.Incremental.Tracking;

/// <summary>
/// Tracks file changes between analysis runs.
/// </summary>
public class GDFileChangeTracker
{
    private readonly Dictionary<string, string> _fileHashes = new();
    private readonly object _lock = new();

    /// <summary>
    /// Updates the tracker with current file state and returns changed files.
    /// </summary>
    public GDFileChanges DetectChanges(GDScriptProject project)
    {
        var changes = new GDFileChanges();
        var currentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        lock (_lock)
        {
            if (project.ScriptFiles == null)
                return changes;

            foreach (var script in project.ScriptFiles)
            {
                if (script.FullPath == null)
                    continue;

                currentFiles.Add(script.FullPath);

                string content;
                try
                {
                    content = File.ReadAllText(script.FullPath);
                }
                catch
                {
                    // File might be locked or deleted
                    continue;
                }

                var hash = GDCacheKey.ComputeContentHash(content);

                if (!_fileHashes.TryGetValue(script.FullPath, out var oldHash))
                {
                    changes.Added.Add(script.FullPath);
                    _fileHashes[script.FullPath] = hash;
                }
                else if (oldHash != hash)
                {
                    changes.Modified.Add(script.FullPath);
                    _fileHashes[script.FullPath] = hash;
                }
                // else: unchanged, don't add to any list
            }

            // Find deleted files
            var deletedFiles = _fileHashes.Keys
                .Where(path => !currentFiles.Contains(path))
                .ToList();

            foreach (var path in deletedFiles)
            {
                changes.Deleted.Add(path);
                _fileHashes.Remove(path);
            }
        }

        return changes;
    }

    /// <summary>
    /// Gets the hash for a file path.
    /// </summary>
    public string? GetHash(string filePath)
    {
        lock (_lock)
        {
            return _fileHashes.TryGetValue(filePath, out var hash) ? hash : null;
        }
    }

    /// <summary>
    /// Sets the hash for a file path.
    /// </summary>
    public void SetHash(string filePath, string hash)
    {
        lock (_lock)
        {
            _fileHashes[filePath] = hash;
        }
    }

    /// <summary>
    /// Removes a file from tracking.
    /// </summary>
    public void Remove(string filePath)
    {
        lock (_lock)
        {
            _fileHashes.Remove(filePath);
        }
    }

    /// <summary>
    /// Clears all tracked files.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _fileHashes.Clear();
        }
    }

    /// <summary>
    /// Gets all tracked file paths.
    /// </summary>
    public IReadOnlyList<string> GetTrackedFiles()
    {
        lock (_lock)
        {
            return _fileHashes.Keys.ToList();
        }
    }

    /// <summary>
    /// Loads state from a dictionary.
    /// </summary>
    public void LoadState(IReadOnlyDictionary<string, string> fileHashes)
    {
        lock (_lock)
        {
            _fileHashes.Clear();
            foreach (var kvp in fileHashes)
            {
                _fileHashes[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// Gets state as a dictionary.
    /// </summary>
    public Dictionary<string, string> GetState()
    {
        lock (_lock)
        {
            return new Dictionary<string, string>(_fileHashes);
        }
    }
}

/// <summary>
/// Represents changes detected between analysis runs.
/// </summary>
public class GDFileChanges
{
    /// <summary>
    /// Files that were added.
    /// </summary>
    public List<string> Added { get; } = new();

    /// <summary>
    /// Files that were modified.
    /// </summary>
    public List<string> Modified { get; } = new();

    /// <summary>
    /// Files that were deleted.
    /// </summary>
    public List<string> Deleted { get; } = new();

    /// <summary>
    /// Whether there are any changes.
    /// </summary>
    public bool HasChanges => Added.Count > 0 || Modified.Count > 0 || Deleted.Count > 0;

    /// <summary>
    /// Total number of changed files.
    /// </summary>
    public int TotalChanged => Added.Count + Modified.Count + Deleted.Count;

    /// <summary>
    /// Gets all files that need reanalysis.
    /// </summary>
    public IEnumerable<string> FilesToAnalyze => Added.Concat(Modified);
}
