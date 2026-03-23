using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    protected readonly IGDLogger _logger;
    private readonly GDGoToDefinitionService _service = new();

    private static string BuiltInTypesDir => GDBuiltInFileHelper.BuiltInTypesDir;

    public GDGoToDefHandler(GDScriptProject project, IGDRuntimeProvider? runtimeProvider = null, IGDLogger? logger = null)
    {
        _project = project;
        _runtimeProvider = runtimeProvider;
        _logger = logger ?? GDNullLogger.Instance;
    }

    /// <inheritdoc />
    public virtual GDDefinitionLocation? FindDefinition(string filePath, int line, int column)
    {
        if (IsTscnFile(filePath))
            return FindDefinitionInTscn(filePath, line, column);

        var script = _project.GetScript(filePath);
        if (script?.Class == null)
        {
            if (IsBuiltInTypeFile(filePath))
                return FindDefinitionInBuiltInFile(filePath, line, column);

            _logger.Debug($"[DEF] Script or Class null for: {filePath}");
            return null;
        }

        var cursor = new GDCursorPosition(line - 1, column - 1);
        var context = new GDRefactoringContext(script, script.Class, cursor, GDSelectionInfo.None, _project);

        _logger.Debug($"[DEF] Cursor 0-based: L{cursor.Line}:C{cursor.Column}");
        _logger.Debug($"[DEF] TokenAtCursor: {(context.TokenAtCursor != null ? $"'{context.TokenAtCursor}' ({context.TokenAtCursor.GetType().Name})" : "null")}");
        _logger.Debug($"[DEF] NodeAtCursor: {(context.NodeAtCursor != null ? $"'{context.NodeAtCursor}' ({context.NodeAtCursor.GetType().Name})" : "null")}");
        _logger.Debug($"[DEF] CanExecute: {_service.CanExecute(context)}");

        if (!_service.CanExecute(context))
            return null;

        var result = _service.GoToDefinition(context);
        _logger.Debug($"[DEF] GoToDefinition: Success={result.Success}, Type={result.DefinitionType}, FilePath={result.FilePath}, Symbol={result.SymbolName}, RequiresGodot={result.RequiresGodotLookup}");
        if (result.Success && result.FilePath != null)
            _logger.Debug($"[DEF] Location: L{result.Line}:C{result.Column}");

        return ConvertResultToLocation(result, filePath);
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDDefinitionLocation> FindDefinitions(string filePath, int line, int column)
    {
        if (IsTscnFile(filePath))
        {
            var loc = FindDefinitionInTscn(filePath, line, column);
            return loc != null ? new[] { loc } : Array.Empty<GDDefinitionLocation>();
        }

        var script = _project.GetScript(filePath);
        if (script?.Class == null)
        {
            if (IsBuiltInTypeFile(filePath))
            {
                var loc = FindDefinitionInBuiltInFile(filePath, line, column);
                return loc != null ? new[] { loc } : Array.Empty<GDDefinitionLocation>();
            }
            return Array.Empty<GDDefinitionLocation>();
        }

        var cursor = new GDCursorPosition(line - 1, column - 1);
        var context = new GDRefactoringContext(script, script.Class, cursor, GDSelectionInfo.None, _project);

        if (!_service.CanExecute(context))
            return Array.Empty<GDDefinitionLocation>();

        var result = _service.GoToDefinition(context);

        if (!result.Success)
            return Array.Empty<GDDefinitionLocation>();

        if (result.SubResults != null && result.SubResults.Count > 0)
        {
            var locations = new List<GDDefinitionLocation>();
            foreach (var sub in result.SubResults)
            {
                var loc = ConvertResultToLocation(sub, filePath);
                if (loc != null)
                    locations.Add(loc);
            }
            return locations;
        }

        var single = ConvertResultToLocation(result, filePath);
        return single != null ? new[] { single } : Array.Empty<GDDefinitionLocation>();
    }

    private GDDefinitionLocation? ConvertResultToLocation(GDGoToDefinitionResult result, string filePath)
    {
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
                ?? FindDefinitionByName(result.SymbolName, filePath)
                ?? ResolveAutoload(result.SymbolName)
                ?? GenerateBuiltInTypeDefinition(result.SymbolName);
        }

        if (result.DefinitionType == GDDefinitionType.ExternalMember && result.SymbolName != null)
            return FindDefinitionByName(result.SymbolName, filePath);

        if (result.DefinitionType == GDDefinitionType.BuiltInMember && result.TypeName != null && result.SymbolName != null)
        {
            var projectLocation = FindMemberInProjectType(result.TypeName, result.SymbolName);
            if (projectLocation != null)
                return projectLocation;

            var autoloadMemberLocation = FindMemberInAutoload(result.TypeName, result.SymbolName);
            if (autoloadMemberLocation != null)
                return autoloadMemberLocation;

            return FindDefinitionInBuiltInType(result.TypeName, result.SymbolName);
        }

        if (result.DefinitionType == GDDefinitionType.BuiltInType && result.SymbolName != null)
            return GenerateBuiltInTypeDefinition(result.SymbolName);

        if (result.DefinitionType == GDDefinitionType.BuiltInFunction && result.SymbolName != null)
            return GenerateBuiltInFunctionDefinition(result.SymbolName);

        if (result.DefinitionType == GDDefinitionType.ResourcePath && result.SymbolName != null)
            return ResolveResourceFilePath(result.SymbolName, filePath);

        if (result.RequiresGodotLookup && result.SymbolName != null)
        {
            var message = result.DefinitionType switch
            {
                GDDefinitionType.NodePath => $"'{result.SymbolName}' is a node path (requires Godot runtime)",
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
            var loc = FindSymbolInFile(_project.GetScript(fromFilePath), symbolName);
            if (loc != null) return loc;
        }

        foreach (var script in _project.ScriptFiles)
        {
            var loc = FindSymbolInFile(script, symbolName);
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

    private GDDefinitionLocation? FindMemberInProjectType(string typeName, string memberName)
    {
        // Try class_name lookup first — use SemanticModel for member resolution
        var script = _project.GetScriptByTypeName(typeName);
        if (script != null)
        {
            var loc = FindSymbolInFile(script, memberName);
            if (loc != null) return loc;
        }

        // Fallback: search for enum value by enum type name across project scripts
        foreach (var s in _project.ScriptFiles)
        {
            var model = s.SemanticModel;
            if (model == null) continue;

            var enumSymbol = model.FindSymbol(typeName);
            if (enumSymbol?.Kind != GDSymbolKind.Enum) continue;

            var valueSymbol = model.GetSymbolsOfKind(GDSymbolKind.EnumValue)
                .FirstOrDefault(ev => ev.Name == memberName && ev.DeclaringScopeNode == enumSymbol.DeclarationNode);

            if (valueSymbol?.DeclarationNode != null)
            {
                var posToken = valueSymbol.PositionToken;
                var line = posToken?.StartLine ?? valueSymbol.DeclarationNode.StartLine;
                var column = posToken?.StartColumn ?? valueSymbol.DeclarationNode.StartColumn;

                return new GDDefinitionLocation
                {
                    FilePath = s.Reference.FullPath,
                    Line = line + 1,
                    Column = column,
                    SymbolName = memberName,
                    Kind = GDSymbolKind.EnumValue
                };
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

    private GDDefinitionLocation? ResolveAutoload(string name)
    {
        var autoload = _project.AutoloadEntries.FirstOrDefault(a => a.Name == name);
        if (autoload == null)
            return null;

        var script = _project.GetScriptByResourcePath(autoload.Path);
        if (script?.Class == null)
            return null;

        var targetToken = script.Class.ClassName?.Identifier
            ?? (GDSyntaxToken?)script.Class.Extends?.Type;
        var line = targetToken?.StartLine ?? 0;
        var col = targetToken?.StartColumn ?? 0;

        return new GDDefinitionLocation
        {
            FilePath = script.Reference.FullPath,
            Line = line + 1,
            Column = col,
            SymbolName = name,
            Kind = GDSymbolKind.Class
        };
    }

    private GDDefinitionLocation? FindMemberInAutoload(string autoloadName, string memberName)
    {
        var autoload = _project.AutoloadEntries.FirstOrDefault(a => a.Name == autoloadName);
        if (autoload == null)
            return null;

        var script = _project.GetScriptByResourcePath(autoload.Path);
        if (script == null)
            return null;

        return FindSymbolInFile(script, memberName);
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
        sb.AppendLine("# This file is auto-generated from Godot metadata.");
        sb.AppendLine("# Actual implementation may differ from what is shown here.");
        sb.AppendLine();

        // Type documentation
        AppendDocComment(sb, typeInfo.BriefDescription);
        if (!string.IsNullOrWhiteSpace(typeInfo.Description) && typeInfo.Description != typeInfo.BriefDescription)
        {
            if (!string.IsNullOrWhiteSpace(typeInfo.BriefDescription))
                sb.AppendLine("##");
            AppendDocComment(sb, typeInfo.Description);
        }

        if (typeInfo.BaseType != null)
            sb.AppendLine($"extends {typeInfo.BaseType}");

        // Track class_name line (1-based)
        var classNameLine = sb.ToString().Split('\n').Length;
        sb.AppendLine($"class_name {typeInfo.Name}");

        if (typeInfo.Members != null && typeInfo.Members.Count > 0)
        {
            var constants = typeInfo.Members.Where(m => m.Kind == GDRuntimeMemberKind.Constant).ToList();
            if (constants.Count > 0)
            {
                sb.AppendLine();
                foreach (var m in constants)
                {
                    AppendDocComment(sb, m.Description);
                    var constValue = m.ConstantValue;
                    if (string.IsNullOrEmpty(constValue) || constValue == m.Name)
                        constValue = $"{typeInfo.Name}.{m.Name}";
                    var constType = MapCSharpTypeToGDScript(m.Type) ?? "Variant";
                    sb.AppendLine($"const {m.Name}: {constType} = {constValue}");
                }
            }

            var signals = typeInfo.Members.Where(m => m.Kind == GDRuntimeMemberKind.Signal).ToList();
            if (signals.Count > 0)
            {
                sb.AppendLine();
                var first = true;
                foreach (var m in signals)
                {
                    if (m.Description != null && !first) sb.AppendLine();
                    first = false;
                    AppendDocComment(sb, m.Description);
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
                var first = true;
                foreach (var m in properties)
                {
                    if (m.Description != null && !first) sb.AppendLine();
                    first = false;
                    AppendDocComment(sb, m.Description);
                    sb.AppendLine($"var {m.Name}: {NormalizeTypeName(m.Type) ?? "Variant"}");
                }
            }

            var methods = typeInfo.Members
                .Where(m => m.Kind == GDRuntimeMemberKind.Method)
                .Where(m => m.Description != null)
                .ToList();
            if (methods.Count > 0)
            {
                sb.AppendLine();
                var first = true;
                foreach (var m in methods)
                {
                    if (!first) sb.AppendLine();
                    first = false;
                    AppendDocComment(sb, m.Description);
                    sb.AppendLine(BuildMethodString(m));
                }
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

        return new GDDefinitionLocation
        {
            FilePath = filePath,
            Line = classNameLine,
            Column = "class_name ".Length,
            SymbolName = typeName,
            Kind = GDSymbolKind.Class
        };
    }

    private GDDefinitionLocation? GenerateBuiltInEnumDefinition(string typeName, GDRuntimeTypeInfo typeInfo)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# This file is auto-generated from Godot metadata.");
        sb.AppendLine("# Actual implementation may differ from what is shown here.");
        sb.AppendLine();

        AppendDocComment(sb, typeInfo.BriefDescription);
        if (!string.IsNullOrWhiteSpace(typeInfo.Description) && typeInfo.Description != typeInfo.BriefDescription)
        {
            if (!string.IsNullOrWhiteSpace(typeInfo.BriefDescription))
                sb.AppendLine("##");
            AppendDocComment(sb, typeInfo.Description);
        }

        var enumLine = sb.ToString().Split('\n').Length;
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
            Line = enumLine,
            Column = "enum ".Length,
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
            {
                if (p.Type == null) return p.Name;
                var normalized = NormalizeTypeName(p.Type) ?? "Variant";
                return $"{p.Name}: {normalized}";
            }));
        }

        var returnType = NormalizeReturnTypeName(m.Type);
        if (returnType != null)
            return $"{prefix}func {m.Name}({parms}) -> {returnType}: pass";

        return $"{prefix}func {m.Name}({parms}): pass";
    }

    private static string? NormalizeReturnTypeName(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName) || typeName == "void")
            return null;

        if (typeName == "Variant")
            return "Variant";

        return NormalizeTypeName(typeName);
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

        typeName = GDGenericTypeHelper.ExtractBaseTypeName(typeName);

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

    private static string? FormatDocComment(string? description, string prefix = "## ")
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var text = description;

        // Remove [csharp]...[/csharp] blocks entirely (keep [gdscript] content)
        text = System.Text.RegularExpressions.Regex.Replace(text,
            @"\[csharp\].*?\[/csharp\]", "", System.Text.RegularExpressions.RegexOptions.Singleline);

        // Remove [codeblocks]/[/codeblocks] wrappers
        text = text.Replace("[codeblocks]", "").Replace("[/codeblocks]", "");

        // Extract [gdscript]...[/gdscript] content (remove tags, keep content)
        text = System.Text.RegularExpressions.Regex.Replace(text,
            @"\[gdscript\](.*?)\[/gdscript\]",
            m =>
            {
                var code = m.Groups[1].Value.Trim();
                var lines = code.Split('\n');
                var sb2 = new System.Text.StringBuilder();
                sb2.AppendLine();
                foreach (var l in lines)
                    sb2.AppendLine("    " + l.TrimEnd());
                return sb2.ToString();
            },
            System.Text.RegularExpressions.RegexOptions.Singleline);

        // Convert BBCode tags to readable text
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[param\s+(\w+)\]", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[code\](.*?)\[/code\]", "`$1`");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[b\](.*?)\[/b\]", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[i\](.*?)\[/i\]", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[url=.*?\](.*?)\[/url\]", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[method\s+(\w+)\]", "$1()");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[member\s+(\w+)\]", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[signal\s+(\w+)\]", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[constant\s+([\w.]+)\]", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[enum\s+([\w.]+)\]", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[theme_item\s+([\w.]+)\]", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[annotation\s+(@[\w.]+)\]", "$1");

        // Type references like [Node], [Vector2], etc.
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([A-Z]\w*)\]", "$1");

        // Clean up any remaining BBCode-like tags
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[/?\w+\]", "");

        // Clean up multiple blank lines
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");

        var lines2 = text.Split('\n');
        var result = new System.Text.StringBuilder();

        foreach (var line in lines2)
        {
            var trimmed = line.TrimEnd();
            if (string.IsNullOrEmpty(trimmed))
                result.AppendLine(prefix.TrimEnd());
            else
                result.AppendLine(prefix + trimmed);
        }

        // Remove trailing empty comment lines
        var resultStr = result.ToString().TrimEnd('\r', '\n');
        while (resultStr.EndsWith("\n" + prefix.TrimEnd()) || resultStr.EndsWith("\r\n" + prefix.TrimEnd()))
        {
            var idx = resultStr.LastIndexOf('\n');
            if (idx >= 0)
                resultStr = resultStr.Substring(0, idx).TrimEnd('\r', '\n');
            else
                break;
        }

        return string.IsNullOrWhiteSpace(resultStr) ? null : resultStr;
    }

    private static void AppendDocComment(System.Text.StringBuilder sb, string? description)
    {
        var comment = FormatDocComment(description);
        if (comment != null)
        {
            sb.AppendLine(comment);
        }
    }

    private GDDefinitionLocation? GenerateBuiltInFunctionDefinition(string functionName)
    {
        if (_runtimeProvider == null)
            return null;

        var funcInfo = _runtimeProvider.GetGlobalFunction(functionName);
        if (funcInfo == null)
            return GDDefinitionLocation.WithInfo($"'{functionName}' is a built-in function (definition not available)");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# This file is auto-generated from Godot metadata.");
        sb.AppendLine("# Actual implementation may differ from what is shown here.");
        sb.AppendLine();
        sb.AppendLine("class_name @GDScript");
        sb.AppendLine();

        // Build function signature
        AppendDocComment(sb, funcInfo.Description);

        var parms = "";
        if (funcInfo.Parameters != null && funcInfo.Parameters.Count > 0)
        {
            parms = string.Join(", ", funcInfo.Parameters.Select(p =>
                p.Type != null ? $"{p.Name}: {NormalizeTypeName(p.Type)}" : p.Name));
        }

        var funcLine = sb.ToString().Split('\n').Length;
        var returnType = NormalizeTypeName(funcInfo.ReturnType);
        if (returnType != null)
            sb.AppendLine($"func {funcInfo.Name}({parms}) -> {returnType}: pass");
        else
            sb.AppendLine($"func {funcInfo.Name}({parms}): pass");

        var code = sb.ToString();

        var dir = Path.Combine(Path.GetTempPath(), "gdshrapt", "builtin_types");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, "@GDScript.gd");
        File.WriteAllText(filePath, code);

        return new GDDefinitionLocation
        {
            FilePath = filePath,
            Line = funcLine,
            Column = "func ".Length,
            SymbolName = functionName,
            Kind = GDSymbolKind.Method
        };
    }

    private GDDefinitionLocation? ResolveResourceFilePath(string path, string currentFilePath)
    {
        string resolvedPath;
        if (path.StartsWith("res://"))
        {
            var relativePath = path.Substring("res://".Length);
            var projectPath = _project.ProjectPath;
            if (string.IsNullOrEmpty(projectPath))
                return GDDefinitionLocation.WithInfo($"Cannot resolve '{path}' — project path unknown");
            resolvedPath = Path.GetFullPath(Path.Combine(projectPath, relativePath));
        }
        else
        {
            var currentDir = Path.GetDirectoryName(currentFilePath) ?? "";
            resolvedPath = Path.GetFullPath(Path.Combine(currentDir, path));
        }

        if (File.Exists(resolvedPath))
        {
            return new GDDefinitionLocation
            {
                FilePath = resolvedPath,
                Line = 1,
                Column = 0,
                SymbolName = Path.GetFileName(resolvedPath),
                Kind = null
            };
        }

        return GDDefinitionLocation.WithInfo($"File not found: {path}");
    }

    private static bool IsBuiltInTypeFile(string filePath)
        => GDBuiltInFileHelper.IsBuiltInTypeFile(filePath);

    private GDDefinitionLocation? FindDefinitionInBuiltInFile(string filePath, int line, int column)
    {
        if (_runtimeProvider == null)
            return null;

        var scriptFile = GDBuiltInFileHelper.GetOrParse(filePath, _runtimeProvider);
        if (scriptFile?.Class == null)
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

        // Walk inheritance chain to find the declaring type
        var currentType = typeName;
        while (currentType != null)
        {
            var location = GenerateBuiltInTypeDefinition(currentType);
            if (location == null || string.IsNullOrEmpty(location.FilePath))
                break;

            var memberLocation = FindMemberInGeneratedFile(location.FilePath, memberName);
            if (memberLocation != null)
                return memberLocation;

            // Member not found on this type — try parent
            var typeInfo = _runtimeProvider.GetTypeInfo(currentType);
            currentType = typeInfo?.BaseType;
        }

        // Fallback: generate definition for the original type
        return GenerateBuiltInTypeDefinition(typeName);
    }

    private static GDDefinitionLocation? FindMemberInGeneratedFile(string filePath, string memberName)
    {
        string content;
        try
        {
            content = File.ReadAllText(filePath);
        }
        catch
        {
            return null;
        }

        var reference = new GDScriptReference(filePath);
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(content);

        if (scriptFile.Class == null)
            return null;

        foreach (var member in scriptFile.Class.Members.OfType<GDIdentifiableClassMember>())
        {
            if (member.Identifier?.Sequence == memberName)
            {
                return new GDDefinitionLocation
                {
                    FilePath = filePath,
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

        return null;
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

    private static bool IsTscnFile(string filePath)
    {
        return filePath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith(".tres", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly Regex TscnResPathRegex = new(@"""(res://[^""]+)""", RegexOptions.Compiled);

    private GDDefinitionLocation? FindDefinitionInTscn(string filePath, int line, int column)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch
        {
            return null;
        }

        if (line < 1 || line > lines.Length)
            return null;

        var lineText = lines[line - 1];
        var col0 = column - 1;

        foreach (Match match in TscnResPathRegex.Matches(lineText))
        {
            var pathStart = match.Index + 1;
            var pathEnd = match.Index + match.Length - 1;

            if (col0 >= pathStart && col0 < pathEnd)
            {
                var resPath = match.Groups[1].Value;
                return ResolveResourceFilePath(resPath, filePath);
            }
        }

        return null;
    }
}
