using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GDShrapt.Abstractions;
using GDShrapt.Semantics.Incremental.Results;

namespace GDShrapt.Semantics.Incremental.Cache;

/// <summary>
/// File-based cache for persistent storage of analysis results.
/// </summary>
public class GDFileCache : IGDAnalysisCache
{
    private readonly string _cacheDirectory;
    private readonly string _entriesDirectory;
    private readonly GDMemoryCache _memoryCache;
    private readonly object _fileLock = new();
    private readonly long _maxCacheSizeBytes;
    private readonly IGDLogger? _logger;
    private readonly Dictionary<string, HashSet<string>> _filePathIndex = new();

    private const string EntriesSubdir = "entries";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GDFileCache(string cacheDirectory, int maxCacheSizeMb = 100, IGDLogger? logger = null)
    {
        _cacheDirectory = cacheDirectory;
        _entriesDirectory = Path.Combine(cacheDirectory, EntriesSubdir);
        _memoryCache = new GDMemoryCache();
        _maxCacheSizeBytes = maxCacheSizeMb <= 0 ? 0 : maxCacheSizeMb * 1024L * 1024L;
        _logger = logger;

        Directory.CreateDirectory(_entriesDirectory);
    }

    public int Count => _memoryCache.Count;

    public bool TryGet(GDCacheKey key, out GDCachedAnalysisEntry? entry)
    {
        // Check memory cache first
        if (_memoryCache.TryGet(key, out entry))
            return true;

        // Try loading from disk
        var filePath = GetEntryPath(key);
        if (!File.Exists(filePath))
        {
            entry = null;
            return false;
        }

        try
        {
            lock (_fileLock)
            {
                var json = File.ReadAllText(filePath);
                entry = JsonSerializer.Deserialize<GDCachedAnalysisEntry>(json, JsonOptions);
            }

            if (entry != null)
            {
                entry.Key = key;
                _memoryCache.Set(key, entry);
                return true;
            }
        }
        catch (JsonException ex)
        {
            // Corrupted cache file, remove it
            _logger?.Warning($"Corrupted cache file {filePath}: {ex.Message}");
            TryDeleteFile(filePath);
        }
        catch (IOException ex)
        {
            // File read error
            _logger?.Warning($"Failed to read cache file {filePath}: {ex.Message}");
        }

        entry = null;
        return false;
    }

    public void Set(GDCacheKey key, GDCachedAnalysisEntry entry)
    {
        entry.Key = key;
        _memoryCache.Set(key, entry);

        // LRU eviction before writing
        if (_maxCacheSizeBytes > 0)
        {
            EnforceCacheSizeLimit();
        }

        // Persist to disk
        var entryPath = GetEntryPath(key);
        try
        {
            var json = JsonSerializer.Serialize(entry, JsonOptions);
            lock (_fileLock)
            {
                var directory = Path.GetDirectoryName(entryPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(entryPath, json);

                // Update file path index
                if (!_filePathIndex.TryGetValue(key.FilePath, out var entryFiles))
                {
                    entryFiles = new HashSet<string>();
                    _filePathIndex[key.FilePath] = entryFiles;
                }
                entryFiles.Add(entryPath);
            }
        }
        catch (JsonException ex)
        {
            _logger?.Warning($"Failed to serialize cache entry: {ex.Message}");
        }
        catch (IOException ex)
        {
            _logger?.Warning($"Failed to write cache file {entryPath}: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.Warning($"Access denied writing cache file {entryPath}: {ex.Message}");
        }
    }

    public void Remove(GDCacheKey key)
    {
        _memoryCache.Remove(key);

        var entryPath = GetEntryPath(key);
        TryDeleteFile(entryPath);

        // Update file path index
        lock (_fileLock)
        {
            if (_filePathIndex.TryGetValue(key.FilePath, out var entryFiles))
            {
                entryFiles.Remove(entryPath);
                if (entryFiles.Count == 0)
                    _filePathIndex.Remove(key.FilePath);
            }
        }
    }

    public void RemoveByFilePath(string filePath)
    {
        _memoryCache.RemoveByFilePath(filePath);

        // Remove all disk files for this file path using the index
        lock (_fileLock)
        {
            if (_filePathIndex.TryGetValue(filePath, out var entryFiles))
            {
                foreach (var file in entryFiles)
                {
                    TryDeleteFile(file);
                }
                _filePathIndex.Remove(filePath);
            }
        }
    }

    public void Clear()
    {
        _memoryCache.Clear();

        lock (_fileLock)
        {
            _filePathIndex.Clear();
        }

        try
        {
            if (Directory.Exists(_entriesDirectory))
            {
                Directory.Delete(_entriesDirectory, recursive: true);
                Directory.CreateDirectory(_entriesDirectory);
            }
        }
        catch (IOException ex)
        {
            _logger?.Warning($"Failed to clear disk cache: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.Warning($"Access denied clearing disk cache: {ex.Message}");
        }
    }

    public IEnumerable<string> GetCachedFilePaths()
    {
        return _memoryCache.GetCachedFilePaths();
    }

    /// <summary>
    /// Loads all cached entries from disk into memory.
    /// </summary>
    public void LoadFromDisk()
    {
        if (!Directory.Exists(_entriesDirectory))
            return;

        try
        {
            var files = Directory.GetFiles(_entriesDirectory, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var entry = JsonSerializer.Deserialize<GDCachedAnalysisEntry>(json, JsonOptions);
                    if (entry != null && !string.IsNullOrEmpty(entry.FilePath))
                    {
                        // Reconstruct key from entry's stored values
                        var key = GDCacheKey.CreateWithHash(entry.FilePath, entry.ContentHash);
                        entry.Key = key;
                        _memoryCache.Set(key, entry);

                        // Restore file path index
                        lock (_fileLock)
                        {
                            if (!_filePathIndex.TryGetValue(entry.FilePath, out var entryFiles))
                            {
                                entryFiles = new HashSet<string>();
                                _filePathIndex[entry.FilePath] = entryFiles;
                            }
                            entryFiles.Add(file);
                        }
                    }
                }
                catch (JsonException)
                {
                    // Skip corrupted files
                    _logger?.Debug($"Removing corrupted cache file: {file}");
                    TryDeleteFile(file);
                }
                catch (IOException)
                {
                    // Skip unreadable files
                    _logger?.Debug($"Skipping unreadable cache file: {file}");
                }
            }
        }
        catch (IOException ex)
        {
            _logger?.Warning($"Failed to load cache from disk: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.Warning($"Access denied loading cache from disk: {ex.Message}");
        }
    }

    /// <summary>
    /// Enforces cache size limit using LRU eviction.
    /// Removes oldest files (by LastWriteTimeUtc) until cache is below 80% of limit.
    /// </summary>
    private void EnforceCacheSizeLimit()
    {
        try
        {
            var totalSize = GetCacheDirectorySize();
            if (totalSize <= _maxCacheSizeBytes)
                return;

            // LRU: remove by LastWriteTimeUtc until 80% of limit
            var targetSize = (long)(_maxCacheSizeBytes * 0.8);
            var files = Directory.GetFiles(_entriesDirectory, "*.json")
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.LastWriteTimeUtc)
                .ToList();

            foreach (var file in files)
            {
                if (totalSize <= targetSize)
                    break;

                totalSize -= file.Length;
                TryDeleteFile(file.FullName);
                _logger?.Debug($"LRU evicted cache entry: {file.Name}");
            }
        }
        catch (IOException ex)
        {
            _logger?.Warning($"Cache size enforcement failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the total size of all cache files in bytes.
    /// </summary>
    private long GetCacheDirectorySize()
    {
        if (!Directory.Exists(_entriesDirectory))
            return 0;

        return Directory.GetFiles(_entriesDirectory, "*.json")
            .Sum(f => new FileInfo(f).Length);
    }

    /// <summary>
    /// Gets the file path for a cache entry.
    /// Uses a hash of the key to avoid path length issues.
    /// </summary>
    private string GetEntryPath(GDCacheKey key)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key.Key)));
        return Path.Combine(_entriesDirectory, $"{hash[..16]}.json");
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // Ignore deletion failures - file may be locked
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore access denied - permissions issue
        }
    }
}
