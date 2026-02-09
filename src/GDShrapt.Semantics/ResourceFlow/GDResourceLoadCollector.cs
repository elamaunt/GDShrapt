using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// AST visitor that collects resource loading patterns: preload(), load(), ResourceLoader.load().
/// Excludes .tscn/.scn (handled by SceneFlow) and .gd (handled by dependencies).
/// </summary>
internal class GDResourceLoadCollector : GDVisitor
{
    public List<GDCodeResourceLoad> ResourceLoads { get; } = new();

    private int _conditionalDepth;

    public override void Visit(GDCallExpression node)
    {
        var callName = GDNodePathExtractor.GetCallName(node);

        if (GDWellKnownFunctions.IsResourceLoader(callName))
            TryCollectDirectLoad(node, callName);
        else if (callName == "load" && IsResourceLoaderCall(node))
            TryCollectResourceLoaderLoad(node);

        base.Visit(node);
    }

    public override void Visit(GDIfStatement s)
    {
        _conditionalDepth++;
        base.Visit(s);
        _conditionalDepth--;
    }

    public override void Visit(GDMatchStatement s)
    {
        _conditionalDepth++;
        base.Visit(s);
        _conditionalDepth--;
    }

    private void TryCollectDirectLoad(GDCallExpression call, string callName)
    {
        var resourcePath = GDNodePathExtractor.ExtractResourcePath(call);
        if (string.IsNullOrEmpty(resourcePath))
            return;

        if (IsSceneOrScriptPath(resourcePath))
            return;

        var source = callName == GDWellKnownFunctions.Preload
            ? GDResourceReferenceSource.CodePreload
            : GDResourceReferenceSource.CodeLoad;

        var confidence = callName == GDWellKnownFunctions.Preload
            ? GDTypeConfidence.High
            : GDTypeConfidence.Medium;

        // Check for variable assignment: var tex = preload("...")
        string? variableName = null;
        if (call.Parent is GDVariableDeclaration varDecl)
            variableName = varDecl.Identifier?.Sequence;

        ResourceLoads.Add(new GDCodeResourceLoad
        {
            ResourcePath = resourcePath,
            Source = source,
            Confidence = confidence,
            VariableName = variableName,
            IsConditional = _conditionalDepth > 0,
            LineNumber = GetLineNumber(call)
        });
    }

    private void TryCollectResourceLoaderLoad(GDCallExpression call)
    {
        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return;

        string? resourcePath = null;
        if (args[0] is GDStringExpression strExpr)
            resourcePath = strExpr.String?.Sequence;
        else if (args[0] is GDStringNameExpression strNameExpr)
            resourcePath = strNameExpr.String?.Sequence;

        if (string.IsNullOrEmpty(resourcePath) || IsSceneOrScriptPath(resourcePath))
            return;

        ResourceLoads.Add(new GDCodeResourceLoad
        {
            ResourcePath = resourcePath,
            Source = GDResourceReferenceSource.CodeResourceLoader,
            Confidence = GDTypeConfidence.Medium,
            IsConditional = _conditionalDepth > 0,
            LineNumber = GetLineNumber(call)
        });
    }

    private static bool IsResourceLoaderCall(GDCallExpression call)
    {
        if (call.CallerExpression is GDMemberOperatorExpression memberExpr &&
            memberExpr.CallerExpression is GDIdentifierExpression idExpr &&
            idExpr.Identifier?.Sequence == "ResourceLoader" &&
            memberExpr.Identifier?.Sequence == "load")
            return true;
        return false;
    }

    private static bool IsSceneOrScriptPath(string path)
    {
        return path.EndsWith(".tscn") || path.EndsWith(".scn") || path.EndsWith(".gd");
    }

    private static int GetLineNumber(GDNode node)
    {
        var tokens = node.AllTokens;
        if (tokens != null)
        {
            foreach (var token in tokens)
            {
                if (token is GDSyntaxToken syntaxToken && syntaxToken.StartLine > 0)
                    return syntaxToken.StartLine;
            }
        }
        return 0;
    }
}

internal class GDCodeResourceLoad
{
    public string ResourcePath { get; init; } = "";
    public GDResourceReferenceSource Source { get; init; }
    public GDTypeConfidence Confidence { get; init; }
    public string? VariableName { get; init; }
    public bool IsConditional { get; init; }
    public int LineNumber { get; init; }
}
