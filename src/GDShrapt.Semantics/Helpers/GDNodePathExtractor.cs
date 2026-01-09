using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Static helper for extracting node paths from various AST patterns.
/// Used by GDNodeTypeInjector for type inference.
/// </summary>
public static class GDNodePathExtractor
{
    /// <summary>
    /// Extracts node path from $Player/Sprite expression.
    /// </summary>
    public static string? ExtractFromGetNodeExpression(GDGetNodeExpression expr)
    {
        var pathList = expr.Path;
        if (pathList == null)
            return null;

        var parts = new List<string>();

        foreach (var layer in pathList.OfType<GDLayersList>())
        {
            foreach (var specifier in layer.OfType<GDPathSpecifier>())
            {
                switch (specifier.Type)
                {
                    case GDPathSpecifierType.Current:
                        parts.Add(".");
                        break;
                    case GDPathSpecifierType.Parent:
                        parts.Add("..");
                        break;
                    case GDPathSpecifierType.Identifier:
                        var id = specifier.IdentifierValue;
                        if (!string.IsNullOrEmpty(id))
                            parts.Add(id);
                        break;
                }
            }
        }

        return parts.Count > 0 ? string.Join("/", parts) : null;
    }

    /// <summary>
    /// Extracts node name from %UniqueNode expression.
    /// </summary>
    public static string? ExtractFromUniqueNodeExpression(GDGetUniqueNodeExpression expr)
    {
        return expr.Name?.Sequence;
    }

    /// <summary>
    /// Extracts path from get_node("Player/Sprite") call.
    /// Supports string literals and statically resolvable variables.
    /// </summary>
    /// <param name="call">The call expression to extract from.</param>
    /// <param name="resolveVariable">Optional function to resolve variable names to their initializers.</param>
    public static string? ExtractFromCallExpression(
        GDCallExpression call,
        Func<string, GDExpression?>? resolveVariable = null)
    {
        if (!IsGetNodeCall(call))
            return null;

        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return null;

        var firstArg = args[0];

        // Direct string literal
        if (firstArg is GDStringExpression strExpr)
            return strExpr.String?.Sequence;

        // Variable - try to get static value
        if (firstArg is GDIdentifierExpression idExpr && resolveVariable != null)
        {
            var varName = idExpr.Identifier?.Sequence;
            if (varName != null)
            {
                var initExpr = resolveVariable(varName);
                if (initExpr is GDStringExpression initStr)
                    return initStr.String?.Sequence;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts resource path from preload/load call.
    /// </summary>
    public static string? ExtractResourcePath(GDCallExpression call)
    {
        if (!IsPreloadOrLoadCall(call))
            return null;

        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return null;

        if (args[0] is GDStringExpression strExpr)
            return strExpr.String?.Sequence;

        return null;
    }

    /// <summary>
    /// Tries to get static string value of a variable.
    /// Supports: const NAME = "value", var name := "value"
    /// </summary>
    public static GDExpression? TryGetStaticStringInitializer(
        GDClassDeclaration classDecl,
        string variableName)
    {
        if (classDecl.Members == null)
            return null;

        foreach (var member in classDecl.Members)
        {
            if (member is GDVariableDeclaration varDecl)
            {
                if (varDecl.Identifier?.Sequence == variableName)
                {
                    // const NAME = "value"
                    if (varDecl.ConstKeyword != null &&
                        varDecl.Initializer is GDStringExpression)
                    {
                        return varDecl.Initializer;
                    }

                    // var name := "value" (type infer syntax: TypeColon present but Type is null)
                    // This pattern: var name := expr means inferred type from initializer
                    if (varDecl.TypeColon != null && varDecl.Type == null &&
                        varDecl.Initializer is GDStringExpression)
                    {
                        return varDecl.Initializer;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the call is a get_node variant.
    /// </summary>
    public static bool IsGetNodeCall(GDCallExpression call)
    {
        var name = GetCallName(call);
        return name == "get_node" || name == "get_node_or_null" || name == "find_node";
    }

    /// <summary>
    /// Checks if the call is a preload or load call.
    /// </summary>
    public static bool IsPreloadOrLoadCall(GDCallExpression call)
    {
        var name = GetCallName(call);
        return name == "preload" || name == "load";
    }

    /// <summary>
    /// Gets the name of the called function.
    /// </summary>
    public static string? GetCallName(GDCallExpression call)
    {
        if (call.CallerExpression is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.Sequence;
        if (call.CallerExpression is GDMemberOperatorExpression memberExpr)
            return memberExpr.Identifier?.Sequence;
        return null;
    }
}
