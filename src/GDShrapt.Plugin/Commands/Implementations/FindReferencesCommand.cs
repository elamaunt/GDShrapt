using GDShrapt.Reader;
using GDShrapt.Semantics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class FindReferencesCommand : Command
{
    private readonly GDFindReferencesService _service = new();
    private ReferencesDock _referencesDock;

    public FindReferencesCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
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
        var contextBuilder = new RefactoringContextBuilder(Plugin.ScriptProject);
        var semanticsContext = contextBuilder.BuildSemanticsContext(controller);

        if (semanticsContext == null)
        {
            Logger.Info("Find References cancelled: could not build refactoring context");
            return Task.CompletedTask;
        }

        // Determine the symbol scope using the semantics service
        var symbolScope = _service.DetermineSymbolScope(semanticsContext);
        if (symbolScope == null)
        {
            Logger.Info("Find References cancelled: could not determine symbol scope");
            return Task.CompletedTask;
        }

        Logger.Info($"Symbol scope: {symbolScope.Type}");

        var allReferences = new List<ReferenceItem>();

        // Collect references from semantics service for current file
        var result = _service.FindReferencesForScope(semanticsContext, symbolScope);
        if (result.Success)
        {
            // Convert semantics references to UI reference items
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

        // For class members and project-wide symbols, also search other scripts
        if (symbolScope.Type == GDSymbolScopeType.ClassMember ||
            symbolScope.Type == GDSymbolScopeType.ExternalMember ||
            symbolScope.Type == GDSymbolScopeType.ProjectWide)
        {
            CollectCrossFileReferences(symbolName, symbolScope, controller.ScriptFile, allReferences);
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
    private ReferenceItem ConvertToReferenceItem(GDFoundReference reference, bool isStrict)
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
    private static ReferenceKind ConvertReferenceKind(GDReferenceKind kind)
    {
        return kind switch
        {
            GDReferenceKind.Declaration => ReferenceKind.Declaration,
            GDReferenceKind.Read => ReferenceKind.Read,
            GDReferenceKind.Write => ReferenceKind.Write,
            GDReferenceKind.Call => ReferenceKind.Call,
            _ => ReferenceKind.Read
        };
    }

    /// <summary>
    /// Collects references from other scripts in the project.
    /// This requires project-level access which is Plugin-specific.
    /// </summary>
    private void CollectCrossFileReferences(
        string symbolName,
        GDSymbolScope scope,
        GDScriptFile currentScript,
        List<ReferenceItem> allReferences)
    {
        // Get the type name for class member lookups
        var typeName = currentScript?.TypeName;
        var isPublic = scope.IsPublic;

        foreach (var script in Plugin.ScriptProject.ScriptFiles)
        {
            // Skip current script (already processed by service)
            if (script == currentScript || script.Class == null) continue;

            var filePath = script.FullPath;

            if (scope.Type == GDSymbolScopeType.ClassMember && isPublic && !string.IsNullOrEmpty(typeName))
            {
                // Look for member access expressions like: instance.symbolName
                foreach (var memberOp in script.Class.AllNodes.OfType<GDMemberOperatorExpression>())
                {
                    if (memberOp.Identifier?.Sequence == symbolName)
                    {
                        // Check if the caller type matches
                        var analyzer = script.Analyzer;
                        if (analyzer != null)
                        {
                            var callerType = analyzer.GetTypeForNode(memberOp.CallerExpression);
                            if (callerType == typeName)
                            {
                                var context = GetContextWithHighlight(memberOp.Identifier, symbolName, out var hlStart, out var hlEnd);
                                allReferences.Add(new ReferenceItem(
                                    filePath,
                                    memberOp.Identifier.StartLine,
                                    memberOp.Identifier.StartColumn,
                                    memberOp.Identifier.EndColumn,
                                    context,
                                    DetermineReferenceKind(memberOp.Identifier),
                                    hlStart,
                                    hlEnd
                                ));
                            }
                        }
                    }
                }
            }
            else if (scope.Type == GDSymbolScopeType.ExternalMember && scope.CallerTypeName != null)
            {
                // Find all references to this member on objects of the same type
                foreach (var memberOp in script.Class.AllNodes.OfType<GDMemberOperatorExpression>())
                {
                    if (memberOp.Identifier?.Sequence != symbolName) continue;

                    var analyzer = script.Analyzer;
                    if (analyzer != null)
                    {
                        var type = analyzer.GetTypeForNode(memberOp.CallerExpression);
                        if (type == scope.CallerTypeName)
                        {
                            var context = GetContextWithHighlight(memberOp.Identifier, symbolName, out var hlStart, out var hlEnd);
                            allReferences.Add(new ReferenceItem(
                                filePath,
                                memberOp.Identifier.StartLine,
                                memberOp.Identifier.StartColumn,
                                memberOp.Identifier.EndColumn,
                                context,
                                DetermineReferenceKind(memberOp.Identifier),
                                hlStart,
                                hlEnd
                            ));
                        }
                    }
                }

                // Also find the declaration in the target type
                var targetScript = Plugin.ScriptProject.GetScriptByTypeName(scope.CallerTypeName);
                if (targetScript?.Class != null && targetScript != currentScript)
                {
                    var targetFilePath = targetScript.FullPath;

                    foreach (var member in targetScript.Class.Members.OfType<GDIdentifiableClassMember>())
                    {
                        if (member.Identifier?.Sequence == symbolName)
                        {
                            var context = GetContextWithHighlight(member.Identifier, symbolName, out var hlStart, out var hlEnd);
                            allReferences.Add(new ReferenceItem(
                                targetFilePath,
                                member.Identifier.StartLine,
                                member.Identifier.StartColumn,
                                member.Identifier.EndColumn,
                                context,
                                ReferenceKind.Declaration,
                                hlStart,
                                hlEnd
                            ));
                        }
                    }
                }
            }
            else if (scope.Type == GDSymbolScopeType.ProjectWide)
            {
                // Fallback: search all scripts for matching identifiers
                foreach (var token in script.Class.AllTokens)
                {
                    if (token is GDIdentifier id && id.Sequence == symbolName)
                    {
                        var kind = DetermineReferenceKind(id);
                        var context = GetContextWithHighlight(id, symbolName, out var hlStart, out var hlEnd);

                        allReferences.Add(new ReferenceItem(
                            filePath,
                            id.StartLine,
                            id.StartColumn,
                            id.EndColumn,
                            context,
                            kind,
                            hlStart,
                            hlEnd
                        ));
                    }
                }
            }
        }
    }

    private static ReferenceKind DetermineReferenceKind(GDIdentifier identifier)
    {
        var parent = identifier.Parent;

        // Check if it's a declaration
        if (parent is GDMethodDeclaration)
            return ReferenceKind.Declaration;
        if (parent is GDVariableDeclaration)
            return ReferenceKind.Declaration;
        if (parent is GDVariableDeclarationStatement)
            return ReferenceKind.Declaration;
        if (parent is GDSignalDeclaration)
            return ReferenceKind.Declaration;
        if (parent is GDParameterDeclaration)
            return ReferenceKind.Declaration;
        if (parent is GDEnumDeclaration)
            return ReferenceKind.Declaration;
        if (parent is GDInnerClassDeclaration)
            return ReferenceKind.Declaration;

        // Check if it's a call
        if (parent is GDIdentifierExpression idExpr)
        {
            if (idExpr.Parent is GDCallExpression)
                return ReferenceKind.Call;
        }

        if (parent is GDMemberOperatorExpression memberOp)
        {
            if (memberOp.Parent is GDCallExpression)
                return ReferenceKind.Call;
        }

        return ReferenceKind.Read;
    }

    /// <summary>
    /// Gets the context line and calculates highlight positions for an identifier token.
    /// </summary>
    private static string GetContextWithHighlight(GDIdentifier identifier, string symbolName, out int highlightStart, out int highlightEnd)
    {
        return GetIdentifierContext(identifier, symbolName, out highlightStart, out highlightEnd);
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

    private static string GetIdentifierContext(GDIdentifier identifier, string symbolName, out int highlightStart, out int highlightEnd)
    {
        highlightStart = 0;
        highlightEnd = 0;

        var parent = identifier.Parent;

        if (parent is GDMethodDeclaration method)
        {
            var text = $"func {method.Identifier?.Sequence ?? ""}(...)";
            highlightStart = 5; // After "func "
            highlightEnd = highlightStart + (method.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDVariableDeclaration variable)
        {
            var text = $"var {variable.Identifier?.Sequence ?? ""}";
            highlightStart = 4; // After "var "
            highlightEnd = highlightStart + (variable.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDVariableDeclarationStatement localVar)
        {
            var text = $"var {localVar.Identifier?.Sequence ?? ""}";
            highlightStart = 4; // After "var "
            highlightEnd = highlightStart + (localVar.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDSignalDeclaration signal)
        {
            var text = $"signal {signal.Identifier?.Sequence ?? ""}";
            highlightStart = 7; // After "signal "
            highlightEnd = highlightStart + (signal.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDParameterDeclaration param)
        {
            var text = $"param {param.Identifier?.Sequence ?? ""}";
            highlightStart = 6; // After "param "
            highlightEnd = highlightStart + (param.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        // For expressions, try to get statement context
        var current = parent;
        while (current != null && !(current is GDStatement) && !(current is GDClassMember))
        {
            current = current.Parent;
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
            var idx = text.IndexOf(symbolName, System.StringComparison.Ordinal);
            if (idx >= 0 && (!wasTruncated || idx + symbolName.Length <= 57))
            {
                highlightStart = idx;
                highlightEnd = idx + symbolName.Length;
            }

            return text;
        }

        highlightEnd = symbolName?.Length ?? 0;
        return symbolName ?? "";
    }
}
