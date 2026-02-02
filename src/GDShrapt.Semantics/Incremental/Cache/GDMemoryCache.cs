using System.Collections.Concurrent;
using GDShrapt.Semantics.Incremental.Results;

namespace GDShrapt.Semantics.Incremental.Cache;

/// <summary>
/// In-memory cache for analysis results.
/// Thread-safe for concurrent access.
/// </summary>
public class GDMemoryCache : IGDAnalysisCache
{
    private readonly ConcurrentDictionary<string, GDCachedAnalysisEntry> _cache = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _filePathToKeys = new();
    private readonly object _lock = new();

    public int Count => _cache.Count;

    public bool TryGet(GDCacheKey key, out GDCachedAnalysisEntry? entry)
    {
        return _cache.TryGetValue(key.Key, out entry);
    }

    public void Set(GDCacheKey key, GDCachedAnalysisEntry entry)
    {
        entry.Key = key;
        _cache[key.Key] = entry;

        // Track key by file path for removal
        lock (_lock)
        {
            if (!_filePathToKeys.TryGetValue(key.FilePath, out var keys))
            {
                keys = new HashSet<string>();
                _filePathToKeys[key.FilePath] = keys;
            }
            keys.Add(key.Key);
        }
    }

    public void Remove(GDCacheKey key)
    {
        _cache.TryRemove(key.Key, out _);

        lock (_lock)
        {
            if (_filePathToKeys.TryGetValue(key.FilePath, out var keys))
            {
                keys.Remove(key.Key);
                if (keys.Count == 0)
                    _filePathToKeys.TryRemove(key.FilePath, out _);
            }
        }
    }

    public void RemoveByFilePath(string filePath)
    {
        lock (_lock)
        {
            if (_filePathToKeys.TryRemove(filePath, out var keys))
            {
                foreach (var key in keys)
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }
    }

    public void Clear()
    {
        _cache.Clear();
        lock (_lock)
        {
            _filePathToKeys.Clear();
        }
    }

    public IEnumerable<string> GetCachedFilePaths()
    {
        lock (_lock)
        {
            return _filePathToKeys.Keys.ToList();
        }
    }
}
