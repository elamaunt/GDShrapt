using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code completion.
/// Extracted from Plugin GDCompletionService.
/// </summary>
public class GDCompletionHandler : IGDCompletionHandler
{
    protected readonly GDScriptProject _project;
    protected readonly IGDRuntimeProvider? _runtimeProvider;
    protected readonly GDProjectSemanticModel? _projectModel;
    protected readonly GDSceneTypesProvider? _sceneTypesProvider;

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

    public GDCompletionHandler(GDScriptProject project, IGDRuntimeProvider? runtimeProvider = null, GDProjectSemanticModel? projectModel = null, GDSceneTypesProvider? sceneTypesProvider = null)
    {
        _project = project;
        _runtimeProvider = runtimeProvider;
        _projectModel = projectModel;
        _sceneTypesProvider = sceneTypesProvider;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCompletionItem> GetCompletions(GDCompletionRequest request)
    {
        var items = new List<GDCompletionItem>();

        switch (request.CompletionType)
        {
            case GDCompletionType.MemberAccess:
                var memberType = request.MemberAccessType;
                if (string.IsNullOrEmpty(memberType) && !string.IsNullOrEmpty(request.MemberAccessExpression))
                    memberType = ResolveExpressionType(request.FilePath, request.MemberAccessExpression);
                if (!string.IsNullOrEmpty(memberType))
                    items.AddRange(GetMemberCompletions(memberType));
                break;

            case GDCompletionType.TypeAnnotation:
                items.AddRange(GetTypeCompletions());
                break;

            case GDCompletionType.NodePath:
                items.AddRange(GetNodePathCompletions(request.FilePath, request.NodePathPrefix));
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
        if (_runtimeProvider == null || string.IsNullOrEmpty(typeName))
            return [];

        var items = new List<GDCompletionItem>();
        var visited = new HashSet<string>();
        CollectMembersRecursive(typeName, items, visited);
        return items;
    }

    private void CollectMembersRecursive(string typeName, List<GDCompletionItem> items, HashSet<string> visited)
    {
        if (!visited.Add(typeName))
            return;

        var typeInfo = _runtimeProvider!.GetTypeInfo(typeName);
        if (typeInfo == null)
            return;

        var source = typeInfo.IsNative ? GDCompletionSource.GodotApi : GDCompletionSource.Project;

        if (typeInfo.Members != null)
        {
            foreach (var member in typeInfo.Members)
            {
                var item = member.Kind switch
                {
                    GDRuntimeMemberKind.Method => GDCompletionItem.Method(
                        member.Name,
                        member.Type ?? "Variant",
                        BuildParameterSignature(member),
                        source),
                    GDRuntimeMemberKind.Property => GDCompletionItem.Property(
                        member.Name,
                        member.Type,
                        source),
                    GDRuntimeMemberKind.Signal => GDCompletionItem.Signal(
                        member.Name,
                        source),
                    GDRuntimeMemberKind.Constant => GDCompletionItem.Constant(
                        member.Name,
                        member.Type,
                        source),
                    _ => (GDCompletionItem?)null
                };

                if (item != null)
                    items.Add(item);
            }
        }

        if (!string.IsNullOrEmpty(typeInfo.BaseType))
            CollectMembersRecursive(typeInfo.BaseType, items, visited);
    }

    private static string? BuildParameterSignature(GDRuntimeMemberInfo member)
    {
        if (member.Parameters == null || member.Parameters.Count == 0)
            return null;

        var parts = new List<string>();
        foreach (var p in member.Parameters)
        {
            var part = !string.IsNullOrEmpty(p.Type)
                ? $"{p.Name}: {p.Type}"
                : p.Name;
            parts.Add(part);
        }

        return string.Join(", ", parts);
    }

    private string? ResolveExpressionType(string filePath, string expression)
    {
        if (_projectModel == null || string.IsNullOrEmpty(expression))
            return null;

        // Handle "self" keyword
        if (expression == "self")
        {
            var script = _project.GetScript(filePath);
            return script?.TypeName ?? script?.SemanticModel?.BaseTypeName;
        }

        // Try to resolve via semantic model
        var file = _project.GetScript(filePath);
        var semanticModel = file?.SemanticModel;
        if (semanticModel == null)
            return null;

        // Check if it's a simple identifier (local variable, parameter, etc.)
        var symbol = semanticModel.FindSymbol(expression);
        if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName))
            return symbol.TypeName;

        // Check if it's a known type name (static member access like Vector2.ZERO)
        if (_runtimeProvider?.IsKnownType(expression) == true)
            return expression;

        return null;
    }

    /// <summary>
    /// Gets node path completions for $ expressions.
    /// </summary>
    public virtual IReadOnlyList<GDCompletionItem> GetNodePathCompletions(string filePath, string? partialPath)
    {
        if (_sceneTypesProvider == null)
            return [];

        // Find scenes that use this script
        var scenes = _sceneTypesProvider.GetScenesForScript(filePath);
        var sceneList = scenes.ToList();
        if (sceneList.Count == 0)
            return [];

        var items = new List<GDCompletionItem>();
        var added = new HashSet<string>();

        foreach (var (scenePath, _) in sceneList)
        {
            if (string.IsNullOrEmpty(partialPath) || partialPath == "$")
            {
                // Show all top-level node paths
                var children = _sceneTypesProvider.GetDirectChildren(scenePath, ".");
                foreach (var child in children)
                {
                    if (added.Add(child.Name))
                    {
                        var nodeType = child.ScriptTypeName ?? child.NodeType;
                        items.Add(new GDCompletionItem
                        {
                            Label = child.Name,
                            Kind = GDCompletionItemKind.Variable,
                            Detail = nodeType,
                            InsertText = child.Name,
                            SortPriority = 1,
                            Source = GDCompletionSource.Project
                        });
                    }
                }
            }
            else
            {
                // Partial path like "Player/" — show children of that node
                var parentPath = partialPath.TrimEnd('/');
                var children = _sceneTypesProvider.GetDirectChildren(scenePath, parentPath);
                foreach (var child in children)
                {
                    if (added.Add(child.Name))
                    {
                        var nodeType = child.ScriptTypeName ?? child.NodeType;
                        items.Add(new GDCompletionItem
                        {
                            Label = child.Name,
                            Kind = GDCompletionItemKind.Variable,
                            Detail = nodeType,
                            InsertText = child.Name,
                            SortPriority = 1,
                            Source = GDCompletionSource.Project
                        });
                    }
                }
            }
        }

        return items;
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

    /// <summary>
    /// Gets override method completions for the current script's base type.
    /// </summary>
    public virtual IReadOnlyList<GDCompletionItem> GetOverrideMethodCompletions(string filePath)
    {
        if (_runtimeProvider == null)
            return [];

        var file = _project.GetScript(filePath);
        var semanticModel = file?.SemanticModel;
        if (semanticModel == null)
            return [];

        var baseType = semanticModel.BaseTypeName;
        if (string.IsNullOrEmpty(baseType))
            return [];

        // Collect existing method names in current script
        var existingMethods = new HashSet<string>();
        foreach (var symbol in semanticModel.Symbols)
        {
            if (symbol.Kind == GDSymbolKind.Method)
                existingMethods.Add(symbol.Name);
        }

        var items = new List<GDCompletionItem>();
        var visited = new HashSet<string>();
        CollectVirtualMethodsRecursive(baseType, items, visited, existingMethods);
        return items;
    }

    private void CollectVirtualMethodsRecursive(string typeName, List<GDCompletionItem> items, HashSet<string> visited, HashSet<string> existingMethods)
    {
        if (!visited.Add(typeName))
            return;

        var typeInfo = _runtimeProvider!.GetTypeInfo(typeName);
        if (typeInfo == null)
            return;

        if (typeInfo.Members != null)
        {
            foreach (var member in typeInfo.Members)
            {
                if (member.Kind != GDRuntimeMemberKind.Method)
                    continue;

                // Virtual methods in Godot are prefixed with _ or marked abstract
                var isVirtual = member.Name.StartsWith("_") || member.IsAbstract;
                if (!isVirtual)
                    continue;

                // Skip if already declared in current script
                if (existingMethods.Contains(member.Name))
                    continue;

                // Build snippet: func _method_name(params) -> ReturnType:\n\t${0:pass}
                var paramSnippet = BuildOverrideParams(member);
                var returnPart = !string.IsNullOrEmpty(member.Type) && member.Type != "void" && member.Type != "Variant"
                    ? $" -> {member.Type}"
                    : "";
                var insertText = $"func {member.Name}({paramSnippet}){returnPart}:\n\t${{0:pass}}";

                items.Add(new GDCompletionItem
                {
                    Label = member.Name,
                    Kind = GDCompletionItemKind.Method,
                    Detail = $"override from {typeName}",
                    InsertText = insertText,
                    Documentation = BuildParameterSignature(member),
                    SortPriority = 3,
                    IsSnippet = true,
                    Source = GDCompletionSource.GodotApi
                });

                existingMethods.Add(member.Name); // Avoid duplicates from inheritance chain
            }
        }

        if (!string.IsNullOrEmpty(typeInfo.BaseType))
            CollectVirtualMethodsRecursive(typeInfo.BaseType, items, visited, existingMethods);
    }

    private static string BuildOverrideParams(GDRuntimeMemberInfo member)
    {
        if (member.Parameters == null || member.Parameters.Count == 0)
            return "";

        var parts = new List<string>();
        for (int i = 0; i < member.Parameters.Count; i++)
        {
            var p = member.Parameters[i];
            var paramStr = !string.IsNullOrEmpty(p.Type)
                ? $"{p.Name}: {p.Type}"
                : p.Name;
            parts.Add(paramStr);
        }
        return string.Join(", ", parts);
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
                var resolvedType = ResolveDisplayType(symbol, semanticModel);
                var item = MapSymbolToCompletionItem(symbol, resolvedType);
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

        // Override method suggestions
        foreach (var item in GetOverrideMethodCompletions(request.FilePath))
        {
            yield return item;
        }
    }

    private static string? ResolveDisplayType(Semantics.GDSymbolInfo symbol, GDSemanticModel semanticModel)
    {
        if (!string.IsNullOrEmpty(symbol.TypeName))
            return symbol.TypeName;

        if ((symbol.Kind == GDSymbolKind.Variable || symbol.Kind == GDSymbolKind.Constant)
            && symbol.DeclarationNode is GDVariableDeclaration varDecl
            && varDecl.Initializer != null)
        {
            var typeInfo = semanticModel.TypeSystem.GetType(varDecl.Initializer);
            if (!typeInfo.IsVariant)
            {
                // Enrich plain container types with usage-based generic parameters
                if (typeInfo.IsContainer)
                {
                    var containerType = semanticModel.TypeSystem.GetContainerElementType(symbol.Name);
                    if (containerType != null && containerType.HasElementTypes)
                        return containerType.ToString();
                }

                return typeInfo.DisplayName;
            }
        }

        return null;
    }

    private static GDCompletionItem? MapSymbolToCompletionItem(Semantics.GDSymbolInfo symbol, string? resolvedType)
    {
        return symbol.Kind switch
        {
            GDSymbolKind.Variable => GDCompletionItem.Variable(symbol.Name, resolvedType, GDCompletionSource.Script),
            GDSymbolKind.Method => GDCompletionItem.Method(symbol.Name, resolvedType ?? "Variant", null, GDCompletionSource.Script),
            GDSymbolKind.Signal => GDCompletionItem.Signal(symbol.Name, GDCompletionSource.Script),
            GDSymbolKind.Constant => GDCompletionItem.Constant(symbol.Name, resolvedType, GDCompletionSource.Script),
            GDSymbolKind.Enum => GDCompletionItem.Class(symbol.Name, GDCompletionSource.Script),
            GDSymbolKind.EnumValue => GDCompletionItem.EnumValue(symbol.Name, null, GDCompletionSource.Script),
            GDSymbolKind.Class => GDCompletionItem.Class(symbol.Name, GDCompletionSource.Script),
            GDSymbolKind.Parameter => GDCompletionItem.Variable(symbol.Name, resolvedType, GDCompletionSource.Local),
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
