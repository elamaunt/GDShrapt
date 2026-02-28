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
    private readonly Dictionary<GDNode, GDTypeNarrowingContext> _narrowingContexts = new();

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
    /// Gets the narrowed type for a variable at a specific location.
    /// Walks up the AST to find the nearest branch with narrowing info.
    /// </summary>
    public string? GetNarrowedType(string variableName, GDNode atLocation)
    {
        if (string.IsNullOrEmpty(variableName) || atLocation == null)
            return null;

        // Walk up the AST checking all narrowing contexts, not just the first one.
        // A nested if may have its own narrowing context that doesn't include
        // the outer if's type guard (e.g., `if event is Foo: if event.x == 1:`)
        var current = (GDNode?)atLocation;
        while (current != null)
        {
            if (_narrowingContexts.TryGetValue(current, out var context))
            {
                var concreteType = context.GetConcreteType(variableName);
                if (concreteType != null)
                    return concreteType.DisplayName;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Finds the narrowing context that applies to a given node location.
    /// </summary>
    public GDTypeNarrowingContext? FindNarrowingContextForNode(GDNode node)
    {
        var current = node;
        while (current != null)
        {
            if (_narrowingContexts.TryGetValue(current, out var context))
                return context;

            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Gets all duck types.
    /// </summary>
    public IEnumerable<KeyValuePair<string, GDDuckType>> GetAllDuckTypes()
    {
        return _duckTypes;
    }

    /// <summary>
    /// Gets all narrowing contexts.
    /// </summary>
    public IReadOnlyDictionary<GDNode, GDTypeNarrowingContext> NarrowingContexts => _narrowingContexts;

    /// <summary>
    /// Sets duck type information for a variable.
    /// </summary>
    internal void SetDuckType(string variableName, GDDuckType duckType)
    {
        if (!string.IsNullOrEmpty(variableName) && duckType != null)
            _duckTypes[variableName] = duckType;
    }

    /// <summary>
    /// Sets narrowing context for a node.
    /// </summary>
    internal void SetNarrowingContext(GDNode node, GDTypeNarrowingContext context)
    {
        if (node != null && context != null)
            _narrowingContexts[node] = context;
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
                if (reference.ReferenceNode?.Parent is GDMemberOperatorExpression memberOp)
                {
                    var narrowedType = GetNarrowedType(symbolName, memberOp);
                    if (string.IsNullOrEmpty(narrowedType))
                    {
                        // This usage is not in a type guard - duck type is needed
                        return false;
                    }
                }
            }
        }

        // All usages are narrowed, suppress duck constraints
        return true;
    }

    /// <summary>
    /// Checks if a type name represents a concrete (non-Variant, non-Unknown) type.
    /// </summary>
    private static bool IsConcreteType(string? typeName)
    {
        return !string.IsNullOrEmpty(typeName)
            && typeName != "Variant"
            && !typeName.StartsWith("Unknown");
    }
}
