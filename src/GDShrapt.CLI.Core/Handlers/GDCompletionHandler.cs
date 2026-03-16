using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code completion with context-aware filtering.
/// </summary>
public class GDCompletionHandler : IGDCompletionHandler
{
    protected readonly GDScriptProject _project;
    protected readonly IGDRuntimeProvider? _runtimeProvider;
    protected readonly GDProjectSemanticModel? _projectModel;
    protected readonly GDSceneTypesProvider? _sceneTypesProvider;

    // Cache for all runtime types
    private IReadOnlyList<string>? _allTypesCache;

    // GDScript keywords for method body context
    private static readonly string[] MethodBodyKeywords =
    {
        "if", "elif", "else", "for", "while", "match", "break", "continue",
        "pass", "return", "is", "in", "as", "self", "super",
        "await", "preload", "load", "assert", "breakpoint",
        "true", "false", "null", "PI", "TAU", "INF", "NAN",
        "and", "or", "not", "var", "const"
    };

    // GDScript keywords for class level context
    private static readonly string[] ClassLevelKeywords =
    {
        "var", "const", "func", "signal", "enum", "class", "class_name",
        "extends", "static", "tool"
    };

    // GDScript annotations
    private static readonly string[] Annotations =
    {
        "@export", "@export_range", "@export_enum", "@export_flags",
        "@export_file", "@export_dir", "@export_multiline",
        "@export_placeholder", "@export_color_no_alpha",
        "@export_node_path", "@export_flags_2d_physics",
        "@export_flags_2d_render", "@export_flags_2d_navigation",
        "@export_flags_3d_physics", "@export_flags_3d_render",
        "@export_flags_3d_navigation", "@export_category",
        "@export_group", "@export_subgroup",
        "@onready", "@tool", "@icon", "@warning_ignore"
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

    // Primitive/value types (not usable in extends)
    private static readonly HashSet<string> PrimitiveTypes = new(StringComparer.Ordinal)
    {
        "void", "bool", "int", "float", "String", "StringName", "NodePath",
        "Color", "RID", "Callable", "Signal", "Variant",
        "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
        "Rect2", "Rect2i", "Transform2D", "Transform3D", "Plane",
        "Quaternion", "AABB", "Basis", "Projection",
        "Dictionary", "Array",
        "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
        "PackedFloat32Array", "PackedFloat64Array", "PackedStringArray",
        "PackedVector2Array", "PackedVector3Array", "PackedColorArray"
    };

    // Common Godot types for type annotation fallback
    private static readonly string[] CommonTypes =
    {
        "void", "Variant", "bool", "int", "float", "String", "Vector2", "Vector2i",
        "Vector3", "Vector3i", "Vector4", "Vector4i",
        "Rect2", "Rect2i", "Transform2D", "Transform3D", "Plane", "Quaternion", "AABB", "Basis", "Projection",
        "Color", "NodePath", "StringName", "RID", "Object", "Callable", "Signal",
        "Dictionary", "Array", "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
        "PackedFloat32Array", "PackedFloat64Array", "PackedStringArray",
        "PackedVector2Array", "PackedVector3Array", "PackedColorArray",
        "Node", "Node2D", "Node3D", "Control", "Resource", "RefCounted"
    };

    // Snippets for method body
    private static readonly (string Name, string InsertText, string Description)[] MethodBodySnippets =
    {
        ("for", "for ${1:i} in range(${2:10}):\n\t${0:pass}", "for loop"),
        ("while", "while ${1:true}:\n\t${0:pass}", "while loop"),
        ("if", "if ${1:condition}:\n\t${0:pass}", "if statement"),
        ("match", "match ${1:value}:\n\t${2:pattern}:\n\t\t${0:pass}", "match statement")
    };

    // Snippets for class level
    private static readonly (string Name, string InsertText, string Description)[] ClassLevelSnippets =
    {
        ("func", "func ${1:name}(${2:}):\n\t${0:pass}", "function definition"),
        ("ready", "func _ready():\n\t${0:pass}", "_ready function"),
        ("process", "func _process(delta: float):\n\t${0:pass}", "_process function"),
        ("physics_process", "func _physics_process(delta: float):\n\t${0:pass}", "_physics_process function"),
        ("signal", "signal ${1:name}(${0:})", "signal declaration"),
        ("var", "var ${1:name}: ${2:type} = ${0:value}", "variable declaration"),
        ("const", "const ${1:NAME}: ${2:type} = ${0:value}", "constant declaration")
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

        // Resolve cursor context if not provided
        var context = request.CursorContext;
        if (context == GDCursorContext.Unknown && request.CompletionType == GDCompletionType.Symbol)
        {
            var file = _project.GetScript(request.FilePath);
            var semanticModel = file?.SemanticModel;
            var line0 = request.Line - 1;
            var col0 = request.Column - 1;
            context = GDCompletionContextResolver.Resolve(semanticModel, line0, col0, request.TextBeforeCursor);
        }

        // Suppress completions in strings and comments
        if (context == GDCursorContext.StringLiteral || context == GDCursorContext.Comment)
            return [];

        switch (request.CompletionType)
        {
            case GDCompletionType.MemberAccess:
                items.AddRange(GetMemberAccessCompletions(request));
                break;

            case GDCompletionType.TypeAnnotation:
                items.AddRange(GetContextualTypeCompletions(context));
                break;

            case GDCompletionType.NodePath:
                items.AddRange(GetNodePathCompletions(request.FilePath, request.NodePathPrefix));
                break;

            case GDCompletionType.Symbol:
            default:
                items.AddRange(GetContextualSymbolCompletions(request, context));
                break;
        }

        // Filter by prefix
        if (!string.IsNullOrEmpty(request.WordPrefix))
            items = FilterByPrefix(items, request.WordPrefix);

        return items
            .OrderBy(i => i.SortPriority)
            .ThenBy(i => i.Label)
            .ToList();
    }

    #region Context-Aware Symbol Completions

    private IEnumerable<GDCompletionItem> GetContextualSymbolCompletions(GDCompletionRequest request, GDCursorContext context)
    {
        return context switch
        {
            GDCursorContext.ClassLevel => GetClassLevelCompletions(request),
            GDCursorContext.MethodBody => GetMethodBodyCompletions(request),
            GDCursorContext.ExtendsClause => GetExtendsCompletions(),
            GDCursorContext.TypeAnnotation => GetContextualTypeCompletions(context),
            GDCursorContext.Annotation => GetAnnotationCompletions(),
            GDCursorContext.FuncCallArgs => GetFuncCallArgCompletions(request),
            GDCursorContext.MatchPattern => GetMatchPatternCompletions(request),
            GDCursorContext.EnumBody => Enumerable.Empty<GDCompletionItem>(),
            GDCursorContext.DictionaryKey => Enumerable.Empty<GDCompletionItem>(),
            _ => GetMethodBodyCompletions(request) // Fallback to method body (most common)
        };
    }

    private IEnumerable<GDCompletionItem> GetClassLevelCompletions(GDCompletionRequest request)
    {
        // Class-level keywords
        foreach (var kw in ClassLevelKeywords)
            yield return GDCompletionItem.Keyword(kw);

        // Annotations
        foreach (var ann in Annotations)
        {
            yield return new GDCompletionItem
            {
                Label = ann,
                Kind = GDCompletionItemKind.Keyword,
                InsertText = ann,
                SortPriority = 5,
                Source = GDCompletionSource.BuiltIn
            };
        }

        // Class-level snippets
        foreach (var (name, insertText, description) in ClassLevelSnippets)
            yield return GDCompletionItem.Snippet(name, insertText, description);

        // Override methods
        foreach (var item in GetOverrideMethodCompletions(request.FilePath))
            yield return item;

        // Class member symbols (for reference, e.g. typing after @export var x = other_var)
        var file = _project.GetScript(request.FilePath);
        var semanticModel = file?.SemanticModel;
        if (semanticModel != null)
        {
            foreach (var symbol in semanticModel.Symbols)
            {
                if (IsClassMember(symbol))
                {
                    var resolvedType = ResolveDisplayType(symbol, semanticModel);
                    var item = MapSymbolToCompletionItem(symbol, resolvedType, GDCursorContext.ClassLevel);
                    if (item != null)
                        yield return item;
                }
            }
        }
    }

    private IEnumerable<GDCompletionItem> GetMethodBodyCompletions(GDCompletionRequest request)
    {
        var file = _project.GetScript(request.FilePath);
        var semanticModel = file?.SemanticModel;
        var line0 = request.Line - 1;
        var col0 = request.Column - 1;

        // Scoped symbols from current file
        if (semanticModel != null)
        {
            // Find containing method for scope filtering
            var contextNode = semanticModel.GetNodeAtPosition(line0, col0);
            GDMethodDeclaration? containingMethod = FindContainingMethod(contextNode);

            foreach (var symbol in semanticModel.Symbols)
            {
                if (IsVisibleAtPosition(symbol, containingMethod, line0))
                {
                    var resolvedType = ResolveDisplayType(symbol, semanticModel);
                    var item = MapSymbolToCompletionItem(symbol, resolvedType, GDCursorContext.MethodBody);
                    if (item != null)
                        yield return item;
                }
            }
        }

        // Built-in functions
        foreach (var func in BuiltInFunctions)
            yield return GDCompletionItem.Method(func, "Variant", null, GDCompletionSource.BuiltIn);

        // Method body keywords
        foreach (var kw in MethodBodyKeywords)
            yield return GDCompletionItem.Keyword(kw);

        // Common types (for type casting, is-checks, etc.)
        foreach (var typeName in CommonTypes)
            yield return GDCompletionItem.Class(typeName, GDCompletionSource.GodotApi);

        // Project types
        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (!string.IsNullOrEmpty(scriptFile.TypeName))
                yield return GDCompletionItem.Class(scriptFile.TypeName, GDCompletionSource.Project);
        }

        // Method body snippets
        foreach (var (name, insertText, description) in MethodBodySnippets)
            yield return GDCompletionItem.Snippet(name, insertText, description);
    }

    private IEnumerable<GDCompletionItem> GetExtendsCompletions()
    {
        // Only class types (not primitives)
        var allTypes = GetAllRuntimeTypes();
        foreach (var typeName in allTypes)
        {
            if (!PrimitiveTypes.Contains(typeName))
            {
                var typeInfo = _runtimeProvider?.GetTypeInfo(typeName);
                // Only include types that can be extended (have base type or are classes)
                if (typeInfo != null)
                    yield return GDCompletionItem.Class(typeName, typeInfo.IsNative ? GDCompletionSource.GodotApi : GDCompletionSource.Project);
            }
        }

        // Project types
        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (!string.IsNullOrEmpty(scriptFile.TypeName))
                yield return GDCompletionItem.Class(scriptFile.TypeName, GDCompletionSource.Project);
        }
    }

    private IEnumerable<GDCompletionItem> GetAnnotationCompletions()
    {
        foreach (var ann in Annotations)
        {
            yield return new GDCompletionItem
            {
                Label = ann,
                Kind = GDCompletionItemKind.Keyword,
                InsertText = ann,
                SortPriority = 1,
                Source = GDCompletionSource.BuiltIn
            };
        }
    }

    private IEnumerable<GDCompletionItem> GetFuncCallArgCompletions(GDCompletionRequest request)
    {
        // Inside function call arguments: show variables and expressions that could be passed
        var file = _project.GetScript(request.FilePath);
        var semanticModel = file?.SemanticModel;
        var line0 = request.Line - 1;
        var col0 = request.Column - 1;

        if (semanticModel != null)
        {
            var contextNode = semanticModel.GetNodeAtPosition(line0, col0);
            GDMethodDeclaration? containingMethod = FindContainingMethod(contextNode);

            foreach (var symbol in semanticModel.Symbols)
            {
                if (IsVisibleAtPosition(symbol, containingMethod, line0))
                {
                    var resolvedType = ResolveDisplayType(symbol, semanticModel);
                    var item = MapSymbolToCompletionItem(symbol, resolvedType, GDCursorContext.FuncCallArgs);
                    if (item != null)
                        yield return item;
                }
            }
        }

        // Built-in functions (can be passed as arguments to higher-order functions)
        foreach (var func in BuiltInFunctions)
            yield return GDCompletionItem.Method(func, "Variant", null, GDCompletionSource.BuiltIn);

        // Constants
        yield return GDCompletionItem.Keyword("true");
        yield return GDCompletionItem.Keyword("false");
        yield return GDCompletionItem.Keyword("null");
        yield return GDCompletionItem.Keyword("self");
        yield return GDCompletionItem.Keyword("preload");
        yield return GDCompletionItem.Keyword("load");
    }

    private IEnumerable<GDCompletionItem> GetMatchPatternCompletions(GDCompletionRequest request)
    {
        // In match patterns: show type names (for type matching), enum values, constants
        var file = _project.GetScript(request.FilePath);
        var semanticModel = file?.SemanticModel;

        // Type names for pattern matching
        foreach (var typeName in CommonTypes)
            yield return GDCompletionItem.Class(typeName, GDCompletionSource.GodotApi);

        // Constants and enum values from current file
        if (semanticModel != null)
        {
            foreach (var symbol in semanticModel.Symbols)
            {
                if (symbol.Kind == GDSymbolKind.Constant || symbol.Kind == GDSymbolKind.EnumValue || symbol.Kind == GDSymbolKind.Enum)
                {
                    var resolvedType = ResolveDisplayType(symbol, semanticModel);
                    var item = MapSymbolToCompletionItem(symbol, resolvedType, GDCursorContext.MatchPattern);
                    if (item != null)
                        yield return item;
                }
            }
        }

        // Keywords used in match patterns
        yield return GDCompletionItem.Keyword("var");
        yield return GDCompletionItem.Keyword("when");
        yield return GDCompletionItem.Keyword("null");
        yield return GDCompletionItem.Keyword("true");
        yield return GDCompletionItem.Keyword("false");
    }

    #endregion

    #region Member Access Completions (Phase 2)

    private IReadOnlyList<GDCompletionItem> GetMemberAccessCompletions(GDCompletionRequest request)
    {
        var isSelfAccess = false;
        string? resolvedType = null;

        // Phase 2: Try AST-based resolution first
        var file = _project.GetScript(request.FilePath);
        var semanticModel = file?.SemanticModel;

        if (semanticModel != null)
        {
            var line0 = request.Line - 1;
            var col0 = request.Column - 1;

            resolvedType = ResolveExpressionTypeAtPosition(file!, semanticModel, line0, col0, out isSelfAccess);
        }

        // Fallback to text-based resolution
        if (string.IsNullOrEmpty(resolvedType))
        {
            resolvedType = request.MemberAccessType;

            if (string.IsNullOrEmpty(resolvedType) && !string.IsNullOrEmpty(request.MemberAccessExpression))
            {
                if (request.MemberAccessExpression == "self")
                    isSelfAccess = true;

                if (request.MemberAccessExpression == "super")
                {
                    resolvedType = file?.SemanticModel?.BaseTypeName;
                }
                else
                {
                    resolvedType = ResolveExpressionType(request.FilePath, request.MemberAccessExpression);
                }
            }
        }

        if (string.IsNullOrEmpty(resolvedType))
            return [];

        var items = new List<GDCompletionItem>();
        var visited = new HashSet<string>();
        CollectMembersRecursive(resolvedType, items, visited, filterPrivate: !isSelfAccess);

        // For self access, also add script-defined symbols
        if (isSelfAccess && semanticModel != null)
        {
            var addedNames = new HashSet<string>(items.Select(i => i.Label));
            foreach (var symbol in semanticModel.Symbols)
            {
                if (IsClassMember(symbol) && addedNames.Add(symbol.Name))
                {
                    var displayType = ResolveDisplayType(symbol, semanticModel);
                    var item = MapSymbolToCompletionItem(symbol, displayType, GDCursorContext.MethodBody);
                    if (item != null)
                        items.Add(item);
                }
            }
        }

        return items;
    }

    private string? ResolveExpressionTypeAtPosition(GDScriptFile file, GDSemanticModel semanticModel, int line0, int col0, out bool isSelfAccess)
    {
        isSelfAccess = false;

        // Try to get the node just before the dot (col0 - 1)
        var beforeDotCol = Math.Max(0, col0 - 1);
        var node = semanticModel.GetNodeAtPosition(line0, beforeDotCol);

        if (node == null)
            return null;

        // Walk up to find the expression that precedes the dot
        var current = node;
        while (current != null)
        {
            switch (current)
            {
                // Identifier expression: check for self/super, then flow-narrowed type, then symbol
                case GDIdentifierExpression identExpr:
                {
                    var name = identExpr.Identifier?.Sequence?.ToString();
                    if (string.IsNullOrEmpty(name))
                        break;

                    if (name == "self")
                    {
                        isSelfAccess = true;
                        return file.TypeName ?? semanticModel.BaseTypeName;
                    }

                    if (name == "super")
                    {
                        return semanticModel.BaseTypeName;
                    }

                    // Flow-narrowed type (highest priority)
                    var flowType = semanticModel.GetVariableTypeAt(name, identExpr);
                    if (flowType != null)
                    {
                        var effectiveDisplayName = flowType.EffectiveType?.DisplayName;
                        if (!string.IsNullOrEmpty(effectiveDisplayName))
                            return effectiveDisplayName;
                    }

                    // Symbol lookup
                    var symbol = semanticModel.FindSymbol(name);
                    if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName))
                        return symbol.TypeName;

                    // Static type access (e.g., Vector2.ZERO)
                    if (_runtimeProvider?.IsKnownType(name) == true)
                        return name;

                    return null;
                }

                // Member access expression: get type of the whole expression
                case GDMemberOperatorExpression memberAccess:
                {
                    var resolvedSemType = semanticModel.TypeSystem.GetType(memberAccess);
                    if (resolvedSemType != null && !resolvedSemType.IsVariant)
                        return resolvedSemType.DisplayName;
                    break;
                }

                // Call expression: get return type
                case GDCallExpression callExpr:
                {
                    var resolvedSemType = semanticModel.TypeSystem.GetType(callExpr);
                    if (resolvedSemType != null && !resolvedSemType.IsVariant)
                        return resolvedSemType.DisplayName;
                    break;
                }

                // Indexer expression: get element type
                case GDIndexerExpression indexer:
                {
                    var resolvedSemType = semanticModel.TypeSystem.GetType(indexer);
                    if (resolvedSemType != null && !resolvedSemType.IsVariant)
                        return resolvedSemType.DisplayName;
                    break;
                }

                // Cast expression (as): use target type from RightExpression
                case GDDualOperatorExpression dualOp when dualOp.OperatorType == GDDualOperatorType.As:
                {
                    var rightExpr = dualOp.RightExpression;
                    if (rightExpr is GDIdentifierExpression typeIdent)
                    {
                        var typeName = typeIdent.Identifier?.Sequence?.ToString();
                        if (!string.IsNullOrEmpty(typeName))
                            return typeName;
                    }
                    break;
                }

                // Expression-level node: try generic type resolution
                case GDExpression expr:
                {
                    var resolvedSemType = semanticModel.TypeSystem.GetType(expr);
                    if (resolvedSemType != null && !resolvedSemType.IsVariant)
                        return resolvedSemType.DisplayName;
                    break;
                }
            }

            // Don't walk past statement boundaries
            if (current is GDStatement)
                break;

            current = current.Parent as GDNode;
        }

        return null;
    }

    #endregion

    #region Type Completions (Phase 3)

    private IEnumerable<GDCompletionItem> GetContextualTypeCompletions(GDCursorContext context)
    {
        var added = new HashSet<string>(StringComparer.Ordinal);

        // All runtime types from provider
        var allTypes = GetAllRuntimeTypes();
        foreach (var typeName in allTypes)
        {
            // For extends: skip primitives
            if (context == GDCursorContext.ExtendsClause && PrimitiveTypes.Contains(typeName))
                continue;

            if (added.Add(typeName))
            {
                var typeInfo = _runtimeProvider?.GetTypeInfo(typeName);
                var source = typeInfo?.IsNative == true ? GDCompletionSource.GodotApi : GDCompletionSource.Project;
                yield return GDCompletionItem.Class(typeName, source);
            }
        }

        // Fallback common types if no runtime provider
        if (_runtimeProvider == null)
        {
            foreach (var typeName in CommonTypes)
            {
                if (added.Add(typeName))
                    yield return GDCompletionItem.Class(typeName, GDCompletionSource.GodotApi);
            }
        }

        // Add Variant explicitly
        if (added.Add("Variant"))
            yield return GDCompletionItem.Class("Variant", GDCompletionSource.GodotApi);

        // Project types
        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (!string.IsNullOrEmpty(scriptFile.TypeName) && added.Add(scriptFile.TypeName))
                yield return GDCompletionItem.Class(scriptFile.TypeName, GDCompletionSource.Project);
        }
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCompletionItem> GetTypeCompletions()
    {
        return GetContextualTypeCompletions(GDCursorContext.TypeAnnotation).ToList();
    }

    private IReadOnlyList<string> GetAllRuntimeTypes()
    {
        if (_allTypesCache != null)
            return _allTypesCache;

        if (_runtimeProvider == null)
        {
            _allTypesCache = CommonTypes;
            return _allTypesCache;
        }

        try
        {
            _allTypesCache = _runtimeProvider.GetAllTypes()?.ToList() ?? (IReadOnlyList<string>)CommonTypes;
        }
        catch
        {
            _allTypesCache = CommonTypes;
        }

        return _allTypesCache;
    }

    #endregion

    #region Member Collection

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCompletionItem> GetMemberCompletions(string typeName)
    {
        if (_runtimeProvider == null || string.IsNullOrEmpty(typeName))
            return [];

        var items = new List<GDCompletionItem>();
        var visited = new HashSet<string>();
        CollectMembersRecursive(typeName, items, visited, filterPrivate: false);
        return items;
    }

    private void CollectMembersRecursive(string typeName, List<GDCompletionItem> items, HashSet<string> visited, bool filterPrivate)
    {
        if (!visited.Add(typeName))
            return;

        if (_runtimeProvider == null)
            return;

        var typeInfo = _runtimeProvider.GetTypeInfo(typeName);
        if (typeInfo == null)
            return;

        var source = typeInfo.IsNative ? GDCompletionSource.GodotApi : GDCompletionSource.Project;
        var isFirstType = visited.Count == 1; // Direct type vs inherited

        if (typeInfo.Members != null)
        {
            foreach (var member in typeInfo.Members)
            {
                // Filter private members for non-self access
                if (filterPrivate && member.Name.StartsWith("_"))
                    continue;

                var memberPriority = isFirstType ? 0 : 5; // Boost direct type members

                var item = member.Kind switch
                {
                    GDRuntimeMemberKind.Method => new GDCompletionItem
                    {
                        Label = member.Name,
                        Kind = GDCompletionItemKind.Method,
                        Detail = member.Type ?? "Variant",
                        Documentation = BuildParameterSignature(member),
                        InsertText = member.Name + "()",
                        SortPriority = 10 + memberPriority,
                        Source = source
                    },
                    GDRuntimeMemberKind.Property => new GDCompletionItem
                    {
                        Label = member.Name,
                        Kind = GDCompletionItemKind.Property,
                        Detail = member.Type,
                        SortPriority = 5 + memberPriority,
                        Source = source
                    },
                    GDRuntimeMemberKind.Signal => new GDCompletionItem
                    {
                        Label = member.Name,
                        Kind = GDCompletionItemKind.Event,
                        SortPriority = 15 + memberPriority,
                        Source = source
                    },
                    GDRuntimeMemberKind.Constant => new GDCompletionItem
                    {
                        Label = member.Name,
                        Kind = GDCompletionItemKind.Constant,
                        Detail = member.Type,
                        SortPriority = 8 + memberPriority,
                        Source = source
                    },
                    _ => (GDCompletionItem?)null
                };

                if (item != null)
                    items.Add(item);
            }
        }

        if (!string.IsNullOrEmpty(typeInfo.BaseType))
            CollectMembersRecursive(typeInfo.BaseType, items, visited, filterPrivate);
    }

    #endregion

    #region Helpers

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
        if (string.IsNullOrEmpty(expression))
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

        var scenes = _sceneTypesProvider.GetScenesForScript(filePath);
        var sceneList = scenes.ToList();
        if (sceneList.Count == 0)
            return [];

        var items = new List<GDCompletionItem>();
        var added = new HashSet<string>();

        foreach (var (scenePath, _) in sceneList)
        {
            var parentPath = string.IsNullOrEmpty(partialPath) || partialPath == "$"
                ? "."
                : partialPath.TrimEnd('/');

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

        return items;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCompletionItem> GetKeywordCompletions()
    {
        return MethodBodyKeywords.Select(k => GDCompletionItem.Keyword(k)).ToList();
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

        if (_runtimeProvider == null)
            return;

        var typeInfo = _runtimeProvider.GetTypeInfo(typeName);
        if (typeInfo == null)
            return;

        if (typeInfo.Members != null)
        {
            foreach (var member in typeInfo.Members)
            {
                if (member.Kind != GDRuntimeMemberKind.Method)
                    continue;

                var isVirtual = member.Name.StartsWith("_") || member.IsAbstract;
                if (!isVirtual)
                    continue;

                if (existingMethods.Contains(member.Name))
                    continue;

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

                existingMethods.Add(member.Name);
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

    private static bool IsClassMember(Semantics.GDSymbolInfo symbol)
    {
        return symbol.Kind is GDSymbolKind.Variable or GDSymbolKind.Method or GDSymbolKind.Signal
            or GDSymbolKind.Constant or GDSymbolKind.Enum or GDSymbolKind.EnumValue or GDSymbolKind.Class
            && symbol.DeclaringScopeNode == null; // No scope node means class-level
    }

    private static bool IsVisibleAtPosition(Semantics.GDSymbolInfo symbol, GDMethodDeclaration? containingMethod, int line0)
    {
        // Class-level members are always visible
        if (IsClassMember(symbol))
            return true;

        // Local symbols: must be in same method scope and declared before cursor
        if (symbol.DeclaringScopeNode != null)
        {
            // Check same scope
            if (containingMethod != null && !ReferenceEquals(symbol.DeclaringScopeNode, containingMethod))
                return false;

            // Check declared before cursor position
            var declToken = symbol.PositionToken;
            if (declToken != null && declToken.StartLine > line0)
                return false;
        }

        // Parameters of containing method are always visible
        if (symbol.Kind == GDSymbolKind.Parameter)
        {
            if (containingMethod != null)
            {
                // Check if this parameter belongs to the containing method
                var paramNode = symbol.DeclarationNode;
                if (paramNode != null)
                {
                    var parent = paramNode.Parent;
                    while (parent != null)
                    {
                        if (ReferenceEquals(parent, containingMethod))
                            return true;
                        parent = parent.Parent as GDNode;
                    }
                    return false;
                }
            }
        }

        return true;
    }

    private static GDMethodDeclaration? FindContainingMethod(GDNode? node)
    {
        var current = node;
        while (current != null)
        {
            if (current is GDMethodDeclaration method)
                return method;
            current = current.Parent as GDNode;
        }
        return null;
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

    private static GDCompletionItem? MapSymbolToCompletionItem(Semantics.GDSymbolInfo symbol, string? resolvedType, GDCursorContext context)
    {
        // Adjust sort priority based on context
        int localVarPriority = context == GDCursorContext.MethodBody ? 1 : 5;
        int paramPriority = context == GDCursorContext.MethodBody ? 1 : 5;
        int classMemberPriority = context == GDCursorContext.MethodBody ? 5 : 10;

        return symbol.Kind switch
        {
            GDSymbolKind.Variable => new GDCompletionItem
            {
                Label = symbol.Name,
                Kind = GDCompletionItemKind.Variable,
                Detail = resolvedType,
                SortPriority = symbol.DeclaringScopeNode != null ? localVarPriority : classMemberPriority,
                Source = symbol.DeclaringScopeNode != null ? GDCompletionSource.Local : GDCompletionSource.Script
            },
            GDSymbolKind.Parameter => new GDCompletionItem
            {
                Label = symbol.Name,
                Kind = GDCompletionItemKind.Variable,
                Detail = resolvedType,
                SortPriority = paramPriority,
                Source = GDCompletionSource.Local
            },
            GDSymbolKind.Method => GDCompletionItem.Method(symbol.Name, resolvedType ?? "Variant", null, GDCompletionSource.Script),
            GDSymbolKind.Signal => GDCompletionItem.Signal(symbol.Name, GDCompletionSource.Script),
            GDSymbolKind.Constant => GDCompletionItem.Constant(symbol.Name, resolvedType, GDCompletionSource.Script),
            GDSymbolKind.Enum => GDCompletionItem.Class(symbol.Name, GDCompletionSource.Script),
            GDSymbolKind.EnumValue => GDCompletionItem.EnumValue(symbol.Name, null, GDCompletionSource.Script),
            GDSymbolKind.Class => GDCompletionItem.Class(symbol.Name, GDCompletionSource.Script),
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

    #endregion
}
