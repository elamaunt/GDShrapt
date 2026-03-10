using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Shared project initialization steps used by CLI commands and LSP server.
/// </summary>
public static class GDProjectInitializer
{
    /// <summary>
    /// Injects scene-defined signal connections into script semantic models.
    /// This enables diagnostics to recognize signal handlers connected in .tscn files.
    /// Should be called after AnalyzeAll().
    /// </summary>
    public static void InjectSceneSignalConnections(GDScriptProject project)
    {
        var sceneProvider = project.SceneTypesProvider;
        if (sceneProvider == null)
            return;

        var connectionsByType = new Dictionary<string, List<GDSignalConnectionEntry>>();

        foreach (var sceneInfo in sceneProvider.AllScenes)
        {
            foreach (var conn in sceneInfo.SignalConnections)
            {
                var toNode = sceneInfo.Nodes.FirstOrDefault(n => n.Path == conn.ToNode);
                var callbackType = toNode?.ScriptTypeName ?? toNode?.NodeType;
                if (string.IsNullOrEmpty(callbackType) || string.IsNullOrEmpty(conn.Method))
                    continue;

                var fromNode = sceneInfo.Nodes.FirstOrDefault(n => n.Path == conn.FromNode);

                if (!connectionsByType.TryGetValue(callbackType, out var list))
                {
                    list = new List<GDSignalConnectionEntry>();
                    connectionsByType[callbackType] = list;
                }

                list.Add(GDSignalConnectionEntry.FromScene(
                    sceneInfo.FullPath,
                    conn.LineNumber,
                    conn.MethodColumn,
                    fromNode?.ScriptTypeName ?? fromNode?.NodeType ?? conn.SourceNodeType ?? "",
                    conn.SignalName,
                    callbackType,
                    conn.Method));
            }
        }

        foreach (var script in project.ScriptFiles)
        {
            if (script.SemanticModel == null)
                continue;

            var typeName = script.TypeName;
            if (string.IsNullOrEmpty(typeName))
                continue;

            if (connectionsByType.TryGetValue(typeName, out var connections))
            {
                script.SemanticModel.InjectExternalSignalConnections(connections);
            }
        }
    }
}
