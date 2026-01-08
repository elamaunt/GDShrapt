using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.LSP.Adapters;
using GDShrapt.LSP.Protocol.Types;
using GDShrapt.LSP.Server;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.LSP.Handlers;

/// <summary>
/// Handles textDocument/completion requests.
/// </summary>
public class GDCompletionHandler
{
    private readonly GDScriptProject _project;
    private readonly GDGodotTypesProvider _godotTypes;

    // GDScript keywords
    private static readonly string[] Keywords =
    [
        "if", "elif", "else", "for", "while", "match", "break", "continue", "pass", "return",
        "class", "class_name", "extends", "is", "as", "self", "signal", "func", "static",
        "const", "enum", "var", "onready", "export", "setget", "tool", "master", "puppet",
        "slave", "remotesync", "sync", "remote", "in", "and", "or", "not", "true", "false",
        "null", "await", "yield", "preload", "load", "assert", "breakpoint", "super"
    ];

    // Built-in types
    private static readonly string[] BuiltinTypes =
    [
        "int", "float", "bool", "String", "Vector2", "Vector2i", "Vector3", "Vector3i",
        "Vector4", "Vector4i", "Rect2", "Rect2i", "Transform2D", "Transform3D",
        "Color", "NodePath", "RID", "Object", "Callable", "Signal", "Dictionary", "Array",
        "PackedByteArray", "PackedInt32Array", "PackedInt64Array", "PackedFloat32Array",
        "PackedFloat64Array", "PackedStringArray", "PackedVector2Array", "PackedVector3Array",
        "PackedColorArray", "StringName", "Basis", "Quaternion", "AABB", "Plane", "Projection"
    ];

    public GDCompletionHandler(GDScriptProject project)
    {
        _project = project;
        _godotTypes = new GDGodotTypesProvider();
    }

    public Task<GDLspCompletionList?> HandleAsync(GDCompletionParams @params, CancellationToken cancellationToken)
    {
        var items = new List<GDLspCompletionItem>();

        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var script = _project.GetScript(filePath);

        // Get trigger context
        var triggerKind = @params.Context?.TriggerKind ?? GDLspCompletionTriggerKind.Invoked;
        var triggerChar = @params.Context?.TriggerCharacter;

        // Member access completion (after '.')
        if (triggerKind == GDLspCompletionTriggerKind.TriggerCharacter && triggerChar == ".")
        {
            // Try to get members for the expression before the dot
            if (script?.Analyzer != null && script.Class != null)
            {
                var line = @params.Position.Line + 1;
                var column = @params.Position.Character; // Before the dot

                // Find the node before the dot to determine type
                var node = GDNodeFinder.FindNodeAtPosition(script.Class, line, column);
                if (node != null)
                {
                    var type = script.Analyzer.GetTypeForNode(node);
                    if (!string.IsNullOrEmpty(type))
                    {
                        // Add members from type resolver
                        AddTypeMembers(items, type);
                    }
                }
            }
        }
        else
        {
            // General completion - add keywords, types, and local symbols
            AddKeywords(items);
            AddBuiltinTypes(items);

            // Add local symbols from the current script
            if (script?.Analyzer != null)
            {
                AddLocalSymbols(items, script);
            }

            // Add global class names from the project
            AddGlobalClassNames(items);
        }

        var result = new GDLspCompletionList
        {
            IsIncomplete = false,
            Items = items.ToArray()
        };

        return Task.FromResult<GDLspCompletionList?>(result);
    }

    private void AddKeywords(List<GDLspCompletionItem> items)
    {
        foreach (var keyword in Keywords)
        {
            items.Add(new GDLspCompletionItem
            {
                Label = keyword,
                Kind = GDLspCompletionItemKind.Keyword,
                Detail = "keyword",
                SortText = "2_" + keyword
            });
        }
    }

    private void AddBuiltinTypes(List<GDLspCompletionItem> items)
    {
        foreach (var type in BuiltinTypes)
        {
            items.Add(new GDLspCompletionItem
            {
                Label = type,
                Kind = GDLspCompletionItemKind.Class,
                Detail = "built-in type",
                SortText = "3_" + type
            });
        }
    }

    private void AddLocalSymbols(List<GDLspCompletionItem> items, GDScriptFile script)
    {
        if (script.Analyzer == null)
            return;

        // Add methods
        foreach (var method in script.Analyzer.GetMethods())
        {
            items.Add(new GDLspCompletionItem
            {
                Label = method.Name,
                Kind = GDLspCompletionItemKind.Method,
                Detail = "func " + method.Name + "()",
                InsertText = method.Name,
                SortText = "0_" + method.Name
            });
        }

        // Add variables
        foreach (var variable in script.Analyzer.GetVariables())
        {
            items.Add(new GDLspCompletionItem
            {
                Label = variable.Name,
                Kind = variable.IsStatic ? GDLspCompletionItemKind.Constant : GDLspCompletionItemKind.Variable,
                Detail = (variable.IsStatic ? "const " : "var ") + variable.Name +
                         (!string.IsNullOrEmpty(variable.TypeName) ? ": " + variable.TypeName : ""),
                InsertText = variable.Name,
                SortText = "0_" + variable.Name
            });
        }

        // Add signals
        foreach (var signal in script.Analyzer.GetSignals())
        {
            items.Add(new GDLspCompletionItem
            {
                Label = signal.Name,
                Kind = GDLspCompletionItemKind.Event,
                Detail = "signal " + signal.Name,
                InsertText = signal.Name,
                SortText = "0_" + signal.Name
            });
        }

        // Add enums
        foreach (var enumSymbol in script.Analyzer.GetEnums())
        {
            items.Add(new GDLspCompletionItem
            {
                Label = enumSymbol.Name,
                Kind = GDLspCompletionItemKind.Enum,
                Detail = "enum " + enumSymbol.Name,
                InsertText = enumSymbol.Name,
                SortText = "0_" + enumSymbol.Name
            });
        }
    }

    private void AddGlobalClassNames(List<GDLspCompletionItem> items)
    {
        foreach (var script in _project.ScriptFiles)
        {
            if (script.IsGlobal && !string.IsNullOrEmpty(script.TypeName))
            {
                items.Add(new GDLspCompletionItem
                {
                    Label = script.TypeName,
                    Kind = GDLspCompletionItemKind.Class,
                    Detail = "class " + script.TypeName,
                    InsertText = script.TypeName,
                    SortText = "1_" + script.TypeName
                });
            }
        }
    }

    private void AddTypeMembers(List<GDLspCompletionItem> items, string typeName)
    {
        // Try to find the type in project scripts
        var script = _project.GetScriptByTypeName(typeName);
        if (script?.Analyzer != null)
        {
            AddLocalSymbols(items, script);
            return;
        }

        // Add members from Godot TypesMap for built-in types
        AddGodotTypeMembers(items, typeName);
    }

    private void AddGodotTypeMembers(List<GDLspCompletionItem> items, string typeName)
    {
        var typeInfo = _godotTypes.GetTypeInfo(typeName);
        if (typeInfo == null)
            return;

        foreach (var member in typeInfo.Members)
        {
            var kind = member.Kind switch
            {
                GDRuntimeMemberKind.Method => GDLspCompletionItemKind.Method,
                GDRuntimeMemberKind.Property => GDLspCompletionItemKind.Property,
                GDRuntimeMemberKind.Signal => GDLspCompletionItemKind.Event,
                GDRuntimeMemberKind.Constant => GDLspCompletionItemKind.Constant,
                _ => GDLspCompletionItemKind.Field
            };

            var detail = member.Kind switch
            {
                GDRuntimeMemberKind.Method => $"func {member.Name}() -> {member.Type}",
                GDRuntimeMemberKind.Property => $"var {member.Name}: {member.Type}",
                GDRuntimeMemberKind.Signal => $"signal {member.Name}",
                GDRuntimeMemberKind.Constant => $"const {member.Name}: {member.Type}",
                _ => member.Name
            };

            items.Add(new GDLspCompletionItem
            {
                Label = member.Name,
                Kind = kind,
                Detail = detail,
                InsertText = member.Kind == GDRuntimeMemberKind.Method
                    ? member.Name + "()"
                    : member.Name,
                SortText = "0_" + member.Name
            });
        }
    }
}
