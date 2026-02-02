using System.Security.Cryptography;
using System.Text;

namespace GDShrapt.Semantics.Incremental.Cache;

/// <summary>
/// Represents a cache key for incremental analysis.
/// Combines file path with content hash for cache invalidation.
/// </summary>
public readonly struct GDCacheKey : IEquatable<GDCacheKey>
{
    /// <summary>
    /// Normalized file path (relative to project root).
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Content hash (SHA256, first 16 bytes as hex).
    /// </summary>
    public string ContentHash { get; }

    /// <summary>
    /// Combined key for dictionary lookups.
    /// </summary>
    public string Key => $"{FilePath}:{ContentHash}";

    private GDCacheKey(string filePath, string contentHash)
    {
        FilePath = filePath;
        ContentHash = contentHash;
    }

    /// <summary>
    /// Creates a cache key from file path and content.
    /// </summary>
    public static GDCacheKey Create(string projectRoot, string fullPath, string content)
    {
        var relativePath = GetRelativePath(projectRoot, fullPath);
        var hash = ComputeContentHash(content);
        return new GDCacheKey(relativePath, hash);
    }

    /// <summary>
    /// Creates a cache key from file path and pre-computed hash.
    /// </summary>
    public static GDCacheKey CreateWithHash(string relativePath, string hash)
    {
        return new GDCacheKey(relativePath, hash);
    }

    /// <summary>
    /// Computes SHA256 hash of content, returns first 16 hex characters.
    /// </summary>
    public static string ComputeContentHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes[..8]); // First 16 hex chars
    }

    /// <summary>
    /// Gets a normalized relative path.
    /// </summary>
    private static string GetRelativePath(string projectRoot, string fullPath)
    {
        var relativePath = Path.GetRelativePath(projectRoot, fullPath);
        // Normalize to forward slashes for cross-platform consistency
        return relativePath.Replace('\\', '/');
    }

    public bool Equals(GDCacheKey other)
    {
        return FilePath == other.FilePath && ContentHash == other.ContentHash;
    }

    public override bool Equals(object? obj)
    {
        return obj is GDCacheKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FilePath, ContentHash);
    }

    public override string ToString() => Key;

    public static bool operator ==(GDCacheKey left, GDCacheKey right) => left.Equals(right);
    public static bool operator !=(GDCacheKey left, GDCacheKey right) => !left.Equals(right);
}
