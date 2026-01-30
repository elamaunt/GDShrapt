using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Analyzes container types (Array, Dictionary) and extracts element/value types.
/// </summary>
internal class GDContainerTypeAnalyzer
{
    private readonly GDScopeStack _scopes;
    private readonly Func<GDExpression, string> _inferType;

    /// <summary>
    /// Creates a new container type analyzer.
    /// </summary>
    /// <param name="scopes">Scope stack for variable lookup.</param>
    /// <param name="inferType">Function to infer type of an expression.</param>
    public GDContainerTypeAnalyzer(GDScopeStack scopes, Func<GDExpression, string> inferType)
    {
        _scopes = scopes;
        _inferType = inferType ?? throw new ArgumentNullException(nameof(inferType));
    }

    /// <summary>
    /// Extracts the union type of elements from an Array initializer expression.
    /// </summary>
    public string ExtractArrayElementTypes(GDArrayInitializerExpression arrayInit)
    {
        if (arrayInit?.Values == null || !arrayInit.Values.Any())
            return null;

        var elementTypes = new HashSet<string>();
        foreach (var element in arrayInit.Values)
        {
            if (element != null)
            {
                var type = _inferType(element);
                if (!string.IsNullOrEmpty(type))
                    elementTypes.Add(type);
            }
        }

        return UnifyTypes(elementTypes);
    }

    /// <summary>
    /// Extracts the union type of values from a Dictionary initializer expression.
    /// </summary>
    public string ExtractDictionaryValueTypes(GDDictionaryInitializerExpression dictInit)
    {
        if (dictInit?.KeyValues == null || !dictInit.KeyValues.Any())
            return null;

        var valueTypes = new HashSet<string>();
        foreach (var kv in dictInit.KeyValues)
        {
            if (kv.Value != null)
            {
                var type = _inferType(kv.Value);
                if (!string.IsNullOrEmpty(type))
                    valueTypes.Add(type);
            }
        }

        return UnifyTypes(valueTypes);
    }

    /// <summary>
    /// Tries to infer the value type of a Dictionary by analyzing its initializer.
    /// Returns union type if multiple value types, single type if uniform, or null if unknown.
    /// </summary>
    public string InferDictionaryValueType(GDExpression dictExpr)
    {
        var dictInit = FindDictionaryInitializer(dictExpr);
        if (dictInit != null)
            return ExtractDictionaryValueTypes(dictInit);

        return null;
    }

    /// <summary>
    /// Infers the type for Dictionary.get("key") with key-specific type lookup.
    /// First tries to find the specific key in the dictionary initializer,
    /// then falls back to union of all values.
    /// </summary>
    public string InferDictionaryGetType(GDCallExpression callExpr, GDExpression dictExpr)
    {
        var args = callExpr.Parameters?.ToList();
        if (args != null && args.Count >= 1)
        {
            // Try to extract the key as a static string
            var resolver = GDStaticStringExtractor.CreateScopeResolver(_scopes, callExpr.RootClassDeclaration);
            var keyStr = GDStaticStringExtractor.TryExtractString(args[0], resolver);

            if (!string.IsNullOrEmpty(keyStr))
            {
                // Try to find the specific key in the dictionary initializer
                var specificType = InferDictionaryValueTypeForKey(dictExpr, keyStr);
                if (!string.IsNullOrEmpty(specificType))
                    return specificType;
            }
        }

        // Fall back to union of all dictionary values
        return InferDictionaryValueType(dictExpr);
    }

    /// <summary>
    /// Gets the value type for a specific key in a Dictionary initializer.
    /// Returns null if the key is not found (falls back to union).
    /// </summary>
    public string InferDictionaryValueTypeForKey(GDExpression dictExpr, string key)
    {
        var dictInit = FindDictionaryInitializer(dictExpr);
        if (dictInit?.KeyValues == null)
            return null;

        // Look for the specific key
        foreach (var kv in dictInit.KeyValues)
        {
            var keyStr = GDStaticStringExtractor.TryExtractString(kv.Key, null);
            if (keyStr == key && kv.Value != null)
                return _inferType(kv.Value);
        }

        // Key not found - return null to fall back to union
        return null;
    }

    /// <summary>
    /// Finds the dictionary initializer expression for a dictionary variable.
    /// Searches in: scope stack, class members, and local statements (AST walk-up).
    /// </summary>
    public GDDictionaryInitializerExpression FindDictionaryInitializer(GDExpression dictExpr)
    {
        if (dictExpr is GDDictionaryInitializerExpression directInit)
            return directInit;

        if (dictExpr is GDIdentifierExpression identExpr)
        {
            var name = identExpr.Identifier?.Sequence;
            if (string.IsNullOrEmpty(name))
                return null;

            // Try scope first (for class-level variables)
            if (_scopes != null)
            {
                var symbol = _scopes.Lookup(name);
                if (symbol?.Declaration is GDVariableDeclaration varDecl &&
                    varDecl.Initializer is GDDictionaryInitializerExpression init)
                {
                    return init;
                }
                if (symbol?.Declaration is GDVariableDeclarationStatement varStmt &&
                    varStmt.Initializer is GDDictionaryInitializerExpression stmtInit)
                {
                    return stmtInit;
                }
            }

            // Try class members
            var classDecl = dictExpr.RootClassDeclaration;
            if (classDecl != null)
            {
                foreach (var member in classDecl.Members ?? Enumerable.Empty<GDClassMember>())
                {
                    if (member is GDVariableDeclaration memberVarDecl &&
                        memberVarDecl.Identifier?.Sequence == name &&
                        memberVarDecl.Initializer is GDDictionaryInitializerExpression memberInit)
                    {
                        return memberInit;
                    }
                }
            }

            // Try local statements by walking up AST to find variable declaration
            // This handles local variables like: var results = {"key": []}
            var localInit = FindLocalVariableInitializer(identExpr, name);
            if (localInit is GDDictionaryInitializerExpression dictLocalInit)
                return dictLocalInit;
        }

        return null;
    }

    /// <summary>
    /// Finds a local variable declaration by walking up the AST from the usage site.
    /// Returns the initializer expression if found.
    /// </summary>
    internal static GDExpression? FindLocalVariableInitializer(GDNode usageSite, string variableName)
    {
        // Walk up to find the containing statements list
        var current = usageSite.Parent;
        while (current != null)
        {
            // Check if we're in a statements list (method body, if body, etc.)
            if (current is GDStatementsList statementsList)
            {
                // Search all statements for the variable declaration
                // GDStatementsList implements IList<GDStatement>, iterate directly
                foreach (var statement in statementsList)
                {
                    if (statement is GDVariableDeclarationStatement varStmt &&
                        varStmt.Identifier?.Sequence == variableName)
                    {
                        return varStmt.Initializer;
                    }

                    // Stop searching when we reach the usage site (don't look at declarations after usage)
                    if (ContainsNode(statement, usageSite))
                        break;
                }
            }

            // Also check for patterns in for loops: for x in array
            if (current is GDForStatement forStmt &&
                forStmt.Variable?.Sequence == variableName)
            {
                return forStmt.Collection;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Checks if a node contains another node in its subtree.
    /// </summary>
    private static bool ContainsNode(GDNode container, GDNode target)
    {
        if (container == target)
            return true;

        foreach (var node in container.AllNodes)
        {
            if (node == target)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Unifies a set of types into a single result.
    /// Returns null if empty, the single type if one, or a union type string if multiple.
    /// </summary>
    private static string UnifyTypes(HashSet<string> types)
    {
        if (types.Count == 0)
            return null;

        if (types.Count == 1)
            return types.First();

        // Multiple types - return Union (sorted alphabetically)
        var sorted = types.OrderBy(t => t);
        return string.Join(" | ", sorted);
    }
}
