using System.Collections.Generic;
using System.Linq;
using Godot;
using GDShrapt.Reader;

namespace GDShrapt.Plugin.Api.Internal;

/// <summary>
/// Implementation of IScriptInfo that wraps GDScriptMap.
/// </summary>
internal class ScriptInfoImpl : IScriptInfo
{
    private readonly GDScriptMap _scriptMap;

    public ScriptInfoImpl(GDScriptMap scriptMap)
    {
        _scriptMap = scriptMap;
    }

    public string FullPath => _scriptMap.Reference?.FullPath ?? string.Empty;

    public string ResourcePath
    {
        get
        {
            var fullPath = FullPath;
            if (string.IsNullOrEmpty(fullPath))
                return string.Empty;

            var projectPath = ProjectSettings.GlobalizePath("res://").Replace("\\", "/");
            var normalizedPath = fullPath.Replace("\\", "/");

            if (normalizedPath.StartsWith(projectPath, System.StringComparison.OrdinalIgnoreCase))
            {
                return "res://" + normalizedPath.Substring(projectPath.Length);
            }

            return fullPath;
        }
    }
    public string? TypeName => _scriptMap.TypeName;
    public bool IsGlobal => _scriptMap.IsGlobal;
    public bool HasParseErrors => _scriptMap.WasReadError;
    public GDClassDeclaration? AstRoot => _scriptMap.Class;

    internal GDScriptMap ScriptMap => _scriptMap;

    public IReadOnlyList<ISymbolInfo> GetDeclarations()
    {
        var result = new List<ISymbolInfo>();
        var classDecl = _scriptMap.Class;
        if (classDecl == null) return result;

        foreach (var member in classDecl.Members)
        {
            if (member is GDMethodDeclaration method)
            {
                result.Add(new SymbolInfoImpl(
                    method.Identifier?.Sequence ?? string.Empty,
                    SymbolKind.Method,
                    method.Identifier?.StartLine ?? 0,
                    method.Identifier?.StartColumn ?? 0,
                    method.ReturnType?.ToString(),
                    null
                ));
            }
            else if (member is GDVariableDeclaration variable)
            {
                result.Add(new SymbolInfoImpl(
                    variable.Identifier?.Sequence ?? string.Empty,
                    SymbolKind.Variable,
                    variable.Identifier?.StartLine ?? 0,
                    variable.Identifier?.StartColumn ?? 0,
                    variable.Type?.ToString(),
                    null
                ));
            }
            else if (member is GDSignalDeclaration signal)
            {
                result.Add(new SymbolInfoImpl(
                    signal.Identifier?.Sequence ?? string.Empty,
                    SymbolKind.Signal,
                    signal.Identifier?.StartLine ?? 0,
                    signal.Identifier?.StartColumn ?? 0,
                    null,
                    null
                ));
            }
            else if (member is GDEnumDeclaration enumDecl)
            {
                result.Add(new SymbolInfoImpl(
                    enumDecl.Identifier?.Sequence ?? string.Empty,
                    SymbolKind.Enum,
                    enumDecl.Identifier?.StartLine ?? 0,
                    enumDecl.Identifier?.StartColumn ?? 0,
                    null,
                    null
                ));
            }
            else if (member is GDInnerClassDeclaration innerClass)
            {
                result.Add(new SymbolInfoImpl(
                    innerClass.Identifier?.Sequence ?? string.Empty,
                    SymbolKind.InnerClass,
                    innerClass.Identifier?.StartLine ?? 0,
                    innerClass.Identifier?.StartColumn ?? 0,
                    null,
                    null
                ));
            }
        }

        return result;
    }

    public IReadOnlyList<IIdentifierInfo> GetIdentifiers()
    {
        var result = new List<IIdentifierInfo>();
        var classDecl = _scriptMap.Class;
        if (classDecl == null) return result;

        foreach (var token in classDecl.AllTokens)
        {
            if (token is GDIdentifier identifier)
            {
                result.Add(new IdentifierInfoImpl(
                    identifier.Sequence ?? string.Empty,
                    identifier.StartLine,
                    identifier.StartColumn,
                    null,
                    IsDeclaration(identifier)
                ));
            }
        }

        return result;
    }

    private static bool IsDeclaration(GDIdentifier identifier)
    {
        var parent = identifier.Parent;
        return parent is GDMethodDeclaration ||
               parent is GDVariableDeclaration ||
               parent is GDSignalDeclaration ||
               parent is GDParameterDeclaration ||
               parent is GDEnumDeclaration ||
               parent is GDInnerClassDeclaration;
    }
}

internal class SymbolInfoImpl : ISymbolInfo
{
    public SymbolInfoImpl(string name, SymbolKind kind, int line, int column, string? typeAnnotation, string? documentation)
    {
        Name = name;
        Kind = kind;
        Line = line;
        Column = column;
        TypeAnnotation = typeAnnotation;
        Documentation = documentation;
    }

    public string Name { get; }
    public SymbolKind Kind { get; }
    public int Line { get; }
    public int Column { get; }
    public string? TypeAnnotation { get; }
    public string? Documentation { get; }
}

internal class IdentifierInfoImpl : IIdentifierInfo
{
    public IdentifierInfoImpl(string name, int line, int column, string? inferredType, bool isDeclaration)
    {
        Name = name;
        Line = line;
        Column = column;
        InferredType = inferredType;
        IsDeclaration = isDeclaration;
    }

    public string Name { get; }
    public int Line { get; }
    public int Column { get; }
    public string? InferredType { get; }
    public bool IsDeclaration { get; }
}
