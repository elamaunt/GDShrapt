using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Collects diagnostics from test project and outputs to DIAGNOSTICS.md file.
/// Run this test to regenerate the diagnostics report.
/// </summary>
[TestClass]
public class DiagnosticsCollector
{
    [TestMethod]
    public void CollectAllDiagnosticsFromTestProject()
    {
        var projectPath = GetTestProjectPath();
        Console.WriteLine($"Test project path: {projectPath}");

        var context = new GDDefaultProjectContext(projectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = true
        });

        project.LoadScripts();
        project.LoadScenes();
        project.AnalyzeAll();

        var allDiagnostics = new List<DiagnosticEntry>();

        // Collect diagnostics from all scripts
        foreach (var scriptFile in project.ScriptFiles)
        {
            var diagnostics = CollectDiagnosticsFromScript(scriptFile, project);
            allDiagnostics.AddRange(diagnostics);
        }

        // Generate plain text report (same format as plugin output)
        var repoRoot = GetRepoRoot();
        var plainText = GeneratePlainTextReport(allDiagnostics, project);
        var txtOutputPath = Path.Combine(repoRoot, "DIAGNOSTICS.txt");
        File.WriteAllText(txtOutputPath, plainText);

        Console.WriteLine($"Diagnostics saved to: {txtOutputPath}");
        Console.WriteLine($"Total diagnostics: {allDiagnostics.Count}");

        // Also print summary
        PrintSummary(allDiagnostics);
    }

    private List<DiagnosticEntry> CollectDiagnosticsFromScript(GDScriptFile scriptFile, GDScriptProject project)
    {
        var entries = new List<DiagnosticEntry>();
        var relativePath = GetRelativePath(scriptFile.FullPath, project);

        if (scriptFile.Class == null)
            return entries;

        // 1. Run GDDiagnosticsService (syntax, validation, linting)
        var config = new GDProjectConfig();
        config.Linting.Enabled = true;
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);
        var diagResult = diagnosticsService.Diagnose(scriptFile);

        foreach (var diag in diagResult.Diagnostics)
        {
            entries.Add(new DiagnosticEntry
            {
                FilePath = relativePath,
                Code = diag.Code,
                Message = diag.Message,
                Severity = diag.Severity.ToString(),
                Source = diag.Source.ToString(),
                StartLine = diag.StartLine,
                StartColumn = diag.StartColumn,
                EndLine = diag.EndLine,
                EndColumn = diag.EndColumn
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
                CheckGenericTypes = true
            };

            var semanticValidator = new GDSemanticValidator(scriptFile.SemanticModel, semanticValidatorOptions);
            var semanticResult = semanticValidator.Validate(scriptFile.Class);

            foreach (var diag in semanticResult.Diagnostics)
            {
                entries.Add(new DiagnosticEntry
                {
                    FilePath = relativePath,
                    Code = diag.CodeString,
                    Message = diag.Message,
                    Severity = diag.Severity.ToString(),
                    Source = "SemanticValidator",
                    StartLine = diag.StartLine,
                    StartColumn = diag.StartColumn,
                    EndLine = diag.EndLine,
                    EndColumn = diag.EndColumn
                });
            }
        }

        return entries;
    }

    private string GeneratePlainTextReport(List<DiagnosticEntry> diagnostics, GDScriptProject project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("GDShrapt Diagnostics Report");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Project: testproject/GDShrapt.TestProject");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        // Summary
        sb.AppendLine("SUMMARY");
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine($"Total diagnostics: {diagnostics.Count}");
        sb.AppendLine($"Scripts analyzed:  {project.ScriptFiles.Count()}");
        sb.AppendLine();

        // By Severity
        var bySeverity = diagnostics.GroupBy(d => d.Severity).OrderBy(g => g.Key);
        sb.AppendLine("By Severity:");
        foreach (var group in bySeverity)
        {
            sb.AppendLine($"  {group.Key,-10}: {group.Count(),5}");
        }
        sb.AppendLine();

        // By Source
        var bySource = diagnostics.GroupBy(d => d.Source).OrderBy(g => g.Key);
        sb.AppendLine("By Source:");
        foreach (var group in bySource)
        {
            sb.AppendLine($"  {group.Key,-20}: {group.Count(),5}");
        }
        sb.AppendLine();

        // By Code (Top 30)
        var byCode = diagnostics.GroupBy(d => d.Code).OrderByDescending(g => g.Count());
        sb.AppendLine("By Code (Top 30):");
        foreach (var group in byCode.Take(30))
        {
            var sample = group.First().Message;
            if (sample.Length > 60)
                sample = sample.Substring(0, 57) + "...";
            sb.AppendLine($"  {group.Key,-10}: {group.Count(),4}  ({sample})");
        }
        sb.AppendLine();

        // Diagnostics by file
        sb.AppendLine("================================================================================");
        sb.AppendLine("DIAGNOSTICS BY FILE");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        var byFile = diagnostics.GroupBy(d => d.FilePath).OrderBy(g => g.Key);
        foreach (var fileGroup in byFile)
        {
            sb.AppendLine($"--- {fileGroup.Key} ({fileGroup.Count()} diagnostics) ---");
            sb.AppendLine();

            var fileDiags = fileGroup.OrderBy(d => d.StartLine).ThenBy(d => d.StartColumn);
            foreach (var diag in fileDiags)
            {
                sb.AppendLine($"  {diag.StartLine,4}:{diag.StartColumn,-3} [{diag.Code}] {diag.Severity} - {diag.Message}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void PrintSummary(List<DiagnosticEntry> diagnostics)
    {
        Console.WriteLine();
        Console.WriteLine("=== DIAGNOSTICS SUMMARY ===");
        Console.WriteLine();

        var byCode = diagnostics.GroupBy(d => d.Code).OrderByDescending(g => g.Count());
        foreach (var group in byCode.Take(15))
        {
            Console.WriteLine($"{group.Key}: {group.Count()} occurrences");
        }

        Console.WriteLine();
        Console.WriteLine("By Severity:");
        var bySeverity = diagnostics.GroupBy(d => d.Severity);
        foreach (var group in bySeverity)
        {
            Console.WriteLine($"  {group.Key}: {group.Count()}");
        }
    }

    private static string GetRelativePath(string? fullPath, GDScriptProject project)
    {
        if (string.IsNullOrEmpty(fullPath))
            return "unknown";

        var projectRoot = project.ProjectPath;
        if (fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        return Path.GetFileName(fullPath);
    }

    private static string GetTestProjectPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var testProjectPath = Path.Combine(baseDir, "TestProject");

        if (Directory.Exists(testProjectPath))
            return testProjectPath;

        // Fallback: look in the repository root
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
            // Look for .git or CLAUDE.md to identify repo root
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

        // Fallback to base directory
        return baseDir;
    }

    private class DiagnosticEntry
    {
        public string FilePath { get; set; } = "";
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Source { get; set; } = "";
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
    }
}
