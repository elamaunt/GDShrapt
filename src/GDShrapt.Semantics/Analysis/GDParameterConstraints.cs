using GDShrapt.Abstractions;
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
    public HashSet<GDSemanticType> PossibleTypes { get; } = new();

    /// <summary>
    /// Types excluded by negative 'is' checks (e.g., if not param is Node adds "Node").
    /// </summary>
    public HashSet<GDSemanticType> ExcludedTypes { get; } = new();

    /// <summary>
    /// Inferred element types for container parameters.
    /// Populated when parameter is used as iterable (for x in param) and x has type checks.
    /// </summary>
    public HashSet<GDSemanticType> ElementTypes { get; } = new();

    /// <summary>
    /// Inferred key types for dictionary parameters.
    /// Populated when parameter is used with indexer or .get(key).
    /// </summary>
    public HashSet<GDSemanticType> KeyTypes { get; } = new();

    #region Per-Type Constraints with Sources

    /// <summary>
    /// Per-type constraints with inference sources.
    /// Key is the semantic type, value contains constraints specific to that type.
    /// </summary>
    internal Dictionary<GDSemanticType, GDTypeSpecificConstraints> TypeConstraints { get; } = new();

    /// <summary>
    /// Gets or creates type-specific constraints for a given type.
    /// </summary>
    internal GDTypeSpecificConstraints GetOrCreateTypeConstraints(GDSemanticType type)
    {
        if (!TypeConstraints.TryGetValue(type, out var constraints))
        {
            constraints = new GDTypeSpecificConstraints(type);
            TypeConstraints[type] = constraints;
        }
        return constraints;
    }

    /// <summary>
    /// Adds a possible type with its inference source.
    /// </summary>
    internal void AddPossibleTypeWithSource(GDSemanticType type, GDTypeInferenceSource source)
    {
        PossibleTypes.Add(type);
        var tc = GetOrCreateTypeConstraints(type);
        tc.InferenceSources.Add(source);
    }

    /// <summary>
    /// Adds element type with the container type it applies to.
    /// </summary>
    internal void AddElementTypeForType(GDSemanticType containerType, GDSemanticType elementType, GDTypeInferenceSource? source = null)
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
    internal void AddKeyTypeForType(GDSemanticType containerType, GDSemanticType keyType, GDTypeInferenceSource? source = null)
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
    public void MarkValueDerivable(GDSemanticType containerType, GDNode? sourceNode, string? reason = null)
    {
        var tc = GetOrCreateTypeConstraints(containerType);
        tc.ValueIsDerivable = true;
        tc.ValueDerivableNode = sourceNode;
        tc.ValueDerivableReason = reason;
    }

    /// <summary>
    /// Marks a type's key slot as derivable (can be inferred further).
    /// </summary>
    public void MarkKeyDerivable(GDSemanticType containerType, GDNode? sourceNode, string? reason = null)
    {
        var tc = GetOrCreateTypeConstraints(containerType);
        tc.KeyIsDerivable = true;
        tc.KeyDerivableNode = sourceNode;
        tc.KeyDerivableReason = reason;
    }

    #endregion

    #region Method Call Argument Types

    /// <summary>
    /// Argument info for methods called on this parameter.
    /// Key = method name, Value = list of argument info arrays per call.
    /// E.g., param.has("key") -> { "has": [[FromType("String")]] }
    /// E.g., param.set(other_param, val) -> { "set": [[FromParameter("other_param"), Unknown()]] }
    /// </summary>
    internal Dictionary<string, List<GDCallArgInfo[]>> MethodCallArgTypes { get; } = new();

    /// <summary>
    /// Records argument info for a method call on this parameter.
    /// </summary>
    internal void AddMethodCallArgTypes(string methodName, GDCallArgInfo[] argInfos)
    {
        if (!MethodCallArgTypes.TryGetValue(methodName, out var calls))
        {
            calls = new List<GDCallArgInfo[]>();
            MethodCallArgTypes[methodName] = calls;
        }
        calls.Add(argInfos);
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
    public void AddPossibleType(GDSemanticType type) => PossibleTypes.Add(type);

    /// <summary>
    /// Adds a type that this parameter cannot be (from negative 'is' check).
    /// </summary>
    public void ExcludeType(GDSemanticType type) => ExcludedTypes.Add(type);

    /// <summary>
    /// Adds an element type for container parameters (from iterator type checks).
    /// </summary>
    public void AddElementType(GDSemanticType? type)
    {
        if (type != null)
            ElementTypes.Add(type);
    }

    /// <summary>
    /// Adds a key type for dictionary parameters (from .get(key) or [key] usage).
    /// </summary>
    public void AddKeyType(GDSemanticType? type)
    {
        if (type != null)
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
            parts.Add($"maybe: [{string.Join("|", PossibleTypes.Select(t => t.DisplayName))}]");
        if (ExcludedTypes.Count > 0)
            parts.Add($"not: [{string.Join("|", ExcludedTypes.Select(t => t.DisplayName))}]");
        if (ElementTypes.Count > 0)
            parts.Add($"elements: [{string.Join("|", ElementTypes.Select(t => t.DisplayName))}]");
        if (KeyTypes.Count > 0)
            parts.Add($"keys: [{string.Join("|", KeyTypes.Select(t => t.DisplayName))}]");
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
internal class GDTypeSpecificConstraints
{
    /// <summary>
    /// The semantic type this constraints apply to.
    /// </summary>
    public GDSemanticType Type { get; }

    /// <summary>
    /// Sources that led to inferring this type.
    /// </summary>
    public List<GDTypeInferenceSource> InferenceSources { get; } = new();

    /// <summary>
    /// Element/value types specific to this container type.
    /// </summary>
    public HashSet<GDSemanticType> ElementTypes { get; } = new();

    /// <summary>
    /// Key types specific to this container type (for Dictionary).
    /// </summary>
    public HashSet<GDSemanticType> KeyTypes { get; } = new();

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
    public GDTypeSpecificConstraints(GDSemanticType type)
    {
        Type = type;
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
            typeStr = ElementTypes.FirstOrDefault()?.DisplayName ?? "Variant";
        }
        else
        {
            typeStr = string.Join(" | ", ElementTypes.Select(t => t.DisplayName).OrderBy(t => t));
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
            typeStr = KeyTypes.FirstOrDefault()?.DisplayName ?? "Variant";
        }
        else
        {
            typeStr = string.Join(" | ", KeyTypes.Select(t => t.DisplayName).OrderBy(t => t));
        }

        return KeyIsDerivable ? $"{typeStr}?" : typeStr;
    }

    /// <summary>
    /// Builds the full formatted type with generics.
    /// </summary>
    public string FormatFullType()
    {
        if (Type.DisplayName == GDWellKnownTypes.Containers.Array)
        {
            var elem = GetElementTypeString();
            if (elem != GDWellKnownTypes.Variant || ValueIsDerivable)
                return GDGenericTypeHelper.CreateArrayType(elem);
            return GDWellKnownTypes.Containers.Array;
        }

        if (Type.DisplayName == GDWellKnownTypes.Containers.Dictionary)
        {
            var key = GetKeyTypeString();
            var val = GetElementTypeString();
            if (key != GDWellKnownTypes.Variant || val != GDWellKnownTypes.Variant || KeyIsDerivable || ValueIsDerivable)
                return GDGenericTypeHelper.CreateDictionaryType(key, val);
            return GDWellKnownTypes.Containers.Dictionary;
        }

        return Type.DisplayName;
    }
}

/// <summary>
/// Info about a single argument in a method call on a parameter.
/// Either a resolved type, a reference to another parameter, or unknown (Variant).
/// </summary>
internal class GDCallArgInfo
{
    /// <summary>Resolved semantic type. Null if unresolved.</summary>
    public GDSemanticType? ResolvedType { get; }

    /// <summary>Name of the parameter this arg references. Null if not a parameter ref.</summary>
    public string? ParameterRef { get; }

    /// <summary>True when the argument type could not be determined.</summary>
    public bool IsUnknown => ResolvedType == null && ParameterRef == null;

    /// <summary>True when the argument is a reference to another method parameter.</summary>
    public bool IsParameterRef => ParameterRef != null;

    private GDCallArgInfo(GDSemanticType? resolvedType, string? parameterRef)
    {
        ResolvedType = resolvedType;
        ParameterRef = parameterRef;
    }

    public static GDCallArgInfo FromType(GDSemanticType type) => new(type, null);
    public static GDCallArgInfo FromParameter(string paramName) => new(null, paramName);
    public static GDCallArgInfo Unknown() => new(null, null);

    /// <summary>
    /// Returns the effective type for compatibility checks.
    /// Returns Variant for unknown/parameter-ref.
    /// </summary>
    public GDSemanticType EffectiveType => ResolvedType ?? GDVariantSemanticType.Instance;
}
