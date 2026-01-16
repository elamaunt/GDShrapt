using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Rich symbol information for semantic analysis.
/// Extends basic GDSymbol with cross-file context like declaring type.
/// </summary>
public class GDSymbolInfo
{
    /// <summary>
    /// The underlying symbol from the validator.
    /// </summary>
    public GDSymbol? Symbol { get; }

    /// <summary>
    /// The symbol name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The kind of symbol (variable, method, signal, etc.).
    /// </summary>
    public GDSymbolKind Kind { get; }

    /// <summary>
    /// The AST node where this symbol is declared.
    /// </summary>
    public GDNode? DeclarationNode { get; }

    /// <summary>
    /// The type name of this symbol (e.g., "int", "String", "Player").
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    /// The full type node (for generics like Array[T]).
    /// </summary>
    public GDTypeNode? TypeNode { get; }

    /// <summary>
    /// The name of the type that declares this symbol.
    /// For inherited members, this is the base type name.
    /// For local/parameter symbols, this may be null.
    /// </summary>
    public string? DeclaringTypeName { get; }

    /// <summary>
    /// The script file that contains this symbol's declaration.
    /// </summary>
    public GDScriptFile? DeclaringScript { get; }

    /// <summary>
    /// The script file where this symbol is being accessed/referenced.
    /// Used to determine if access is inherited.
    /// </summary>
    public GDScriptFile? AccessingScript { get; }

    /// <summary>
    /// True if this symbol is accessed via inheritance (declared in a base class).
    /// </summary>
    public bool IsInherited => DeclaringScript != null &&
                               AccessingScript != null &&
                               DeclaringScript != AccessingScript;

    /// <summary>
    /// The confidence level when resolving this symbol.
    /// Strict for statically resolved symbols, Potential for duck-typed access.
    /// </summary>
    public GDReferenceConfidence Confidence { get; }

    /// <summary>
    /// Human-readable reason for the confidence level.
    /// </summary>
    public string? ConfidenceReason { get; }

    /// <summary>
    /// True if this is a static symbol.
    /// </summary>
    public bool IsStatic { get; }

    /// <summary>
    /// Creates a symbol info from a GDSymbol with additional semantic context.
    /// </summary>
    public GDSymbolInfo(
        GDSymbol symbol,
        string? declaringTypeName = null,
        GDScriptFile? declaringScript = null,
        GDScriptFile? accessingScript = null,
        GDReferenceConfidence confidence = GDReferenceConfidence.Strict,
        string? confidenceReason = null)
    {
        Symbol = symbol;
        Name = symbol.Name;
        Kind = symbol.Kind;
        DeclarationNode = symbol.Declaration;
        TypeName = symbol.TypeName;
        TypeNode = symbol.TypeNode;
        IsStatic = symbol.IsStatic;
        DeclaringTypeName = declaringTypeName;
        DeclaringScript = declaringScript;
        AccessingScript = accessingScript;
        Confidence = confidence;
        ConfidenceReason = confidenceReason;
    }

    /// <summary>
    /// Creates a symbol info from runtime member info (for Godot built-in types).
    /// </summary>
    public GDSymbolInfo(
        string name,
        GDSymbolKind kind,
        string? typeName,
        string declaringTypeName,
        GDReferenceConfidence confidence = GDReferenceConfidence.Strict,
        string? confidenceReason = null,
        bool isStatic = false)
    {
        Symbol = null;
        Name = name;
        Kind = kind;
        DeclarationNode = null;
        TypeName = typeName;
        TypeNode = null;
        IsStatic = isStatic;
        DeclaringTypeName = declaringTypeName;
        DeclaringScript = null;  // Built-in, no script
        AccessingScript = null;
        Confidence = confidence;
        ConfidenceReason = confidenceReason;
    }

    /// <summary>
    /// Creates symbol info for a local variable (always Strict confidence).
    /// </summary>
    public static GDSymbolInfo Local(
        GDSymbol symbol,
        GDScriptFile? script = null)
    {
        return new GDSymbolInfo(
            symbol,
            declaringTypeName: null,
            declaringScript: script,
            accessingScript: script,
            confidence: GDReferenceConfidence.Strict,
            confidenceReason: "Local variable in scope");
    }

    /// <summary>
    /// Creates symbol info for a class member.
    /// </summary>
    public static GDSymbolInfo ClassMember(
        GDSymbol symbol,
        string declaringTypeName,
        GDScriptFile declaringScript,
        GDScriptFile? accessingScript = null)
    {
        var isInherited = accessingScript != null && accessingScript != declaringScript;
        return new GDSymbolInfo(
            symbol,
            declaringTypeName: declaringTypeName,
            declaringScript: declaringScript,
            accessingScript: accessingScript ?? declaringScript,
            confidence: GDReferenceConfidence.Strict,
            confidenceReason: isInherited
                ? $"Inherited member from {declaringTypeName}"
                : $"Class member in {declaringTypeName}");
    }

    /// <summary>
    /// Creates symbol info for a member accessed via duck typing.
    /// </summary>
    public static GDSymbolInfo DuckTyped(
        string memberName,
        GDSymbolKind kind,
        string? typeName,
        string? confidenceReason = null)
    {
        return new GDSymbolInfo(
            memberName,
            kind,
            typeName,
            declaringTypeName: "Unknown",
            confidence: GDReferenceConfidence.Potential,
            confidenceReason: confidenceReason ?? "Duck-typed access, caller type unknown");
    }

    /// <summary>
    /// Creates symbol info for a built-in Godot member.
    /// </summary>
    public static GDSymbolInfo BuiltIn(
        GDRuntimeMemberInfo memberInfo,
        string declaringTypeName)
    {
        var kind = memberInfo.Kind switch
        {
            GDRuntimeMemberKind.Method => GDSymbolKind.Method,
            GDRuntimeMemberKind.Property => GDSymbolKind.Property,
            GDRuntimeMemberKind.Signal => GDSymbolKind.Signal,
            GDRuntimeMemberKind.Constant => GDSymbolKind.Constant,
            _ => GDSymbolKind.Variable
        };

        return new GDSymbolInfo(
            memberInfo.Name,
            kind,
            memberInfo.Type,
            declaringTypeName,
            confidence: GDReferenceConfidence.Strict,
            confidenceReason: $"Built-in Godot member from {declaringTypeName}",
            isStatic: memberInfo.IsStatic);
    }

    public override string ToString()
    {
        var inherited = IsInherited ? " (inherited)" : "";
        var declaring = DeclaringTypeName != null ? $" in {DeclaringTypeName}" : "";
        return $"{Kind}: {Name}{declaring}{inherited} [{Confidence}]";
    }
}
