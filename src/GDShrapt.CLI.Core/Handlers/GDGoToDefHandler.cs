using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GDShrapt.Abstractions;
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

        if (result.DefinitionType == GDDefinitionType.BuiltInMember && result.TypeName != null && result.SymbolName != null)
            return FindDefinitionInBuiltInType(result.TypeName, result.SymbolName);

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

        if (typeInfo.IsEnum)
            return GenerateBuiltInEnumDefinition(typeName, typeInfo);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# Built-in Godot type: {typeInfo.Name}");

        if (typeInfo.BaseType != null)
            sb.AppendLine($"extends {typeInfo.BaseType}");

        sb.AppendLine($"class_name {typeInfo.Name}");

        if (typeInfo.Members != null && typeInfo.Members.Count > 0)
        {
            var constants = typeInfo.Members.Where(m => m.Kind == GDRuntimeMemberKind.Constant).ToList();
            if (constants.Count > 0)
            {
                sb.AppendLine();
                foreach (var m in constants)
                {
                    var constValue = m.ConstantValue ?? "0";
                    var constType = MapCSharpTypeToGDScript(m.Type) ?? "Variant";
                    sb.AppendLine($"const {m.Name}: {constType} = {constValue}");
                }
            }

            var signals = typeInfo.Members.Where(m => m.Kind == GDRuntimeMemberKind.Signal).ToList();
            if (signals.Count > 0)
            {
                sb.AppendLine();
                foreach (var m in signals)
                {
                    if (m.Parameters != null && m.Parameters.Count > 0)
                    {
                        var parms = string.Join(", ", m.Parameters.Select(p =>
                        {
                            var pt = NormalizeTypeName(p.Type);
                            return pt != null ? $"{p.Name}: {pt}" : p.Name;
                        }));
                        sb.AppendLine($"signal {m.Name}({parms})");
                    }
                    else
                    {
                        sb.AppendLine($"signal {m.Name}");
                    }
                }
            }

            var properties = typeInfo.Members.Where(m => m.Kind == GDRuntimeMemberKind.Property).ToList();
            if (properties.Count > 0)
            {
                sb.AppendLine();
                foreach (var m in properties)
                    sb.AppendLine($"var {m.Name}: {NormalizeTypeName(m.Type) ?? "Variant"}");
            }

            var methods = typeInfo.Members.Where(m => m.Kind == GDRuntimeMemberKind.Method).ToList();
            if (methods.Count > 0)
            {
                sb.AppendLine();
                foreach (var m in methods)
                    sb.AppendLine(BuildMethodString(m));
            }
        }
        else
        {
            sb.AppendLine("pass");
        }

        var code = sb.ToString();

        var dir = Path.Combine(Path.GetTempPath(), "gdshrapt", "builtin_types");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{typeName}.gd");
        File.WriteAllText(filePath, code);

        // class_name is on line 3 if extends present, line 2 otherwise
        var classNameLine = typeInfo.BaseType != null ? 3 : 2;

        return new GDDefinitionLocation
        {
            FilePath = filePath,
            Line = classNameLine,
            Column = 0,
            SymbolName = typeName,
            Kind = GDSymbolKind.Class
        };
    }

    private GDDefinitionLocation? GenerateBuiltInEnumDefinition(string typeName, GDRuntimeTypeInfo typeInfo)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# Built-in Godot type: {typeInfo.Name}");
        sb.AppendLine($"enum {typeInfo.Name} {{");

        if (typeInfo.EnumValues != null && typeInfo.EnumValues.Count > 0)
        {
            foreach (var kvp in typeInfo.EnumValues)
            {
                sb.AppendLine($"\t{kvp.Key} = {kvp.Value},");
            }
        }
        else if (typeInfo.Members != null)
        {
            int idx = 0;
            foreach (var m in typeInfo.Members.Where(m => m.Kind == GDRuntimeMemberKind.Constant))
            {
                sb.AppendLine($"\t{m.Name} = {idx++},");
            }
        }

        sb.AppendLine("}");

        var code = sb.ToString();

        var dir = Path.Combine(Path.GetTempPath(), "gdshrapt", "builtin_types");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{typeName}.gd");
        File.WriteAllText(filePath, code);

        return new GDDefinitionLocation
        {
            FilePath = filePath,
            Line = 2,
            Column = 0,
            SymbolName = typeName,
            Kind = GDSymbolKind.Enum
        };
    }

    private static string? MapCSharpTypeToGDScript(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        switch (typeName)
        {
            case "Int32":
            case "Int64":
            case "UInt32":
            case "UInt64":
            case "Byte":
            case "SByte":
            case "Int16":
            case "UInt16":
                return "int";
            case "Single":
            case "Double":
                return "float";
            case "Boolean":
                return "bool";
            case "String":
                return "String";
            default:
                return typeName;
        }
    }

    private static string BuildMethodString(GDRuntimeMemberInfo m)
    {
        var prefix = m.IsStatic ? "static " : "";

        var parms = "";
        if (m.Parameters != null && m.Parameters.Count > 0)
        {
            parms = string.Join(", ", m.Parameters.Select(p =>
                p.Type != null ? $"{p.Name}: {NormalizeTypeName(p.Type)}" : p.Name));
        }

        var returnType = NormalizeTypeName(m.Type);
        if (returnType != null)
            return $"{prefix}func {m.Name}({parms}) -> {returnType}: pass";

        return $"{prefix}func {m.Name}({parms}): pass";
    }

    private static string? NormalizeTypeName(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName) || typeName == "Variant" || typeName == "void")
            return null;

        // Single uppercase letter is a type variable (T, K, V) → show as Node (Godot convention)
        if (typeName.Length == 1 && char.IsUpper(typeName[0]))
            return "Node";

        if (typeName.StartsWith("enum::") || typeName.StartsWith("bitfield::"))
            return "int";

        if (typeName.StartsWith("typedarray::"))
            return "Array";

        var bracketIdx = typeName.IndexOf('[');
        if (bracketIdx > 0)
            typeName = typeName.Substring(0, bracketIdx);

        if (typeName.Length == 0 || !IsValidTypeIdentifier(typeName))
            return "Variant";

        return typeName;
    }

    private static bool IsValidTypeIdentifier(string value)
    {
        if (char.IsDigit(value[0]))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '.')
                return false;
        }

        return true;
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

    private GDDefinitionLocation? FindDefinitionInBuiltInType(string typeName, string memberName)
    {
        if (_runtimeProvider == null)
            return null;

        var location = GenerateBuiltInTypeDefinition(typeName);
        if (location == null || string.IsNullOrEmpty(location.FilePath))
            return null;

        string content;
        try
        {
            content = File.ReadAllText(location.FilePath);
        }
        catch
        {
            return location;
        }

        var reference = new GDScriptReference(location.FilePath);
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(content);

        if (scriptFile.Class == null)
            return location;

        foreach (var member in scriptFile.Class.Members.OfType<GDIdentifiableClassMember>())
        {
            if (member.Identifier?.Sequence == memberName)
            {
                return new GDDefinitionLocation
                {
                    FilePath = location.FilePath,
                    Line = member.Identifier.StartLine + 1,
                    Column = member.Identifier.StartColumn,
                    SymbolName = memberName,
                    Kind = member switch
                    {
                        GDMethodDeclaration => GDSymbolKind.Method,
                        GDSignalDeclaration => GDSymbolKind.Signal,
                        GDVariableDeclaration => GDSymbolKind.Variable,
                        _ => null
                    }
                };
            }
        }

        return location;
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
