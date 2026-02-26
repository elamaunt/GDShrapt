using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for project-wide index queries.
/// Queries the semantic model to enumerate project entities.
/// </summary>
public class GDListHandler : IGDListHandler
{
    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel _projectModel;

    public GDListHandler(GDScriptProject project, GDProjectSemanticModel projectModel)
    {
        _project = project;
        _projectModel = projectModel;
    }

    public IReadOnlyList<GDListItemInfo> ListClasses(
        bool abstractOnly = false,
        string? extendsType = null,
        string? implementsType = null,
        bool innerOnly = false,
        bool topLevelOnly = false)
    {
        var results = new List<GDListItemInfo>();
        var projectRoot = _project.ProjectPath;
        var typeSystem = _projectModel.TypeSystem;

        foreach (var file in _project.ScriptFiles)
        {
            if (file.SemanticModel == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectRoot);
            var typeName = file.TypeName ?? Path.GetFileNameWithoutExtension(file.FullPath ?? "");

            // Top-level class
            if (!innerOnly)
            {
                var baseTypeName = file.SemanticModel.BaseTypeName;
                var typeInfo = typeSystem.ResolveType(typeName);
                var isAbstract = typeInfo?.IsAbstract ?? false;

                if (ShouldIncludeClass(typeName, isAbstract, baseTypeName, abstractOnly, extendsType, implementsType))
                {
                    results.Add(new GDListItemInfo
                    {
                        Name = typeName,
                        Kind = GDListItemKind.Class,
                        FilePath = relativePath,
                        Line = 1,
                        SemanticType = baseTypeName,
                        Metadata = isAbstract ? new Dictionary<string, string> { ["abstract"] = "true" } : null
                    });
                }
            }

            // Inner classes via semantic model
            if (!topLevelOnly)
            {
                foreach (var innerSymbol in file.SemanticModel.GetInnerClasses())
                {
                    if (innerSymbol.IsInherited)
                        continue;

                    var innerName = innerSymbol.Name;
                    if (string.IsNullOrEmpty(innerName))
                        continue;

                    var innerTypeInfo = typeSystem.ResolveType(innerName);
                    var innerBaseType = innerTypeInfo?.BaseType;
                    var innerIsAbstract = innerTypeInfo?.IsAbstract ?? false;

                    if (ShouldIncludeClass(innerName, innerIsAbstract, innerBaseType, abstractOnly, extendsType, implementsType))
                    {
                        var metadata = new Dictionary<string, string> { ["scope"] = "inner" };
                        if (innerIsAbstract)
                            metadata["abstract"] = "true";

                        results.Add(new GDListItemInfo
                        {
                            Name = innerName,
                            Kind = GDListItemKind.Class,
                            FilePath = relativePath,
                            Line = (innerSymbol.DeclarationNode?.StartLine ?? 0) + 1,
                            OwnerScope = innerSymbol.DeclaringTypeName ?? typeName,
                            SemanticType = innerBaseType,
                            Metadata = metadata
                        });
                    }
                }
            }
        }

        return results;
    }

    private bool ShouldIncludeClass(string? className, bool isAbstract, string? baseTypeName, bool abstractOnly, string? extendsType, string? implementsType)
    {
        if (abstractOnly && !isAbstract)
            return false;

        if (extendsType != null && !string.Equals(baseTypeName, extendsType, StringComparison.OrdinalIgnoreCase))
            return false;

        if (implementsType != null && className != null)
        {
            if (!_projectModel.TypeSystem.IsAssignableTo(className, implementsType))
                return false;
        }

        return true;
    }

    public IReadOnlyList<GDListItemInfo> ListSignals(string? scenePath = null, bool connectedOnly = false, bool unconnectedOnly = false)
    {
        var results = new List<GDListItemInfo>();
        var projectRoot = _project.ProjectPath;

        HashSet<string>? connectedSignals = null;
        if (connectedOnly || unconnectedOnly)
        {
            connectedSignals = new HashSet<string>(
                _projectModel.SignalConnectionRegistry.GetAllConnectedSignals(),
                StringComparer.OrdinalIgnoreCase);
        }

        foreach (var file in _project.ScriptFiles)
        {
            if (file.SemanticModel == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectRoot);
            var signals = file.SemanticModel.GetSignals();

            foreach (var signal in signals)
            {
                var isConnected = connectedSignals?.Contains(signal.Name) ?? false;

                if (connectedOnly && !isConnected)
                    continue;
                if (unconnectedOnly && isConnected)
                    continue;

                var metadata = new Dictionary<string, string>();
                if (connectedSignals != null)
                    metadata["connected"] = isConnected.ToString().ToLowerInvariant();

                results.Add(new GDListItemInfo
                {
                    Name = signal.Name,
                    Kind = GDListItemKind.Signal,
                    FilePath = relativePath,
                    Line = (signal.DeclarationNode?.StartLine ?? 0) + 1,
                    OwnerScope = file.TypeName,
                    SemanticType = signal.TypeName,
                    Metadata = metadata.Count > 0 ? metadata : null
                });
            }
        }

        return results;
    }

    public IReadOnlyList<GDListItemInfo> ListAutoloads()
    {
        var results = new List<GDListItemInfo>();

        foreach (var entry in _project.AutoloadEntries)
        {
            var metadata = new Dictionary<string, string>
            {
                ["enabled"] = entry.Enabled.ToString().ToLowerInvariant(),
                ["type"] = entry.Extension switch
                {
                    ".gd" => "script",
                    ".tscn" or ".scn" => "scene",
                    ".cs" => "csharp",
                    _ => "unknown"
                }
            };

            results.Add(new GDListItemInfo
            {
                Name = entry.Name,
                Kind = GDListItemKind.Autoload,
                FilePath = entry.Path,
                Line = 1,
                Metadata = metadata
            });
        }

        return results;
    }

    public IReadOnlyList<GDListItemInfo> ListEngineCallbacks()
    {
        var results = new List<GDListItemInfo>();
        var projectRoot = _project.ProjectPath;

        foreach (var file in _project.ScriptFiles)
        {
            if (file.SemanticModel == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectRoot);

            foreach (var method in file.SemanticModel.GetMethods())
            {
                if (method.IsInherited)
                    continue;

                if (!GDSpecialMethodHelper.IsKnownVirtualMethod(method.Name))
                    continue;

                var category = CategorizeEngineCallback(method.Name);

                results.Add(new GDListItemInfo
                {
                    Name = method.Name,
                    Kind = GDListItemKind.EngineCallback,
                    FilePath = relativePath,
                    Line = (method.DeclarationNode?.StartLine ?? 0) + 1,
                    OwnerScope = file.TypeName,
                    Metadata = new Dictionary<string, string> { ["category"] = category }
                });
            }
        }

        return results;
    }

    private static string CategorizeEngineCallback(string name)
    {
        return name switch
        {
            GDSpecialMethodHelper.Init or
            GDSpecialMethodHelper.Ready or
            GDSpecialMethodHelper.EnterTree or
            GDSpecialMethodHelper.ExitTree or
            GDSpecialMethodHelper.Notification or
            GDSpecialMethodHelper.GetConfigurationWarnings or
            GDSpecialMethodHelper.StaticInit
                => "lifecycle",

            GDSpecialMethodHelper.Process or
            GDSpecialMethodHelper.PhysicsProcess
                => "process",

            GDSpecialMethodHelper.Input or
            GDSpecialMethodHelper.UnhandledInput or
            GDSpecialMethodHelper.UnhandledKeyInput or
            GDSpecialMethodHelper.ShortcutInput
                => "input",

            GDSpecialMethodHelper.Get or
            GDSpecialMethodHelper.Set or
            GDSpecialMethodHelper.GetPropertyList or
            GDSpecialMethodHelper.PropertyCanRevert or
            GDSpecialMethodHelper.PropertyGetRevert or
            GDSpecialMethodHelper.ValidateProperty
                => "property",

            GDSpecialMethodHelper.Draw
                => "draw",

            _ => "other"
        };
    }

    public IReadOnlyList<GDListItemInfo> ListMethods(bool staticOnly = false, bool virtualOnly = false, string? visibility = null)
    {
        var results = new List<GDListItemInfo>();
        var projectRoot = _project.ProjectPath;

        foreach (var file in _project.ScriptFiles)
        {
            if (file.SemanticModel == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectRoot);
            var methods = file.SemanticModel.GetMethods();

            foreach (var method in methods)
            {
                if (method.IsInherited)
                    continue;

                if (staticOnly && !method.IsStatic)
                    continue;

                if (virtualOnly && !method.Name.StartsWith("_"))
                    continue;

                if (visibility != null)
                {
                    var isPublic = method.IsPublic;
                    if (visibility == "public" && !isPublic)
                        continue;
                    if (visibility == "private" && isPublic)
                        continue;
                }

                var metadata = new Dictionary<string, string>();
                if (method.IsStatic)
                    metadata["static"] = "true";
                if (method.ReturnTypeName != null)
                    metadata["returns"] = method.ReturnTypeName;
                if (method.Parameters != null && method.Parameters.Count > 0)
                    metadata["params"] = string.Join(", ", method.Parameters.Select(p => p.Name));

                results.Add(new GDListItemInfo
                {
                    Name = method.Name,
                    Kind = GDListItemKind.Method,
                    FilePath = relativePath,
                    Line = (method.DeclarationNode?.StartLine ?? 0) + 1,
                    OwnerScope = file.TypeName,
                    SemanticType = method.ReturnTypeName,
                    Metadata = metadata.Count > 0 ? metadata : null
                });
            }
        }

        return results;
    }

    public IReadOnlyList<GDListItemInfo> ListVariables(bool constOnly = false, bool staticOnly = false, string? visibility = null)
    {
        var results = new List<GDListItemInfo>();
        var projectRoot = _project.ProjectPath;

        foreach (var file in _project.ScriptFiles)
        {
            if (file.SemanticModel == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectRoot);

            // Include vars (unless constOnly)
            if (!constOnly)
            {
                foreach (var variable in file.SemanticModel.GetVariables())
                {
                    if (variable.IsInherited)
                        continue;

                    if (variable.ScopeType == GDSymbolScopeType.LocalVariable)
                        continue;

                    if (staticOnly && !variable.IsStatic)
                        continue;

                    if (visibility != null)
                    {
                        var isPublic = variable.IsPublic;
                        if (visibility == "public" && !isPublic) continue;
                        if (visibility == "private" && isPublic) continue;
                    }

                    var metadata = new Dictionary<string, string>();
                    if (variable.IsStatic)
                        metadata["static"] = "true";

                    results.Add(new GDListItemInfo
                    {
                        Name = variable.Name,
                        Kind = GDListItemKind.Variable,
                        FilePath = relativePath,
                        Line = (variable.DeclarationNode?.StartLine ?? 0) + 1,
                        OwnerScope = file.TypeName,
                        SemanticType = variable.TypeName,
                        Metadata = metadata.Count > 0 ? metadata : null
                    });
                }
            }

            // Include constants
            foreach (var constant in file.SemanticModel.GetConstants())
            {
                if (constant.IsInherited)
                    continue;

                if (visibility != null)
                {
                    var isPublic = constant.IsPublic;
                    if (visibility == "public" && !isPublic) continue;
                    if (visibility == "private" && isPublic) continue;
                }

                var metadata = new Dictionary<string, string> { ["const"] = "true" };
                if (constant.IsStatic)
                    metadata["static"] = "true";

                results.Add(new GDListItemInfo
                {
                    Name = constant.Name,
                    Kind = GDListItemKind.Variable,
                    FilePath = relativePath,
                    Line = (constant.DeclarationNode?.StartLine ?? 0) + 1,
                    OwnerScope = file.TypeName,
                    SemanticType = constant.TypeName,
                    Metadata = metadata
                });
            }
        }

        return results;
    }

    public IReadOnlyList<GDListItemInfo> ListExports(string? typeFilter = null)
    {
        var results = new List<GDListItemInfo>();
        var projectRoot = _project.ProjectPath;

        foreach (var file in _project.ScriptFiles)
        {
            if (file.SemanticModel == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectRoot);
            var variables = file.SemanticModel.GetVariables();

            foreach (var variable in variables)
            {
                if (variable.IsInherited)
                    continue;

                if (!file.SemanticModel.IsExportVariable(variable.Name))
                    continue;

                if (typeFilter != null && !string.Equals(variable.TypeName, typeFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                results.Add(new GDListItemInfo
                {
                    Name = variable.Name,
                    Kind = GDListItemKind.Export,
                    FilePath = relativePath,
                    Line = (variable.DeclarationNode?.StartLine ?? 0) + 1,
                    OwnerScope = file.TypeName,
                    SemanticType = variable.TypeName
                });
            }
        }

        return results;
    }

    public IReadOnlyList<GDListItemInfo> ListNodes(string scenePath, string? typeFilter = null)
    {
        var results = new List<GDListItemInfo>();
        var sceneProvider = _project.SceneTypesProvider;
        if (sceneProvider == null)
            return results;

        var nodePaths = sceneProvider.GetNodePaths(scenePath);
        foreach (var nodePath in nodePaths)
        {
            var nodeType = sceneProvider.GetNodeType(scenePath, nodePath);
            if (typeFilter != null && !string.Equals(nodeType, typeFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            var nodeName = nodePath.Contains('/') ? nodePath.Substring(nodePath.LastIndexOf('/') + 1) : nodePath;

            results.Add(new GDListItemInfo
            {
                Name = nodeName,
                Kind = GDListItemKind.Node,
                FilePath = scenePath,
                Line = 0,
                OwnerScope = scenePath,
                SemanticType = nodeType,
                Metadata = new Dictionary<string, string> { ["path"] = nodePath }
            });
        }

        return results;
    }

    public IReadOnlyList<GDListItemInfo> ListScenes()
    {
        var results = new List<GDListItemInfo>();
        var sceneProvider = _project.SceneTypesProvider;
        if (sceneProvider == null)
            return results;

        foreach (var scene in sceneProvider.AllScenes)
        {
            var metadata = new Dictionary<string, string>
            {
                ["nodes"] = scene.Nodes.Count.ToString()
            };

            var rootType = scene.Nodes.Count > 0 ? scene.Nodes[0].NodeType : null;

            results.Add(new GDListItemInfo
            {
                Name = Path.GetFileName(scene.ScenePath),
                Kind = GDListItemKind.Scene,
                FilePath = scene.ScenePath,
                Line = 1,
                SemanticType = rootType,
                Metadata = metadata
            });
        }

        return results;
    }

    public IReadOnlyList<GDListItemInfo> ListResources(bool unusedOnly = false, bool missingOnly = false, string? category = null)
    {
        var results = new List<GDListItemInfo>();
        var resourceFlow = _projectModel.ResourceFlow;

        if (unusedOnly)
        {
            foreach (var path in resourceFlow.FindUnusedResources())
            {
                results.Add(new GDListItemInfo
                {
                    Name = Path.GetFileName(path),
                    Kind = GDListItemKind.Resource,
                    FilePath = path,
                    Line = 0,
                    Metadata = new Dictionary<string, string> { ["status"] = "unused" }
                });
            }
            return results;
        }

        if (missingOnly)
        {
            foreach (var path in resourceFlow.FindMissingResources())
            {
                results.Add(new GDListItemInfo
                {
                    Name = Path.GetFileName(path),
                    Kind = GDListItemKind.Resource,
                    FilePath = path,
                    Line = 0,
                    Metadata = new Dictionary<string, string> { ["status"] = "missing" }
                });
            }
            return results;
        }

        var report = resourceFlow.AnalyzeProject();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in report.AllEdges)
        {
            if (!seen.Add(edge.ResourcePath))
                continue;

            results.Add(new GDListItemInfo
            {
                Name = Path.GetFileName(edge.ResourcePath),
                Kind = GDListItemKind.Resource,
                FilePath = edge.ResourcePath,
                Line = edge.LineNumber,
                OwnerScope = edge.ConsumerPath
            });
        }

        return results;
    }

    public IReadOnlyList<GDListItemInfo> ListEnums()
    {
        var results = new List<GDListItemInfo>();
        var projectRoot = _project.ProjectPath;

        foreach (var file in _project.ScriptFiles)
        {
            if (file.SemanticModel == null)
                continue;

            var relativePath = GetRelativePath(file.FullPath, projectRoot);
            var enums = file.SemanticModel.GetEnums();

            foreach (var enumSymbol in enums)
            {
                if (enumSymbol.IsInherited)
                    continue;

                var metadata = new Dictionary<string, string>();

                // Get enum values via semantic model
                var enumValues = file.SemanticModel.GetSymbolsOfKind(GDSymbolKind.EnumValue)
                    .Where(ev => ev.DeclaringScopeNode == enumSymbol.DeclarationNode)
                    .Select(ev => ev.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                if (enumValues.Count > 0)
                    metadata["values"] = string.Join(", ", enumValues);

                results.Add(new GDListItemInfo
                {
                    Name = enumSymbol.Name,
                    Kind = GDListItemKind.Enum,
                    FilePath = relativePath,
                    Line = (enumSymbol.DeclarationNode?.StartLine ?? 0) + 1,
                    OwnerScope = file.TypeName,
                    Metadata = metadata.Count > 0 ? metadata : null
                });
            }
        }

        return results;
    }

    private static string GetRelativePath(string? fullPath, string? basePath)
    {
        if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(basePath))
            return fullPath ?? "";

        try
        {
            return Path.GetRelativePath(basePath, fullPath);
        }
        catch (ArgumentException)
        {
            return fullPath;
        }
    }
}
