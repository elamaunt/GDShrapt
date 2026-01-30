using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Constraints collected from parameter usage within a method body.
/// Used for duck typing inference - if a parameter is used like a certain type,
/// we can infer it likely IS that type.
/// </summary>
public class GDParameterConstraints
{
    /// <summary>
    /// The parameter name.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Methods called on this parameter (e.g., data.get() adds "get" here).
    /// </summary>
    public HashSet<string> RequiredMethods { get; } = new();

    /// <summary>
    /// Properties accessed on this parameter (e.g., player.health adds "health" here).
    /// </summary>
    public HashSet<string> RequiredProperties { get; } = new();

    /// <summary>
    /// Whether the parameter is used as an iterable (e.g., for x in param).
    /// </summary>
    public bool IsIterable { get; private set; }

    /// <summary>
    /// Whether the parameter is used with indexer (e.g., param[0]).
    /// </summary>
    public bool IsIndexable { get; private set; }

    /// <summary>
    /// Tracks where this parameter is passed to other methods (for cross-method inference).
    /// Each entry contains the call expression and the argument index.
    /// </summary>
    public List<(GDCallExpression Call, int ArgIndex)> PassedToCalls { get; } = new();

    /// <summary>
    /// Possible types from 'is' type checks (e.g., if param is Node adds "Node").
    /// </summary>
    public HashSet<string> PossibleTypes { get; } = new();

    /// <summary>
    /// Types excluded by negative 'is' checks (e.g., if not param is Node adds "Node").
    /// </summary>
    public HashSet<string> ExcludedTypes { get; } = new();

    /// <summary>
    /// Inferred element types for container parameters.
    /// Populated when parameter is used as iterable (for x in param) and x has type checks.
    /// </summary>
    public HashSet<string> ElementTypes { get; } = new();

    /// <summary>
    /// Inferred key types for dictionary parameters.
    /// Populated when parameter is used with indexer or .get(key).
    /// </summary>
    public HashSet<string> KeyTypes { get; } = new();

    #region Per-Type Constraints with Sources

    /// <summary>
    /// Per-type constraints with inference sources.
    /// Key is the type name, value contains constraints specific to that type.
    /// </summary>
    public Dictionary<string, GDTypeSpecificConstraints> TypeConstraints { get; } = new();

    /// <summary>
    /// Gets or creates type-specific constraints for a given type.
    /// </summary>
    public GDTypeSpecificConstraints GetOrCreateTypeConstraints(string typeName)
    {
        if (!TypeConstraints.TryGetValue(typeName, out var constraints))
        {
            constraints = new GDTypeSpecificConstraints(typeName);
            TypeConstraints[typeName] = constraints;
        }
        return constraints;
    }

    /// <summary>
    /// Adds a possible type with its inference source.
    /// </summary>
    public void AddPossibleTypeWithSource(string type, GDTypeInferenceSource source)
    {
        PossibleTypes.Add(type);
        var tc = GetOrCreateTypeConstraints(type);
        tc.InferenceSources.Add(source);
    }

    /// <summary>
    /// Adds element type with the container type it applies to.
    /// </summary>
    public void AddElementTypeForType(string containerType, string elementType, GDTypeInferenceSource? source = null)
    {
        ElementTypes.Add(elementType);
        var tc = GetOrCreateTypeConstraints(containerType);
        tc.ElementTypes.Add(elementType);
        if (source != null)
            tc.ElementTypeSources.Add(source);
    }

    /// <summary>
    /// Adds key type with the container type it applies to.
    /// </summary>
    public void AddKeyTypeForType(string containerType, string keyType, GDTypeInferenceSource? source = null)
    {
        KeyTypes.Add(keyType);
        var tc = GetOrCreateTypeConstraints(containerType);
        tc.KeyTypes.Add(keyType);
        if (source != null)
            tc.KeyTypeSources.Add(source);
    }

    /// <summary>
    /// Marks a type's value slot as derivable (can be inferred further).
    /// </summary>
    public void MarkValueDerivable(string containerType, GDNode? sourceNode, string? reason = null)
    {
        var tc = GetOrCreateTypeConstraints(containerType);
        tc.ValueIsDerivable = true;
        tc.ValueDerivableNode = sourceNode;
        tc.ValueDerivableReason = reason;
    }

    /// <summary>
    /// Marks a type's key slot as derivable (can be inferred further).
    /// </summary>
    public void MarkKeyDerivable(string containerType, GDNode? sourceNode, string? reason = null)
    {
        var tc = GetOrCreateTypeConstraints(containerType);
        tc.KeyIsDerivable = true;
        tc.KeyDerivableNode = sourceNode;
        tc.KeyDerivableReason = reason;
    }

    #endregion

    /// <summary>
    /// Creates a new constraints container for a parameter.
    /// </summary>
    public GDParameterConstraints(string name)
    {
        ParameterName = name;
    }

    /// <summary>
    /// Adds a method that must exist on this parameter's type.
    /// </summary>
    public void AddRequiredMethod(string name) => RequiredMethods.Add(name);

    /// <summary>
    /// Adds a property that must exist on this parameter's type.
    /// </summary>
    public void AddRequiredProperty(string name) => RequiredProperties.Add(name);

    /// <summary>
    /// Marks this parameter as being used as an iterable.
    /// </summary>
    public void AddIterableConstraint() => IsIterable = true;

    /// <summary>
    /// Marks this parameter as being used with indexer access.
    /// </summary>
    public void AddIndexableConstraint() => IsIndexable = true;

    /// <summary>
    /// Records that this parameter is passed to another method call.
    /// </summary>
    public void AddPassedToCall(GDCallExpression call, int argIndex)
        => PassedToCalls.Add((call, argIndex));

    /// <summary>
    /// Adds a type that this parameter could be (from 'is' check).
    /// </summary>
    public void AddPossibleType(string type) => PossibleTypes.Add(type);

    /// <summary>
    /// Adds a type that this parameter cannot be (from negative 'is' check).
    /// </summary>
    public void ExcludeType(string type) => ExcludedTypes.Add(type);

    /// <summary>
    /// Adds an element type for container parameters (from iterator type checks).
    /// </summary>
    public void AddElementType(string type)
    {
        if (!string.IsNullOrEmpty(type))
            ElementTypes.Add(type);
    }

    /// <summary>
    /// Adds a key type for dictionary parameters (from .get(key) or [key] usage).
    /// </summary>
    public void AddKeyType(string type)
    {
        if (!string.IsNullOrEmpty(type))
            KeyTypes.Add(type);
    }

    /// <summary>
    /// Returns true if any constraints have been collected.
    /// </summary>
    public bool HasConstraints =>
        RequiredMethods.Count > 0 ||
        RequiredProperties.Count > 0 ||
        IsIterable ||
        IsIndexable ||
        PassedToCalls.Count > 0 ||
        PossibleTypes.Count > 0 ||
        ElementTypes.Count > 0 ||
        KeyTypes.Count > 0;

    /// <summary>
    /// Returns a string representation for debugging.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();

        if (RequiredMethods.Count > 0)
            parts.Add($"methods: [{string.Join(", ", RequiredMethods)}]");
        if (RequiredProperties.Count > 0)
            parts.Add($"props: [{string.Join(", ", RequiredProperties)}]");
        if (IsIterable)
            parts.Add("iterable");
        if (IsIndexable)
            parts.Add("indexable");
        if (PossibleTypes.Count > 0)
            parts.Add($"maybe: [{string.Join("|", PossibleTypes)}]");
        if (ExcludedTypes.Count > 0)
            parts.Add($"not: [{string.Join("|", ExcludedTypes)}]");
        if (ElementTypes.Count > 0)
            parts.Add($"elements: [{string.Join("|", ElementTypes)}]");
        if (KeyTypes.Count > 0)
            parts.Add($"keys: [{string.Join("|", KeyTypes)}]");
        if (PassedToCalls.Count > 0)
            parts.Add($"passed:{PassedToCalls.Count}x");

        return parts.Count > 0
            ? $"{ParameterName}({string.Join(", ", parts)})"
            : $"{ParameterName}(no constraints)";
    }
}

/// <summary>
/// Constraints specific to a particular type in a union.
/// E.g., for "Dictionary | Array", Dictionary has KeyTypes while Array does not.
/// </summary>
public class GDTypeSpecificConstraints
{
    /// <summary>
    /// The type name this constraints apply to.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Sources that led to inferring this type.
    /// </summary>
    public List<GDTypeInferenceSource> InferenceSources { get; } = new();

    /// <summary>
    /// Element/value types specific to this container type.
    /// </summary>
    public HashSet<string> ElementTypes { get; } = new();

    /// <summary>
    /// Key types specific to this container type (for Dictionary).
    /// </summary>
    public HashSet<string> KeyTypes { get; } = new();

    /// <summary>
    /// Sources for element type inference.
    /// </summary>
    public List<GDTypeInferenceSource> ElementTypeSources { get; } = new();

    /// <summary>
    /// Sources for key type inference.
    /// </summary>
    public List<GDTypeInferenceSource> KeyTypeSources { get; } = new();

    /// <summary>
    /// Whether the value type can be inferred further.
    /// </summary>
    public bool ValueIsDerivable { get; set; }

    /// <summary>
    /// Node to navigate to for deriving value type.
    /// </summary>
    public GDNode? ValueDerivableNode { get; set; }

    /// <summary>
    /// Reason why value type is derivable.
    /// </summary>
    public string? ValueDerivableReason { get; set; }

    /// <summary>
    /// Whether the key type can be inferred further.
    /// </summary>
    public bool KeyIsDerivable { get; set; }

    /// <summary>
    /// Node to navigate to for deriving key type.
    /// </summary>
    public GDNode? KeyDerivableNode { get; set; }

    /// <summary>
    /// Reason why key type is derivable.
    /// </summary>
    public string? KeyDerivableReason { get; set; }

    /// <summary>
    /// Creates type-specific constraints.
    /// </summary>
    public GDTypeSpecificConstraints(string typeName)
    {
        TypeName = typeName;
    }

    /// <summary>
    /// Formats the element type string.
    /// </summary>
    public string GetElementTypeString()
    {
        if (ElementTypes.Count == 0)
            return ValueIsDerivable ? "<Derivable>" : "Variant";

        string typeStr;
        if (ElementTypes.Count == 1)
        {
            // Safe: use FirstOrDefault to avoid race condition
            typeStr = ElementTypes.FirstOrDefault() ?? "Variant";
        }
        else
        {
            typeStr = string.Join(" | ", ElementTypes.OrderBy(t => t));
        }

        return ValueIsDerivable ? $"{typeStr}?" : typeStr;
    }

    /// <summary>
    /// Formats the key type string.
    /// </summary>
    public string GetKeyTypeString()
    {
        if (KeyTypes.Count == 0)
            return KeyIsDerivable ? "<Derivable>" : "Variant";

        string typeStr;
        if (KeyTypes.Count == 1)
        {
            // Safe: use FirstOrDefault to avoid race condition
            typeStr = KeyTypes.FirstOrDefault() ?? "Variant";
        }
        else
        {
            typeStr = string.Join(" | ", KeyTypes.OrderBy(t => t));
        }

        return KeyIsDerivable ? $"{typeStr}?" : typeStr;
    }

    /// <summary>
    /// Builds the full formatted type with generics.
    /// </summary>
    public string FormatFullType()
    {
        if (TypeName == "Array")
        {
            var elem = GetElementTypeString();
            if (elem != "Variant" || ValueIsDerivable)
                return $"Array[{elem}]";
            return "Array";
        }

        if (TypeName == "Dictionary")
        {
            var key = GetKeyTypeString();
            var val = GetElementTypeString();
            if (key != "Variant" || val != "Variant" || KeyIsDerivable || ValueIsDerivable)
                return $"Dictionary[{key}, {val}]";
            return "Dictionary";
        }

        return TypeName;
    }
}
