using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for file dependency analysis.
/// Wraps GDDependencyService.
/// </summary>
public class GDDependencyHandler : IGDDependencyHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDProjectSemanticModel _projectModel;
    protected readonly GDDependencyService _service;

    public GDDependencyHandler(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel ?? throw new ArgumentNullException(nameof(projectModel));
        _project = projectModel.Project;
        _service = projectModel.Dependencies;
    }

    /// <inheritdoc />
    public virtual GDFileDependencyInfo AnalyzeFile(string filePath)
    {
        var file = _project.GetScript(filePath);
        if (file == null)
            return new GDFileDependencyInfo(filePath);

        return _service.AnalyzeFile(file);
    }

    /// <inheritdoc />
    public virtual GDProjectDependencyReport AnalyzeProject()
    {
        return _service.AnalyzeProject();
    }

    /// <inheritdoc />
    public virtual GDFileDependencyEdges GetFileEdges(string filePath)
    {
        var normalizedPath = filePath.Replace('\\', '/');
        var outgoing = new List<GDFileDependencyEdge>();
        var incoming = new List<GDFileDependencyEdge>();

        // Get project report for full graph
        var report = _service.AnalyzeProject();

        // Find info for the target file
        var targetInfo = report.GetFile(normalizedPath);

        if (targetInfo != null)
        {
            // Outgoing: code edges from this file
            if (!string.IsNullOrEmpty(targetInfo.ExtendsScript))
            {
                outgoing.Add(new GDFileDependencyEdge
                {
                    FromPath = normalizedPath,
                    ToPath = targetInfo.ExtendsScript,
                    Kind = GDDependencyEdgeKind.ExtendsPath
                });
            }
            else if (targetInfo.ExtendsProjectClass && !string.IsNullOrEmpty(targetInfo.ExtendsClass))
            {
                outgoing.Add(new GDFileDependencyEdge
                {
                    FromPath = normalizedPath,
                    ToPath = targetInfo.ExtendsClass,
                    Kind = GDDependencyEdgeKind.Extends,
                    Detail = targetInfo.ExtendsClass
                });
            }

            foreach (var preload in targetInfo.Preloads)
            {
                outgoing.Add(new GDFileDependencyEdge
                {
                    FromPath = normalizedPath,
                    ToPath = preload,
                    Kind = GDDependencyEdgeKind.Preload,
                    Detail = preload
                });
            }

            foreach (var load in targetInfo.Loads)
            {
                outgoing.Add(new GDFileDependencyEdge
                {
                    FromPath = normalizedPath,
                    ToPath = load,
                    Kind = GDDependencyEdgeKind.Load,
                    Detail = load
                });
            }
        }

        // Incoming: code edges from other files pointing at this file
        foreach (var file in report.Files)
        {
            if (PathEquals(file.FilePath, normalizedPath))
                continue;

            if (!string.IsNullOrEmpty(file.ExtendsScript) && PathEquals(file.ExtendsScript, normalizedPath))
            {
                incoming.Add(new GDFileDependencyEdge
                {
                    FromPath = file.FilePath,
                    ToPath = normalizedPath,
                    Kind = GDDependencyEdgeKind.ExtendsPath
                });
            }

            foreach (var preload in file.Preloads)
            {
                if (PathEquals(preload, normalizedPath))
                {
                    incoming.Add(new GDFileDependencyEdge
                    {
                        FromPath = file.FilePath,
                        ToPath = normalizedPath,
                        Kind = GDDependencyEdgeKind.Preload
                    });
                }
            }

            foreach (var load in file.Loads)
            {
                if (PathEquals(load, normalizedPath))
                {
                    incoming.Add(new GDFileDependencyEdge
                    {
                        FromPath = file.FilePath,
                        ToPath = normalizedPath,
                        Kind = GDDependencyEdgeKind.Load
                    });
                }
            }
        }

        // Signal edges
        var signalRegistry = _projectModel.SignalConnectionRegistry;
        if (signalRegistry != null)
        {
            var allConnections = signalRegistry.GetAllConnections();
            foreach (var conn in allConnections)
            {
                if (PathEquals(conn.SourceFilePath, normalizedPath))
                {
                    outgoing.Add(new GDFileDependencyEdge
                    {
                        FromPath = normalizedPath,
                        ToPath = conn.EmitterType ?? "",
                        Kind = conn.IsSceneConnection ? GDDependencyEdgeKind.SignalScene : GDDependencyEdgeKind.SignalCode,
                        Detail = conn.SignalName
                    });
                }
            }
        }

        // Scene edges
        var sceneFlow = _projectModel.SceneFlow;
        if (sceneFlow != null)
        {
            var sceneReport = sceneFlow.AnalyzeProject();
            foreach (var edge in sceneReport.AllEdges)
            {
                if (PathEquals(edge.SourceScene, normalizedPath))
                {
                    var kind = edge.EdgeType switch
                    {
                        GDSceneFlowEdgeType.StaticSubScene => GDDependencyEdgeKind.SceneSubScene,
                        _ => GDDependencyEdgeKind.SceneScript
                    };
                    outgoing.Add(new GDFileDependencyEdge
                    {
                        FromPath = normalizedPath,
                        ToPath = edge.TargetScene,
                        Kind = kind,
                        Detail = edge.NodePathInParent
                    });
                }
                else if (PathEquals(edge.TargetScene, normalizedPath))
                {
                    var kind = edge.EdgeType switch
                    {
                        GDSceneFlowEdgeType.StaticSubScene => GDDependencyEdgeKind.SceneSubScene,
                        _ => GDDependencyEdgeKind.SceneScript
                    };
                    incoming.Add(new GDFileDependencyEdge
                    {
                        FromPath = edge.SourceScene,
                        ToPath = normalizedPath,
                        Kind = kind,
                        Detail = edge.NodePathInParent
                    });
                }
            }
        }

        return new GDFileDependencyEdges
        {
            FilePath = normalizedPath,
            Outgoing = outgoing,
            Incoming = incoming
        };
    }

    private static bool PathEquals(string? a, string? b)
    {
        if (a == null || b == null) return false;
        return string.Equals(a.Replace('\\', '/'), b.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }
}
