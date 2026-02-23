using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

internal class GDExpressionDispatchCollector
{
    private readonly GDScriptProject _project;
    private readonly GDStringValueResolver _stringResolver;

    internal GDExpressionDispatchCollector(GDScriptProject project, GDStringValueResolver stringResolver)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _stringResolver = stringResolver ?? throw new ArgumentNullException(nameof(stringResolver));
    }

    public IReadOnlyList<GDExpressionDispatchEntry> CollectAll()
    {
        var entries = new List<GDExpressionDispatchEntry>();

        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (scriptFile.Class == null)
                continue;

            CollectFromExpressionExecute(scriptFile, entries);
            CollectFromStringReplaceChains(scriptFile, entries);
            CollectFromStringLiterals(scriptFile, entries);
        }

        return entries;
    }

    private void CollectFromExpressionExecute(GDScriptFile scriptFile, List<GDExpressionDispatchEntry> entries)
    {
        var classDecl = scriptFile.Class!;

        var parseCalls = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .Where(c =>
            {
                if (c.CallerExpression is GDMemberOperatorExpression mem)
                    return mem.Identifier?.Sequence == "parse";
                return false;
            });

        foreach (var parseCall in parseCalls)
        {
            var args = parseCall.Parameters?.ToList();
            if (args == null || args.Count == 0)
                continue;

            var stringArg = args[0];
            var resolvedStrings = _stringResolver.ResolveStringValues(stringArg, classDecl);

            foreach (var str in resolvedStrings)
            {
                var methodNames = TryParseGDScriptCallNames(str);
                if (methodNames.Count == 0)
                    continue;

                var receiverType = FindExecuteReceiverType(parseCall, classDecl, scriptFile);

                foreach (var methodName in methodNames)
                {
                    entries.Add(new GDExpressionDispatchEntry(
                        methodName,
                        receiverType,
                        scriptFile.FullPath ?? "",
                        parseCall.StartLine));
                }
            }
        }
    }

    private void CollectFromStringReplaceChains(GDScriptFile scriptFile, List<GDExpressionDispatchEntry> entries)
    {
        var classDecl = scriptFile.Class!;

        var replaceCalls = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .Where(c =>
            {
                if (c.CallerExpression is GDMemberOperatorExpression mem)
                    return mem.Identifier?.Sequence == "replace";
                return false;
            });

        foreach (var replaceCall in replaceCalls)
        {
            var args = replaceCall.Parameters?.ToList();
            if (args == null || args.Count < 2)
                continue;

            var newValue = GDStringValueResolver.ExtractStringLiteral(args[1]);
            if (string.IsNullOrEmpty(newValue))
                continue;

            var methodNames = TryParseGDScriptCallNames(newValue);
            if (methodNames.Count == 0)
                continue;

            var containingMethod = FindContainingMethod(replaceCall);
            var receiverType = ResolveReceiverFromMethodBody(containingMethod, classDecl, scriptFile);

            foreach (var methodName in methodNames)
            {
                entries.Add(new GDExpressionDispatchEntry(
                    methodName,
                    receiverType,
                    scriptFile.FullPath ?? "",
                    replaceCall.StartLine));
            }
        }
    }

    private void CollectFromStringLiterals(GDScriptFile scriptFile, List<GDExpressionDispatchEntry> entries)
    {
        var classDecl = scriptFile.Class!;

        foreach (var strExpr in classDecl.AllNodes.OfType<GDStringExpression>())
        {
            var code = strExpr.String?.Sequence;
            if (string.IsNullOrEmpty(code))
                continue;

            if (!code.Contains("("))
                continue;

            var methodNames = TryParseGDScriptCallNames(code);
            if (methodNames.Count == 0)
                continue;

            foreach (var methodName in methodNames)
            {
                entries.Add(new GDExpressionDispatchEntry(
                    methodName,
                    null,
                    scriptFile.FullPath ?? "",
                    strExpr.StartLine));
            }
        }
    }

    private static List<string> TryParseGDScriptCallNames(string code)
    {
        var result = new List<string>();

        try
        {
            var reader = new GDScriptReader();
            var parsed = reader.ParseExpression(code);
            if (parsed == null)
                return result;

            foreach (var call in parsed.AllNodes.OfType<GDCallExpression>())
            {
                var methodName = GetCalleeMethodName(call);
                if (!string.IsNullOrEmpty(methodName))
                    result.Add(methodName);
            }

            if (result.Count == 0 && parsed is GDCallExpression topCall)
            {
                var name = GetCalleeMethodName(topCall);
                if (!string.IsNullOrEmpty(name))
                    result.Add(name);
            }
        }
        catch
        {
            // String is not valid GDScript — ignore
        }

        return result;
    }

    private static string? FindExecuteReceiverType(
        GDCallExpression parseCall,
        GDClassDeclaration classDecl,
        GDScriptFile scriptFile)
    {
        var containingMethod = FindContainingMethod(parseCall);
        return ResolveReceiverFromMethodBody(containingMethod, classDecl, scriptFile);
    }

    private static string? ResolveReceiverFromMethodBody(
        GDMethodDeclaration? method,
        GDClassDeclaration classDecl,
        GDScriptFile scriptFile)
    {
        if (method == null)
            return null;

        var executeCalls = method.AllNodes
            .OfType<GDCallExpression>()
            .Where(c =>
            {
                if (c.CallerExpression is GDMemberOperatorExpression mem)
                    return mem.Identifier?.Sequence == "execute";
                return false;
            });

        foreach (var execCall in executeCalls)
        {
            var execArgs = execCall.Parameters?.ToList();
            if (execArgs == null || execArgs.Count < 2)
                continue;

            var baseInstance = execArgs[1];
            if (baseInstance is GDIdentifierExpression idExpr &&
                idExpr.Identifier?.Sequence == "self")
            {
                return scriptFile.TypeName ?? classDecl.ClassName?.Identifier?.Sequence;
            }
        }

        return null;
    }

    private static GDMethodDeclaration? FindContainingMethod(GDNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is GDMethodDeclaration method)
                return method;
            current = current.Parent;
        }
        return null;
    }

    private static string? GetCalleeMethodName(GDCallExpression callExpr)
    {
        if (callExpr.CallerExpression is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.Sequence;
        if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            return memberOp.Identifier?.Sequence;
        return null;
    }
}
