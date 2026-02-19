using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Traces argument type provenance through call sites, signal connections,
/// and container usage. Builds recursive GDCallSiteProvenanceEntry chains.
/// </summary>
public static class GDProvenanceTracer
{
    public static List<GDCallSiteProvenanceEntry> TraceArgumentOrigin(
        GDScriptProject project,
        GDProjectSemanticModel? projectModel,
        IGDRuntimeProvider? runtimeProvider,
        GDScriptFile callSiteFile,
        string argVarName,
        GDExpression? argExpr,
        int maxDepth = 3,
        int callSiteLine = 0)
    {
        if (maxDepth <= 0 || projectModel == null)
            return new List<GDCallSiteProvenanceEntry>();

        var chain = new List<GDCallSiteProvenanceEntry>();
        var enclosingType = callSiteFile.TypeName;
        if (string.IsNullOrEmpty(enclosingType))
            return chain;

        // 1. For-loop variable -> trace container
        if (argExpr != null)
        {
            var forStmt = FindEnclosingForStatement(argExpr, argVarName);
            if (forStmt != null)
            {
                var collectionName = (forStmt.Collection as GDIdentifierExpression)
                    ?.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(collectionName))
                {
                    var forLine = (forStmt.AllTokens.FirstOrDefault()?.StartLine ?? 0) + 1;
                    var innerChain = TraceContainerOrigin(
                        project, projectModel, runtimeProvider,
                        callSiteFile, enclosingType, collectionName, maxDepth - 1);
                    chain.Add(new GDCallSiteProvenanceEntry(
                        callSiteFile.FullPath ?? "", forLine,
                        $"for {argVarName} in {collectionName}", innerChain));
                    return chain;
                }
            }
        }
        else if (callSiteLine > 0 && callSiteFile.Class != null)
        {
            // Fallback: find for-loop by line number when no AST expression available
            var forStmt = FindForStatementAtLine(callSiteFile, argVarName, callSiteLine);
            if (forStmt != null)
            {
                var collectionName = (forStmt.Collection as GDIdentifierExpression)
                    ?.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(collectionName))
                {
                    var forLine = (forStmt.AllTokens.FirstOrDefault()?.StartLine ?? 0) + 1;
                    var innerChain = TraceContainerOrigin(
                        project, projectModel, runtimeProvider,
                        callSiteFile, enclosingType, collectionName, maxDepth - 1);
                    chain.Add(new GDCallSiteProvenanceEntry(
                        callSiteFile.FullPath ?? "", forLine,
                        $"for {argVarName} in {collectionName}", innerChain));
                    return chain;
                }
            }
        }

        // 2. Parameter -> trace callers (signals + call sites)
        var method = argExpr != null
            ? FindEnclosingMethod(argExpr)
            : (callSiteLine > 0 ? FindMethodAtLine(callSiteFile, callSiteLine) : null);
        if (method != null)
        {
            var paramIdx = FindParameterIndex(method, argVarName);
            if (paramIdx >= 0)
            {
                var methodName = method.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(methodName))
                {
                    // 2a: Signal connections -> signal parameter types
                    try
                    {
                        var connections = projectModel.SignalConnectionRegistry
                            .GetSignalsCallingMethod(enclosingType, methodName);
                        foreach (var conn in connections)
                        {
                            if (string.IsNullOrEmpty(conn.EmitterType)) continue;
                            var signalParams = GetSignalParameterTypes(
                                project, runtimeProvider, conn.EmitterType, conn.SignalName);
                            if (signalParams != null && paramIdx < signalParams.Count)
                            {
                                chain.Add(new GDCallSiteProvenanceEntry(
                                    conn.SourceFilePath ?? callSiteFile.FullPath ?? "",
                                    conn.Line,
                                    $"{conn.EmitterType}.{conn.SignalName} signal -> " +
                                    $"{methodName}({argVarName}: {signalParams[paramIdx]})"));
                            }
                        }
                    }
                    catch { }

                    // 2b: Direct call sites -> recurse into argument
                    if (chain.Count == 0)
                    {
                        try
                        {
                            var collector = new GDCallSiteCollector(project);
                            var callSites = collector.CollectCallSites(
                                enclosingType, methodName);
                            foreach (var cs in callSites)
                            {
                                var arg = cs.GetArgument(paramIdx);
                                if (arg?.Expression == null) continue;
                                var innerChain = TraceArgumentOrigin(
                                    project, projectModel, runtimeProvider,
                                    cs.SourceScript, arg.ExpressionText,
                                    arg.Expression, maxDepth - 1);
                                chain.Add(new GDCallSiteProvenanceEntry(
                                    cs.FilePath, cs.Line + 1,
                                    arg.ExpressionText, innerChain));
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        return chain;
    }

    public static string? TryNarrowTypeFromChain(
        IGDRuntimeProvider? runtimeProvider,
        IReadOnlyList<GDCallSiteProvenanceEntry> chain,
        string currentType)
    {
        if (runtimeProvider == null || chain.Count == 0)
            return null;

        string? narrowest = null;

        foreach (var entry in chain)
        {
            var signalType = ExtractSignalParamType(entry.Expression);
            if (!string.IsNullOrEmpty(signalType)
                && signalType != "Variant"
                && signalType != currentType
                && runtimeProvider.IsAssignableTo(signalType, currentType) == true)
            {
                if (narrowest == null
                    || runtimeProvider.IsAssignableTo(signalType, narrowest) == true)
                    narrowest = signalType;
            }

            var innerNarrowed = TryNarrowTypeFromChain(runtimeProvider, entry.InnerChain.ToList(), currentType);
            if (innerNarrowed != null)
            {
                if (narrowest == null
                    || runtimeProvider.IsAssignableTo(innerNarrowed, narrowest) == true)
                    narrowest = innerNarrowed;
            }
        }

        return narrowest;
    }

    public static List<GDCallSiteProvenanceEntry> TraceContainerOrigin(
        GDScriptProject project,
        GDProjectSemanticModel? projectModel,
        IGDRuntimeProvider? runtimeProvider,
        GDScriptFile file, string enclosingType,
        string containerVarName, int maxDepth)
    {
        var chain = new List<GDCallSiteProvenanceEntry>();
        if (maxDepth <= 0 || projectModel == null) return chain;

        var profile = projectModel.GetMergedContainerProfile(
            enclosingType, containerVarName);

        if (profile == null)
        {
            if (runtimeProvider != null)
            {
                var baseType = runtimeProvider.GetBaseType(enclosingType);
                while (!string.IsNullOrEmpty(baseType) && profile == null)
                {
                    profile = projectModel.GetMergedContainerProfile(baseType, containerVarName);
                    if (profile != null)
                    {
                        var baseScript = project.ScriptFiles.FirstOrDefault(s => s.TypeName == baseType);
                        if (baseScript != null) file = baseScript;
                        break;
                    }
                    baseType = runtimeProvider.GetBaseType(baseType);
                }
            }

            if (profile == null)
            {
                var script = project.ScriptFiles.FirstOrDefault(s => s.TypeName == enclosingType)
                    ?? file;
                var model = projectModel.GetSemanticModel(script) ?? script.SemanticModel;
                var flowType = model?.GetFlowVariableType(containerVarName, null);
                if (flowType?.DeclaredType != null && flowType.DeclaredType.DisplayName?.Contains("[") == true)
                {
                    var declLine = FindVariableDeclarationLine(script, containerVarName);
                    chain.Add(new GDCallSiteProvenanceEntry(
                        script.FullPath ?? file.FullPath ?? "",
                        declLine,
                        $"var {containerVarName}: {flowType.DeclaredType.DisplayName}")
                    { IsExplicitType = true });
                    return chain;
                }
                return chain;
            }
        }

        var elementType = profile.ComputeInferredType();
        if (elementType?.HasElementTypes == true)
        {
            var elType = elementType.EffectiveElementType?.DisplayName;
            if (!string.IsNullOrEmpty(elType) && elType != "Variant")
            {
                chain.Add(new GDCallSiteProvenanceEntry(
                    file.FullPath ?? "",
                    profile.DeclarationLine + 1,
                    $"var {containerVarName} ~: Array[{elType}]")
                { IsExplicitType = false });
                return chain;
            }
        }

        foreach (var usage in profile.ValueUsages)
        {
            if (usage.Node == null) continue;
            if (usage.Kind != GDContainerUsageKind.Append
                && usage.Kind != GDContainerUsageKind.PushBack
                && usage.Kind != GDContainerUsageKind.PushFront) continue;

            var callNode = usage.Node is GDCallExpression ce
                ? ce : FindParentOfType<GDCallExpression>(usage.Node);
            if (callNode == null) continue;

            var args = callNode.Parameters?.ToList();
            if (args == null || args.Count == 0) continue;
            if (args[0] is not GDIdentifierExpression appendedIdent) continue;

            var appendedVarName = appendedIdent.Identifier?.Sequence;
            if (string.IsNullOrEmpty(appendedVarName)) continue;

            var method = FindEnclosingMethod(usage.Node);
            if (method == null) continue;

            var paramIdx = FindParameterIndex(method, appendedVarName);
            if (paramIdx < 0) continue;

            var methodName = method.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName)) continue;

            try
            {
                var connections = projectModel.SignalConnectionRegistry
                    .GetSignalsCallingMethod(enclosingType, methodName);
                foreach (var conn in connections)
                {
                    if (string.IsNullOrEmpty(conn.EmitterType)) continue;
                    var signalParams = GetSignalParameterTypes(
                        project, runtimeProvider, conn.EmitterType, conn.SignalName);
                    if (signalParams != null && paramIdx < signalParams.Count)
                    {
                        var paramType = signalParams[paramIdx];
                        if (!string.IsNullOrEmpty(paramType) && paramType != "Variant")
                        {
                            var usageLine = (usage.Node.AllTokens.FirstOrDefault()?.StartLine ?? 0) + 1;
                            chain.Add(new GDCallSiteProvenanceEntry(
                                file.FullPath ?? "", usageLine,
                                $"{containerVarName}.append({appendedVarName}) " +
                                $"<- {methodName}({appendedVarName}: {paramType}) " +
                                $"<- {conn.EmitterType}.{conn.SignalName} signal"));
                        }
                    }
                }
            }
            catch { }
        }

        return chain;
    }

    internal static IReadOnlyList<string>? GetSignalParameterTypes(
        GDScriptProject project,
        IGDRuntimeProvider? runtimeProvider,
        string emitterType, string signalName)
    {
        if (runtimeProvider == null)
            return null;

        var visited = new HashSet<string>();
        var current = emitterType;
        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            var member = runtimeProvider.GetMember(current, signalName);
            if (member?.Kind == GDRuntimeMemberKind.Signal && member.Parameters != null && member.Parameters.Count > 0)
            {
                return member.Parameters.Select(p => p.Type ?? "Variant").ToList();
            }

            var script = project.GetScriptByTypeName(current);
            if (script?.Class != null)
            {
                var signalDecl = script.Class.Members
                    .OfType<GDSignalDeclaration>()
                    .FirstOrDefault(s => s.Identifier?.Sequence == signalName);
                if (signalDecl?.Parameters != null)
                {
                    return signalDecl.Parameters
                        .Select(p => p.Type?.BuildName() ?? "Variant")
                        .ToList();
                }
            }

            current = runtimeProvider.GetBaseType(current);
        }

        return null;
    }

    internal static string? ExtractSignalParamType(string expression)
    {
        var arrowIdx = expression.IndexOf("-> ");
        if (arrowIdx < 0) return null;

        var colonIdx = expression.LastIndexOf(": ");
        if (colonIdx < arrowIdx) return null;

        var closeParen = expression.IndexOf(')', colonIdx);
        if (closeParen < 0) return null;

        return expression.Substring(colonIdx + 2, closeParen - colonIdx - 2).Trim();
    }

    internal static GDMethodDeclaration? FindEnclosingMethod(GDNode? node)
    {
        var current = node;
        while (current != null)
        {
            if (current is GDMethodDeclaration method)
                return method;
            current = current.Parent;
        }
        return null;
    }

    internal static GDForStatement? FindEnclosingForStatement(GDNode? node, string varName)
    {
        var current = node;
        while (current != null)
        {
            if (current is GDForStatement forStmt && forStmt.Variable?.Sequence == varName)
                return forStmt;
            if (current is GDMethodDeclaration)
                break;
            current = current.Parent;
        }
        return null;
    }

    internal static int FindParameterIndex(GDMethodDeclaration method, string paramName)
    {
        if (method.Parameters == null)
            return -1;

        int index = 0;
        foreach (var param in method.Parameters)
        {
            if (param is GDParameterDeclaration pd && pd.Identifier?.Sequence == paramName)
                return index;
            index++;
        }
        return -1;
    }

    internal static int FindVariableDeclarationLine(GDScriptFile script, string varName)
    {
        if (script.Class == null) return 0;
        foreach (var member in script.Class.Members)
        {
            if (member is GDVariableDeclaration varDecl
                && varDecl.Identifier?.Sequence == varName)
                return varDecl.Identifier.StartLine + 1;
        }
        return 0;
    }

    internal static GDMethodDeclaration? FindMethodAtLine(GDScriptFile file, int line1Based)
    {
        if (file.Class == null) return null;
        var line0 = line1Based - 1;
        foreach (var method in file.Class.Members.OfType<GDMethodDeclaration>())
        {
            if (method.StartLine <= line0 && line0 <= method.EndLine)
                return method;
        }
        return null;
    }

    internal static GDForStatement? FindForStatementAtLine(GDScriptFile file, string varName, int line1Based)
    {
        var method = FindMethodAtLine(file, line1Based);
        if (method?.Statements == null) return null;
        return method.AllNodes.OfType<GDForStatement>()
            .FirstOrDefault(f => f.Variable?.Sequence == varName
                && f.StartLine <= line1Based - 1 && line1Based - 1 <= f.EndLine);
    }

    private static T? FindParentOfType<T>(GDSyntaxToken? node) where T : GDNode
    {
        var current = node?.Parent;
        while (current != null)
        {
            if (current is T target)
                return target;
            current = current.Parent;
        }
        return null;
    }
}
