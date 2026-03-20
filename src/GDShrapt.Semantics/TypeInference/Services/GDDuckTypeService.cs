using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for duck type constraint management.
/// Tracks required methods and properties for Variant/untyped parameters.
/// </summary>
internal class GDDuckTypeService
{
    private readonly Dictionary<string, GDDuckType> _duckTypes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GDDuckTypeService"/> class.
    /// </summary>
    public GDDuckTypeService()
    {
    }

    /// <summary>
    /// Gets the duck type constraints for a variable.
    /// </summary>
    public GDDuckType? GetDuckType(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return null;

        return _duckTypes.TryGetValue(variableName, out var duckType) ? duckType : null;
    }

    /// <summary>
    /// Gets all duck types.
    /// </summary>
    public IEnumerable<KeyValuePair<string, GDDuckType>> GetAllDuckTypes()
    {
        return _duckTypes;
    }

    /// <summary>
    /// Sets duck type information for a variable.
    /// </summary>
    internal void SetDuckType(string variableName, GDDuckType duckType)
    {
        if (!string.IsNullOrEmpty(variableName) && duckType != null)
            _duckTypes[variableName] = duckType;
    }

    /// <summary>
    /// Checks if duck type constraints should be suppressed for a symbol.
    /// </summary>
    /// <param name="symbolName">The symbol name.</param>
    /// <param name="symbolTypeName">The symbol's declared type (if any).</param>
    /// <param name="unionType">The union type for the symbol (if any).</param>
    /// <param name="references">The references to the symbol.</param>
    /// <returns>True if duck constraints should be suppressed.</returns>
    public bool ShouldSuppressDuckConstraints(
        string symbolName,
        string? symbolTypeName,
        GDUnionType? unionType,
        IEnumerable<GDReference>? references)
    {
        if (string.IsNullOrEmpty(symbolName))
            return true;

        // If symbol has known concrete type, suppress duck constraints
        if (unionType?.IsSingleType == true)
        {
            var type = unionType.EffectiveType.DisplayName;
            if (IsConcreteType(type))
                return true;
        }

        if (symbolTypeName != null && IsConcreteType(symbolTypeName))
            return true;

        // If no duck type requirements, nothing to suppress
        var duckType = GetDuckType(symbolName);
        if (duckType == null || !duckType.HasRequirements)
            return true;

        if (references != null)
        {
            foreach (var reference in references)
            {
                if (reference.ReferenceNode?.Parent is GDMemberOperatorExpression)
                {
                    // Member access on untyped variable — duck type is needed
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsConcreteType(string? typeName) => GDWellKnownTypes.IsConcreteType(typeName);
}
