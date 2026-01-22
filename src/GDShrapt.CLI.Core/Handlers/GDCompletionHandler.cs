using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code completion.
/// Extracted from Plugin GDCompletionService.
/// </summary>
public class GDCompletionHandler : IGDCompletionHandler
{
    protected readonly GDScriptProject _project;

    // GDScript keywords
    private static readonly string[] Keywords =
    {
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
    private static readonly string[] BuiltInFunctions =
    {
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
    private static readonly string[] CommonTypes =
    {
        "void", "bool", "int", "float", "String", "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
        "Rect2", "Rect2i", "Transform2D", "Transform3D", "Plane", "Quaternion", "AABB", "Basis", "Projection",
        "Color", "NodePath", "StringName", "RID", "Object", "Callable", "Signal",
        "Dictionary", "Array", "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
        "PackedFloat32Array", "PackedFloat64Array", "PackedStringArray",
        "PackedVector2Array", "PackedVector3Array", "PackedColorArray",
        "Node", "Node2D", "Node3D", "Control", "Resource", "RefCounted"
    };

    // Common snippets
    private static readonly (string Name, string InsertText, string Description)[] Snippets =
    {
        ("for", "for i in range(10):\n\t", "for loop"),
        ("while", "while true:\n\t", "while loop"),
        ("if", "if true:\n\t", "if statement"),
        ("func", "func _():\n\t", "function definition"),
        ("ready", "func _ready():\n\t", "_ready function"),
        ("process", "func _process(delta):\n\t", "_process function"),
        ("physics_process", "func _physics_process(delta):\n\t", "_physics_process function")
    };

    public GDCompletionHandler(GDScriptProject project)
    {
        _project = project;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCompletionItem> GetCompletions(GDCompletionRequest request)
    {
        var items = new List<GDCompletionItem>();

        switch (request.CompletionType)
        {
            case GDCompletionType.MemberAccess:
                if (!string.IsNullOrEmpty(request.MemberAccessType))
                    items.AddRange(GetMemberCompletions(request.MemberAccessType));
                break;

            case GDCompletionType.TypeAnnotation:
                items.AddRange(GetTypeCompletions());
                break;

            case GDCompletionType.Symbol:
            default:
                items.AddRange(GetSymbolCompletions(request));
                break;
        }

        // Filter by prefix
        if (!string.IsNullOrEmpty(request.WordPrefix))
        {
            items = FilterByPrefix(items, request.WordPrefix);
        }

        return items
            .OrderBy(i => i.SortPriority)
            .ThenBy(i => i.Label)
            .ToList();
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCompletionItem> GetMemberCompletions(string typeName)
    {
        // TODO: Use GDTypeResolver when available in CLI.Core
        // For now, return empty - Plugin will use its own type resolver
        return [];
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCompletionItem> GetTypeCompletions()
    {
        var items = new List<GDCompletionItem>();

        // Common types
        foreach (var typeName in CommonTypes)
        {
            items.Add(GDCompletionItem.Class(typeName, GDCompletionSource.GodotApi));
        }

        // Project types
        foreach (var file in _project.ScriptFiles)
        {
            if (!string.IsNullOrEmpty(file.TypeName))
            {
                items.Add(GDCompletionItem.Class(file.TypeName, GDCompletionSource.Project));
            }
        }

        return items;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCompletionItem> GetKeywordCompletions()
    {
        return Keywords.Select(k => GDCompletionItem.Keyword(k)).ToList();
    }

    private IEnumerable<GDCompletionItem> GetSymbolCompletions(GDCompletionRequest request)
    {
        // Local symbols from current file (via SemanticModel per Rule 11)
        var file = _project.GetScript(request.FilePath);
        var semanticModel = file?.SemanticModel;
        if (semanticModel != null)
        {
            foreach (var symbol in semanticModel.Symbols)
            {
                var item = MapSymbolToCompletionItem(symbol);
                if (item != null)
                    yield return item;
            }
        }

        // Built-in functions
        foreach (var func in BuiltInFunctions)
        {
            yield return GDCompletionItem.Method(func, "Variant", null, GDCompletionSource.BuiltIn);
        }

        // Keywords
        foreach (var keyword in Keywords)
        {
            yield return GDCompletionItem.Keyword(keyword);
        }

        // Built-in types (Godot API)
        foreach (var typeName in CommonTypes)
        {
            yield return GDCompletionItem.Class(typeName, GDCompletionSource.GodotApi);
        }

        // Project types
        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (!string.IsNullOrEmpty(scriptFile.TypeName))
            {
                yield return GDCompletionItem.Class(scriptFile.TypeName, GDCompletionSource.Project);
            }
        }

        // Snippets
        foreach (var (name, insertText, description) in Snippets)
        {
            yield return GDCompletionItem.Snippet(name, insertText, description);
        }
    }

    private static GDCompletionItem? MapSymbolToCompletionItem(Semantics.GDSymbolInfo symbol)
    {
        return symbol.Kind switch
        {
            GDSymbolKind.Variable => GDCompletionItem.Variable(symbol.Name, symbol.TypeName, GDCompletionSource.Script),
            GDSymbolKind.Method => GDCompletionItem.Method(symbol.Name, symbol.TypeName ?? "Variant", null, GDCompletionSource.Script),
            GDSymbolKind.Signal => GDCompletionItem.Signal(symbol.Name, GDCompletionSource.Script),
            GDSymbolKind.Constant => GDCompletionItem.Constant(symbol.Name, symbol.TypeName, GDCompletionSource.Script),
            GDSymbolKind.Enum => GDCompletionItem.Class(symbol.Name, GDCompletionSource.Script),
            GDSymbolKind.EnumValue => GDCompletionItem.EnumValue(symbol.Name, null, GDCompletionSource.Script),
            GDSymbolKind.Class => GDCompletionItem.Class(symbol.Name, GDCompletionSource.Script),
            GDSymbolKind.Parameter => GDCompletionItem.Variable(symbol.Name, symbol.TypeName, GDCompletionSource.Local),
            _ => null
        };
    }

    private static List<GDCompletionItem> FilterByPrefix(List<GDCompletionItem> items, string prefix)
    {
        var lowerPrefix = prefix.ToLowerInvariant();

        return items
            .Where(i =>
            {
                var lowerLabel = i.Label.ToLowerInvariant();
                return lowerLabel.StartsWith(lowerPrefix) || lowerLabel.Contains(lowerPrefix);
            })
            .OrderBy(i =>
            {
                var lowerLabel = i.Label.ToLowerInvariant();
                return lowerLabel.StartsWith(lowerPrefix) ? 0 : 1;
            })
            .ThenBy(i => i.SortPriority)
            .ToList();
    }
}
