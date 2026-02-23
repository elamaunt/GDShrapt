using System.Collections.Generic;
using System.Linq;
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
    /// The identifier token within the declaration node.
    /// </summary>
    public GDSyntaxToken? DeclarationIdentifier { get; }

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
    /// For local symbols (variables, parameters, iterators), the method/lambda node
    /// where this symbol is declared. Null for class-level symbols.
    /// Used to isolate local symbols by their enclosing scope.
    /// </summary>
    public GDNode? DeclaringScopeNode { get; }

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
    /// For methods: the declared return type name (null if no annotation).
    /// </summary>
    public string? ReturnTypeName { get; }

    /// <summary>
    /// For methods: parameter information.
    /// </summary>
    public IReadOnlyList<GDParameterSymbolInfo>? Parameters { get; }

    /// <summary>
    /// For methods: count of parameters (0 if not a method).
    /// </summary>
    public int ParameterCount => Parameters?.Count ?? 0;

    /// <summary>
    /// The best available position token for this symbol.
    /// Prefers the declaration identifier; falls back to the first token of the declaration node.
    /// </summary>
    public GDSyntaxToken? PositionToken => DeclarationIdentifier
        ?? DeclarationNode?.AllTokens.FirstOrDefault();

    /// <summary>
    /// Creates a symbol info from a GDSymbol with additional semantic context.
    /// </summary>
    public GDSymbolInfo(
        GDSymbol symbol,
        string? declaringTypeName = null,
        GDScriptFile? declaringScript = null,
        GDScriptFile? accessingScript = null,
        GDReferenceConfidence confidence = GDReferenceConfidence.Strict,
        string? confidenceReason = null,
        GDNode? declaringScopeNode = null)
    {
        Symbol = symbol;
        Name = symbol.Name;
        Kind = symbol.Kind;
        DeclarationNode = symbol.Declaration;
        DeclarationIdentifier = ResolveDeclarationIdentifier(symbol.Declaration, symbol.Name);
        TypeName = symbol.TypeName;
        TypeNode = symbol.TypeNode;
        IsStatic = symbol.IsStatic;
        ReturnTypeName = symbol.ReturnTypeName;
        Parameters = symbol.Parameters;
        DeclaringTypeName = declaringTypeName;
        DeclaringScript = declaringScript;
        AccessingScript = accessingScript;
        Confidence = confidence;
        ConfidenceReason = confidenceReason;
        DeclaringScopeNode = declaringScopeNode;
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
    /// <param name="symbol">The symbol to wrap</param>
    /// <param name="script">The script file containing the symbol</param>
    /// <param name="declaringScopeNode">The method/lambda node where this local is declared (for scope isolation)</param>
    public static GDSymbolInfo Local(
        GDSymbol symbol,
        GDScriptFile? script = null,
        GDNode? declaringScopeNode = null)
    {
        return new GDSymbolInfo(
            symbol,
            declaringTypeName: null,
            declaringScript: script,
            accessingScript: script,
            confidence: GDReferenceConfidence.Strict,
            confidenceReason: "Local variable in scope",
            declaringScopeNode: declaringScopeNode);
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

    private static GDSyntaxToken? ResolveDeclarationIdentifier(GDNode? node, string name)
    {
        if (node == null)
            return null;

        if (node is GDIdentifiableClassMember identifiable && identifiable.Identifier?.Sequence == name)
            return identifiable.Identifier;

        if (node is GDVariableDeclaration varDecl && varDecl.Identifier?.Sequence == name)
            return varDecl.Identifier;

        if (node is GDParameterDeclaration param && param.Identifier?.Sequence == name)
            return param.Identifier;

        if (node is GDMatchCaseVariableExpression matchVar && matchVar.Identifier?.Sequence == name)
            return matchVar.Identifier;

        return null;
    }
}
