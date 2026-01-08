namespace GDShrapt.Plugin.Api;

/// <summary>
/// Information about a symbol declaration.
/// </summary>
public interface ISymbolInfo
{
    /// <summary>
    /// Name of the symbol.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Kind of symbol.
    /// </summary>
    SymbolKind Kind { get; }

    /// <summary>
    /// Line where declared (0-based).
    /// </summary>
    int Line { get; }

    /// <summary>
    /// Column where declared (0-based).
    /// </summary>
    int Column { get; }

    /// <summary>
    /// Type annotation if present.
    /// </summary>
    string? TypeAnnotation { get; }

    /// <summary>
    /// Documentation comment if present.
    /// </summary>
    string? Documentation { get; }
}

/// <summary>
/// Kind of symbol.
/// </summary>
public enum SymbolKind
{
    /// <summary>Variable declaration.</summary>
    Variable,
    /// <summary>Method/function declaration.</summary>
    Method,
    /// <summary>Signal declaration.</summary>
    Signal,
    /// <summary>Constant declaration.</summary>
    Constant,
    /// <summary>Enum declaration.</summary>
    Enum,
    /// <summary>Enum value.</summary>
    EnumValue,
    /// <summary>Class declaration (class_name).</summary>
    Class,
    /// <summary>Inner class declaration.</summary>
    InnerClass,
    /// <summary>Parameter declaration.</summary>
    Parameter
}
