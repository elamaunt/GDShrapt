using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

internal class GDStringValueResolver
{
    private readonly GDScriptProject _project;

    internal GDStringValueResolver(GDScriptProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
    }

    public HashSet<string> ResolveStringValues(GDExpression expr, GDClassDeclaration classDecl)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        var literal = ExtractStringLiteral(expr);
        if (literal != null)
        {
            result.Add(literal);
            return result;
        }

        if (expr is GDIndexerExpression indexer)
        {
            var dictExpr = indexer.CallerExpression;
            var keyExpr = indexer.InnerExpression;
            var keyLiteral = ExtractStringLiteral(keyExpr);

            if (dictExpr is GDIdentifierExpression dictId && keyLiteral != null)
            {
                var dictVarName = dictId.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(dictVarName))
                {
                    CollectDictionaryValuesForKey(classDecl, dictVarName, keyLiteral, result);
                }
            }
        }
        else if (expr is GDMemberOperatorExpression memberOp)
        {
            var dictExpr = memberOp.CallerExpression;
            var memberName = memberOp.Identifier?.Sequence;

            if (dictExpr is GDIdentifierExpression dictId && !string.IsNullOrEmpty(memberName))
            {
                var dictVarName = dictId.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(dictVarName))
                {
                    CollectDictionaryValuesForKey(classDecl, dictVarName, memberName, result);
                }
            }
        }

        return result;
    }

    public static string? ExtractStringLiteral(GDExpression expr)
    {
        if (expr is GDStringExpression strExpr)
            return strExpr.String?.Sequence;
        if (expr is GDStringNameExpression strNameExpr)
            return strNameExpr.String?.Sequence;
        return null;
    }

    private void CollectDictionaryValuesForKey(
        GDClassDeclaration classDecl,
        string varName,
        string key,
        HashSet<string> result)
    {
        var forLoops = classDecl.AllNodes
            .OfType<GDForStatement>()
            .Where(f => f.Variable?.Sequence == varName);

        foreach (var forLoop in forLoops)
        {
            var iterable = forLoop.Collection;
            if (iterable == null)
                continue;

            CollectDictionaryValuesFromIterable(classDecl, iterable, key, result);
        }

        var varDecls = classDecl.AllNodes
            .OfType<GDVariableDeclaration>()
            .Where(v => v.Identifier?.Sequence == varName);

        foreach (var varDecl in varDecls)
        {
            if (varDecl.Initializer is GDArrayInitializerExpression arrayInit)
            {
                ExtractDictionaryValuesFromArray(arrayInit, key, result);
            }
        }
    }

    private void CollectDictionaryValuesFromIterable(
        GDClassDeclaration classDecl,
        GDExpression iterable,
        string key,
        HashSet<string> result)
    {
        if (iterable is GDArrayInitializerExpression arrayInit)
        {
            ExtractDictionaryValuesFromArray(arrayInit, key, result);
            return;
        }

        if (iterable is GDIdentifierExpression idExpr)
        {
            var iterVarName = idExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(iterVarName))
            {
                var decls = classDecl.AllNodes
                    .OfType<GDVariableDeclaration>()
                    .Where(v => v.Identifier?.Sequence == iterVarName);

                foreach (var decl in decls)
                {
                    if (decl.Initializer is GDArrayInitializerExpression arr)
                    {
                        ExtractDictionaryValuesFromArray(arr, key, result);
                    }
                }

                var localDecls = classDecl.AllNodes
                    .OfType<GDVariableDeclarationStatement>()
                    .Where(v => v.Identifier?.Sequence == iterVarName);

                foreach (var decl in localDecls)
                {
                    if (decl.Initializer is GDArrayInitializerExpression arr)
                    {
                        ExtractDictionaryValuesFromArray(arr, key, result);
                    }
                }
            }
            return;
        }

        if (iterable is GDCallExpression callExpr)
        {
            var callee = GetCalleeMethodName(callExpr);
            if (!string.IsNullOrEmpty(callee))
            {
                CollectReturnedDictionaryValues(classDecl, callee, key, result);

                if (callExpr.CallerExpression is GDMemberOperatorExpression memberCall)
                {
                    var methodName = memberCall.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(methodName))
                    {
                        CollectReturnedDictionaryValues(classDecl, methodName, key, result);

                        foreach (var scriptFile in _project.ScriptFiles)
                        {
                            if (scriptFile.Class == null || scriptFile.Class == classDecl)
                                continue;

                            CollectReturnedDictionaryValues(scriptFile.Class, methodName, key, result);
                        }
                    }
                }
            }
        }
    }

    private static void CollectReturnedDictionaryValues(
        GDClassDeclaration classDecl,
        string methodName,
        string key,
        HashSet<string> result)
    {
        var methods = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .Where(m => m.Identifier?.Sequence == methodName);

        foreach (var method in methods)
        {
            var returns = method.AllNodes
                .OfType<GDReturnExpression>();

            foreach (var ret in returns)
            {
                if (ret.Expression is GDArrayInitializerExpression arrayInit)
                {
                    ExtractDictionaryValuesFromArray(arrayInit, key, result);
                }
            }
        }
    }

    private static void ExtractDictionaryValuesFromArray(
        GDArrayInitializerExpression arrayInit,
        string key,
        HashSet<string> result)
    {
        if (arrayInit.Values == null)
            return;

        foreach (var element in arrayInit.Values)
        {
            if (element is GDDictionaryInitializerExpression dictInit)
            {
                ExtractValueForKey(dictInit, key, result);
            }
        }
    }

    private static void ExtractValueForKey(
        GDDictionaryInitializerExpression dictInit,
        string key,
        HashSet<string> result)
    {
        if (dictInit.KeyValues == null)
            return;

        foreach (var kv in dictInit.KeyValues)
        {
            var keyLiteral = ExtractStringLiteral(kv.Key);
            if (keyLiteral == key)
            {
                var valueLiteral = ExtractStringLiteral(kv.Value);
                if (valueLiteral != null)
                    result.Add(valueLiteral);
            }
        }
    }

    internal static string? GetCalleeMethodName(GDCallExpression callExpr)
    {
        if (callExpr.CallerExpression is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.Sequence;
        if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            return memberOp.Identifier?.Sequence;
        return null;
    }
}
