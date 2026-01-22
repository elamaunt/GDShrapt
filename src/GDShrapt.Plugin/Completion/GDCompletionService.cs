using GDShrapt.CLI.Core;

namespace GDShrapt.Plugin;

/// <summary>
/// Service that provides code completion suggestions.
/// </summary>
internal class GDCompletionService
{
    private readonly GDScriptProject _scriptProject;
    private readonly GDTypeResolver _typeResolver;
    private readonly IGDSymbolsHandler _symbolsHandler;

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

    public GDCompletionService(GDScriptProject scriptProject, GDTypeResolver typeResolver, IGDSymbolsHandler symbolsHandler)
    {
        _scriptProject = scriptProject;
        _typeResolver = typeResolver;
        _symbolsHandler = symbolsHandler;
    }

    /// <summary>
    /// Gets completion items for the given context.
    /// </summary>
    public IReadOnlyList<GDCompletionItem> GetCompletions(GDCompletionContext context)
    {
        if (context.ShouldSuppress)
            return Array.Empty<GDCompletionItem>();

        var items = new List<GDCompletionItem>();

        switch (context.GDCompletionType)
        {
            case GDCompletionType.MemberAccess:
                items.AddRange(GetMemberCompletions(context));
                break;

            case GDCompletionType.TypeAnnotation:
                items.AddRange(GetTypeCompletions(context));
                break;

            case GDCompletionType.Symbol:
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
    private IEnumerable<GDCompletionItem> GetMemberCompletions(GDCompletionContext context)
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
                GDRuntimeMemberKind.Method => GDCompletionItem.Method(
                    member.Name,
                    member.Type ?? "Variant",
                    null,
                    GDCompletionSource.GodotApi),
                GDRuntimeMemberKind.Property => GDCompletionItem.Property(
                    member.Name,
                    member.Type ?? "Variant",
                    GDCompletionSource.GodotApi),
                GDRuntimeMemberKind.Signal => GDCompletionItem.Signal(
                    member.Name,
                    GDCompletionSource.GodotApi),
                GDRuntimeMemberKind.Constant => GDCompletionItem.Constant(
                    member.Name,
                    member.Type,
                    GDCompletionSource.GodotApi),
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

    private IEnumerable<GDCompletionItem> GetMemberCompletionsForType(string typeName)
    {
        var typeInfo = _typeResolver.GetTypeInfo(typeName);
        if (typeInfo == null)
            yield break;

        foreach (var member in typeInfo.Members)
        {
            var item = member.Kind switch
            {
                GDRuntimeMemberKind.Method => GDCompletionItem.Method(
                    member.Name,
                    member.Type ?? "Variant",
                    null,
                    GDCompletionSource.GodotApi),
                GDRuntimeMemberKind.Property => GDCompletionItem.Property(
                    member.Name,
                    member.Type ?? "Variant",
                    GDCompletionSource.GodotApi),
                GDRuntimeMemberKind.Signal => GDCompletionItem.Signal(
                    member.Name,
                    GDCompletionSource.GodotApi),
                GDRuntimeMemberKind.Constant => GDCompletionItem.Constant(
                    member.Name,
                    member.Type,
                    GDCompletionSource.GodotApi),
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
    private IEnumerable<GDCompletionItem> GetTypeCompletions(GDCompletionContext context)
    {
        // Add common types
        foreach (var typeName in CommonTypes)
        {
            yield return GDCompletionItem.Class(typeName, GDCompletionSource.GodotApi);
        }

        // Add project types
        foreach (var ScriptFile in _scriptProject.ScriptFiles)
        {
            if (!string.IsNullOrEmpty(ScriptFile.TypeName))
            {
                yield return GDCompletionItem.Class(ScriptFile.TypeName, GDCompletionSource.Project);
            }
        }
    }

    /// <summary>
    /// Gets symbol completions (variables, functions, types).
    /// </summary>
    private IEnumerable<GDCompletionItem> GetSymbolCompletions(GDCompletionContext context)
    {
        // 1. Local symbols from current script (highest priority)
        foreach (var item in GetLocalSymbols(context))
        {
            yield return item;
        }

        // 2. Built-in functions
        foreach (var func in BuiltInFunctions)
        {
            yield return GDCompletionItem.Method(func, "Variant", null, GDCompletionSource.BuiltIn);
        }

        // 3. Keywords (if at start of line or after certain tokens)
        if (ShouldSuggestKeywords(context))
        {
            foreach (var keyword in Keywords)
            {
                yield return GDCompletionItem.Keyword(keyword);
            }
        }

        // 4. Project types
        foreach (var ScriptFile in _scriptProject.ScriptFiles)
        {
            if (!string.IsNullOrEmpty(ScriptFile.TypeName))
            {
                yield return GDCompletionItem.Class(ScriptFile.TypeName, GDCompletionSource.Project);
            }
        }

        // 5. Common snippets
        yield return GDCompletionItem.Snippet("for", "for i in range(10):\n\t", "for loop");
        yield return GDCompletionItem.Snippet("while", "while true:\n\t", "while loop");
        yield return GDCompletionItem.Snippet("if", "if true:\n\t", "if statement");
        yield return GDCompletionItem.Snippet("func", "func _():\n\t", "function definition");
        yield return GDCompletionItem.Snippet("ready", "func _ready():\n\t", "_ready function");
        yield return GDCompletionItem.Snippet("process", "func _process(delta):\n\t", "_process function");
        yield return GDCompletionItem.Snippet("physics_process", "func _physics_process(delta):\n\t", "_physics_process function");
    }

    /// <summary>
    /// Gets local symbols from the current script via IGDSymbolsHandler (Rule 11).
    /// </summary>
    private IEnumerable<GDCompletionItem> GetLocalSymbols(GDCompletionContext context)
    {
        var filePath = context.ScriptFile?.FullPath;
        if (string.IsNullOrEmpty(filePath))
            yield break;

        // Use IGDSymbolsHandler instead of direct SemanticModel access (Rule 11)
        var symbols = _symbolsHandler.GetSymbols(filePath);
        foreach (var symbol in symbols)
        {
            GDCompletionItem item = symbol.Kind switch
            {
                GDSymbolKind.Variable => GDCompletionItem.Variable(
                    symbol.Name,
                    symbol.Type,
                    GDCompletionSource.Script),
                GDSymbolKind.Property => GDCompletionItem.Property(
                    symbol.Name,
                    symbol.Type,
                    GDCompletionSource.Script),
                GDSymbolKind.Method => GDCompletionItem.Method(
                    symbol.Name,
                    symbol.Type ?? "Variant",
                    null,
                    GDCompletionSource.Script),
                GDSymbolKind.Signal => GDCompletionItem.Signal(
                    symbol.Name,
                    GDCompletionSource.Script),
                GDSymbolKind.Constant => GDCompletionItem.Constant(
                    symbol.Name,
                    symbol.Type,
                    GDCompletionSource.Script),
                GDSymbolKind.Enum => GDCompletionItem.Class(
                    symbol.Name,
                    GDCompletionSource.Script),
                GDSymbolKind.EnumValue => GDCompletionItem.EnumValue(
                    symbol.Name,
                    null,
                    GDCompletionSource.Script),
                GDSymbolKind.Class => GDCompletionItem.Class(
                    symbol.Name,
                    GDCompletionSource.Script),
                GDSymbolKind.Parameter => GDCompletionItem.Variable(
                    symbol.Name,
                    symbol.Type,
                    GDCompletionSource.Local),
                _ => null
            };

            if (item != null)
                yield return item;
        }
    }

    private static bool ShouldSuggestKeywords(GDCompletionContext context)
    {
        var trimmed = context.TextBeforeCursor.TrimStart();

        // Suggest keywords at start of line or after certain patterns
        return trimmed.Length == 0 ||
               trimmed == context.WordPrefix ||
               trimmed.EndsWith("\t") ||
               trimmed.EndsWith(" ");
    }

    private string? TryInferTypeFromExpression(GDCompletionContext context)
    {
        var expression = context.MemberAccessExpression;
        if (string.IsNullOrEmpty(expression))
            return null;

        // Check for common patterns
        if (expression == "self")
        {
            // Return the current class type
            return context.ScriptFile?.TypeName ?? context.ScriptFile?.Class?.Extends?.Type?.BuildName() ?? "RefCounted";
        }

        // Use IGDSymbolsHandler.FindSymbolByName instead of direct SemanticModel access (Rule 11)
        var filePath = context.ScriptFile?.FullPath;
        if (!string.IsNullOrEmpty(filePath))
        {
            var symbol = _symbolsHandler.FindSymbolByName(expression, filePath);
            if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName))
            {
                return symbol.TypeName;
            }
        }

        return null;
    }

    private static List<GDCompletionItem> FilterByPrefix(List<GDCompletionItem> items, string prefix)
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
