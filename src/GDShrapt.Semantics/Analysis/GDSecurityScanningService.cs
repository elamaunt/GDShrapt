using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GDShrapt.Semantics;

/// <summary>
/// Security vulnerability scanner for GDScript projects.
/// Detects hardcoded secrets, unsafe patterns, and potential vulnerabilities.
/// </summary>
public class GDSecurityScanningService
{
    private readonly GDProjectSemanticModel _projectModel;
    private readonly GDScriptProject _project;

    private static readonly Regex ApiKeyPattern = new(
        @"(?:var\s+)?(?:api[_-]?key|apikey|access[_-]?key|secret[_-]?key)(?:\s*:\s*\w+)?\s*=\s*['""]([a-zA-Z0-9_\-]{16,})['""]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PasswordPattern = new(
        @"(?:var\s+)?(?:password|passwd|pwd)(?:\s*:\s*\w+)?\s*=\s*['""]([^'""]{4,})['""]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TokenPattern = new(
        @"(?:var\s+)?(?:token|bearer|auth[_-]?token)(?:\s*:\s*\w+)?\s*=\s*['""]([a-zA-Z0-9_\-\.]{20,})['""]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public GDSecurityScanningService(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel ?? throw new ArgumentNullException(nameof(projectModel));
        _project = projectModel.Project;
    }

    /// <summary>
    /// Analyzes the project for security vulnerabilities.
    /// </summary>
    public GDSecurityReport AnalyzeProject()
    {
        var issues = new List<GDSecurityIssue>();

        foreach (var file in _project.ScriptFiles)
        {
            issues.AddRange(FindHardcodedSecrets(file));
            issues.AddRange(FindUnsafePatterns(file));
            issues.AddRange(FindPathTraversalRisks(file));
            issues.AddRange(FindInsecureNetworkCalls(file));
        }

        return new GDSecurityReport
        {
            Issues = issues,
            TotalIssues = issues.Count,
            CriticalCount = issues.Count(i => i.Severity == GDSecuritySeverity.Critical),
            HighCount = issues.Count(i => i.Severity == GDSecuritySeverity.High),
            MediumCount = issues.Count(i => i.Severity == GDSecuritySeverity.Medium),
            LowCount = issues.Count(i => i.Severity == GDSecuritySeverity.Low)
        };
    }

    private IEnumerable<GDSecurityIssue> FindHardcodedSecrets(GDScriptFile file)
    {
        var content = file.LastContent ?? "";
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNum = i + 1;

            var apiMatch = ApiKeyPattern.Match(line);
            if (apiMatch.Success)
            {
                yield return new GDSecurityIssue
                {
                    Category = GDSecurityCategory.HardcodedSecret,
                    Severity = GDSecuritySeverity.Critical,
                    FilePath = file.FullPath ?? "",
                    Line = lineNum,
                    Code = "SEC001",
                    Message = "Hardcoded API key detected",
                    Recommendation = "Store API keys in environment variables or secure configuration"
                };
            }

            var passMatch = PasswordPattern.Match(line);
            if (passMatch.Success && !IsTestFile(file.FullPath ?? ""))
            {
                yield return new GDSecurityIssue
                {
                    Category = GDSecurityCategory.HardcodedSecret,
                    Severity = GDSecuritySeverity.Critical,
                    FilePath = file.FullPath ?? "",
                    Line = lineNum,
                    Code = "SEC002",
                    Message = "Hardcoded password detected",
                    Recommendation = "Never hardcode passwords. Use secure credential storage"
                };
            }

            var tokenMatch = TokenPattern.Match(line);
            if (tokenMatch.Success)
            {
                yield return new GDSecurityIssue
                {
                    Category = GDSecurityCategory.HardcodedSecret,
                    Severity = GDSecuritySeverity.High,
                    FilePath = file.FullPath ?? "",
                    Line = lineNum,
                    Code = "SEC003",
                    Message = "Hardcoded token detected",
                    Recommendation = "Store tokens securely and rotate them regularly"
                };
            }
        }
    }

    private IEnumerable<GDSecurityIssue> FindUnsafePatterns(GDScriptFile file)
    {
        var semanticModel = _projectModel.GetSemanticModel(file);
        if (semanticModel == null)
            yield break;

        foreach (var reference in semanticModel.GetMemberAccesses("OS", "execute"))
        {
            yield return new GDSecurityIssue
            {
                Category = GDSecurityCategory.UnsafePattern,
                Severity = GDSecuritySeverity.High,
                FilePath = file.FullPath ?? "",
                Line = reference.ReferenceNode?.StartLine ?? 0,
                Code = "SEC010",
                Message = "OS.execute() can execute arbitrary commands",
                Recommendation = "Validate and sanitize all inputs. Consider using safer alternatives"
            };
        }

        foreach (var reference in semanticModel.GetMemberAccesses("OS", "shell_open"))
        {
            yield return new GDSecurityIssue
            {
                Category = GDSecurityCategory.UnsafePattern,
                Severity = GDSecuritySeverity.Medium,
                FilePath = file.FullPath ?? "",
                Line = reference.ReferenceNode?.StartLine ?? 0,
                Code = "SEC011",
                Message = "OS.shell_open() can open arbitrary URLs/files",
                Recommendation = "Validate URLs before opening. Use allowlists for permitted domains"
            };
        }

        foreach (var reference in semanticModel.GetGlobalFunctionAccesses("str2var"))
        {
            yield return new GDSecurityIssue
            {
                Category = GDSecurityCategory.UnsafePattern,
                Severity = GDSecuritySeverity.High,
                FilePath = file.FullPath ?? "",
                Line = reference.ReferenceNode?.StartLine ?? 0,
                Code = "SEC012",
                Message = "str2var() can deserialize arbitrary data",
                Recommendation = "Never use str2var() with untrusted input. Use JSON parsing instead"
            };
        }
    }

    private IEnumerable<GDSecurityIssue> FindPathTraversalRisks(GDScriptFile file)
    {
        var content = file.LastContent ?? "";
        var lines = content.Split('\n');

        var pathConcatPattern = new Regex(
            @"(File\.open|load|preload)\s*\([^)]*\+",
            RegexOptions.Compiled);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNum = i + 1;

            if (pathConcatPattern.IsMatch(line))
            {
                if (line.Contains("get_text()") ||
                    line.Contains("get_line_edit") ||
                    Regex.IsMatch(line, @"\buser\b|\binput\b|\bpath\b", RegexOptions.IgnoreCase))
                {
                    yield return new GDSecurityIssue
                    {
                        Category = GDSecurityCategory.PathTraversal,
                        Severity = GDSecuritySeverity.High,
                        FilePath = file.FullPath ?? "",
                        Line = lineNum,
                        Code = "SEC020",
                        Message = "Potential path traversal vulnerability",
                        Recommendation = "Validate file paths. Ensure paths don't contain '..' and stay within allowed directories"
                    };
                }
            }
        }
    }

    private IEnumerable<GDSecurityIssue> FindInsecureNetworkCalls(GDScriptFile file)
    {
        var content = file.LastContent ?? "";
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNum = i + 1;

            if (Regex.IsMatch(line, @"http://[^\s""']+", RegexOptions.IgnoreCase) &&
                !line.Contains("localhost") &&
                !line.Contains("127.0.0.1"))
            {
                yield return new GDSecurityIssue
                {
                    Category = GDSecurityCategory.InsecureNetwork,
                    Severity = GDSecuritySeverity.Medium,
                    FilePath = file.FullPath ?? "",
                    Line = lineNum,
                    Code = "SEC030",
                    Message = "Insecure HTTP URL detected",
                    Recommendation = "Use HTTPS for secure communication"
                };
            }

            if (line.Contains("ssl_certificate") ||
                (line.Contains("verify") && line.Contains("false")))
            {
                yield return new GDSecurityIssue
                {
                    Category = GDSecurityCategory.InsecureNetwork,
                    Severity = GDSecuritySeverity.High,
                    FilePath = file.FullPath ?? "",
                    Line = lineNum,
                    Code = "SEC031",
                    Message = "SSL verification may be disabled",
                    Recommendation = "Always verify SSL certificates in production"
                };
            }
        }
    }

    private static bool IsTestFile(string filePath)
    {
        var name = Path.GetFileName(filePath).ToLowerInvariant();
        return name.Contains("test") || name.Contains("spec") || name.Contains("mock");
    }
}
