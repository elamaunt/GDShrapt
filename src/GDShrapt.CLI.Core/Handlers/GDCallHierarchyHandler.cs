using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for call hierarchy navigation.
/// Converts positions and delegates to GDCallHierarchyService.
/// </summary>
public class GDCallHierarchyHandler : IGDCallHierarchyHandler
{
    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel _projectModel;
    private readonly GDCallHierarchyService _service;

    public GDCallHierarchyHandler(GDScriptProject project, GDProjectSemanticModel projectModel)
    {
        _project = project;
        _projectModel = projectModel;
        _service = projectModel.Services.CallHierarchy;
    }

    /// <inheritdoc />
    public GDCallHierarchyItem? Prepare(string filePath, int line, int column)
    {
        var script = _project.GetScript(filePath);
        if (script?.Class == null)
            return null;

        var cursor = new GDCursorPosition(line - 1, column - 1);
        var context = new GDRefactoringContext(script, script.Class, cursor, GDSelectionInfo.None, _project);

        var symbol = _service.ResolveMethodSymbol(context);
        if (symbol == null)
            return null;

        return new GDCallHierarchyItem
        {
            Name = symbol.Name,
            ClassName = symbol.ClassName,
            FilePath = symbol.FilePath,
            Line = symbol.Line + 1,
            Column = symbol.Column
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<GDIncomingCall> GetIncomingCalls(GDCallHierarchyItem item)
    {
        var entries = _service.GetCallers(item.ClassName, item.Name);
        if (entries.Count == 0)
            return [];

        return entries.Select(e => new GDIncomingCall
        {
            From = new GDCallHierarchyItem
            {
                Name = e.Symbol.Name,
                ClassName = e.Symbol.ClassName,
                FilePath = e.Symbol.FilePath,
                Line = e.Symbol.Line + 1,
                Column = e.Symbol.Column
            },
            FromRanges = e.CallSites.Select(cs => new GDCallRange
            {
                Line = cs.Line + 1,
                Column = cs.Column
            }).ToList()
        }).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<GDOutgoingCall> GetOutgoingCalls(GDCallHierarchyItem item)
    {
        var entries = _service.GetCallees(item.FilePath, item.Name);
        if (entries.Count == 0)
            return [];

        return entries.Select(e => new GDOutgoingCall
        {
            To = new GDCallHierarchyItem
            {
                Name = e.Symbol.Name,
                ClassName = e.Symbol.ClassName,
                FilePath = e.Symbol.FilePath,
                Line = e.Symbol.Line + 1,
                Column = e.Symbol.Column
            },
            FromRanges = e.CallSites.Select(cs => new GDCallRange
            {
                Line = cs.Line + 1,
                Column = cs.Column
            }).ToList()
        }).ToList();
    }
}
