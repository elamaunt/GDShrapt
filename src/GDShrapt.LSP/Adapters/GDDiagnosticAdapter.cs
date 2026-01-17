using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

/// <summary>
/// Adapts GDShrapt validation results to LSP diagnostics.
/// </summary>
public static class GDDiagnosticAdapter
{
    /// <summary>
    /// Converts GDShrapt invalid tokens to LSP diagnostics.
    /// </summary>
    public static GDLspDiagnostic[] FromInvalidTokens(GDClassDeclaration? classDecl, string filePath)
    {
        if (classDecl == null)
            return [];

        var diagnostics = new List<GDLspDiagnostic>();

        foreach (var token in classDecl.AllInvalidTokens)
        {
            diagnostics.Add(new GDLspDiagnostic
            {
                Range = GDLocationAdapter.ToLspRange(
                    token.StartLine,
                    token.StartColumn,
                    token.EndLine,
                    token.EndColumn),
                Severity = GDLspDiagnosticSeverity.Error,
                Source = "gdshrapt",
                Code = "GDS001",
                Message = $"Syntax error: unexpected token '{token}'"
            });
        }

        return diagnostics.ToArray();
    }

    /// <summary>
    /// Converts GDShrapt script analysis to LSP diagnostics.
    /// Uses default validation without linting.
    /// </summary>
    public static GDLspDiagnostic[] FromScript(GDScriptFile script)
    {
        // Use default diagnostics service (validator only, no linter)
        var service = new GDDiagnosticsService();
        return FromScript(script, service);
    }

    /// <summary>
    /// Converts GDShrapt script analysis to LSP diagnostics using a diagnostics service.
    /// This method uses GDDiagnosticsService which includes both validator and linter.
    /// </summary>
    public static GDLspDiagnostic[] FromScript(GDScriptFile script, GDDiagnosticsService service)
    {
        var result = service.Diagnose(script);
        return result.Diagnostics.Select(d => FromUnifiedDiagnostic(d)).ToArray();
    }

    /// <summary>
    /// Converts a unified diagnostic to LSP diagnostic.
    /// </summary>
    public static GDLspDiagnostic FromUnifiedDiagnostic(GDUnifiedDiagnostic diagnostic)
    {
        var severity = diagnostic.Severity switch
        {
            GDUnifiedDiagnosticSeverity.Error => GDLspDiagnosticSeverity.Error,
            GDUnifiedDiagnosticSeverity.Warning => GDLspDiagnosticSeverity.Warning,
            GDUnifiedDiagnosticSeverity.Info => GDLspDiagnosticSeverity.Information,
            GDUnifiedDiagnosticSeverity.Hint => GDLspDiagnosticSeverity.Hint,
            _ => GDLspDiagnosticSeverity.Information
        };

        return new GDLspDiagnostic
        {
            Range = GDLocationAdapter.ToLspRange(
                diagnostic.StartLine,
                diagnostic.StartColumn,
                diagnostic.EndLine,
                diagnostic.EndColumn),
            Severity = severity,
            Source = "gdshrapt",
            Code = diagnostic.Code,
            Message = diagnostic.Message
        };
    }

    /// <summary>
    /// Converts GDShrapt validation results to LSP diagnostics.
    /// </summary>
    public static IEnumerable<GDLspDiagnostic> FromValidation(GDValidationResult validationResult)
    {
        foreach (var diagnostic in validationResult.Diagnostics)
        {
            var severity = diagnostic.Severity switch
            {
                Reader.GDDiagnosticSeverity.Error => GDLspDiagnosticSeverity.Error,
                Reader.GDDiagnosticSeverity.Warning => GDLspDiagnosticSeverity.Warning,
                Reader.GDDiagnosticSeverity.Hint => GDLspDiagnosticSeverity.Hint,
                _ => GDLspDiagnosticSeverity.Information
            };

            yield return new GDLspDiagnostic
            {
                Range = GDLocationAdapter.ToLspRange(
                    diagnostic.StartLine,
                    diagnostic.StartColumn,
                    diagnostic.EndLine,
                    diagnostic.EndColumn),
                Severity = severity,
                Source = "gdshrapt",
                Code = diagnostic.CodeString,
                Message = diagnostic.Message
            };
        }
    }

    /// <summary>
    /// Creates an LSP diagnostic from raw values.
    /// </summary>
    public static GDLspDiagnostic Create(
        int startLine, int startColumn, int endLine, int endColumn,
        GDLspDiagnosticSeverity severity, string code, string message)
    {
        return new GDLspDiagnostic
        {
            Range = GDLocationAdapter.ToLspRange(startLine, startColumn, endLine, endColumn),
            Severity = severity,
            Source = "gdshrapt",
            Code = code,
            Message = message
        };
    }

    /// <summary>
    /// Creates an error diagnostic.
    /// </summary>
    public static GDLspDiagnostic Error(int line, int column, string code, string message)
    {
        return new GDLspDiagnostic
        {
            Range = new GDLspRange(line - 1, column - 1, line - 1, column),
            Severity = GDLspDiagnosticSeverity.Error,
            Source = "gdshrapt",
            Code = code,
            Message = message
        };
    }

    /// <summary>
    /// Creates a warning diagnostic.
    /// </summary>
    public static GDLspDiagnostic Warning(int line, int column, string code, string message)
    {
        return new GDLspDiagnostic
        {
            Range = new GDLspRange(line - 1, column - 1, line - 1, column),
            Severity = GDLspDiagnosticSeverity.Warning,
            Source = "gdshrapt",
            Code = code,
            Message = message
        };
    }
}
