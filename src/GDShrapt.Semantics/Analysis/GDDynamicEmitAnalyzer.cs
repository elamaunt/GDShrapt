using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

internal class GDDynamicEmitAnalyzer
{
    private readonly GDScriptProject _project;
    private readonly GDCallSiteRegistry? _callSiteRegistry;

    internal GDDynamicEmitAnalyzer(GDScriptProject project, GDCallSiteRegistry? callSiteRegistry)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _callSiteRegistry = callSiteRegistry;
    }

    public bool IsSignalEmittedDynamically(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        string signalName,
        IReadOnlyList<string> effectiveNames)
    {
        var emitSignalCalls = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .Where(c => GetCalleeMethodName(c) == "emit_signal")
            .ToList();

        foreach (var emitCall in emitSignalCalls)
        {
            var args = emitCall.Parameters?.ToList();
            if (args == null || args.Count == 0)
                continue;

            var firstArg = args[0];

            if (firstArg is GDStringExpression || firstArg is GDStringNameExpression)
                continue;

            if (firstArg is GDIdentifierExpression idExpr)
            {
                var argName = idExpr.Identifier?.Sequence;
                if (string.IsNullOrEmpty(argName))
                    continue;

                var containingMethod = FindContainingMethod(emitCall);
                if (containingMethod == null)
                    continue;

                var methodName = containingMethod.Identifier?.Sequence;
                if (string.IsNullOrEmpty(methodName))
                    continue;

                var paramIndex = GetParameterIndex(containingMethod, argName);
                if (paramIndex < 0)
                    continue;

                if (TraceStringArgumentToCallers(file, classDecl, methodName, paramIndex, signalName, effectiveNames))
                    return true;
            }
        }

        return false;
    }

    private bool TraceStringArgumentToCallers(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        string methodName,
        int paramIndex,
        string signalName,
        IReadOnlyList<string> effectiveNames)
    {
        // Check local direct calls
        var localCalls = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .Where(c => GetCalleeMethodName(c) == methodName);

        foreach (var call in localCalls)
        {
            var callArgs = call.Parameters?.ToList();
            if (callArgs == null || callArgs.Count <= paramIndex)
                continue;

            var argAtIndex = callArgs[paramIndex];
            var literalValue = GDStringValueResolver.ExtractStringLiteral(argAtIndex);
            if (literalValue == signalName)
                return true;
        }

        // Check .bind() patterns
        var bindCalls = classDecl.AllNodes
            .OfType<GDCallExpression>()
            .Where(c =>
            {
                if (c.CallerExpression is GDMemberOperatorExpression mem &&
                    mem.Identifier?.Sequence == "bind")
                {
                    if (mem.CallerExpression is GDIdentifierExpression callerRef)
                        return callerRef.Identifier?.Sequence == methodName;
                }
                return false;
            });

        foreach (var bindCall in bindCalls)
        {
            var bindArgs = bindCall.Parameters?.ToList();
            if (bindArgs == null || bindArgs.Count == 0)
                continue;

            var methodDecl = classDecl.Members
                .OfType<GDMethodDeclaration>()
                .FirstOrDefault(m => m.Identifier?.Sequence == methodName);

            if (methodDecl == null)
                continue;

            var totalParams = methodDecl.Parameters?.Count() ?? 0;
            int firstBindParamIndex = totalParams - bindArgs.Count;

            if (paramIndex >= firstBindParamIndex && paramIndex < totalParams)
            {
                int bindArgIndex = paramIndex - firstBindParamIndex;
                var literalValue = GDStringValueResolver.ExtractStringLiteral(bindArgs[bindArgIndex]);
                if (literalValue == signalName)
                    return true;
            }
        }

        // Check cross-file calls via call site registry
        if (_callSiteRegistry != null)
        {
            foreach (var name in effectiveNames)
            {
                var callers = _callSiteRegistry.GetCallersOf(name, methodName);
                foreach (var caller in callers)
                {
                    var callerFile = _project.ScriptFiles
                        .FirstOrDefault(f => f.FullPath != null &&
                            f.FullPath.Equals(caller.SourceFilePath, StringComparison.OrdinalIgnoreCase));

                    if (callerFile?.Class == null)
                        continue;

                    var callerCalls = callerFile.Class.AllNodes
                        .OfType<GDCallExpression>()
                        .Where(c =>
                        {
                            var callee = GetCalleeMethodName(c);
                            if (callee == methodName)
                                return true;
                            if (c.CallerExpression is GDMemberOperatorExpression mem)
                                return mem.Identifier?.Sequence == methodName;
                            return false;
                        });

                    foreach (var callerCall in callerCalls)
                    {
                        var callArgs = callerCall.Parameters?.ToList();
                        if (callArgs == null || callArgs.Count <= paramIndex)
                            continue;

                        var argAtIndex = callArgs[paramIndex];
                        var literalValue = GDStringValueResolver.ExtractStringLiteral(argAtIndex);
                        if (literalValue == signalName)
                            return true;
                    }
                }
            }
        }

        return false;
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

    private static int GetParameterIndex(GDMethodDeclaration method, string paramName)
    {
        var parameters = method.Parameters;
        if (parameters == null)
            return -1;

        int index = 0;
        foreach (var param in parameters)
        {
            if (param.Identifier?.Sequence == paramName)
                return index;
            index++;
        }
        return -1;
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
