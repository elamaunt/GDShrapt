using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// AST visitor that collects scene instantiation, add_child, set_script, and removal patterns.
/// </summary>
internal class GDSceneInstantiationCollector : GDVisitor
{
    public List<GDInstantiationInfo> Instantiations { get; } = new();
    public List<GDAddChildInfo> AddChildCalls { get; } = new();
    public List<GDSetScriptInfo> SetScriptCalls { get; } = new();
    public List<GDRemoveNodeInfo> RemoveNodeCalls { get; } = new();

    private int _conditionalDepth;

    public override void Visit(GDCallExpression node)
    {
        var callName = GDNodePathExtractor.GetCallName(node);

        if (callName == "instantiate")
            TryCollectInstantiation(node);
        else if (callName == "add_child" || callName == "add_sibling")
            TryCollectAddChild(node, callName);
        else if (callName == "set_script")
            TryCollectSetScript(node);
        else if (callName == "queue_free" || callName == "remove_child" || callName == "free")
            TryCollectRemoval(node, callName);

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

    private void TryCollectInstantiation(GDCallExpression call)
    {
        var caller = call.CallerExpression;
        if (caller is not GDMemberOperatorExpression memberExpr)
            return;

        string? scenePath = null;
        var confidence = GDTypeConfidence.Low;

        // Direct: preload("scene.tscn").instantiate()
        if (memberExpr.CallerExpression is GDCallExpression preloadCall)
        {
            var preloadName = GDNodePathExtractor.GetCallName(preloadCall);
            if (GDWellKnownFunctions.IsResourceLoader(preloadName))
            {
                scenePath = GDNodePathExtractor.ExtractResourcePath(preloadCall);
                confidence = preloadName == GDWellKnownFunctions.Preload
                    ? GDTypeConfidence.High
                    : GDTypeConfidence.Medium;
            }
        }
        // Variable: scene_var.instantiate()
        else if (memberExpr.CallerExpression is GDIdentifierExpression identExpr)
        {
            var varName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var classDecl = call.RootClassDeclaration;
                if (classDecl != null)
                {
                    var initExpr = FindVariableInitializer(classDecl, varName);
                    if (initExpr is GDCallExpression varPreload)
                    {
                        var varPreloadName = GDNodePathExtractor.GetCallName(varPreload);
                        if (GDWellKnownFunctions.IsResourceLoader(varPreloadName))
                        {
                            scenePath = GDNodePathExtractor.ExtractResourcePath(varPreload);
                            confidence = varPreloadName == GDWellKnownFunctions.Preload
                                ? GDTypeConfidence.High
                                : GDTypeConfidence.Medium;
                        }
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(scenePath) && (scenePath.EndsWith(".tscn") || scenePath.EndsWith(".scn")))
        {
            Instantiations.Add(new GDInstantiationInfo
            {
                ScenePath = scenePath,
                Confidence = confidence,
                IsConditional = _conditionalDepth > 0,
                LineNumber = GetLineNumber(call)
            });
        }
    }

    private void TryCollectAddChild(GDCallExpression call, string callName)
    {
        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return;

        // Determine parent node path from caller
        string? parentPath = null;
        var caller = call.CallerExpression;
        if (caller is GDMemberOperatorExpression memberExpr)
        {
            if (memberExpr.CallerExpression is GDIdentifierExpression identExpr)
                parentPath = identExpr.Identifier?.Sequence;
            else if (memberExpr.CallerExpression is GDGetNodeExpression getNodeExpr)
                parentPath = GDNodePathExtractor.ExtractFromGetNodeExpression(getNodeExpr);
        }

        AddChildCalls.Add(new GDAddChildInfo
        {
            ParentPath = parentPath,
            IsConditional = _conditionalDepth > 0,
            LineNumber = GetLineNumber(call)
        });
    }

    private void TryCollectSetScript(GDCallExpression call)
    {
        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return;

        // Extract script path from argument: set_script(preload("script.gd"))
        string? scriptPath = null;
        var confidence = GDTypeConfidence.Low;

        if (args[0] is GDCallExpression preloadCall)
        {
            var preloadName = GDNodePathExtractor.GetCallName(preloadCall);
            if (GDWellKnownFunctions.IsResourceLoader(preloadName))
            {
                scriptPath = GDNodePathExtractor.ExtractResourcePath(preloadCall);
                confidence = preloadName == GDWellKnownFunctions.Preload
                    ? GDTypeConfidence.High
                    : GDTypeConfidence.Medium;
            }
        }

        if (string.IsNullOrEmpty(scriptPath))
            return;

        // Determine which node/variable set_script is called on
        string? nodeVariable = null;
        var caller = call.CallerExpression;
        if (caller is GDMemberOperatorExpression memberExpr)
        {
            if (memberExpr.CallerExpression is GDIdentifierExpression identExpr)
                nodeVariable = identExpr.Identifier?.Sequence;
            else if (memberExpr.CallerExpression is GDGetNodeExpression getNodeExpr)
                nodeVariable = GDNodePathExtractor.ExtractFromGetNodeExpression(getNodeExpr);
        }

        SetScriptCalls.Add(new GDSetScriptInfo
        {
            NodeVariable = nodeVariable,
            ScriptPath = scriptPath,
            Confidence = confidence,
            IsConditional = _conditionalDepth > 0,
            LineNumber = GetLineNumber(call)
        });
    }

    private void TryCollectRemoval(GDCallExpression call, string callName)
    {
        string? nodePath = null;

        if (callName == "queue_free" || callName == "free")
        {
            // self.queue_free() or $Node.queue_free()
            var caller = call.CallerExpression;
            if (caller is GDMemberOperatorExpression memberExpr)
            {
                if (memberExpr.CallerExpression is GDIdentifierExpression identExpr)
                    nodePath = identExpr.Identifier?.Sequence;
                else if (memberExpr.CallerExpression is GDGetNodeExpression getNodeExpr)
                    nodePath = GDNodePathExtractor.ExtractFromGetNodeExpression(getNodeExpr);
            }
        }
        else if (callName == "remove_child")
        {
            // remove_child(node)
            var args = call.Parameters?.ToList();
            if (args != null && args.Count > 0 && args[0] is GDIdentifierExpression argIdent)
                nodePath = argIdent.Identifier?.Sequence;
        }

        RemoveNodeCalls.Add(new GDRemoveNodeInfo
        {
            NodePath = nodePath,
            Method = callName,
            IsConditional = _conditionalDepth > 0,
            LineNumber = GetLineNumber(call)
        });
    }

    private static GDExpression? FindVariableInitializer(GDClassDeclaration classDecl, string variableName)
    {
        if (classDecl.Members == null)
            return null;

        foreach (var member in classDecl.Members)
        {
            if (member is GDVariableDeclaration varDecl &&
                varDecl.Identifier?.Sequence == variableName)
            {
                return varDecl.Initializer;
            }
        }

        return null;
    }

    private static int GetLineNumber(GDNode node)
    {
        // Try to get line number from token position
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

internal class GDInstantiationInfo
{
    public string ScenePath { get; init; } = "";
    public GDTypeConfidence Confidence { get; init; }
    public bool IsConditional { get; init; }
    public int LineNumber { get; init; }
}

internal class GDAddChildInfo
{
    public string? ParentPath { get; init; }
    public bool IsConditional { get; init; }
    public int LineNumber { get; init; }
}

internal class GDSetScriptInfo
{
    public string? NodeVariable { get; init; }
    public string ScriptPath { get; init; } = "";
    public GDTypeConfidence Confidence { get; init; }
    public bool IsConditional { get; init; }
    public int LineNumber { get; init; }
}

internal class GDRemoveNodeInfo
{
    public string? NodePath { get; init; }
    public string Method { get; init; } = "";
    public bool IsConditional { get; init; }
    public int LineNumber { get; init; }
}
