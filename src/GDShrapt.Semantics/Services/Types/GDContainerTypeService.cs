using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for container type inference and profiling.
/// Tracks element types for Array, Dictionary, and packed array variables.
/// </summary>
public class GDContainerTypeService
{
    private readonly Dictionary<string, GDContainerUsageProfile> _containerProfiles = new();
    private readonly Dictionary<string, GDContainerUsageProfile> _classContainerProfiles = new();
    private readonly Dictionary<string, GDContainerElementType> _containerTypeCache = new();
    private readonly IGDRuntimeProvider? _runtimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDContainerTypeService"/> class.
    /// </summary>
    public GDContainerTypeService(IGDRuntimeProvider? runtimeProvider = null)
    {
        _runtimeProvider = runtimeProvider;
    }

    /// <summary>
    /// Gets the container usage profile for a local variable.
    /// </summary>
    public GDContainerUsageProfile? GetContainerProfile(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        return _containerProfiles.TryGetValue(variableName, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets the inferred container element type for a variable.
    /// </summary>
    public GDContainerElementType? GetInferredContainerType(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        // Check cache first
        if (_containerTypeCache.TryGetValue(variableName, out var cached))
            return cached;

        // Compute from profile
        var profile = GetContainerProfile(variableName);
        if (profile == null)
            return null;

        var containerType = profile.ComputeInferredType();
        EnrichUnionTypes(containerType);
        _containerTypeCache[variableName] = containerType;
        return containerType;
    }

    /// <summary>
    /// Gets the container profile for a class-level variable.
    /// </summary>
    public GDContainerUsageProfile? GetClassContainerProfile(string className, string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        var key = string.IsNullOrEmpty(className) ? variableName : $"{className}.{variableName}";
        return _classContainerProfiles.TryGetValue(key, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets all container usage profiles for local variables.
    /// </summary>
    public IEnumerable<GDContainerUsageProfile> GetAllContainerProfiles()
    {
        return _containerProfiles.Values;
    }

    /// <summary>
    /// Gets all class-level container profiles.
    /// </summary>
    public IReadOnlyDictionary<string, GDContainerUsageProfile> ClassContainerProfiles => _classContainerProfiles;

    /// <summary>
    /// Gets a merged container profile combining local and class-level data.
    /// Returns local profile if available, otherwise class profile.
    /// </summary>
    public GDContainerUsageProfile? GetMergedContainerProfile(
        string? className,
        string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        // Prefer local profile (more specific context)
        var localProfile = GetContainerProfile(variableName);
        if (localProfile != null)
            return localProfile;

        // Fallback to class-level profile
        return GetClassContainerProfile(className ?? "", variableName);
    }

    /// <summary>
    /// Sets the container profile for a local variable.
    /// </summary>
    internal void SetContainerProfile(string variableName, GDContainerUsageProfile profile)
    {
        if (!string.IsNullOrEmpty(variableName) && profile != null)
        {
            _containerProfiles[variableName] = profile;
        }
    }

    /// <summary>
    /// Sets the container profile for a class-level variable.
    /// </summary>
    internal void SetClassContainerProfile(string className, string variableName, GDContainerUsageProfile profile)
    {
        if (string.IsNullOrEmpty(variableName) || profile == null)
            return;

        var key = string.IsNullOrEmpty(className) ? variableName : $"{className}.{variableName}";
        _classContainerProfiles[key] = profile;
    }

    /// <summary>
    /// Clears container profiles for a variable (used during reassignment).
    /// </summary>
    internal void ClearContainerProfile(string variableName)
    {
        if (!string.IsNullOrEmpty(variableName))
        {
            _containerProfiles.Remove(variableName);
            _containerTypeCache.Remove(variableName);
        }
    }

    /// <summary>
    /// Enriches union types in container element type with common base type info.
    /// </summary>
    private void EnrichUnionTypes(GDContainerElementType? containerType)
    {
        if (containerType == null || _runtimeProvider == null)
            return;

        var resolver = new GDUnionTypeResolver(_runtimeProvider);

        if (containerType.ElementUnionType != null)
            resolver.EnrichUnionType(containerType.ElementUnionType);

        if (containerType.KeyUnionType != null)
            resolver.EnrichUnionType(containerType.KeyUnionType);
    }
}
