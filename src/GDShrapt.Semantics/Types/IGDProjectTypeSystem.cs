using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Project-level type system that provides cross-file type resolution.
/// Extends the file-level <see cref="IGDTypeSystem"/> with project-wide capabilities.
/// </summary>
public interface IGDProjectTypeSystem
{
    // ========================================
    // Cross-File Type Queries
    // ========================================

    /// <summary>
    /// Gets the semantic type for an AST node. Auto-finds the containing file.
    /// </summary>
    GDSemanticType GetType(GDNode node);

    /// <summary>
    /// Gets the type info for an AST node. Auto-finds the containing file.
    /// </summary>
    GDTypeInfo? GetTypeInfo(GDNode node);

    /// <summary>
    /// Resolves a type name to its runtime type info.
    /// Searches both Godot types and project-defined types.
    /// </summary>
    GDRuntimeTypeInfo? ResolveType(string typeName);

    // ========================================
    // File-Level Type System Access
    // ========================================

    /// <summary>
    /// Gets the file-level type system for a specific file.
    /// </summary>
    IGDTypeSystem? GetFileTypeSystem(GDScriptFile file);

    /// <summary>
    /// Gets the file-level type system for a file containing the given node.
    /// </summary>
    IGDTypeSystem? GetFileTypeSystem(GDNode node);

    // ========================================
    // Type Compatibility
    // ========================================

    /// <summary>
    /// Checks if source type can be assigned to target type.
    /// Uses project-wide type knowledge.
    /// </summary>
    bool AreTypesCompatible(string sourceType, string targetType);

    /// <summary>
    /// Checks if a type is assignable to another type.
    /// Uses the project's composite runtime provider.
    /// </summary>
    bool IsAssignableTo(string sourceType, string targetType);

    // ========================================
    // Type Resolution
    // ========================================

    /// <summary>
    /// Finds the common base type for multiple types.
    /// </summary>
    string? FindCommonBaseType(params string[] types);

    /// <summary>
    /// Gets the runtime provider used for type resolution.
    /// </summary>
    IGDRuntimeProvider? RuntimeProvider { get; }
}
