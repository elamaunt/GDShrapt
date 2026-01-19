using System.Collections.Generic;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Unified access to diagnostics and validation services.
/// Combines syntax checking, validation, linting, and code fixes.
///
/// <para>
/// Obtain this through <see cref="GDProjectSemanticModel.Diagnostics"/>.
/// </para>
///
/// <example>
/// <code>
/// var model = GDProjectSemanticModel.Load("/path/to/project");
///
/// // Validate a single file
/// var result = model.Diagnostics.ValidateFile(scriptFile);
///
/// // Validate entire project
/// var projectResult = model.Diagnostics.ValidateProject();
///
/// // Get code fixes for a diagnostic
/// var fixes = model.Diagnostics.GetFixes(diagnostic);
/// </code>
/// </example>
/// </summary>
public class GDDiagnosticsServices
{
    private readonly GDScriptProject _project;
    private GDDiagnosticsService? _diagnosticsService;
    private GDFixProvider? _fixProvider;

    internal GDDiagnosticsServices(GDScriptProject project)
    {
        _project = project ?? throw new System.ArgumentNullException(nameof(project));
    }

    /// <summary>
    /// Gets the underlying diagnostics service.
    /// Created lazily with default options.
    /// </summary>
    public GDDiagnosticsService Service => _diagnosticsService ??= new GDDiagnosticsService();

    /// <summary>
    /// Configures the diagnostics service from project configuration.
    /// </summary>
    /// <param name="config">Project configuration.</param>
    public void Configure(GDProjectConfig config)
    {
        _diagnosticsService = GDDiagnosticsService.FromConfig(config);
    }

    /// <summary>
    /// Gets the fix provider for generating code fixes.
    /// </summary>
    public GDFixProvider FixProvider => _fixProvider ??= new GDFixProvider();

    /// <summary>
    /// Validates a single file and returns unified diagnostics.
    /// Combines syntax, scope, type, and semantic validation.
    /// </summary>
    /// <param name="file">The script file to validate.</param>
    /// <returns>Diagnostics result with errors, warnings, and hints.</returns>
    public GDDiagnosticsResult ValidateFile(GDScriptFile file)
    {
        if (file == null)
            return new GDDiagnosticsResult();

        return Service.Diagnose(file);
    }

    /// <summary>
    /// Validates a parsed class declaration.
    /// </summary>
    /// <param name="classDecl">The class declaration to validate.</param>
    /// <returns>Diagnostics result.</returns>
    public GDDiagnosticsResult ValidateClass(GDClassDeclaration classDecl)
    {
        if (classDecl == null)
            return new GDDiagnosticsResult();

        return Service.Diagnose(classDecl);
    }

    /// <summary>
    /// Validates all files in the project.
    /// </summary>
    /// <returns>Aggregated diagnostics for all project files.</returns>
    public GDProjectDiagnosticsResult ValidateProject()
    {
        var result = new GDProjectDiagnosticsResult();

        foreach (var file in _project.ScriptFiles)
        {
            var fileResult = ValidateFile(file);
            result.AddFile(file.FullPath ?? file.Reference?.FullPath ?? "unknown", fileResult);
        }

        return result;
    }

    /// <summary>
    /// Gets available code fixes for a diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to get fixes for.</param>
    /// <param name="node">The AST node associated with the diagnostic.</param>
    /// <param name="analyzer">Optional member access analyzer for enhanced fixes.</param>
    /// <param name="runtimeProvider">Optional runtime provider for type-aware fixes.</param>
    /// <returns>List of available code fixes.</returns>
    public IEnumerable<GDFixDescriptor> GetFixes(
        GDUnifiedDiagnostic diagnostic,
        GDNode? node = null,
        IGDMemberAccessAnalyzer? analyzer = null,
        IGDRuntimeProvider? runtimeProvider = null)
    {
        if (diagnostic == null || node == null)
            return System.Array.Empty<GDFixDescriptor>();

        return FixProvider.GetFixes(diagnostic.Code, node, analyzer, runtimeProvider);
    }
}

/// <summary>
/// Aggregated diagnostics result for all files in a project.
/// </summary>
public class GDProjectDiagnosticsResult
{
    private readonly Dictionary<string, GDDiagnosticsResult> _fileResults = new();

    /// <summary>
    /// Diagnostics grouped by file path.
    /// </summary>
    public IReadOnlyDictionary<string, GDDiagnosticsResult> FileResults => _fileResults;

    /// <summary>
    /// Total error count across all files.
    /// </summary>
    public int TotalErrors => GetTotalCount(GDUnifiedDiagnosticSeverity.Error);

    /// <summary>
    /// Total warning count across all files.
    /// </summary>
    public int TotalWarnings => GetTotalCount(GDUnifiedDiagnosticSeverity.Warning);

    /// <summary>
    /// Total hint count across all files.
    /// </summary>
    public int TotalHints => GetTotalCount(GDUnifiedDiagnosticSeverity.Hint);

    /// <summary>
    /// Total diagnostic count across all files.
    /// </summary>
    public int TotalDiagnostics
    {
        get
        {
            var count = 0;
            foreach (var result in _fileResults.Values)
                count += result.Diagnostics.Count;
            return count;
        }
    }

    /// <summary>
    /// Whether the project has any errors.
    /// </summary>
    public bool HasErrors => TotalErrors > 0;

    /// <summary>
    /// Whether the project is valid (no errors).
    /// </summary>
    public bool IsValid => !HasErrors;

    /// <summary>
    /// Adds diagnostics for a file.
    /// </summary>
    internal void AddFile(string filePath, GDDiagnosticsResult result)
    {
        _fileResults[filePath] = result;
    }

    private int GetTotalCount(GDUnifiedDiagnosticSeverity severity)
    {
        var count = 0;
        foreach (var result in _fileResults.Values)
        {
            foreach (var diag in result.Diagnostics)
            {
                if (diag.Severity == severity)
                    count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Gets all diagnostics across all files.
    /// </summary>
    public IEnumerable<(string FilePath, GDUnifiedDiagnostic Diagnostic)> AllDiagnostics
    {
        get
        {
            foreach (var (path, result) in _fileResults)
            {
                foreach (var diag in result.Diagnostics)
                {
                    yield return (path, diag);
                }
            }
        }
    }
}
