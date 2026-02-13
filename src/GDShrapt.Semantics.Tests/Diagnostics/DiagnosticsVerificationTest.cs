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
/// The test passes when: unverified = 0 AND falsePositives = 0
/// </summary>
[TestClass]
public class DiagnosticsVerificationTest
{
    private GDScriptProject _project = null!;
    private GDProjectSemanticModel _projectModel = null!;
    private string _projectPath = null!;
    private List<DiagnosticInfo> _allDiagnostics = null!;

    [TestInitialize]
    public void Setup()
    {
        _projectPath = GetTestProjectPath();
        var context = new GDDefaultProjectContext(_projectPath);
        _project = new GDScriptProject(context, new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = true
        });

        _project.LoadScripts();
        _project.LoadScenes();
        _project.AnalyzeAll();

        _projectModel = new GDProjectSemanticModel(_project);

        _allDiagnostics = CollectAllDiagnostics();
    }

    /// <summary>
    /// Main TDD test: all diagnostics must be verified or excluded.
    /// Add markers to GDScript files in format: # LINE:COL-CODE-SUFFIX
    /// Also checks for orphaned markers (markers without corresponding diagnostics).
    /// </summary>
    [TestMethod]
    public void AllDiagnostics_MustBeVerifiedOrExcluded()
    {
        // 1. Load markers from all GDScript files
        var parser = new DiagnosticMarkerParser();
        var allMarkers = LoadAllMarkers(parser);

        // 2. Classify diagnostics and track which markers are matched
        var verified = new List<DiagnosticInfo>();
        var unverified = new List<DiagnosticInfo>();
        var falsePositives = new List<DiagnosticInfo>();
        var skipped = new List<DiagnosticInfo>();

        // Track which markers were matched to a diagnostic
        var matchedMarkers = new HashSet<(string FilePath, int Line, int Col, string Code)>();

        foreach (var diag in _allDiagnostics)
        {
            var key = (diag.StartLine, diag.StartColumn, diag.Code);
            var fullPath = GetFullPath(diag.FilePath);

            if (!allMarkers.TryGetValue(fullPath, out var fileMarkers) ||
                !fileMarkers.TryGetValue(key, out var markerType))
            {
                unverified.Add(diag);
            }
            else
            {
                // Mark this marker as matched
                matchedMarkers.Add((fullPath, diag.StartLine, diag.StartColumn, diag.Code));

                switch (markerType)
                {
                    case DiagnosticMarkerParser.MarkerType.OK:
                        verified.Add(diag);
                        break;
                    case DiagnosticMarkerParser.MarkerType.FP:
                        falsePositives.Add(diag);
                        break;
                    case DiagnosticMarkerParser.MarkerType.Skip:
                        skipped.Add(diag);
                        break;
                }
            }
        }

        // 3. Find orphaned markers (markers without corresponding diagnostics)
        var orphanedMarkers = new List<OrphanedMarker>();

        foreach (var (filePath, fileMarkers) in allMarkers)
        {
            foreach (var ((line, col, code), markerType) in fileMarkers)
            {
                // Skip markers are allowed to be orphaned (explicitly marked as inactive)
                if (markerType == DiagnosticMarkerParser.MarkerType.Skip)
                    continue;

                if (!matchedMarkers.Contains((filePath, line, col, code)))
                {
                    orphanedMarkers.Add(new OrphanedMarker(filePath, line, col, code, markerType));
                }
            }
        }

        // 4. Generate verification report
        GenerateVerificationReport(verified, unverified, falsePositives, skipped, orphanedMarkers);

        // 5. Print summary
        Console.WriteLine("=== DIAGNOSTICS VERIFICATION ===");
        Console.WriteLine($"Total:           {_allDiagnostics.Count}");
        Console.WriteLine($"Verified (OK):   {verified.Count}");
        Console.WriteLine($"Unverified:      {unverified.Count}");
        Console.WriteLine($"False Positives: {falsePositives.Count}");
        Console.WriteLine($"Skipped:         {skipped.Count}");
        Console.WriteLine($"Orphaned Markers:{orphanedMarkers.Count}");
        Console.WriteLine();

        // 6. Print first 20 unverified for quick reference
        if (unverified.Count > 0)
        {
            Console.WriteLine("UNVERIFIED (first 20):");
            foreach (var diag in unverified.Take(20))
            {
                Console.WriteLine($"  {diag.FilePath}:{diag.StartLine}:{diag.StartColumn} [{diag.Code}] {diag.Message}");
            }
            Console.WriteLine();
        }

        // 7. Print false positives
        if (falsePositives.Count > 0)
        {
            Console.WriteLine("FALSE POSITIVES (bugs to fix):");
            foreach (var diag in falsePositives)
            {
                Console.WriteLine($"  {diag.FilePath}:{diag.StartLine}:{diag.StartColumn} [{diag.Code}] {diag.Message}");
            }
            Console.WriteLine();
        }

        // 8. Print orphaned markers
        if (orphanedMarkers.Count > 0)
        {
            Console.WriteLine("ORPHANED MARKERS (diagnostic no longer produced):");
            foreach (var marker in orphanedMarkers.Take(20))
            {
                Console.WriteLine($"  {marker}");
            }
            if (orphanedMarkers.Count > 20)
            {
                Console.WriteLine($"  ... and {orphanedMarkers.Count - 20} more");
            }
            Console.WriteLine();
        }

        // 9. Assert
        unverified.Should().BeEmpty(
            $"All diagnostics must be verified. Found {unverified.Count} unverified. " +
            $"First: {unverified.FirstOrDefault()?.ToString() ?? "none"}");

        falsePositives.Should().BeEmpty(
            $"All false positives must be fixed in the analyzer core. " +
            $"Found {falsePositives.Count} FP diagnostics.");

        orphanedMarkers.Should().BeEmpty(
            $"All markers must have corresponding diagnostics. " +
            $"Found {orphanedMarkers.Count} orphaned markers (diagnostic no longer produced). " +
            $"First: {orphanedMarkers.FirstOrDefault()?.ToString() ?? "none"}");
    }

    /// <summary>
    /// Utility method: generates marker suggestions for all unverified diagnostics.
    /// Run manually to get markers to add to GDScript files.
    /// </summary>
    [TestMethod]
    [Ignore("Utility method - run manually")]
    public void GenerateMarkerSuggestions()
    {
        var parser = new DiagnosticMarkerParser();
        var allMarkers = LoadAllMarkers(parser);

        var unverified = _allDiagnostics.Where(diag =>
        {
            var key = (diag.StartLine, diag.StartColumn, diag.Code);
            var fullPath = GetFullPath(diag.FilePath);
            return !allMarkers.TryGetValue(fullPath, out var fileMarkers) ||
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
        var targetFile = "test_scripts/diagnostics/validator/type_errors.gd"; // Change as needed

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

    private Dictionary<string, Dictionary<(int, int, string), DiagnosticMarkerParser.MarkerType>> LoadAllMarkers(
        DiagnosticMarkerParser parser)
    {
        return parser.ParseDirectory(_projectPath);
    }

    private string GetFullPath(string relativePath)
    {
        // Normalize path separators
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                     .Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(_projectPath, normalized);
    }

    private void GenerateVerificationReport(
        List<DiagnosticInfo> verified,
        List<DiagnosticInfo> unverified,
        List<DiagnosticInfo> falsePositives,
        List<DiagnosticInfo> skipped,
        List<OrphanedMarker> orphanedMarkers)
    {
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
            sb.AppendLine("These markers exist in GDScript files but no corresponding diagnostic was produced.");
            sb.AppendLine("This may indicate:");
            sb.AppendLine("  - A bug was fixed and the diagnostic is no longer emitted");
            sb.AppendLine("  - Code was changed and the marker is outdated");
            sb.AppendLine("  - The marker coordinates are incorrect");
            sb.AppendLine();

            var byFile = orphanedMarkers.GroupBy(m => m.FilePath).OrderBy(g => g.Key);
            foreach (var fileGroup in byFile)
            {
                var fileName = Path.GetFileName(fileGroup.Key);
                sb.AppendLine($"--- {fileName} ({fileGroup.Count()} orphaned) ---");
                foreach (var marker in fileGroup.OrderBy(m => m.Line).ThenBy(m => m.Column))
                {
                    sb.AppendLine($"  # {marker.Line}:{marker.Column}-{marker.Code}-{marker.Type} <- No diagnostic at this location");
                    sb.AppendLine($"    Action: Remove marker or investigate why diagnostic disappeared");
                }
                sb.AppendLine();
            }
        }

        // Write to file
        var repoRoot = GetRepoRoot();
        var outputPath = Path.Combine(repoRoot, "verification", "DIAGNOSTICS_VERIFICATION.txt");
        File.WriteAllText(outputPath, sb.ToString());

        Console.WriteLine($"Verification report saved to: {outputPath}");
    }

    private List<DiagnosticInfo> CollectAllDiagnostics()
    {
        var entries = new List<DiagnosticInfo>();

        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (scriptFile.Class == null)
                continue;

            var relativePath = GetRelativePath(scriptFile.FullPath);

            // 1. Run GDDiagnosticsService (syntax, validation, linting)
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

            // 2. Run GDSemanticValidator (type-aware validation)
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

    private string GetRelativePath(string? fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return "unknown";

        var normalizedFullPath = fullPath.Replace('\\', '/');
        var normalizedProjectPath = _projectPath.Replace('\\', '/');

        if (normalizedFullPath.StartsWith(normalizedProjectPath, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedFullPath.Substring(normalizedProjectPath.Length).TrimStart('/');
        }
        return Path.GetFileName(fullPath);
    }

    private static string GetTestProjectPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var testProjectPath = Path.Combine(baseDir, "TestProject");

        if (Directory.Exists(testProjectPath))
            return testProjectPath;

        var currentDir = baseDir;
        for (int i = 0; i < 10; i++)
        {
            var candidatePath = Path.Combine(currentDir, "testproject", "GDShrapt.TestProject");
            if (Directory.Exists(candidatePath))
                return candidatePath;
            currentDir = Path.GetDirectoryName(currentDir) ?? currentDir;
        }

        throw new DirectoryNotFoundException("Test project not found.");
    }

    private static string GetRepoRoot()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var currentDir = baseDir;

        for (int i = 0; i < 15; i++)
        {
            if (File.Exists(Path.Combine(currentDir, "CLAUDE.md")) ||
                Directory.Exists(Path.Combine(currentDir, ".git")))
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

    /// <summary>
    /// Represents a marker in GDScript file that has no corresponding diagnostic.
    /// This indicates either:
    /// - A bug was fixed and the diagnostic is no longer emitted
    /// - Code was changed and the marker is outdated
    /// - The marker coordinates are incorrect
    /// </summary>
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
