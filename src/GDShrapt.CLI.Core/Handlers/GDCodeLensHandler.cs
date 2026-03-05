using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for CodeLens operations.
/// Shows reference counts above class members, similar to Visual Studio Enterprise.
/// </summary>
public class GDCodeLensHandler : IGDCodeLensHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDProjectSemanticModel _projectModel;

    public GDCodeLensHandler(GDScriptProject project, GDProjectSemanticModel projectModel)
    {
        _project = project;
        _projectModel = projectModel;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCodeLens> GetCodeLenses(string filePath)
    {
        var script = _project.GetScript(filePath);
        var semanticModel = script?.SemanticModel;
        if (script?.Class == null || semanticModel == null)
            return [];

        var lenses = new List<GDCodeLens>();

        // CodeLens for class_name
        CollectClassNameLens(script, lenses);

        // CodeLens for all class-level members
        CollectMemberLenses(semanticModel, lenses);

        return lenses;
    }

    /// <summary>
    /// Creates a CodeLens for the class_name declaration showing cross-file reference count.
    /// </summary>
    private void CollectClassNameLens(GDScriptFile script, List<GDCodeLens> lenses)
    {
        var classNameDecl = script.Class?.ClassName;
        if (classNameDecl == null)
            return;

        var identifier = classNameDecl.Identifier;
        if (identifier == null)
            return;

        var typeName = identifier.Sequence;
        if (string.IsNullOrEmpty(typeName))
            return;

        // Count cross-file references to this type name
        var count = 0;
        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (scriptFile == script)
                continue;

            var model = _projectModel.GetSemanticModel(scriptFile);
            if (model == null)
                continue;

            var symbols = model.FindSymbols(typeName);
            foreach (var symbol in symbols)
            {
                count += model.GetReferencesTo(symbol).Count;
            }
        }

        var label = FormatReferenceLabel(count);
        lenses.Add(new GDCodeLens
        {
            Line = identifier.StartLine + 1,
            StartColumn = identifier.StartColumn + 1,
            EndColumn = identifier.StartColumn + typeName.Length + 1,
            Label = label,
            CommandName = "gdshrapt.findReferences",
            CommandArgument = typeName
        });
    }

    /// <summary>
    /// Creates CodeLens items for all class-level members (methods, variables, signals, etc.).
    /// </summary>
    private void CollectMemberLenses(GDSemanticModel semanticModel, List<GDCodeLens> lenses)
    {
        // Collect all class-level symbols (skip parameters, iterators, match bindings)
        var classLevelKinds = new HashSet<GDSymbolKind>
        {
            GDSymbolKind.Method,
            GDSymbolKind.Variable,
            GDSymbolKind.Property,
            GDSymbolKind.Signal,
            GDSymbolKind.Constant,
            GDSymbolKind.Enum,
            GDSymbolKind.Class
        };

        foreach (var symbol in semanticModel.Symbols)
        {
            if (!classLevelKinds.Contains(symbol.Kind))
                continue;

            // Skip inherited symbols
            if (symbol.IsInherited)
                continue;

            var posToken = symbol.PositionToken;
            if (posToken == null)
                continue;

            // Count references across the project
            var count = CountProjectReferences(symbol.Name);

            var label = FormatReferenceLabel(count);
            var nameLength = symbol.Name?.Length ?? 1;

            lenses.Add(new GDCodeLens
            {
                Line = posToken.StartLine + 1,
                StartColumn = posToken.StartColumn + 1,
                EndColumn = posToken.StartColumn + nameLength + 1,
                Label = label,
                CommandName = "gdshrapt.findReferences",
                CommandArgument = symbol.Name
            });
        }
    }

    /// <summary>
    /// Counts references to a symbol by name across the entire project.
    /// Uses FindSymbols (plural) to handle cases where multiple symbols share the same name.
    /// </summary>
    private int CountProjectReferences(string? symbolName)
    {
        if (string.IsNullOrEmpty(symbolName))
            return 0;

        var count = 0;
        foreach (var scriptFile in _project.ScriptFiles)
        {
            var model = _projectModel.GetSemanticModel(scriptFile);
            if (model == null)
                continue;

            var symbols = model.FindSymbols(symbolName);
            foreach (var symbol in symbols)
            {
                count += model.GetReferencesTo(symbol).Count;
            }
        }
        return count;
    }

    /// <summary>
    /// Formats the reference count label.
    /// </summary>
    protected static string FormatReferenceLabel(int count)
    {
        return count switch
        {
            0 => "0 references",
            1 => "1 reference",
            _ => $"{count} references"
        };
    }
}
