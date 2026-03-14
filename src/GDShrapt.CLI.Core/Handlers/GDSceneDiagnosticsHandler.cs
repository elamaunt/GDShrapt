using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Validates scene files (.tscn/.tres) and reports diagnostics.
/// Currently checks signal connections target existing methods (GD4013).
/// </summary>
public class GDSceneDiagnosticsHandler
{
    private readonly GDScriptProject _project;
    private IGDRuntimeProvider? _runtimeProvider;

    public GDSceneDiagnosticsHandler(GDScriptProject project)
    {
        _project = project;
    }

    public List<GDUnifiedDiagnostic> AnalyzeScene(string filePath)
    {
        var diagnostics = new List<GDUnifiedDiagnostic>();
        var sceneProvider = _project.SceneTypesProvider;
        if (sceneProvider == null)
            return diagnostics;

        var resPath = sceneProvider.ToResourcePath(filePath);
        if (string.IsNullOrEmpty(resPath))
            return diagnostics;

        var sceneInfo = sceneProvider.GetSceneInfo(resPath);
        if (sceneInfo == null)
            return diagnostics;

        ValidateSignalConnections(sceneInfo, diagnostics);

        return diagnostics;
    }

    private void ValidateSignalConnections(GDSceneInfo sceneInfo, List<GDUnifiedDiagnostic> diagnostics)
    {
        foreach (var conn in sceneInfo.SignalConnections)
        {
            if (string.IsNullOrEmpty(conn.Method) || conn.LineNumber <= 0)
                continue;

            var targetNode = sceneInfo.Nodes.FirstOrDefault(n =>
                n.Path != null && n.Path.Equals(conn.ToNode, StringComparison.Ordinal));

            if (targetNode?.ScriptPath == null)
                continue;

            var script = _project.GetScriptByResourcePath(targetNode.ScriptPath);
            if (script?.Class == null)
                continue;

            if (HasMethod(script, conn.Method))
                continue;

            diagnostics.Add(new GDUnifiedDiagnostic
            {
                Code = $"GD{(int)GDDiagnosticCode.InvalidSignalConnectionMethod:D4}",
                Message = $"Signal connection targets method '{conn.Method}' which does not exist in '{Path.GetFileName(targetNode.ScriptPath)}'",
                Severity = GDUnifiedDiagnosticSeverity.Error,
                Source = GDDiagnosticSource.SemanticValidator,
                StartLine = conn.LineNumber,
                StartColumn = conn.MethodColumn,
                EndLine = conn.LineNumber,
                EndColumn = conn.MethodColumn + conn.Method.Length
            });
        }
    }

    private bool HasMethod(GDScriptFile script, string methodName)
    {
        // Check script's own methods
        if (script.Class!.Methods.Any(m =>
            m.Identifier?.Sequence != null &&
            m.Identifier.Sequence.Equals(methodName, StringComparison.Ordinal)))
            return true;

        // Check base class chain via runtime type info
        var extendsType = script.Class.Extends?.Type?.ToString();
        if (!string.IsNullOrEmpty(extendsType))
        {
            if (HasMethodInBaseChain(extendsType, methodName))
                return true;
        }

        return false;
    }

    private bool HasMethodInBaseChain(string typeName, string methodName)
    {
        try
        {
            _runtimeProvider ??= _project.CreateRuntimeProvider();
        }
        catch
        {
            return false;
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = typeName;
        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            var typeInfo = _runtimeProvider.GetTypeInfo(current);
            if (typeInfo == null)
                break;

            if (typeInfo.Members != null &&
                typeInfo.Members.Any(m =>
                    m.Kind == GDRuntimeMemberKind.Method &&
                    m.Name.Equals(methodName, StringComparison.Ordinal)))
                return true;

            current = typeInfo.BaseType;
        }

        return false;
    }

    public static bool IsSceneFile(string filePath)
    {
        return filePath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith(".tres", StringComparison.OrdinalIgnoreCase);
    }
}
