using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Project-wide registry for class-level container profiles.
/// Aggregates container usage information from all script files,
/// including cross-file usages where one class's container is modified
/// from another class.
/// </summary>
internal class GDClassContainerRegistry
{
    private readonly Dictionary<string, GDContainerUsageProfile> _profiles = new();
    private readonly Dictionary<string, HashSet<string>> _fileContainers = new();

    /// <summary>
    /// Registers a container profile for a class variable.
    /// </summary>
    /// <param name="className">The class containing the container.</param>
    /// <param name="containerName">The container variable name.</param>
    /// <param name="profile">The usage profile.</param>
    /// <param name="sourceFilePath">Optional source file path for tracking.</param>
    public void Register(
        string className,
        string containerName,
        GDContainerUsageProfile profile,
        string? sourceFilePath = null)
    {
        var key = BuildKey(className, containerName);
        _profiles[key] = profile;

        if (!string.IsNullOrEmpty(sourceFilePath))
        {
            if (!_fileContainers.TryGetValue(sourceFilePath, out var containers))
            {
                containers = new HashSet<string>();
                _fileContainers[sourceFilePath] = containers;
            }
            containers.Add(key);
        }
    }

    /// <summary>
    /// Gets a container profile by class and variable name.
    /// </summary>
    /// <param name="className">The class containing the container.</param>
    /// <param name="containerName">The container variable name.</param>
    /// <returns>The profile, or null if not found.</returns>
    public GDContainerUsageProfile? GetProfile(string className, string containerName)
    {
        var key = BuildKey(className, containerName);
        return _profiles.TryGetValue(key, out var profile) ? profile : null;
    }

    /// <summary>
    /// Merges cross-file usages into an existing profile.
    /// </summary>
    /// <param name="className">The class containing the container.</param>
    /// <param name="containerName">The container variable name.</param>
    /// <param name="observations">Cross-file usage observations.</param>
    public void MergeCrossFileUsages(
        string className,
        string containerName,
        IReadOnlyList<GDContainerUsageObservation> observations)
    {
        var key = BuildKey(className, containerName);
        if (!_profiles.TryGetValue(key, out var profile))
            return;

        var merged = GDCrossFileContainerUsageCollector.MergeProfiles(profile, observations);
        _profiles[key] = merged;
    }

    /// <summary>
    /// Invalidates all profiles from a specific file (for incremental updates).
    /// </summary>
    /// <param name="filePath">The file path to invalidate.</param>
    public void InvalidateFile(string filePath)
    {
        if (!_fileContainers.TryGetValue(filePath, out var containers))
            return;

        foreach (var key in containers)
        {
            _profiles.Remove(key);
        }

        _fileContainers.Remove(filePath);
    }

    /// <summary>
    /// Gets all container profiles for a class.
    /// </summary>
    /// <param name="className">The class name.</param>
    /// <returns>Dictionary of container name to profile.</returns>
    public IReadOnlyDictionary<string, GDContainerUsageProfile> GetProfilesForClass(string className)
    {
        var result = new Dictionary<string, GDContainerUsageProfile>();
        var prefix = className + ".";

        foreach (var kv in _profiles)
        {
            if (kv.Key.StartsWith(prefix))
            {
                var containerName = kv.Key.Substring(prefix.Length);
                result[containerName] = kv.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all registered profiles.
    /// </summary>
    public IReadOnlyDictionary<string, GDContainerUsageProfile> AllProfiles => _profiles;

    /// <summary>
    /// Clears all registered profiles.
    /// </summary>
    public void Clear()
    {
        _profiles.Clear();
        _fileContainers.Clear();
    }

    private static string BuildKey(string className, string containerName)
    {
        return $"{className}.{containerName}";
    }
}
