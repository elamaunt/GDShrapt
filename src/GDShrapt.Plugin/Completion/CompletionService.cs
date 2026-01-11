using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Service that provides code completion suggestions.
/// </summary>
internal class CompletionService
{
    private readonly GDProjectMap _projectMap;
    private readonly GDTypeResolver _typeResolver;
    private readonly GDGodotTypesProvider _godotTypesProvider;

    // GDScript keywords
    private static readonly string[] Keywords = {
        "if", "elif", "else", "for", "while", "match", "break", "continue",
        "pass", "return", "class", "class_name", "extends", "is", "in", "as",
        "self", "signal", "func", "static", "const", "enum", "var",
        "onready", "export", "setget", "tool", "master", "puppet", "slave",
        "remote", "sync", "remotesync", "mastersync", "puppetsync",
        "yield", "await", "preload", "load", "assert", "breakpoint",
        "true", "false", "null", "PI", "TAU", "INF", "NAN",
        "and", "or", "not"
    };

    // Built-in functions
    private static readonly string[] BuiltInFunctions = {
        "print", "prints", "printt", "printraw", "printerr", "push_error", "push_warning",
        "str", "int", "float", "bool", "range", "len", "abs", "sign", "floor", "ceil", "round",
        "min", "max", "clamp", "lerp", "inverse_lerp", "range_lerp", "smoothstep",
        "sqrt", "pow", "exp", "log", "sin", "cos", "tan", "asin", "acos", "atan", "atan2",
        "deg_to_rad", "rad_to_deg", "fmod", "fposmod", "posmod",
        "typeof", "type_exists", "is_instance_valid", "is_instance_of",
        "weakref", "funcref", "convert", "hash", "seed", "randomize", "randi", "randf", "randf_range", "randi_range",
        "var_to_str", "str_to_var", "var_to_bytes", "bytes_to_var"
    };

    // Common Godot types for type annotation
    private static readonly string[] CommonTypes = {
        "void", "bool", "int", "float", "String", "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
        "Rect2", "Rect2i", "Transform2D", "Transform3D", "Plane", "Quaternion", "AABB", "Basis", "Projection",
        "Color", "NodePath", "StringName", "RID", "Object", "Callable", "Signal",
        "Dictionary", "Array", "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
        "PackedFloat32Array", "PackedFloat64Array", "PackedStringArray",
        "PackedVector2Array", "PackedVector3Array", "PackedColorArray",
        "Node", "Node2D", "Node3D", "Control", "Resource", "RefCounted"
    };

    public CompletionService(GDProjectMap projectMap, GDTypeResolver typeResolver, GDGodotTypesProvider godotTypesProvider)
    {
        _projectMap = projectMap;
        _typeResolver = typeResolver;
        _godotTypesProvider = godotTypesProvider;
    }

    /// <summary>
    /// Gets completion items for the given context.
    /// </summary>
    public IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context)
    {
        if (context.ShouldSuppress)
            return Array.Empty<CompletionItem>();

        var items = new List<CompletionItem>();

        switch (context.CompletionType)
        {
            case CompletionType.MemberAccess:
                items.AddRange(GetMemberCompletions(context));
                break;

            case CompletionType.TypeAnnotation:
                items.AddRange(GetTypeCompletions(context));
                break;

            case CompletionType.Symbol:
            default:
                items.AddRange(GetSymbolCompletions(context));
                break;
        }

        // Filter by prefix
        if (!string.IsNullOrEmpty(context.WordPrefix))
        {
            items = FilterByPrefix(items, context.WordPrefix);
        }

        // Sort by priority
        return items
            .OrderBy(i => i.SortPriority)
            .ThenBy(i => i.Label)
            .ToList();
    }

    /// <summary>
    /// Gets member completions for a type (after '.').
    /// </summary>
    private IEnumerable<CompletionItem> GetMemberCompletions(CompletionContext context)
    {
        var typeName = context.MemberAccessType;
        if (string.IsNullOrEmpty(typeName))
        {
            // Try to infer from expression directly
            typeName = TryInferTypeFromExpression(context);
        }

        if (string.IsNullOrEmpty(typeName))
            yield break;

        // Get type info from runtime provider
        var typeInfo = _typeResolver.GetTypeInfo(typeName);
        if (typeInfo == null)
            yield break;

        // Add all members
        foreach (var member in typeInfo.Members)
        {
            var item = member.Kind switch
            {
                GDRuntimeMemberKind.Method => CompletionItem.Method(
                    member.Name,
                    member.Type ?? "Variant",
                    null,
                    CompletionSource.GodotApi),
                GDRuntimeMemberKind.Property => CompletionItem.Property(
                    member.Name,
                    member.Type ?? "Variant",
                    CompletionSource.GodotApi),
                GDRuntimeMemberKind.Signal => CompletionItem.Signal(
                    member.Name,
                    CompletionSource.GodotApi),
                GDRuntimeMemberKind.Constant => CompletionItem.Constant(
                    member.Name,
                    member.Type,
                    CompletionSource.GodotApi),
                _ => null
            };

            if (item != null)
                yield return item;
        }

        // Also check base type
        if (!string.IsNullOrEmpty(typeInfo.BaseType))
        {
            foreach (var item in GetMemberCompletionsForType(typeInfo.BaseType))
            {
                yield return item;
            }
        }
    }

    private IEnumerable<CompletionItem> GetMemberCompletionsForType(string typeName)
    {
        var typeInfo = _typeResolver.GetTypeInfo(typeName);
        if (typeInfo == null)
            yield break;

        foreach (var member in typeInfo.Members)
        {
            var item = member.Kind switch
            {
                GDRuntimeMemberKind.Method => CompletionItem.Method(
                    member.Name,
                    member.Type ?? "Variant",
                    null,
                    CompletionSource.GodotApi),
                GDRuntimeMemberKind.Property => CompletionItem.Property(
                    member.Name,
                    member.Type ?? "Variant",
                    CompletionSource.GodotApi),
                GDRuntimeMemberKind.Signal => CompletionItem.Signal(
                    member.Name,
                    CompletionSource.GodotApi),
                GDRuntimeMemberKind.Constant => CompletionItem.Constant(
                    member.Name,
                    member.Type,
                    CompletionSource.GodotApi),
                _ => null
            };

            if (item != null)
                yield return item;
        }

        if (!string.IsNullOrEmpty(typeInfo.BaseType))
        {
            foreach (var item in GetMemberCompletionsForType(typeInfo.BaseType))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Gets type completions for type annotations.
    /// </summary>
    private IEnumerable<CompletionItem> GetTypeCompletions(CompletionContext context)
    {
        // Add common types
        foreach (var typeName in CommonTypes)
        {
            yield return CompletionItem.Class(typeName, CompletionSource.GodotApi);
        }

        // Add project types
        foreach (var scriptMap in _projectMap.Scripts)
        {
            if (!string.IsNullOrEmpty(scriptMap.TypeName))
            {
                yield return CompletionItem.Class(scriptMap.TypeName, CompletionSource.Project);
            }
        }
    }

    /// <summary>
    /// Gets symbol completions (variables, functions, types).
    /// </summary>
    private IEnumerable<CompletionItem> GetSymbolCompletions(CompletionContext context)
    {
        // 1. Local symbols from current script (highest priority)
        foreach (var item in GetLocalSymbols(context))
        {
            yield return item;
        }

        // 2. Built-in functions
        foreach (var func in BuiltInFunctions)
        {
            yield return CompletionItem.Method(func, "Variant", null, CompletionSource.BuiltIn);
        }

        // 3. Keywords (if at start of line or after certain tokens)
        if (ShouldSuggestKeywords(context))
        {
            foreach (var keyword in Keywords)
            {
                yield return CompletionItem.Keyword(keyword);
            }
        }

        // 4. Project types
        foreach (var scriptMap in _projectMap.Scripts)
        {
            if (!string.IsNullOrEmpty(scriptMap.TypeName))
            {
                yield return CompletionItem.Class(scriptMap.TypeName, CompletionSource.Project);
            }
        }

        // 5. Common snippets
        yield return CompletionItem.Snippet("for", "for i in range(10):\n\t", "for loop");
        yield return CompletionItem.Snippet("while", "while true:\n\t", "while loop");
        yield return CompletionItem.Snippet("if", "if true:\n\t", "if statement");
        yield return CompletionItem.Snippet("func", "func _():\n\t", "function definition");
        yield return CompletionItem.Snippet("ready", "func _ready():\n\t", "_ready function");
        yield return CompletionItem.Snippet("process", "func _process(delta):\n\t", "_process function");
        yield return CompletionItem.Snippet("physics_process", "func _physics_process(delta):\n\t", "_physics_process function");
    }

    /// <summary>
    /// Gets local symbols from the current script.
    /// </summary>
    private IEnumerable<CompletionItem> GetLocalSymbols(CompletionContext context)
    {
        var analyzer = context.ScriptMap?.Analyzer;
        if (analyzer == null)
            yield break;

        foreach (var symbol in analyzer.Symbols)
        {
            CompletionItem item = symbol.Kind switch
            {
                GDSymbolKind.Variable => CompletionItem.Variable(
                    symbol.Name,
                    symbol.TypeName,
                    CompletionSource.Script),
                GDSymbolKind.Method => CompletionItem.Method(
                    symbol.Name,
                    symbol.TypeName ?? "Variant",
                    null,
                    CompletionSource.Script),
                GDSymbolKind.Signal => CompletionItem.Signal(
                    symbol.Name,
                    CompletionSource.Script),
                GDSymbolKind.Constant => CompletionItem.Constant(
                    symbol.Name,
                    symbol.TypeName,
                    CompletionSource.Script),
                GDSymbolKind.Enum => CompletionItem.Class(
                    symbol.Name,
                    CompletionSource.Script),
                GDSymbolKind.EnumValue => CompletionItem.EnumValue(
                    symbol.Name,
                    null,
                    CompletionSource.Script),
                GDSymbolKind.Class => CompletionItem.Class(
                    symbol.Name,
                    CompletionSource.Script),
                GDSymbolKind.Parameter => CompletionItem.Variable(
                    symbol.Name,
                    symbol.TypeName,
                    CompletionSource.Local),
                _ => null
            };

            if (item != null)
                yield return item;
        }
    }

    private static bool ShouldSuggestKeywords(CompletionContext context)
    {
        var trimmed = context.TextBeforeCursor.TrimStart();

        // Suggest keywords at start of line or after certain patterns
        return trimmed.Length == 0 ||
               trimmed == context.WordPrefix ||
               trimmed.EndsWith("\t") ||
               trimmed.EndsWith(" ");
    }

    private string? TryInferTypeFromExpression(CompletionContext context)
    {
        var expression = context.MemberAccessExpression;
        if (string.IsNullOrEmpty(expression))
            return null;

        // Check for common patterns
        if (expression == "self")
        {
            // Return the current class type
            return context.ScriptMap?.TypeName ?? context.ScriptMap?.Class?.Extends?.Type?.BuildName() ?? "RefCounted";
        }

        // Check local symbols
        var analyzer = context.ScriptMap?.Analyzer;
        if (analyzer != null)
        {
            foreach (var symbol in analyzer.Symbols)
            {
                if (symbol.Name == expression && !string.IsNullOrEmpty(symbol.TypeName))
                {
                    return symbol.TypeName;
                }
            }
        }

        return null;
    }

    private static List<CompletionItem> FilterByPrefix(List<CompletionItem> items, string prefix)
    {
        var lowerPrefix = prefix.ToLowerInvariant();

        return items
            .Where(i =>
            {
                var lowerLabel = i.Label.ToLowerInvariant();
                // Match: starts with prefix, or contains prefix (fuzzy)
                return lowerLabel.StartsWith(lowerPrefix) ||
                       lowerLabel.Contains(lowerPrefix);
            })
            .OrderBy(i =>
            {
                var lowerLabel = i.Label.ToLowerInvariant();
                // Prioritize exact prefix matches
                if (lowerLabel.StartsWith(lowerPrefix))
                    return 0;
                return 1;
            })
            .ThenBy(i => i.SortPriority)
            .ToList();
    }
}
