using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for code actions.
/// Returns quick fixes based on diagnostics from GDDiagnosticsService.
/// </summary>
public class GDCodeActionHandler : IGDCodeActionHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDDiagnosticsService _diagnosticsService;

    public GDCodeActionHandler(GDScriptProject project)
    {
        _project = project;
        _diagnosticsService = new GDDiagnosticsService();
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDCodeAction> GetCodeActions(string filePath, int startLine, int endLine)
    {
        var script = _project.GetScript(filePath);
        if (script?.Class == null)
            return [];

        var actions = new List<GDCodeAction>();

        // Get diagnostics for this file
        var result = _diagnosticsService.Diagnose(script);

        foreach (var diagnostic in result.Diagnostics)
        {
            // Skip diagnostics outside the requested range
            if (diagnostic.StartLine < startLine || diagnostic.StartLine > endLine)
                continue;

            // Convert each fix descriptor to a code action
            foreach (var fix in diagnostic.FixDescriptors)
            {
                var action = ConvertFixToCodeAction(fix, diagnostic);
                if (action != null)
                    actions.Add(action);
            }
        }

        return actions;
    }

    /// <summary>
    /// Converts a fix descriptor to a code action.
    /// </summary>
    protected virtual GDCodeAction? ConvertFixToCodeAction(GDFixDescriptor fix, GDUnifiedDiagnostic diagnostic)
    {
        return fix switch
        {
            GDTextEditFixDescriptor textEdit => CreateTextEditAction(textEdit, diagnostic),
            GDSuppressionFixDescriptor suppression => CreateSuppressionAction(suppression, diagnostic),
            GDTypoFixDescriptor typo => CreateTypoFixAction(typo, diagnostic),
            GDTypeGuardFixDescriptor typeGuard => CreateTypeGuardAction(typeGuard, diagnostic),
            GDMethodGuardFixDescriptor methodGuard => CreateMethodGuardAction(methodGuard, diagnostic),
            _ => null
        };
    }

    private static GDCodeAction CreateTextEditAction(GDTextEditFixDescriptor fix, GDUnifiedDiagnostic diagnostic)
    {
        var edit = new GDCodeActionEdit
        {
            StartLine = fix.Line,
            StartColumn = fix.StartColumn,
            EndLine = fix.Line,
            EndColumn = fix.EndColumn,
            NewText = fix.NewText
        };

        return new GDCodeAction
        {
            Title = fix.Title,
            Kind = GDCodeActionKind.QuickFix,
            IsPreferred = true,
            DiagnosticCode = diagnostic.Code,
            Edits = [edit]
        };
    }

    private static GDCodeAction CreateSuppressionAction(GDSuppressionFixDescriptor fix, GDUnifiedDiagnostic diagnostic)
    {
        GDCodeActionEdit edit;

        if (fix.IsInline)
        {
            // Add comment at end of line
            edit = new GDCodeActionEdit
            {
                StartLine = fix.TargetLine,
                StartColumn = 10000, // Will be clamped to end of line
                EndLine = fix.TargetLine,
                EndColumn = 10000,
                NewText = $"  # gd:ignore {fix.DiagnosticCode}"
            };
        }
        else
        {
            // Add comment on line above
            edit = new GDCodeActionEdit
            {
                StartLine = fix.TargetLine,
                StartColumn = 0,
                EndLine = fix.TargetLine,
                EndColumn = 0,
                NewText = $"# gd:ignore {fix.DiagnosticCode}\n"
            };
        }

        return new GDCodeAction
        {
            Title = fix.Title,
            Kind = GDCodeActionKind.QuickFix,
            IsPreferred = false, // Suppression is not preferred over actual fixes
            DiagnosticCode = diagnostic.Code,
            Edits = [edit]
        };
    }

    private static GDCodeAction CreateTypoFixAction(GDTypoFixDescriptor fix, GDUnifiedDiagnostic diagnostic)
    {
        var edit = new GDCodeActionEdit
        {
            StartLine = fix.Line,
            StartColumn = fix.StartColumn,
            EndLine = fix.Line,
            EndColumn = fix.EndColumn,
            NewText = fix.SuggestedName
        };

        return new GDCodeAction
        {
            Title = fix.Title,
            Kind = GDCodeActionKind.QuickFix,
            IsPreferred = true,
            DiagnosticCode = diagnostic.Code,
            Edits = [edit]
        };
    }

    private static GDCodeAction CreateTypeGuardAction(GDTypeGuardFixDescriptor fix, GDUnifiedDiagnostic diagnostic)
    {
        // Generate: if variable is TypeName:
        var indent = new string('\t', fix.IndentLevel);
        var guardText = $"{indent}if {fix.VariableName} is {fix.TypeName}:\n";

        var edit = new GDCodeActionEdit
        {
            StartLine = fix.StatementLine,
            StartColumn = 0,
            EndLine = fix.StatementLine,
            EndColumn = 0,
            NewText = guardText
        };

        return new GDCodeAction
        {
            Title = fix.Title,
            Kind = GDCodeActionKind.QuickFix,
            IsPreferred = false,
            DiagnosticCode = diagnostic.Code,
            Edits = [edit]
        };
    }

    private static GDCodeAction CreateMethodGuardAction(GDMethodGuardFixDescriptor fix, GDUnifiedDiagnostic diagnostic)
    {
        // Generate: if variable.has_method("methodName"):
        var indent = new string('\t', fix.IndentLevel);
        var guardText = $"{indent}if {fix.VariableName}.has_method(\"{fix.MethodName}\"):\n";

        var edit = new GDCodeActionEdit
        {
            StartLine = fix.StatementLine,
            StartColumn = 0,
            EndLine = fix.StatementLine,
            EndColumn = 0,
            NewText = guardText
        };

        return new GDCodeAction
        {
            Title = fix.Title,
            Kind = GDCodeActionKind.QuickFix,
            IsPreferred = false,
            DiagnosticCode = diagnostic.Code,
            Edits = [edit]
        };
    }
}
