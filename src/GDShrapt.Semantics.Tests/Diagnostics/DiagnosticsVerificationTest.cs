using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GDShrapt.Semantics.Tests.Diagnostics;

/// <summary>
/// TDD verification test for diagnostics.
/// Ensures all diagnostics are either verified (OK), marked as false positives (FP), or skipped (SKIP).
/// Split into per-file tests via DynamicData for granular failure reporting.
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class DiagnosticsVerificationTest
{
    private static GDScriptProject _project = null!;
    private static GDProjectSemanticModel _projectModel = null!;
    private static string _projectPath = null!;
    private static List<DiagnosticInfo> _allDiagnostics = null!;
    private static Dictionary<string, Dictionary<(int, int, string), DiagnosticMarkerParser.MarkerType>> _allMarkers = null!;
    private static HashSet<(string FilePath, int Line, int Col, string Code)> _matchedMarkers = null!;
    private static bool _initialized;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        if (_initialized) return;

        _project = TestProjectFixture.Project;
        _projectModel = TestProjectFixture.ProjectModel;
        _projectPath = TestProjectFixture.ProjectPath;

        _allDiagnostics = CollectAllDiagnostics();
        _allMarkers = LoadAllMarkers(new DiagnosticMarkerParser());

        // Pre-compute matched markers for orphaned marker detection
        _matchedMarkers = new HashSet<(string, int, int, string)>();
        foreach (var diag in _allDiagnostics)
        {
            var fullPath = GetFullPath(diag.FilePath);
            if (_allMarkers.TryGetValue(fullPath, out var fileMarkers))
            {
                var key = (diag.StartLine, diag.StartColumn, diag.Code);
                if (fileMarkers.ContainsKey(key))
                {
                    _matchedMarkers.Add((fullPath, diag.StartLine, diag.StartColumn, diag.Code));
                }
            }
        }

        // Always generate verification report (artifact guarantee)
        try
        {
            GenerateVerificationReport();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to generate verification report: {ex.Message}");
        }

        _initialized = true;
    }

    /// <summary>
    /// Returns unique file paths that have diagnostics or markers.
    /// </summary>
    public static IEnumerable<object[]> GetDiagnosticFiles()
    {
        // Ensure fixture is initialized (DynamicData discovery happens before ClassInitialize)
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        var files = new HashSet<string>();

        foreach (var scriptFile in project.ScriptFiles)
        {
            if (scriptFile.FullPath == null) continue;
            var relativePath = GetRelativePath(scriptFile.FullPath, projectPath);
            if (relativePath.Contains("diagnostics/") || relativePath.Contains("diagnostics\\"))
            {
                files.Add(relativePath);
            }
        }

        // Also include files that have diagnostics but might not be in the diagnostics folder
        // (ensure we don't miss any)
        if (_allDiagnostics != null)
        {
            foreach (var diag in _allDiagnostics)
            {
                files.Add(diag.FilePath);
            }
        }

        foreach (var file in files.OrderBy(f => f))
        {
            yield return new object[] { file };
        }
    }

    /// <summary>
    /// Per-file diagnostic verification.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetDiagnosticFiles), DynamicDataSourceType.Method)]
    public void Diagnostics_File(string relativePath)
    {
        var fileDiags = _allDiagnostics.Where(d => d.FilePath == relativePath).ToList();
        var fullPath = GetFullPath(relativePath);

        var unverified = new List<DiagnosticInfo>();
        var falsePositives = new List<DiagnosticInfo>();

        foreach (var diag in fileDiags)
        {
            var key = (diag.StartLine, diag.StartColumn, diag.Code);

            if (!_allMarkers.TryGetValue(fullPath, out var fileMarkers) ||
                !fileMarkers.TryGetValue(key, out var markerType))
            {
                unverified.Add(diag);
            }
            else
            {
                switch (markerType)
                {
                    case DiagnosticMarkerParser.MarkerType.FP:
                        falsePositives.Add(diag);
                        break;
                    case DiagnosticMarkerParser.MarkerType.OK:
                    case DiagnosticMarkerParser.MarkerType.Skip:
                        break;
                }
            }
        }

        // Check for orphaned markers in this file
        var orphaned = new List<string>();
        if (_allMarkers.TryGetValue(fullPath, out var markers))
        {
            foreach (var ((line, col, code), markerType) in markers)
            {
                if (markerType == DiagnosticMarkerParser.MarkerType.Skip)
                    continue;
                if (!_matchedMarkers.Contains((fullPath, line, col, code)))
                {
                    orphaned.Add($"  # {line}:{col}-{code}-{markerType} <- No diagnostic");
                }
            }
        }

        var errors = new StringBuilder();

        if (unverified.Count > 0)
        {
            errors.AppendLine($"{unverified.Count} unverified diagnostic(s):");
            foreach (var d in unverified)
                errors.AppendLine($"  {d.StartLine}:{d.StartColumn} [{d.Code}] {d.Message}");
        }

        if (falsePositives.Count > 0)
        {
            errors.AppendLine($"{falsePositives.Count} false positive(s):");
            foreach (var d in falsePositives)
                errors.AppendLine($"  {d.StartLine}:{d.StartColumn} [{d.Code}] {d.Message}");
        }

        if (orphaned.Count > 0)
        {
            errors.AppendLine($"{orphaned.Count} orphaned marker(s):");
            foreach (var o in orphaned)
                errors.AppendLine(o);
        }

        if (errors.Length > 0)
        {
            Assert.Fail($"{relativePath}:\n{errors}");
        }
    }

    /// <summary>
    /// Summary test: prints overall statistics.
    /// </summary>
    [TestMethod]
    public void Diagnostics_Summary()
    {
        int verified = 0, unverified = 0, falsePositives = 0, skipped = 0;

        foreach (var diag in _allDiagnostics)
        {
            var fullPath = GetFullPath(diag.FilePath);
            var key = (diag.StartLine, diag.StartColumn, diag.Code);

            if (!_allMarkers.TryGetValue(fullPath, out var fileMarkers) ||
                !fileMarkers.TryGetValue(key, out var markerType))
            {
                unverified++;
            }
            else
            {
                switch (markerType)
                {
                    case DiagnosticMarkerParser.MarkerType.OK: verified++; break;
                    case DiagnosticMarkerParser.MarkerType.FP: falsePositives++; break;
                    case DiagnosticMarkerParser.MarkerType.Skip: skipped++; break;
                }
            }
        }

        Console.WriteLine("=== DIAGNOSTICS VERIFICATION SUMMARY ===");
        Console.WriteLine($"Total:           {_allDiagnostics.Count}");
        Console.WriteLine($"Verified (OK):   {verified}");
        Console.WriteLine($"Unverified:      {unverified}");
        Console.WriteLine($"False Positives: {falsePositives}");
        Console.WriteLine($"Skipped:         {skipped}");
    }

    /// <summary>
    /// Utility method: generates marker suggestions for all unverified diagnostics.
    /// </summary>
    [TestMethod]
    [Ignore("Utility method - run manually")]
    public void GenerateMarkerSuggestions()
    {
        var unverified = _allDiagnostics.Where(diag =>
        {
            var key = (diag.StartLine, diag.StartColumn, diag.Code);
            var fullPath = GetFullPath(diag.FilePath);
            return !_allMarkers.TryGetValue(fullPath, out var fileMarkers) ||
                   !fileMarkers.ContainsKey(key);
        }).ToList();

        var byFile = unverified.GroupBy(d => d.FilePath).OrderBy(g => g.Key);

        foreach (var fileGroup in byFile)
        {
            Console.WriteLine($"\n=== {fileGroup.Key} ===");
            foreach (var diag in fileGroup.OrderBy(d => d.StartLine).ThenBy(d => d.StartColumn))
            {
                Console.WriteLine($"# {diag.StartLine}:{diag.StartColumn}-{diag.Code}-OK");
            }
        }
    }

    /// <summary>
    /// Utility method: generates markers for a specific file.
    /// </summary>
    [TestMethod]
    [Ignore("Utility method - run manually")]
    public void GenerateMarkersForFile()
    {
        var targetFile = "test_scripts/diagnostics/validator/type_errors.gd";

        var fileDiagnostics = _allDiagnostics
            .Where(d => d.FilePath.Replace('\\', '/').Contains(targetFile.Replace('\\', '/')))
            .OrderBy(d => d.StartLine)
            .ThenBy(d => d.StartColumn)
            .ToList();

        Console.WriteLine($"// Markers for {targetFile} ({fileDiagnostics.Count} diagnostics):");
        Console.WriteLine();

        foreach (var diag in fileDiagnostics)
        {
            Console.WriteLine($"// Line {diag.StartLine}: # {diag.StartLine}:{diag.StartColumn}-{diag.Code}-OK");
            Console.WriteLine($"//   Message: {diag.Message}");
        }
    }

    #region Helper Methods

    private static Dictionary<string, Dictionary<(int, int, string), DiagnosticMarkerParser.MarkerType>> LoadAllMarkers(
        DiagnosticMarkerParser parser)
    {
        return parser.ParseDirectory(_projectPath);
    }

    private static string GetFullPath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                     .Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(_projectPath, normalized);
    }

    private static void GenerateVerificationReport()
    {
        var verified = new List<DiagnosticInfo>();
        var unverified = new List<DiagnosticInfo>();
        var falsePositives = new List<DiagnosticInfo>();
        var skipped = new List<DiagnosticInfo>();
        var orphanedMarkers = new List<OrphanedMarker>();

        foreach (var diag in _allDiagnostics)
        {
            var key = (diag.StartLine, diag.StartColumn, diag.Code);
            var fullPath = GetFullPath(diag.FilePath);

            if (!_allMarkers.TryGetValue(fullPath, out var fileMarkers) ||
                !fileMarkers.TryGetValue(key, out var markerType))
            {
                unverified.Add(diag);
            }
            else
            {
                switch (markerType)
                {
                    case DiagnosticMarkerParser.MarkerType.OK: verified.Add(diag); break;
                    case DiagnosticMarkerParser.MarkerType.FP: falsePositives.Add(diag); break;
                    case DiagnosticMarkerParser.MarkerType.Skip: skipped.Add(diag); break;
                }
            }
        }

        foreach (var (filePath, fileMarkers) in _allMarkers)
        {
            foreach (var ((line, col, code), markerType) in fileMarkers)
            {
                if (markerType == DiagnosticMarkerParser.MarkerType.Skip)
                    continue;
                if (!_matchedMarkers.Contains((filePath, line, col, code)))
                {
                    orphanedMarkers.Add(new OrphanedMarker(filePath, line, col, code, markerType));
                }
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("DIAGNOSTICS VERIFICATION REPORT");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("================================================================================");
        sb.AppendLine();
        sb.AppendLine("SUMMARY");
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine($"Total:           {verified.Count + unverified.Count + falsePositives.Count + skipped.Count}");
        sb.AppendLine($"Verified (OK):   {verified.Count}");
        sb.AppendLine($"Unverified:      {unverified.Count}");
        sb.AppendLine($"False Positives: {falsePositives.Count}");
        sb.AppendLine($"Skipped:         {skipped.Count}");
        sb.AppendLine($"Orphaned Markers:{orphanedMarkers.Count}");
        sb.AppendLine();

        if (unverified.Count > 0)
        {
            sb.AppendLine("================================================================================");
            sb.AppendLine("UNVERIFIED DIAGNOSTICS");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            var byFile = unverified.GroupBy(d => d.FilePath).OrderBy(g => g.Key);
            foreach (var fileGroup in byFile)
            {
                sb.AppendLine($"--- {fileGroup.Key} ({fileGroup.Count()} diagnostics) ---");
                foreach (var diag in fileGroup.OrderBy(d => d.StartLine).ThenBy(d => d.StartColumn))
                {
                    sb.AppendLine($"  {diag.StartLine}:{diag.StartColumn} [{diag.Code}] {diag.Message}");
                    sb.AppendLine($"    Add: # {diag.StartLine}:{diag.StartColumn}-{diag.Code}-OK");
                }
                sb.AppendLine();
            }
        }

        if (falsePositives.Count > 0)
        {
            sb.AppendLine("================================================================================");
            sb.AppendLine("FALSE POSITIVES (BUGS TO FIX)");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            foreach (var diag in falsePositives.OrderBy(d => d.Code))
            {
                sb.AppendLine($"  {diag.FilePath}:{diag.StartLine}:{diag.StartColumn}");
                sb.AppendLine($"    [{diag.Code}] {diag.Message}");
            }
            sb.AppendLine();
        }

        if (skipped.Count > 0)
        {
            sb.AppendLine("================================================================================");
            sb.AppendLine("SKIPPED (NEEDS ANALYSIS)");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            foreach (var diag in skipped.OrderBy(d => d.FilePath).ThenBy(d => d.StartLine))
            {
                sb.AppendLine($"  {diag.FilePath}:{diag.StartLine}:{diag.StartColumn} [{diag.Code}]");
            }
            sb.AppendLine();
        }

        if (orphanedMarkers.Count > 0)
        {
            sb.AppendLine("================================================================================");
            sb.AppendLine("ORPHANED MARKERS (MARKERS WITHOUT DIAGNOSTICS)");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            var byFile = orphanedMarkers.GroupBy(m => m.FilePath).OrderBy(g => g.Key);
            foreach (var fileGroup in byFile)
            {
                var fileName = Path.GetFileName(fileGroup.Key);
                sb.AppendLine($"--- {fileName} ({fileGroup.Count()} orphaned) ---");
                foreach (var marker in fileGroup.OrderBy(m => m.Line).ThenBy(m => m.Column))
                {
                    sb.AppendLine($"  # {marker.Line}:{marker.Column}-{marker.Code}-{marker.Type} <- No diagnostic at this location");
                }
                sb.AppendLine();
            }
        }

        var repoRoot = GetRepoRoot();
        var outputPath = Path.Combine(repoRoot, "verification", "DIAGNOSTICS_VERIFICATION.txt");
        File.WriteAllText(outputPath, sb.ToString());
    }

    private static List<DiagnosticInfo> CollectAllDiagnostics()
    {
        var entries = new List<DiagnosticInfo>();

        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (scriptFile.Class == null)
                continue;

            var relativePath = GetRelativePath(scriptFile.FullPath, _projectPath);

            var config = new GDProjectConfig();
            config.Linting.Enabled = true;
            var diagnosticsService = GDDiagnosticsService.FromConfig(config);
            var diagResult = diagnosticsService.Diagnose(scriptFile);

            foreach (var diag in diagResult.Diagnostics)
            {
                entries.Add(new DiagnosticInfo
                {
                    FilePath = relativePath,
                    Code = diag.Code,
                    Message = diag.Message,
                    Severity = diag.Severity.ToString(),
                    Source = diag.Source.ToString(),
                    StartLine = diag.StartLine,
                    StartColumn = diag.StartColumn
                });
            }

            if (scriptFile.SemanticModel != null)
            {
                var semanticValidatorOptions = new GDSemanticValidatorOptions
                {
                    CheckTypes = true,
                    CheckMemberAccess = true,
                    CheckArgumentTypes = true,
                    CheckIndexers = true,
                    CheckSignalTypes = true,
                    CheckGenericTypes = true,
                    CheckNodePaths = true,
                    CheckNodeLifecycle = true,
                    ProjectModel = _projectModel,
                    CheckReturnConsistency = true,
                    CheckAnnotationNarrowing = true,
                    CheckContainerSpecialization = true,
                    CheckTypeWidening = true,
                    CheckParameterTypeHints = true,
                    CheckUntypedContainerAccess = true,
                    CheckRedundantAnnotations = true,
                };

                var semanticValidator = new GDSemanticValidator(scriptFile.SemanticModel, semanticValidatorOptions);
                var semanticResult = semanticValidator.Validate(scriptFile.Class);

                foreach (var diag in semanticResult.Diagnostics)
                {
                    entries.Add(new DiagnosticInfo
                    {
                        FilePath = relativePath,
                        Code = diag.CodeString,
                        Message = diag.Message,
                        Severity = diag.Severity.ToString(),
                        Source = "SemanticValidator",
                        StartLine = diag.StartLine,
                        StartColumn = diag.StartColumn
                    });
                }
            }
        }

        return entries;
    }

    private static string GetRelativePath(string? fullPath, string projectPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return "unknown";

        var normalizedFullPath = fullPath.Replace('\\', '/');
        var normalizedProjectPath = projectPath.Replace('\\', '/');

        if (normalizedFullPath.StartsWith(normalizedProjectPath, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedFullPath.Substring(normalizedProjectPath.Length).TrimStart('/');
        }
        return Path.GetFileName(fullPath);
    }

    private static string GetRepoRoot()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var currentDir = baseDir;

        for (int i = 0; i < 15; i++)
        {
            var gitPath = Path.Combine(currentDir, ".git");
            if (File.Exists(Path.Combine(currentDir, "CLAUDE.md")) ||
                Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return currentDir;
            }
            var parent = Path.GetDirectoryName(currentDir);
            if (parent == null || parent == currentDir)
                break;
            currentDir = parent;
        }

        return baseDir;
    }

    #endregion

    #region DiagnosticInfo

    private class DiagnosticInfo
    {
        public string FilePath { get; set; } = "";
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Source { get; set; } = "";
        public int StartLine { get; set; }
        public int StartColumn { get; set; }

        public override string ToString()
        {
            return $"{FilePath}:{StartLine}:{StartColumn} [{Code}] {Message}";
        }
    }

    #endregion

    #region OrphanedMarker

    private record OrphanedMarker(
        string FilePath,
        int Line,
        int Column,
        string Code,
        DiagnosticMarkerParser.MarkerType Type)
    {
        public override string ToString()
        {
            var relativePath = Path.GetFileName(FilePath);
            return $"{relativePath}:{Line}:{Column} [{Code}] (was: {Type})";
        }
    }

    #endregion
}
