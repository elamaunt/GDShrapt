using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// A declared symbol in GDScript (variable, method, signal, etc.).
/// Single source of truth for symbol information.
/// </summary>
public class GDSymbol
{
    // === Base properties ===

    /// <summary>
    /// The name of this symbol.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The kind of symbol (variable, method, signal, etc.).
    /// </summary>
    public GDSymbolKind Kind { get; }

    /// <summary>
    /// The AST node where this symbol is declared.
    /// </summary>
    public GDNode? Declaration { get; }

    /// <summary>
    /// The resolved type of this symbol.
    /// </summary>
    public GDType? Type { get; }

    /// <summary>
    /// The type name as string (e.g., "int", "String", "Player").
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    /// The full type node from AST (includes generic type arguments for Array[T], Dictionary[K,V]).
    /// </summary>
    public GDTypeNode? TypeNode { get; }

    /// <summary>
    /// True if this symbol is static.
    /// </summary>
    public bool IsStatic { get; }

    /// <summary>
    /// True if this symbol is a constant.
    /// </summary>
    public bool IsConst => Kind == GDSymbolKind.Constant;

    /// <summary>
    /// For methods: the declared return type name (null if no annotation).
    /// </summary>
    public string? ReturnTypeName { get; set; }

    /// <summary>
    /// For methods: parameter information extracted during symbol registration.
    /// </summary>
    public IReadOnlyList<GDParameterSymbolInfo>? Parameters { get; set; }

    // === Semantic properties ===

    /// <summary>
    /// The name of the type that declares this symbol.
    /// For inherited members, this is the base type name.
    /// For local/parameter symbols, this may be null.
    /// </summary>
    public string? DeclaringTypeName { get; set; }

    /// <summary>
    /// The file path of the script that contains this symbol's declaration.
    /// </summary>
    public string? DeclaringScriptPath { get; set; }

    /// <summary>
    /// True if this symbol is accessed via inheritance (declared in a base class).
    /// </summary>
    public bool IsInherited { get; set; }

    /// <summary>
    /// The confidence level when resolving this symbol.
    /// Strict for statically resolved symbols, Potential for duck-typed access.
    /// </summary>
    public GDReferenceConfidence Confidence { get; set; } = GDReferenceConfidence.Strict;

    /// <summary>
    /// Human-readable reason for the confidence level.
    /// </summary>
    public string? ConfidenceReason { get; set; }

    // === Constructor ===

    public GDSymbol(
        string name,
        GDSymbolKind kind,
        GDNode? declaration = null,
        GDType? type = null,
        string? typeName = null,
        bool isStatic = false,
        GDTypeNode? typeNode = null)
    {
        Name = name;
        Kind = kind;
        Declaration = declaration;
        Type = type;
        TypeName = typeName;
        IsStatic = isStatic;
        TypeNode = typeNode;
    }

    // === Factory methods for basic symbols ===

    public static GDSymbol Variable(string name, GDNode declaration, GDType? type = null, string? typeName = null, bool isStatic = false, GDTypeNode? typeNode = null)
        => new GDSymbol(name, GDSymbolKind.Variable, declaration, type, typeName, isStatic, typeNode);

    public static GDSymbol Constant(string name, GDNode declaration, GDType? type = null, string? typeName = null, GDTypeNode? typeNode = null)
        => new GDSymbol(name, GDSymbolKind.Constant, declaration, type, typeName, typeNode: typeNode);

    public static GDSymbol Parameter(string name, GDNode declaration, GDType? type = null, string? typeName = null, GDTypeNode? typeNode = null)
        => new GDSymbol(name, GDSymbolKind.Parameter, declaration, type, typeName, typeNode: typeNode);

    public static GDSymbol Method(string name, GDNode declaration, bool isStatic = false)
        => new GDSymbol(name, GDSymbolKind.Method, declaration, isStatic: isStatic);

    public static GDSymbol Signal(string name, GDNode declaration)
        => new GDSymbol(name, GDSymbolKind.Signal, declaration);

    /// <summary>
    /// Creates a property symbol (a variable with get/set accessors).
    /// </summary>
    public static GDSymbol Property(string name, GDNode declaration, GDType? type = null, string? typeName = null, bool isStatic = false, GDTypeNode? typeNode = null)
        => new GDSymbol(name, GDSymbolKind.Property, declaration, type, typeName, isStatic, typeNode);

    public static GDSymbol Class(string name, GDNode declaration)
        => new GDSymbol(name, GDSymbolKind.Class, declaration);

    public static GDSymbol Enum(string name, GDNode declaration)
        => new GDSymbol(name, GDSymbolKind.Enum, declaration);

    public static GDSymbol EnumValue(string name, GDNode declaration)
        => new GDSymbol(name, GDSymbolKind.EnumValue, declaration);

    /// <summary>
    /// For-loop iterator variable.
    /// </summary>
    public static GDSymbol Iterator(string name, GDNode declaration, GDType? type = null, string? typeName = null, GDTypeNode? typeNode = null)
        => new GDSymbol(name, GDSymbolKind.Iterator, declaration, type, typeName, typeNode: typeNode);

    /// <summary>
    /// Match case binding variable (var x in match patterns).
    /// </summary>
    public static GDSymbol MatchCaseBinding(string name, GDNode declaration, GDType? type = null, string? typeName = null)
        => new GDSymbol(name, GDSymbolKind.MatchCaseBinding, declaration, type, typeName);

    // === Factory methods for semantic context ===

    /// <summary>
    /// Creates a local symbol (always Strict confidence).
    /// </summary>
    public static GDSymbol Local(
        string name,
        GDSymbolKind kind,
        GDNode? declaration,
        string? scriptPath = null)
    {
        return new GDSymbol(name, kind, declaration)
        {
            DeclaringScriptPath = scriptPath,
            Confidence = GDReferenceConfidence.Strict,
            ConfidenceReason = "Local symbol in scope"
        };
    }

    /// <summary>
    /// Creates a class member symbol.
    /// </summary>
    public static GDSymbol ClassMember(
        string name,
        GDSymbolKind kind,
        GDNode? declaration,
        string declaringTypeName,
        string? declaringScriptPath,
        bool isInherited = false)
    {
        return new GDSymbol(name, kind, declaration)
        {
            DeclaringTypeName = declaringTypeName,
            DeclaringScriptPath = declaringScriptPath,
            IsInherited = isInherited,
            Confidence = GDReferenceConfidence.Strict,
            ConfidenceReason = isInherited
                ? $"Inherited member from {declaringTypeName}"
                : $"Class member in {declaringTypeName}"
        };
    }

    /// <summary>
    /// Creates a duck-typed symbol (Potential confidence).
    /// </summary>
    public static GDSymbol DuckTyped(
        string memberName,
        GDSymbolKind kind,
        string? typeName = null,
        string? confidenceReason = null)
    {
        return new GDSymbol(memberName, kind, null, typeName: typeName)
        {
            DeclaringTypeName = "Unknown",
            Confidence = GDReferenceConfidence.Potential,
            ConfidenceReason = confidenceReason ?? "Duck-typed access, caller type unknown"
        };
    }

    /// <summary>
    /// Creates a symbol for built-in Godot member.
    /// </summary>
    public static GDSymbol BuiltIn(
        string name,
        GDSymbolKind kind,
        string? typeName,
        string declaringTypeName,
        bool isStatic = false)
    {
        return new GDSymbol(name, kind, null, typeName: typeName, isStatic: isStatic)
        {
            DeclaringTypeName = declaringTypeName,
            Confidence = GDReferenceConfidence.Strict,
            ConfidenceReason = $"Built-in Godot member from {declaringTypeName}"
        };
    }

    /// <summary>
    /// Creates a symbol from runtime member info (for Godot built-in types).
    /// </summary>
    public static GDSymbol FromRuntimeMember(
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

        return BuiltIn(memberInfo.Name, kind, memberInfo.Type, declaringTypeName, memberInfo.IsStatic);
    }

    public override string ToString()
    {
        var inherited = IsInherited ? " (inherited)" : "";
        var declaring = DeclaringTypeName != null ? $" in {DeclaringTypeName}" : "";
        return $"{Kind}: {Name}{declaring}{inherited} [{Confidence}]";
    }
}
