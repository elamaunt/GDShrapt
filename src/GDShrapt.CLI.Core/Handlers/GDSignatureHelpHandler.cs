using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for signature help.
/// Provides function signature information when typing function arguments.
/// </summary>
public class GDSignatureHelpHandler : IGDSignatureHelpHandler
{
    protected readonly GDScriptProject _project;

    public GDSignatureHelpHandler(GDScriptProject project)
    {
        _project = project;
    }

    /// <inheritdoc />
    public virtual GDSignatureHelpResult? GetSignatureHelp(string filePath, int line, int column)
    {
        var script = _project.GetScript(filePath);
        if (script?.Class == null || script.SemanticModel == null)
            return null;

        // Find the call expression containing the cursor
        var callExpression = FindEnclosingCallExpression(script.Class, line, column);
        if (callExpression == null)
            return null;

        // Get the method name from the call expression
        var methodName = GetMethodName(callExpression);
        if (string.IsNullOrEmpty(methodName))
            return null;

        // Find the method declaration
        var methodInfo = FindMethodInfo(script, callExpression, methodName);
        if (methodInfo == null)
            return null;

        // Calculate active parameter index based on cursor position
        var activeParameter = CalculateActiveParameter(callExpression, line, column);

        return new GDSignatureHelpResult
        {
            Signatures = [methodInfo],
            ActiveSignature = 0,
            ActiveParameter = activeParameter
        };
    }

    /// <summary>
    /// Finds the innermost GDCallExpression containing the cursor position.
    /// </summary>
    protected static GDCallExpression? FindEnclosingCallExpression(GDNode root, int line, int column)
    {
        GDCallExpression? result = null;

        foreach (var node in root.AllNodes)
        {
            if (node is GDCallExpression call)
            {
                if (IsPositionInCallArguments(call, line, column))
                {
                    if (result == null || IsMoreSpecific(call, result))
                    {
                        result = call;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if the cursor position is within the call's argument list.
    /// </summary>
    protected static bool IsPositionInCallArguments(GDCallExpression call, int line, int column)
    {
        var openBracket = call.OpenBracket;
        var closeBracket = call.CloseBracket;

        if (openBracket == null)
            return false;

        // After open bracket
        if (line < openBracket.EndLine || (line == openBracket.EndLine && column <= openBracket.EndColumn))
            return false;

        // Before close bracket (or no close bracket yet)
        if (closeBracket != null)
        {
            if (line > closeBracket.StartLine || (line == closeBracket.StartLine && column > closeBracket.StartColumn))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines if call1 is more specific (nested inside) than call2.
    /// </summary>
    protected static bool IsMoreSpecific(GDCallExpression call1, GDCallExpression call2)
    {
        if (call1.StartLine > call2.StartLine)
            return true;
        if (call1.StartLine == call2.StartLine && call1.StartColumn > call2.StartColumn)
            return true;
        return false;
    }

    /// <summary>
    /// Extracts the method name from a call expression.
    /// </summary>
    protected static string? GetMethodName(GDCallExpression call)
    {
        var caller = call.CallerExpression;

        // Direct function call: func_name()
        if (caller is GDIdentifierExpression idExpr)
        {
            return idExpr.Identifier?.ToString();
        }

        // Member access: obj.method_name()
        if (caller is GDMemberOperatorExpression memberExpr)
        {
            return memberExpr.Identifier?.ToString();
        }

        return null;
    }

    /// <summary>
    /// Finds method information from SemanticModel or built-in types.
    /// </summary>
    protected virtual GDSignatureInfo? FindMethodInfo(GDScriptFile script, GDCallExpression call, string methodName)
    {
        // First, try to find the method in the script's SemanticModel (per Rule 11)
        var semanticModel = script.SemanticModel;
        if (semanticModel != null)
        {
            var methods = semanticModel.Symbols.Where(s => s.Kind == GDSymbolKind.Method);
            foreach (var method in methods)
            {
                if (method.Name == methodName && method.DeclarationNode is GDMethodDeclaration methodDecl)
                {
                    return CreateSignatureFromDeclaration(methodDecl);
                }
            }
        }

        // Check if it's a method call on a typed object
        var caller = call.CallerExpression;
        if (caller is GDMemberOperatorExpression memberExpr && memberExpr.CallerExpression != null)
        {
            var callerType = GetExpressionTypeName(script, memberExpr.CallerExpression);
            if (!string.IsNullOrEmpty(callerType))
            {
                var builtinSignature = GetBuiltinMethodSignature(callerType, methodName);
                if (builtinSignature != null)
                    return builtinSignature;
            }
        }

        // Check built-in global functions
        var globalSignature = GetBuiltinGlobalFunctionSignature(methodName);
        if (globalSignature != null)
            return globalSignature;

        return null;
    }

    /// <summary>
    /// Creates a signature information object from a method declaration.
    /// </summary>
    protected static GDSignatureInfo CreateSignatureFromDeclaration(GDMethodDeclaration methodDecl)
    {
        var parameters = new List<GDParameterInfo>();
        var paramLabels = new List<string>();

        if (methodDecl.Parameters != null)
        {
            foreach (var param in methodDecl.Parameters)
            {
                var paramLabel = param.Identifier?.ToString() ?? "arg";

                if (param.Type != null)
                {
                    paramLabel += ": " + param.Type.ToString();
                }

                if (param.DefaultValue != null)
                {
                    paramLabel += " = " + param.DefaultValue.ToString();
                }

                paramLabels.Add(paramLabel);
                parameters.Add(new GDParameterInfo { Label = paramLabel });
            }
        }

        var methodName = methodDecl.Identifier?.ToString() ?? "unknown";
        var returnType = methodDecl.ReturnType != null ? " -> " + methodDecl.ReturnType.ToString() : "";
        var label = $"func {methodName}({string.Join(", ", paramLabels)}){returnType}";

        return new GDSignatureInfo
        {
            Label = label,
            Parameters = parameters
        };
    }

    /// <summary>
    /// Tries to get the type name of an expression via SemanticModel.
    /// </summary>
    protected static string? GetExpressionTypeName(GDScriptFile script, GDExpression expression)
    {
        var semanticModel = script.SemanticModel;
        if (semanticModel == null)
            return null;

        // Use SemanticModel for type inference (per Rule 11)
        var typeInfo = semanticModel.TypeSystem.GetType(expression);
        if (!typeInfo.IsVariant)
            return typeInfo.DisplayName;

        // Fallback for identifier lookup via SemanticModel
        if (expression is GDIdentifierExpression idExpr)
        {
            var symbol = semanticModel.GetSymbolForNode(idExpr);
            if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName))
            {
                return symbol.TypeName;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets signature for a built-in method on a type.
    /// </summary>
    protected static GDSignatureInfo? GetBuiltinMethodSignature(string typeName, string methodName)
    {
        var signature = (typeName, methodName) switch
        {
            // String methods
            ("String", "substr") => ("func substr(from: int, len: int = -1) -> String", new[] { "from: int", "len: int = -1" }),
            ("String", "find") => ("func find(what: String, from: int = 0) -> int", new[] { "what: String", "from: int = 0" }),
            ("String", "replace") => ("func replace(what: String, forwhat: String) -> String", new[] { "what: String", "forwhat: String" }),
            ("String", "split") => ("func split(delimiter: String, allow_empty: bool = true, maxsplit: int = 0) -> PackedStringArray", new[] { "delimiter: String", "allow_empty: bool = true", "maxsplit: int = 0" }),
            ("String", "format") => ("func format(values: Variant, placeholder: String = \"{_}\") -> String", new[] { "values: Variant", "placeholder: String = \"{_}\"" }),

            // Array methods
            ("Array", "append") => ("func append(value: Variant) -> void", new[] { "value: Variant" }),
            ("Array", "insert") => ("func insert(position: int, value: Variant) -> void", new[] { "position: int", "value: Variant" }),
            ("Array", "remove_at") => ("func remove_at(position: int) -> void", new[] { "position: int" }),
            ("Array", "find") => ("func find(what: Variant, from: int = 0) -> int", new[] { "what: Variant", "from: int = 0" }),
            ("Array", "slice") => ("func slice(begin: int, end: int = INT_MAX, step: int = 1, deep: bool = false) -> Array", new[] { "begin: int", "end: int = INT_MAX", "step: int = 1", "deep: bool = false" }),
            ("Array", "map") => ("func map(method: Callable) -> Array", new[] { "method: Callable" }),
            ("Array", "filter") => ("func filter(method: Callable) -> Array", new[] { "method: Callable" }),
            ("Array", "reduce") => ("func reduce(method: Callable, accum: Variant = null) -> Variant", new[] { "method: Callable", "accum: Variant = null" }),

            // Dictionary methods
            ("Dictionary", "get") => ("func get(key: Variant, default: Variant = null) -> Variant", new[] { "key: Variant", "default: Variant = null" }),
            ("Dictionary", "has") => ("func has(key: Variant) -> bool", new[] { "key: Variant" }),
            ("Dictionary", "merge") => ("func merge(dictionary: Dictionary, overwrite: bool = false) -> void", new[] { "dictionary: Dictionary", "overwrite: bool = false" }),

            // Node methods
            ("Node", "add_child") => ("func add_child(node: Node, force_readable_name: bool = false, internal: InternalMode = 0) -> void", new[] { "node: Node", "force_readable_name: bool = false", "internal: InternalMode = 0" }),
            ("Node", "get_node") => ("func get_node(path: NodePath) -> Node", new[] { "path: NodePath" }),
            ("Node", "find_child") => ("func find_child(pattern: String, recursive: bool = true, owned: bool = true) -> Node", new[] { "pattern: String", "recursive: bool = true", "owned: bool = true" }),
            ("Node", "queue_free") => ("func queue_free() -> void", Array.Empty<string>()),

            _ => (null, null)
        };

        if (signature.Item1 == null || signature.Item2 == null)
            return null;

        var parameters = new List<GDParameterInfo>();
        foreach (var param in signature.Item2)
        {
            parameters.Add(new GDParameterInfo { Label = param });
        }

        return new GDSignatureInfo
        {
            Label = signature.Item1,
            Parameters = parameters
        };
    }

    /// <summary>
    /// Gets signature for a built-in global function.
    /// </summary>
    protected static GDSignatureInfo? GetBuiltinGlobalFunctionSignature(string functionName)
    {
        var signature = functionName switch
        {
            "print" => ("func print(...) -> void", new[] { "..." }),
            "print_rich" => ("func print_rich(...) -> void", new[] { "..." }),
            "printerr" => ("func printerr(...) -> void", new[] { "..." }),
            "push_error" => ("func push_error(message: String) -> void", new[] { "message: String" }),
            "push_warning" => ("func push_warning(message: String) -> void", new[] { "message: String" }),

            "str" => ("func str(...) -> String", new[] { "..." }),
            "int" => ("func int(value: Variant) -> int", new[] { "value: Variant" }),
            "float" => ("func float(value: Variant) -> float", new[] { "value: Variant" }),
            "bool" => ("func bool(value: Variant) -> bool", new[] { "value: Variant" }),

            "len" => ("func len(var: Variant) -> int", new[] { "var: Variant" }),
            "range" => ("func range(start_or_end: int, end: int = 0, step: int = 1) -> Array", new[] { "start_or_end: int", "end: int = 0", "step: int = 1" }),

            "abs" => ("func abs(value: Variant) -> Variant", new[] { "value: Variant" }),
            "sign" => ("func sign(value: Variant) -> Variant", new[] { "value: Variant" }),
            "min" => ("func min(a: Variant, b: Variant) -> Variant", new[] { "a: Variant", "b: Variant" }),
            "max" => ("func max(a: Variant, b: Variant) -> Variant", new[] { "a: Variant", "b: Variant" }),
            "clamp" => ("func clamp(value: Variant, min: Variant, max: Variant) -> Variant", new[] { "value: Variant", "min: Variant", "max: Variant" }),
            "lerp" => ("func lerp(from: Variant, to: Variant, weight: float) -> Variant", new[] { "from: Variant", "to: Variant", "weight: float" }),
            "floor" => ("func floor(value: float) -> float", new[] { "value: float" }),
            "ceil" => ("func ceil(value: float) -> float", new[] { "value: float" }),
            "round" => ("func round(value: float) -> float", new[] { "value: float" }),
            "sqrt" => ("func sqrt(value: float) -> float", new[] { "value: float" }),
            "pow" => ("func pow(base: float, exp: float) -> float", new[] { "base: float", "exp: float" }),
            "sin" => ("func sin(angle_rad: float) -> float", new[] { "angle_rad: float" }),
            "cos" => ("func cos(angle_rad: float) -> float", new[] { "angle_rad: float" }),
            "tan" => ("func tan(angle_rad: float) -> float", new[] { "angle_rad: float" }),

            "load" => ("func load(path: String) -> Resource", new[] { "path: String" }),
            "preload" => ("func preload(path: String) -> Resource", new[] { "path: String" }),

            "typeof" => ("func typeof(variable: Variant) -> int", new[] { "variable: Variant" }),
            "type_string" => ("func type_string(type: int) -> String", new[] { "type: int" }),
            "is_instance_valid" => ("func is_instance_valid(instance: Object) -> bool", new[] { "instance: Object" }),
            "instance_from_id" => ("func instance_from_id(instance_id: int) -> Object", new[] { "instance_id: int" }),

            "await" => ("func await(signal: Signal) -> Variant", new[] { "signal: Signal" }),

            _ => (null, null)
        };

        if (signature.Item1 == null || signature.Item2 == null)
            return null;

        var parameters = new List<GDParameterInfo>();
        foreach (var param in signature.Item2)
        {
            parameters.Add(new GDParameterInfo { Label = param });
        }

        return new GDSignatureInfo
        {
            Label = signature.Item1,
            Parameters = parameters
        };
    }

    /// <summary>
    /// Calculates the active parameter index based on comma count before cursor.
    /// </summary>
    protected static int CalculateActiveParameter(GDCallExpression call, int line, int column)
    {
        var paramIndex = 0;

        if (call.Parameters == null)
            return 0;

        foreach (var token in call.AllTokens)
        {
            if (token.StartLine > line || (token.StartLine == line && token.StartColumn >= column))
                break;

            if (token is GDComma)
            {
                paramIndex++;
            }
        }

        return paramIndex;
    }
}
