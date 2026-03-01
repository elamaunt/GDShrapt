using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Builder;
using GDShrapt.Formatter;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for go-to-definition navigation.
/// Delegates to GDGoToDefinitionService for single-file resolution,
/// then performs cross-file resolution for external types via GDScriptProject.
/// </summary>
public class GDGoToDefHandler : IGDGoToDefHandler
{
    protected readonly GDScriptProject _project;
    protected readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDGoToDefinitionService _service = new();

    private static readonly string BuiltInTypesDir =
        Path.Combine(Path.GetTempPath(), "gdshrapt", "builtin_types");

    public GDGoToDefHandler(GDScriptProject project, IGDRuntimeProvider? runtimeProvider = null)
    {
        _project = project;
        _runtimeProvider = runtimeProvider;
    }

    /// <inheritdoc />
    public virtual GDDefinitionLocation? FindDefinition(string filePath, int line, int column)
    {
        var script = _project.GetScript(filePath);
        if (script?.Class == null)
        {
            if (IsBuiltInTypeFile(filePath))
                return FindDefinitionInBuiltInFile(filePath, line, column);

            Console.Error.WriteLine($"[DEF] Script or Class null for: {filePath}");
            return null;
        }

        var cursor = new GDCursorPosition(line - 1, column - 1);
        var context = new GDRefactoringContext(script, script.Class, cursor, GDSelectionInfo.None, _project);

        Console.Error.WriteLine($"[DEF] Cursor 0-based: L{cursor.Line}:C{cursor.Column}");
        Console.Error.WriteLine($"[DEF] TokenAtCursor: {(context.TokenAtCursor != null ? $"'{context.TokenAtCursor}' ({context.TokenAtCursor.GetType().Name})" : "null")}");
        Console.Error.WriteLine($"[DEF] NodeAtCursor: {(context.NodeAtCursor != null ? $"'{context.NodeAtCursor}' ({context.NodeAtCursor.GetType().Name})" : "null")}");
        Console.Error.WriteLine($"[DEF] CanExecute: {_service.CanExecute(context)}");

        if (!_service.CanExecute(context))
            return null;

        var result = _service.GoToDefinition(context);
        Console.Error.WriteLine($"[DEF] GoToDefinition: Success={result.Success}, Type={result.DefinitionType}, FilePath={result.FilePath}, Symbol={result.SymbolName}, RequiresGodot={result.RequiresGodotLookup}");
        if (result.Success && result.FilePath != null)
            Console.Error.WriteLine($"[DEF] Location: L{result.Line}:C{result.Column}");

        if (!result.Success)
            return null;

        if (result.FilePath != null)
        {
            return new GDDefinitionLocation
            {
                FilePath = result.FilePath,
                Line = result.Line + 1,
                Column = result.Column,
                SymbolName = result.SymbolName,
                Kind = MapDefinitionTypeToKind(result.DefinitionType)
            };
        }

        if (result.DefinitionType == GDDefinitionType.ExternalType && result.SymbolName != null)
        {
            return ResolveExternalType(result.SymbolName)
                ?? GenerateBuiltInTypeDefinition(result.SymbolName);
        }

        if (result.DefinitionType == GDDefinitionType.ExternalMember && result.SymbolName != null)
            return FindDefinitionByName(result.SymbolName, filePath);

        if (result.DefinitionType == GDDefinitionType.BuiltInType && result.SymbolName != null)
            return GenerateBuiltInTypeDefinition(result.SymbolName);

        if (result.RequiresGodotLookup && result.SymbolName != null)
        {
            var message = result.DefinitionType switch
            {
                GDDefinitionType.NodePath => $"'{result.SymbolName}' is a node path (requires Godot runtime)",
                GDDefinitionType.ResourcePath => $"'{result.SymbolName}' is a resource path",
                _ => $"'{result.SymbolName}' cannot be resolved statically"
            };
            return GDDefinitionLocation.WithInfo(message);
        }

        return null;
    }

    /// <inheritdoc />
    public virtual GDDefinitionLocation? FindDefinitionByName(string symbolName, string? fromFilePath = null)
    {
        if (!string.IsNullOrEmpty(fromFilePath))
        {
            var loc = FindSymbolInFile(_project.GetScript(fromFilePath), symbolName)
                   ?? FindEnumValueInFile(_project.GetScript(fromFilePath), symbolName);
            if (loc != null) return loc;
        }

        foreach (var script in _project.ScriptFiles)
        {
            var loc = FindSymbolInFile(script, symbolName)
                   ?? FindEnumValueInFile(script, symbolName);
            if (loc != null) return loc;
        }

        return ResolveExternalType(symbolName);
    }

    private GDDefinitionLocation? FindSymbolInFile(GDScriptFile? script, string symbolName)
    {
        var symbol = script?.SemanticModel?.FindSymbol(symbolName);
        if (symbol?.DeclarationNode == null)
            return null;

        var posToken = symbol.PositionToken;
        var line = posToken?.StartLine ?? symbol.DeclarationNode.StartLine;
        var column = posToken?.StartColumn ?? symbol.DeclarationNode.StartColumn;

        return new GDDefinitionLocation
        {
            FilePath = script!.Reference.FullPath,
            Line = line + 1,
            Column = column,
            SymbolName = symbol.Name,
            Kind = symbol.Kind
        };
    }

    private GDDefinitionLocation? FindEnumValueInFile(GDScriptFile? script, string valueName)
    {
        if (script?.Class == null)
            return null;

        foreach (var enumDecl in script.Class.Members.OfType<GDEnumDeclaration>())
        {
            foreach (var enumValue in enumDecl.Values?.OfType<GDEnumValueDeclaration>() ?? Enumerable.Empty<GDEnumValueDeclaration>())
            {
                if (enumValue.Identifier?.Sequence == valueName)
                {
                    return new GDDefinitionLocation
                    {
                        FilePath = script.Reference.FullPath,
                        Line = enumValue.Identifier.StartLine + 1,
                        Column = enumValue.Identifier.StartColumn,
                        SymbolName = valueName,
                        Kind = GDSymbolKind.EnumValue
                    };
                }
            }
        }

        return null;
    }

    private GDDefinitionLocation? ResolveExternalType(string typeName)
    {
        var typeScript = _project.GetScriptByTypeName(typeName);
        var identifierToken = typeScript?.Class?.ClassName?.Identifier;
        if (identifierToken == null)
            return null;

        return new GDDefinitionLocation
        {
            FilePath = typeScript!.Reference.FullPath,
            Line = identifierToken.StartLine + 1,
            Column = identifierToken.StartColumn,
            SymbolName = typeName,
            Kind = GDSymbolKind.Class
        };
    }

    private GDDefinitionLocation? GenerateBuiltInTypeDefinition(string typeName)
    {
        if (_runtimeProvider == null)
            return null;

        var typeInfo = _runtimeProvider.GetTypeInfo(typeName);
        if (typeInfo == null)
            return null;

        var membersList = new List<GDClassMember>();

        if (typeInfo.BaseType != null)
            membersList.Add(GD.Attribute.Extends(typeInfo.BaseType));

        membersList.Add(GD.Attribute.ClassName(typeInfo.Name));

        if (typeInfo.Members != null && typeInfo.Members.Count > 0)
        {
            foreach (var m in typeInfo.Members.Where(m => m.Kind == GDRuntimeMemberKind.Constant))
                membersList.Add(GD.Declaration.Const(m.Name, m.Type ?? "Variant", GD.Expression.Number(0)));

            foreach (var m in typeInfo.Members.Where(m => m.Kind == GDRuntimeMemberKind.Signal))
            {
                if (m.Parameters != null && m.Parameters.Count > 0)
                    membersList.Add(GD.Declaration.Signal(GD.Syntax.Identifier(m.Name), GD.List.Parameters(BuildParameterDeclarations(m.Parameters))));
                else
                    membersList.Add(GD.Declaration.Signal(m.Name));
            }

            foreach (var m in typeInfo.Members.Where(m => m.Kind == GDRuntimeMemberKind.Property))
                membersList.Add(GD.Declaration.Variable(m.Name, m.Type ?? "Variant"));

            foreach (var m in typeInfo.Members.Where(m => m.Kind == GDRuntimeMemberKind.Method))
                membersList.Add(BuildMethodDeclaration(m));
        }
        else
        {
            membersList.Add(new GDPassDeclaration());
        }

        var classDecl = GD.Declaration.Class(membersList.ToArray());

        classDecl.UpdateIntendation();

        var formatter = new GDFormatter(GDFormatterOptions.Default);
        formatter.Format(classDecl);

        var code = $"# Built-in Godot type: {typeInfo.Name}\n" + classDecl.ToString();

        var dir = Path.Combine(Path.GetTempPath(), "gdshrapt", "builtin_types");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{typeName}.gd");
        File.WriteAllText(filePath, code);

        var classNameIdentifier = classDecl.ClassName?.Identifier;
        var classNameLine = classNameIdentifier != null
            ? classNameIdentifier.StartLine + 2  // +1 for comment line, +1 for 0→1 based
            : 1;

        return new GDDefinitionLocation
        {
            FilePath = filePath,
            Line = classNameLine,
            Column = 0,
            SymbolName = typeName,
            Kind = GDSymbolKind.Class
        };
    }

    private static GDParameterDeclaration[] BuildParameterDeclarations(IReadOnlyList<GDRuntimeParameterInfo> parameters)
    {
        return parameters.Select(p =>
        {
            if (!string.IsNullOrEmpty(p.Type))
                return GD.Declaration.Parameter(p.Name, GD.Type.Single(p.Type));

            return GD.Declaration.Parameter(p.Name);
        }).ToArray();
    }

    private static GDMethodDeclaration BuildMethodDeclaration(GDRuntimeMemberInfo m)
    {
        var parameters = m.Parameters != null && m.Parameters.Count > 0
            ? GD.List.Parameters(BuildParameterDeclarations(m.Parameters))
            : null;

        var hasReturnType = !string.IsNullOrEmpty(m.Type);

        GDMethodDeclaration method;

        if (parameters != null && hasReturnType)
        {
            method = GD.Declaration.AbstractMethod(m.Name, parameters, GD.Type.Single(m.Type!));
        }
        else if (parameters != null)
        {
            method = GD.Declaration.Method(GD.Syntax.Identifier(m.Name), parameters,
                GD.Expression.Pass());
        }
        else if (hasReturnType)
        {
            method = GD.Declaration.AbstractMethod(m.Name, GD.Type.Single(m.Type!));
        }
        else
        {
            method = GD.Declaration.AbstractMethod(m.Name);
        }

        if (m.IsStatic)
        {
            method.StaticKeyword = new GDStaticKeyword();
            method[1] = GD.Syntax.Space();
        }

        return method;
    }

    private bool IsBuiltInTypeFile(string filePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var builtInDir = Path.GetFullPath(BuiltInTypesDir);
            return fullPath.StartsWith(builtInDir, StringComparison.OrdinalIgnoreCase)
                && fullPath.EndsWith(".gd", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private GDDefinitionLocation? FindDefinitionInBuiltInFile(string filePath, int line, int column)
    {
        if (_runtimeProvider == null)
            return null;

        string content;
        try
        {
            content = File.ReadAllText(filePath);
        }
        catch
        {
            return null;
        }

        // Parse the generated file to get AST
        var reference = new GDScriptReference(filePath);
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(content);

        if (scriptFile.Class == null)
            return null;

        // Use GDGoToDefinitionService for AST-based resolution
        var cursor = new GDCursorPosition(line - 1, column - 1);
        var context = new GDRefactoringContext(scriptFile, scriptFile.Class, cursor, GDSelectionInfo.None);

        if (!_service.CanExecute(context))
            return null;

        var result = _service.GoToDefinition(context);
        if (!result.Success)
            return null;

        // Local resolution within the file (class member, variable, etc.)
        if (result.FilePath != null)
        {
            return new GDDefinitionLocation
            {
                FilePath = filePath,
                Line = result.Line + 1,
                Column = result.Column,
                SymbolName = result.SymbolName,
                Kind = MapDefinitionTypeToKind(result.DefinitionType)
            };
        }

        // External type — generate its pseudo-definition
        if (result.SymbolName != null &&
            (result.DefinitionType == GDDefinitionType.ExternalType ||
             result.DefinitionType == GDDefinitionType.BuiltInType))
        {
            if (_runtimeProvider.IsKnownType(result.SymbolName))
                return GenerateBuiltInTypeDefinition(result.SymbolName);

            return ResolveExternalType(result.SymbolName);
        }

        // Unresolvable — show info message
        var containingType = Path.GetFileNameWithoutExtension(filePath);
        return GDDefinitionLocation.WithInfo(
            $"'{result.SymbolName ?? "symbol"}' is an internal type of '{containingType}' (enum or bitfield)");
    }

    private static GDSymbolKind? MapDefinitionTypeToKind(GDDefinitionType type)
    {
        return type switch
        {
            GDDefinitionType.LocalVariable => GDSymbolKind.Variable,
            GDDefinitionType.MethodParameter => GDSymbolKind.Parameter,
            GDDefinitionType.ForLoopVariable => GDSymbolKind.Iterator,
            GDDefinitionType.ClassMember => null,
            GDDefinitionType.TypeDeclaration => GDSymbolKind.Class,
            GDDefinitionType.ExternalType => GDSymbolKind.Class,
            _ => null
        };
    }
}
