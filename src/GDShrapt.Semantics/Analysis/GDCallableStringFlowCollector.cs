using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

internal class GDCallableStringFlowCollector
{
    private readonly GDScriptProject _project;
    private readonly GDStringValueResolver _stringResolver;

    internal GDCallableStringFlowCollector(GDScriptProject project, GDStringValueResolver stringResolver)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _stringResolver = stringResolver ?? throw new ArgumentNullException(nameof(stringResolver));
    }

    public IReadOnlyList<GDCallableStringFlowEntry> CollectAll()
    {
        var entries = new List<GDCallableStringFlowEntry>();

        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (scriptFile.Class == null)
                continue;

            var callableConstructions = scriptFile.Class.AllNodes
                .OfType<GDCallExpression>()
                .Where(c => GDStringValueResolver.GetCalleeMethodName(c) == "Callable");

            foreach (var callableCall in callableConstructions)
            {
                var args = callableCall.Parameters?.ToList();
                if (args == null || args.Count < 2)
                    continue;

                var resolvedNames = _stringResolver.ResolveStringValues(args[1], scriptFile.Class);
                if (resolvedNames.Count == 0)
                    continue;

                var receiverType = ResolveReceiverType(args[0], scriptFile);

                foreach (var methodName in resolvedNames)
                {
                    entries.Add(new GDCallableStringFlowEntry(
                        methodName,
                        receiverType,
                        scriptFile.FullPath ?? "",
                        callableCall.StartLine));
                }
            }
        }

        return entries;
    }

    private static string? ResolveReceiverType(GDExpression receiverExpr, GDScriptFile callSiteFile)
    {
        if (receiverExpr is GDIdentifierExpression idExpr &&
            idExpr.Identifier?.Sequence == "self")
        {
            return callSiteFile.TypeName ?? callSiteFile.Class?.ClassName?.Identifier?.Sequence;
        }

        // Non-self receiver — return null for permissive matching
        return null;
    }
}
