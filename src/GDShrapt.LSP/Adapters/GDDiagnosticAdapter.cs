using System.Collections.Generic;
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
                // Token is 0-based, ToLspRange expects 1-based Line (converts to 0-based LSP)
                Range = GDLocationAdapter.ToLspRange(
                    token.StartLine + 1,  // Convert 0-based to 1-based
                    token.StartColumn,    // Keep 0-based
                    token.EndLine + 1,    // Convert 0-based to 1-based
                    token.EndColumn),     // Keep 0-based
                Severity = GDLspDiagnosticSeverity.Error,
                Source = "gdshrapt",
                Code = "GDS001",
                Message = $"Syntax error: unexpected token '{token}'"
            });
        }

        return diagnostics.ToArray();
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
    /// Line is 1-based, Column is 0-based.
    /// </summary>
    public static GDLspDiagnostic Error(int line, int column, string code, string message)
    {
        return new GDLspDiagnostic
        {
            Range = new GDLspRange(line - 1, column, line - 1, column + 1),  // Line: 1-based → 0-based
            Severity = GDLspDiagnosticSeverity.Error,
            Source = "gdshrapt",
            Code = code,
            Message = message
        };
    }

    /// <summary>
    /// Creates a warning diagnostic.
    /// Line is 1-based, Column is 0-based.
    /// </summary>
    public static GDLspDiagnostic Warning(int line, int column, string code, string message)
    {
        return new GDLspDiagnostic
        {
            Range = new GDLspRange(line - 1, column, line - 1, column + 1),  // Line: 1-based → 0-based
            Severity = GDLspDiagnosticSeverity.Warning,
            Source = "gdshrapt",
            Code = code,
            Message = message
        };
    }
}
