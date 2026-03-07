using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Abstractions;

public enum GDAbstractValueKind
{
    Literal,
    ResourcePath,
    Enum,
    Composite,
    Constrained
}

/// <summary>
/// Immutable. A concrete value tracked through the data flow.
/// </summary>
public abstract class GDAbstractValue
{
    public abstract GDAbstractValueKind Kind { get; }
    public abstract string DisplayValue { get; }
    public override string ToString() => DisplayValue;
}

/// <summary>
/// Literal value: int, float, bool, String, StringName, NodePath.
/// </summary>
public sealed class GDLiteralValue : GDAbstractValue
{
    public override GDAbstractValueKind Kind => GDAbstractValueKind.Literal;
    public object RawValue { get; }
    public GDSemanticType LiteralType { get; }

    public GDLiteralValue(object rawValue, GDSemanticType literalType)
    {
        RawValue = rawValue;
        LiteralType = literalType;
    }

    public override string DisplayValue => RawValue is string s ? $"\"{s}\"" : RawValue?.ToString() ?? "null";
}

/// <summary>
/// Resource path: preload("res://..."), load("res://...").
/// </summary>
public sealed class GDResourcePathValue : GDAbstractValue
{
    public override GDAbstractValueKind Kind => GDAbstractValueKind.ResourcePath;
    public string ResourcePath { get; }
    public string? ResolvedType { get; }

    public GDResourcePathValue(string resourcePath, string? resolvedType = null)
    {
        ResourcePath = resourcePath;
        ResolvedType = resolvedType;
    }

    public override string DisplayValue => $"\"{ResourcePath}\"";
}

/// <summary>
/// Enum value: Color.RED, KEY_ESCAPE.
/// </summary>
public sealed class GDEnumValue : GDAbstractValue
{
    public override GDAbstractValueKind Kind => GDAbstractValueKind.Enum;
    public string EnumTypeName { get; }
    public string MemberName { get; }
    public long NumericValue { get; }

    public GDEnumValue(string enumTypeName, string memberName, long numericValue)
    {
        EnumTypeName = enumTypeName;
        MemberName = memberName;
        NumericValue = numericValue;
    }

    public override string DisplayValue => $"{EnumTypeName}.{MemberName}";
}

/// <summary>
/// Composite value: Vector2(10, 20), Color(1,0,0,1).
/// </summary>
public sealed class GDCompositeValue : GDAbstractValue
{
    public override GDAbstractValueKind Kind => GDAbstractValueKind.Composite;
    public string TypeName { get; }
    public IReadOnlyList<GDAbstractValue> Components { get; }

    public GDCompositeValue(string typeName, IReadOnlyList<GDAbstractValue> components)
    {
        TypeName = typeName;
        Components = components;
    }

    public override string DisplayValue => $"{TypeName}({string.Join(", ", Components.Select(c => c.DisplayValue))})";
}

/// <summary>
/// Value is unknown but constrained (e.g., "> 0", "non-empty").
/// </summary>
public sealed class GDConstrainedValue : GDAbstractValue
{
    public override GDAbstractValueKind Kind => GDAbstractValueKind.Constrained;
    public string Constraint { get; }

    public GDConstrainedValue(string constraint)
    {
        Constraint = constraint;
    }

    public override string DisplayValue => $"({Constraint})";
}
