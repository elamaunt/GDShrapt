using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Information about a method/signal parameter extracted during symbol registration.
/// Provides parameter data without exposing AST nodes.
/// </summary>
public class GDParameterSymbolInfo
{
    public string Name { get; }
    public string? TypeName { get; }
    public bool HasDefaultValue { get; }
    public int Position { get; }

    public GDParameterSymbolInfo(string name, string? typeName, bool hasDefaultValue, int position)
    {
        Name = name;
        TypeName = typeName;
        HasDefaultValue = hasDefaultValue;
        Position = position;
    }

    public override string ToString() => HasDefaultValue ? $"{Name}: {TypeName} = ..." : $"{Name}: {TypeName}";
}
