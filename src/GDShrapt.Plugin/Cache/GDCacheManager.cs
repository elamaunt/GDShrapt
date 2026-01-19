using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GDShrapt.Plugin;

/// <summary>
/// Manages caching of lint results to improve performance.
/// Cache is stored in .gdshrapt/cache/ directory.
/// </summary>
internal class GDCacheManager : IDisposable
{
    private const string CacheDirectoryName = ".gdshrapt";
    private const string CacheSubDirectory = "cache";
    private const string LintSubDirectory = "lint";
    private const string IndexFileName = "cache.index.json";

    private const int CurrentCacheVersion = 10;

    private readonly string _projectPath;
    private readonly string _cacheDirectory;
    private readonly string _lintCacheDirectory;
    private readonly string _indexPath;

    private GDCacheIndex _index;
    private bool _indexDirty;
    private bool _disposedValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a new CacheManager for the specified project.
    /// </summary>
    public GDCacheManager(string projectPath)
    {
        _projectPath = projectPath;
        _cacheDirectory = Path.Combine(projectPath, CacheDirectoryName, CacheSubDirectory);
        _lintCacheDirectory = Path.Combine(_cacheDirectory, LintSubDirectory);
        _indexPath = Path.Combine(_cacheDirectory, IndexFileName);

        _index = new GDCacheIndex();

        EnsureDirectoriesExist();
        LoadIndex();
    }

    /// <summary>
    /// Tries to get cached lint results for a script.
    /// </summary>
    public bool TryGetLintCache(GDScriptFile script, string contentHash, out IReadOnlyList<GDPluginDiagnostic> diagnostics)
    {
        diagnostics = Array.Empty<GDPluginDiagnostic>();

        try
        {
            var key = GetCacheKey(script);

            if (!_index.Files.TryGetValue(key, out var fileInfo))
                return false;

            // Check if content hash matches
            if (fileInfo.ContentHash != contentHash)
                return false;

            // Check if cache file exists
            if (string.IsNullOrEmpty(fileInfo.LintCacheFile))
                return false;

            var cachePath = Path.Combine(_lintCacheDirectory, fileInfo.LintCacheFile);
            if (!File.Exists(cachePath))
                return false;

            // Load and deserialize
            var json = File.ReadAllText(cachePath);
            var entry = JsonSerializer.Deserialize<GDLintCacheEntry>(json, JsonOptions);

            if (entry == null || entry.ContentHash != contentHash)
                return false;

            // Convert to diagnostics
            diagnostics = entry.Diagnostics
                .Select(d => d.ToDiagnostic(script))
                .ToList();

            Logger.Debug($"Cache hit for {script.FullPath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Debug($"Cache read error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stores lint results in cache.
    /// </summary>
    public void StoreLintCache(GDScriptFile script, string contentHash, IReadOnlyList<GDPluginDiagnostic> diagnostics)
    {
        try
        {
            var key = GetCacheKey(script);
            var cacheFileName = ComputeHash(key) + ".json";
            var cachePath = Path.Combine(_lintCacheDirectory, cacheFileName);

            // Create cache entry
            var entry = new GDLintCacheEntry
            {
                ContentHash = contentHash,
                AnalyzedAt = DateTime.UtcNow,
                Diagnostics = diagnostics.Select(GDSerializedDiagnostic.FromDiagnostic).ToList()
            };

            // Write cache file
            var json = JsonSerializer.Serialize(entry, JsonOptions);
            File.WriteAllText(cachePath, json);

            // Update index
            _index.Files[key] = new GDCacheFileInfo
            {
                ContentHash = contentHash,
                CachedAt = DateTime.UtcNow,
                LintCacheFile = cacheFileName
            };

            _indexDirty = true;
            SaveIndexIfDirty();

            Logger.Debug($"Cached lint results for {script.FullPath}");
        }
        catch (Exception ex)
        {
            Logger.Debug($"Cache write error: {ex.Message}");
        }
    }

    /// <summary>
    /// Invalidates cache for a specific script.
    /// </summary>
    public void Invalidate(GDScriptFile script)
    {
        try
        {
            var key = GetCacheKey(script);

            if (_index.Files.TryGetValue(key, out var fileInfo))
            {
                // Delete cache file
                if (!string.IsNullOrEmpty(fileInfo.LintCacheFile))
                {
                    var cachePath = Path.Combine(_lintCacheDirectory, fileInfo.LintCacheFile);
                    if (File.Exists(cachePath))
                        File.Delete(cachePath);
                }

                _index.Files.Remove(key);
                _indexDirty = true;
                SaveIndexIfDirty();
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Cache invalidation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Invalidates all cached data.
    /// </summary>
    public void InvalidateAll()
    {
        try
        {
            // Delete all cache files
            if (Directory.Exists(_lintCacheDirectory))
            {
                foreach (var file in Directory.GetFiles(_lintCacheDirectory, "*.json"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"Failed to delete cache file '{file}': {ex.Message}");
                    }
                }
            }

            _index = new GDCacheIndex();
            _indexDirty = true;
            SaveIndexIfDirty();

            Logger.Info("Cache invalidated");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error invalidating cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans up old cache entries.
    /// </summary>
    public void CleanupOldEntries(int maxAgeDays = 30)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-maxAgeDays);
            var keysToRemove = new List<string>();

            foreach (var kvp in _index.Files)
            {
                if (kvp.Value.CachedAt < cutoff)
                {
                    keysToRemove.Add(kvp.Key);

                    // Delete cache file
                    if (!string.IsNullOrEmpty(kvp.Value.LintCacheFile))
                    {
                        var cachePath = Path.Combine(_lintCacheDirectory, kvp.Value.LintCacheFile);
                        if (File.Exists(cachePath))
                            File.Delete(cachePath);
                    }
                }
            }

            foreach (var key in keysToRemove)
            {
                _index.Files.Remove(key);
            }

            if (keysToRemove.Count > 0)
            {
                _indexDirty = true;
                SaveIndexIfDirty();
                Logger.Info($"Cleaned up {keysToRemove.Count} old cache entries");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error cleaning up cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public GDCacheStats GetStats()
    {
        return new GDCacheStats
        {
            TotalEntries = _index.Files.Count,
            LastUpdated = _index.LastUpdated,
            CacheDirectory = _cacheDirectory
        };
    }

    private void EnsureDirectoriesExist()
    {
        try
        {
            if (!Directory.Exists(_cacheDirectory))
                Directory.CreateDirectory(_cacheDirectory);

            if (!Directory.Exists(_lintCacheDirectory))
                Directory.CreateDirectory(_lintCacheDirectory);

            // Add .gitignore to cache directory
            var gitignorePath = Path.Combine(_cacheDirectory, ".gitignore");
            if (!File.Exists(gitignorePath))
            {
                File.WriteAllText(gitignorePath, "*\n!.gitignore\n");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Could not create cache directories: {ex.Message}");
        }
    }

    private void LoadIndex()
    {
        try
        {
            if (File.Exists(_indexPath))
            {
                var json = File.ReadAllText(_indexPath);
                var index = JsonSerializer.Deserialize<GDCacheIndex>(json, JsonOptions);

                if (index != null && index.Version == CurrentCacheVersion)
                {
                    _index = index;
                    Logger.Debug($"Loaded cache index with {_index.Files.Count} entries");
                    return;
                }

                // Version mismatch - invalidate
                Logger.Info("Cache version mismatch, invalidating...");
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Could not load cache index: {ex.Message}");
        }

        _index = new GDCacheIndex { Version = CurrentCacheVersion };
    }

    private void SaveIndexIfDirty()
    {
        if (!_indexDirty)
            return;

        try
        {
            _index.LastUpdated = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(_index, JsonOptions);
            File.WriteAllText(_indexPath, json);
            _indexDirty = false;
        }
        catch (Exception ex)
        {
            Logger.Debug($"Could not save cache index: {ex.Message}");
        }
    }

    private static string GetCacheKey(GDScriptFile script)
    {
        // Normalize path for consistent keys
        return script.FullPath.Replace('\\', '/').ToLowerInvariant();
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        // Use first 16 bytes for shorter filename
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                SaveIndexIfDirty();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Statistics about the cache.
/// </summary>
internal class GDCacheStats
{
    public int TotalEntries { get; init; }
    public DateTime LastUpdated { get; init; }
    public string CacheDirectory { get; init; } = "";
}
