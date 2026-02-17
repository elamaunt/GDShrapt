using GDShrapt.Reader;
using GDShrapt.Semantics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class FindReferencesCommand : Command
{
    private GDFindReferencesService? _service;
    private ReferencesDock _referencesDock;

    public FindReferencesCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
    }

    private GDFindReferencesService GetService()
    {
        return _service ??= new GDFindReferencesService(Plugin.ScriptProject);
    }

    internal void SetReferencesDock(ReferencesDock dock)
    {
        _referencesDock = dock;
    }

    public override Task Execute(IScriptEditor controller)
    {
        Logger.Info("Find References requested");

        if (!controller.IsValid)
        {
            Logger.Info("Find References cancelled: Editor is not valid");
            return Task.CompletedTask;
        }

        var line = controller.CursorLine;
        var column = controller.CursorColumn;

        var @class = controller.GetClass();
        if (@class == null)
        {
            Logger.Info("Find References cancelled: no class declaration");
            return Task.CompletedTask;
        }

        // Use GDPositionFinder for optimized identifier lookup
        var finder = new GDPositionFinder(@class);
        var identifier = finder.FindIdentifierAtPosition(line, column);

        if (identifier == null)
        {
            Logger.Info("Find References cancelled: no identifier at cursor");
            return Task.CompletedTask;
        }

        var symbolName = identifier.Sequence;
        Logger.Info($"Finding references for '{symbolName}'");

        // Build refactoring context for semantics service
        var contextBuilder = new GDPluginRefactoringContextBuilder(Plugin.ScriptProject);
        var semanticsContext = contextBuilder.BuildSemanticsContext(controller);

        if (semanticsContext == null)
        {
            Logger.Info("Find References cancelled: could not build refactoring context");
            return Task.CompletedTask;
        }

        var service = GetService();

        // Determine the symbol scope using the semantics service
        var symbolScope = service.DetermineSymbolScope(semanticsContext);
        if (symbolScope == null)
        {
            Logger.Info("Find References cancelled: could not determine symbol scope");
            return Task.CompletedTask;
        }

        Logger.Info($"Symbol scope: {symbolScope.Type}");

        var allReferences = new List<ReferenceItem>();

        // Collect references from semantics service (includes cross-file when project context is available)
        var result = service.FindReferencesForScope(semanticsContext, symbolScope);
        if (result.Success)
        {
            foreach (var reference in result.StrictReferences)
            {
                var refItem = ConvertToReferenceItem(reference, isStrict: true);
                if (refItem != null)
                    allReferences.Add(refItem);
            }

            foreach (var reference in result.PotentialReferences)
            {
                var refItem = ConvertToReferenceItem(reference, isStrict: false);
                if (refItem != null)
                    allReferences.Add(refItem);
            }
        }

        if (allReferences.Count == 0)
        {
            Logger.Info($"No references found for '{symbolName}'");
        }
        else
        {
            Logger.Info($"Found {allReferences.Count} references for '{symbolName}'");
        }

        // Show the references dock
        if (_referencesDock != null)
        {
            _referencesDock.ShowReferences(symbolName, allReferences);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Converts a semantics reference to a UI reference item.
    /// </summary>
    private ReferenceItem ConvertToReferenceItem(Semantics.GDReferenceLocation reference, bool isStrict)
    {
        var context = GetContextWithHighlight(reference.Node, reference.SymbolName, out var hlStart, out var hlEnd);
        var kind = ConvertReferenceKind(reference.Kind);
        var endColumn = reference.Node?.EndColumn ?? reference.Column;

        return new ReferenceItem(
            reference.FilePath,
            reference.Line,
            reference.Column,
            endColumn,
            context,
            kind,
            hlStart,
            hlEnd
        );
    }

    /// <summary>
    /// Converts semantics reference kind to UI reference kind.
    /// </summary>
    private static GDPluginReferenceKind ConvertReferenceKind(GDReferenceKind kind)
    {
        return kind switch
        {
            GDReferenceKind.Declaration => GDPluginReferenceKind.Declaration,
            GDReferenceKind.Read => GDPluginReferenceKind.Read,
            GDReferenceKind.Write => GDPluginReferenceKind.Write,
            GDReferenceKind.Call => GDPluginReferenceKind.Call,
            _ => GDPluginReferenceKind.Read
        };
    }

    /// <summary>
    /// Gets the context line and calculates highlight positions.
    /// </summary>
    private static string GetContextWithHighlight(GDNode node, string symbolName, out int highlightStart, out int highlightEnd)
    {
        highlightStart = 0;
        highlightEnd = 0;

        if (node == null)
            return symbolName ?? "";

        // For nodes, get statement context
        var current = node;
        while (current != null && !(current is GDStatement) && !(current is GDClassMember))
        {
            current = current.Parent as GDNode;
        }

        if (current != null)
        {
            var text = current.ToString();
            var wasTruncated = false;

            if (text.Length > 60)
            {
                text = text.Substring(0, 57) + "...";
                wasTruncated = true;
            }

            text = text.Trim().Replace("\n", " ").Replace("\r", "");

            // Find the symbol within the context text
            if (!string.IsNullOrEmpty(symbolName))
            {
                var idx = text.IndexOf(symbolName, System.StringComparison.Ordinal);
                if (idx >= 0 && (!wasTruncated || idx + symbolName.Length <= 57))
                {
                    highlightStart = idx;
                    highlightEnd = idx + symbolName.Length;
                }
            }

            return text;
        }

        highlightEnd = symbolName?.Length ?? 0;
        return symbolName ?? "";
    }

}
