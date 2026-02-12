using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for finding symbol references.
/// Uses GDSemanticModel for symbol lookup per Rule 11.
/// </summary>
public class GDFindRefsHandler : IGDFindRefsHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDProjectSemanticModel? _projectModel;

    public GDFindRefsHandler(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project;
        _projectModel = projectModel;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDReferenceGroup> FindReferences(string symbolName, string? filePath = null)
    {
        var rawGroups = new List<RawGroup>();

        if (!string.IsNullOrEmpty(filePath))
        {
            var script = _project.GetScript(filePath);
            if (script != null)
                CollectGroupFromScript(script, symbolName, rawGroups);
        }
        else
        {
            foreach (var script in _project.ScriptFiles)
                CollectGroupFromScript(script, symbolName, rawGroups);
        }

        return MergeOverrides(rawGroups);
    }

    private void CollectGroupFromScript(
        GDScriptFile script,
        string symbolName,
        List<RawGroup> rawGroups)
    {
        var semanticModel = script.SemanticModel;
        if (semanticModel == null)
            return;

        var symbol = semanticModel.FindSymbol(symbolName);
        if (symbol == null)
            return;

        var hasLocalDeclaration = symbol.DeclarationNode != null;
        var isOverride = hasLocalDeclaration && IsOverrideDeclaration(script, symbol);
        // Symbol used but not declared/overridden locally — inherited usage
        var isInherited = !hasLocalDeclaration && HasInheritedSymbol(script, symbolName);
        var locations = new List<GDReferenceLocation>();

        var refs = semanticModel.GetReferencesTo(symbol);

        foreach (var reference in refs)
        {
            var node = reference.ReferenceNode;
            if (node == null)
                continue;

            var isDecl = node == symbol.DeclarationNode;
            locations.Add(new GDReferenceLocation
            {
                FilePath = script.Reference.FullPath,
                Line = node.StartLine + 1,
                Column = node.StartColumn,
                IsDeclaration = isDecl,
                IsOverride = isDecl && isOverride,
                IsWrite = reference.IsWrite
            });
        }

        int declLine = 0;
        int declColumn = 0;

        if (symbol.DeclarationNode != null)
        {
            declLine = symbol.DeclarationNode.StartLine + 1;
            declColumn = symbol.DeclarationNode.StartColumn;
            var declarationIncluded = locations.Any(r =>
                r.Line == declLine &&
                r.Column == declColumn &&
                r.FilePath == script.Reference.FullPath);

            if (!declarationIncluded)
            {
                locations.Insert(0, new GDReferenceLocation
                {
                    FilePath = script.Reference.FullPath,
                    Line = declLine,
                    Column = declColumn,
                    IsDeclaration = true,
                    IsOverride = isOverride,
                    IsWrite = false
                });
            }
        }

        var extendsTypeForSuper = GetExtendsTypeName(script);
        if (!string.IsNullOrEmpty(extendsTypeForSuper))
        {
            var model = _projectModel?.GetSemanticModel(script) ?? script.SemanticModel;
            if (model != null)
            {
                var memberAccesses = model.GetMemberAccesses(extendsTypeForSuper, symbolName);
                foreach (var access in memberAccesses)
                {
                    var accessNode = access.ReferenceNode;
                    if (accessNode == null) continue;

                    int accessLine, accessColumn;
                    if (accessNode is GDMemberOperatorExpression memberOp && memberOp.Identifier != null)
                    {
                        accessLine = memberOp.Identifier.StartLine + 1;
                        accessColumn = memberOp.Identifier.StartColumn;
                    }
                    else
                    {
                        accessLine = accessNode.StartLine + 1;
                        accessColumn = accessNode.StartColumn;
                    }

                    var alreadyIncluded = locations.Any(r =>
                        r.Line == accessLine &&
                        r.Column == accessColumn &&
                        r.FilePath == script.Reference.FullPath);

                    if (!alreadyIncluded)
                    {
                        locations.Add(new GDReferenceLocation
                        {
                            FilePath = script.Reference.FullPath,
                            Line = accessLine,
                            Column = accessColumn,
                            IsSuperCall = true,
                            IsWrite = false
                        });
                    }
                }
            }
        }

        if (locations.Count > 0)
        {
            var className = script.TypeName;
            var extendsType = GetExtendsTypeName(script);

            rawGroups.Add(new RawGroup
            {
                ClassName = className,
                ExtendsType = extendsType,
                FilePath = script.Reference.FullPath,
                DeclLine = declLine,
                DeclColumn = declColumn,
                IsOverride = isOverride,
                IsInherited = isInherited,
                Locations = locations
            });
        }
    }

    private IReadOnlyList<GDReferenceGroup> MergeOverrides(List<RawGroup> rawGroups)
    {
        var roots = rawGroups.Where(g => !g.IsOverride && !g.IsInherited).ToList();
        var dependents = rawGroups.Where(g => g.IsOverride || g.IsInherited).ToList();

        var merged = new List<GDReferenceGroup>();

        foreach (var root in roots)
        {
            merged.Add(new GDReferenceGroup
            {
                ClassName = root.ClassName,
                DeclarationFilePath = root.FilePath,
                DeclarationLine = root.DeclLine,
                DeclarationColumn = root.DeclColumn,
                IsOverride = false,
                Locations = new List<GDReferenceLocation>(root.Locations)
            });
        }

        foreach (var dep in dependents)
        {
            var rootGroup = FindRootGroup(dep, merged);
            if (rootGroup != null)
            {
                rootGroup.Overrides.Add(new GDReferenceGroup
                {
                    ClassName = dep.ClassName,
                    DeclarationFilePath = dep.FilePath,
                    DeclarationLine = dep.DeclLine,
                    DeclarationColumn = dep.DeclColumn,
                    IsOverride = dep.IsOverride,
                    IsInherited = dep.IsInherited,
                    Locations = new List<GDReferenceLocation>(dep.Locations)
                });
            }
            else
            {
                merged.Add(new GDReferenceGroup
                {
                    ClassName = dep.ClassName,
                    DeclarationFilePath = dep.FilePath,
                    DeclarationLine = dep.DeclLine,
                    DeclarationColumn = dep.DeclColumn,
                    IsOverride = dep.IsOverride,
                    IsInherited = dep.IsInherited,
                    Locations = new List<GDReferenceLocation>(dep.Locations)
                });
            }
        }

        return merged;
    }

    private GDReferenceGroup? FindRootGroup(RawGroup ovr, List<GDReferenceGroup> merged)
    {
        // Walk inheritance chain from override's base type
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = ovr.ExtendsType;

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            // Check if any root group matches this type
            var match = merged.FirstOrDefault(g =>
                string.Equals(g.ClassName, current, StringComparison.Ordinal));
            if (match != null)
                return match;

            // Walk up
            var baseScript = _project.GetScriptByTypeName(current);
            if (baseScript != null)
            {
                current = GetExtendsTypeName(baseScript);
                continue;
            }

            break;
        }

        return null;
    }

    private bool IsOverrideDeclaration(GDScriptFile script, Semantics.GDSymbolInfo symbol)
    {
        if (symbol.DeclarationNode == null)
            return false;

        if (symbol.Kind != GDSymbolKind.Method && symbol.Kind != GDSymbolKind.Variable)
            return false;

        var extendsType = GetExtendsTypeName(script);
        if (string.IsNullOrEmpty(extendsType))
            return false;

        return HasMemberInBaseType(extendsType, symbol.Name);
    }

    private bool HasMemberInBaseType(string typeName, string memberName)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = typeName;

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            var baseScript = _project.GetScriptByTypeName(current);
            if (baseScript?.SemanticModel != null)
            {
                var baseSymbol = baseScript.SemanticModel.FindSymbol(memberName);
                if (baseSymbol != null && baseSymbol.DeclarationNode != null)
                    return true;

                current = GetExtendsTypeName(baseScript);
                continue;
            }

            var runtimeProvider = GetRuntimeProvider();
            if (runtimeProvider != null)
            {
                var member = runtimeProvider.GetMember(current, memberName);
                if (member != null)
                    return true;

                current = runtimeProvider.GetBaseType(current);
                continue;
            }

            break;
        }

        return false;
    }

    private bool HasInheritedSymbol(GDScriptFile script, string symbolName)
    {
        var extendsType = GetExtendsTypeName(script);
        if (string.IsNullOrEmpty(extendsType))
            return false;

        return HasMemberInBaseType(extendsType, symbolName);
    }

    private string? GetExtendsTypeName(GDScriptFile script)
    {
        var model = _projectModel?.GetSemanticModel(script) ?? script.SemanticModel;
        return model?.BaseTypeName;
    }

    private IGDRuntimeProvider? GetRuntimeProvider()
    {
        if (_projectModel != null)
        {
            foreach (var script in _project.ScriptFiles)
            {
                var model = _projectModel.GetSemanticModel(script);
                if (model?.RuntimeProvider != null)
                    return model.RuntimeProvider;
            }
        }

        return _project.ScriptFiles
            .FirstOrDefault(s => s.SemanticModel != null)
            ?.SemanticModel?.RuntimeProvider;
    }

    private class RawGroup
    {
        public string? ClassName;
        public string? ExtendsType;
        public required string FilePath;
        public int DeclLine;
        public int DeclColumn;
        public bool IsOverride;
        public bool IsInherited;
        public required List<GDReferenceLocation> Locations;
    }

}
