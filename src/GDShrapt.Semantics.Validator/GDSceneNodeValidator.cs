using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Validates node path expressions ($Path, %Unique, get_node()) against scene data.
/// Reports GD4011 (InvalidNodePath) and GD4012 (InvalidUniqueNode).
/// </summary>
public class GDSceneNodeValidator : GDValidationVisitor
{
    private readonly GDSemanticModel _semanticModel;
    private readonly GDProjectSemanticModel _projectModel;
    private readonly GDDiagnosticSeverity _severity;

    public GDSceneNodeValidator(
        GDValidationContext context,
        GDSemanticModel semanticModel,
        GDSemanticValidatorOptions options)
        : base(context)
    {
        _semanticModel = semanticModel;
        _projectModel = options.ProjectModel!;
        _severity = options.NodePathSeverity;
    }

    public void Validate(GDNode? node)
    {
        node?.WalkIn(this);
    }

    public override void Visit(GDGetNodeExpression expr)
    {
        var nodePath = GDNodePathExtractor.ExtractFromGetNodeExpression(expr);
        if (string.IsNullOrEmpty(nodePath))
            return;

        ValidateNodePath(nodePath, expr);
    }

    public override void Visit(GDGetUniqueNodeExpression expr)
    {
        var nodeName = GDNodePathExtractor.ExtractFromUniqueNodeExpression(expr);
        if (string.IsNullOrEmpty(nodeName))
            return;

        ValidateUniqueNode(nodeName, expr);
    }

    public override void Visit(GDCallExpression call)
    {
        var callName = GDNodePathExtractor.GetCallName(call);

        // Only validate get_node() — skip get_node_or_null() and find_node() as they're designed for optional access
        if (callName != "get_node")
            return;

        Func<string, GDExpression?>? resolveVariable = null;
        var classDecl = call.RootClassDeclaration;
        if (classDecl != null)
        {
            resolveVariable = varName =>
                GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, varName);
        }

        var nodePath = GDNodePathExtractor.ExtractFromCallExpression(call, resolveVariable);
        if (string.IsNullOrEmpty(nodePath))
            return;

        ValidateNodePath(nodePath, call);
    }

    private void ValidateNodePath(string nodePath, GDNode reportNode)
    {
        var sceneProvider = _projectModel.Project.SceneTypesProvider;
        if (sceneProvider == null)
            return;

        var scriptPath = _semanticModel.ScriptFile?.ResPath;
        if (string.IsNullOrEmpty(scriptPath))
            return;

        var scenes = sceneProvider.GetScenesForScript(scriptPath).ToList();
        if (scenes.Count == 0)
            return;

        var missingScenes = new List<string>();

        foreach (var (scenePath, scriptNodePath) in scenes)
        {
            var fullPath = ResolveRelativePath(scriptNodePath, nodePath);
            var nodeType = sceneProvider.GetNodeType(scenePath, fullPath);
            if (string.IsNullOrEmpty(nodeType))
                missingScenes.Add(scenePath);
        }

        if (missingScenes.Count == scenes.Count)
        {
            var message = scenes.Count == 1
                ? $"Node path '{nodePath}' not found in scene '{GetSceneName(scenes[0].scenePath)}'"
                : $"Node path '{nodePath}' not found in any of {scenes.Count} scenes using this script";

            ReportDiagnostic(GDDiagnosticCode.InvalidNodePath, message, reportNode);
        }
    }

    private void ValidateUniqueNode(string nodeName, GDNode reportNode)
    {
        var sceneProvider = _projectModel.Project.SceneTypesProvider;
        if (sceneProvider == null)
            return;

        var scriptPath = _semanticModel.ScriptFile?.ResPath;
        if (string.IsNullOrEmpty(scriptPath))
            return;

        var scenes = sceneProvider.GetScenesForScript(scriptPath).ToList();
        if (scenes.Count == 0)
            return;

        var missingScenes = new List<string>();

        foreach (var (scenePath, _) in scenes)
        {
            var nodeType = sceneProvider.GetUniqueNodeType(scenePath, nodeName);
            if (string.IsNullOrEmpty(nodeType))
                missingScenes.Add(scenePath);
        }

        if (missingScenes.Count == scenes.Count)
        {
            var message = scenes.Count == 1
                ? $"Unique node '%{nodeName}' not found in scene '{GetSceneName(scenes[0].scenePath)}'"
                : $"Unique node '%{nodeName}' not found in any of {scenes.Count} scenes using this script";

            ReportDiagnostic(GDDiagnosticCode.InvalidUniqueNode, message, reportNode);
        }
    }

    private void ReportDiagnostic(GDDiagnosticCode code, string message, GDNode node)
    {
        switch (_severity)
        {
            case GDDiagnosticSeverity.Error:
                ReportError(code, message, node);
                break;
            case GDDiagnosticSeverity.Warning:
                ReportWarning(code, message, node);
                break;
            case GDDiagnosticSeverity.Hint:
                ReportHint(code, message, node);
                break;
        }
    }

    private static string ResolveRelativePath(string basePath, string relativePath)
    {
        if (relativePath.StartsWith("/"))
            return relativePath.TrimStart('/');

        if (basePath == ".")
            return relativePath;

        var parts = basePath.Split('/').ToList();
        var relParts = relativePath.Split('/');

        foreach (var part in relParts)
        {
            if (part == "..")
            {
                if (parts.Count > 0)
                    parts.RemoveAt(parts.Count - 1);
            }
            else if (part != "." && !string.IsNullOrEmpty(part))
            {
                parts.Add(part);
            }
        }

        return string.Join("/", parts);
    }

    private static string GetSceneName(string scenePath)
    {
        var lastSlash = scenePath.LastIndexOf('/');
        return lastSlash >= 0 ? scenePath.Substring(lastSlash + 1) : scenePath;
    }
}
