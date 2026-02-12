using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Detects duplicated code blocks in GDScript projects.
/// Supports method-level and block-level detection with similarity matching.
/// </summary>
public class GDDuplicateDetectionService
{
    private readonly GDProjectSemanticModel _projectModel;
    private readonly GDScriptProject _project;

    private static readonly HashSet<string> Keywords = new()
    {
        "if", "elif", "else", "for", "while", "match", "break", "continue",
        "pass", "return", "class", "class_name", "extends", "is", "in", "as",
        "self", "signal", "func", "static", "const", "enum", "var", "onready",
        "export", "setget", "tool", "yield", "assert", "breakpoint", "preload",
        "await", "true", "false", "null", "and", "or", "not", "PI", "TAU", "INF", "NAN"
    };

    public GDDuplicateDetectionService(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel ?? throw new ArgumentNullException(nameof(projectModel));
        _project = projectModel.Project;
    }

    /// <summary>
    /// Analyzes the project for duplicated code blocks.
    /// </summary>
    public GDDuplicateReport AnalyzeProject(GDDuplicateOptions options)
    {
        var allBlocks = new List<GDCodeBlock>();
        int totalProjectLines = 0;

        foreach (var file in _project.ScriptFiles)
        {
            if (file.FullPath == null) continue;
            if (ShouldIgnoreFile(file.FullPath, options)) continue;

            var semanticModel = _projectModel.GetSemanticModel(file);
            if (semanticModel == null) continue;

            totalProjectLines += CountLines(file.FullPath);

            foreach (var methodSymbol in semanticModel.GetMethods())
            {
                if (methodSymbol.DeclarationNode is not GDMethodDeclaration method)
                    continue;

                var blocks = ExtractCodeBlocks(file, methodSymbol, method, options);
                allBlocks.AddRange(blocks);
            }
        }

        var duplicates = options.SimilarityThreshold < 1.0
            ? FindSimilarDuplicates(allBlocks, options)
            : FindExactDuplicates(allBlocks, options);

        if (options.IncludeCodePreview)
        {
            foreach (var group in duplicates)
            {
                EnrichGroupWithPreview(group);
                SuggestRefactoring(group);
            }
        }
        else
        {
            foreach (var group in duplicates)
            {
                SuggestRefactoring(group);
            }
        }

        var totalDuplicateLines = duplicates.Sum(g => g.TotalDuplicateLines);
        var duplicationPercentage = totalProjectLines > 0
            ? (double)totalDuplicateLines / totalProjectLines * 100.0
            : 0.0;

        return new GDDuplicateReport
        {
            Duplicates = duplicates,
            TotalBlocksAnalyzed = allBlocks.Count,
            TotalDuplicateGroups = duplicates.Count,
            TotalDuplicateLines = totalDuplicateLines,
            TotalProjectLines = totalProjectLines,
            DuplicationPercentage = Math.Round(duplicationPercentage, 2)
        };
    }

    /// <summary>
    /// Analyzes the project with baseline comparison.
    /// </summary>
    public GDDuplicateReport AnalyzeProjectWithBaseline(GDDuplicateOptions options, GDDuplicateReport? baseline)
    {
        var report = AnalyzeProject(options);

        if (baseline != null)
        {
            var currentHashes = new HashSet<string>(report.Duplicates.Select(d => d.Hash));
            var baselineHashes = new HashSet<string>(baseline.Duplicates.Select(d => d.Hash));

            report.NewDuplicateGroups = currentHashes.Except(baselineHashes).Count();
            report.FixedDuplicateGroups = baselineHashes.Except(currentHashes).Count();
        }

        return report;
    }

    private List<GDCodeBlock> ExtractCodeBlocks(
        GDScriptFile file,
        GDSymbolInfo methodSymbol,
        GDMethodDeclaration method,
        GDDuplicateOptions options)
    {
        var results = new List<GDCodeBlock>();
        var statements = method.Statements;
        if (statements == null) return results;

        var tokensWithLines = new List<(string Token, int Line)>();

        foreach (var node in statements.AllTokens)
        {
            var tokenText = NormalizeToken(node.ToString(), options);
            if (!string.IsNullOrEmpty(tokenText))
            {
                tokensWithLines.Add((tokenText, node.StartLine));
            }
        }

        if (tokensWithLines.Count == 0) return results;

        var startLine = methodSymbol.DeclarationNode?.StartLine ?? method.AllTokens.FirstOrDefault()?.StartLine ?? 0;
        var endLine = method.AllTokens.LastOrDefault()?.EndLine ?? 0;
        var lineCount = endLine - startLine + 1;

        if (options.Granularity == GDDuplicateGranularity.Method)
        {
            if (tokensWithLines.Count >= options.MinTokens && lineCount >= options.MinLines)
            {
                var tokens = tokensWithLines.Select(t => t.Token).ToList();
                results.Add(new GDCodeBlock
                {
                    FilePath = file.FullPath ?? "",
                    MethodName = methodSymbol.Name,
                    StartLine = startLine,
                    EndLine = endLine,
                    LineCount = lineCount,
                    TokenCount = tokens.Count,
                    Hash = ComputeHash(tokens),
                    NormalizedTokens = tokens,
                    TokensWithLines = tokensWithLines
                });
            }
        }
        else
        {
            var windows = ExtractSlidingWindows(
                file.FullPath ?? "",
                methodSymbol.Name,
                tokensWithLines,
                options);
            results.AddRange(windows);
        }

        return results;
    }

    private List<GDCodeBlock> ExtractSlidingWindows(
        string filePath,
        string methodName,
        List<(string Token, int Line)> tokensWithLines,
        GDDuplicateOptions options)
    {
        var results = new List<GDCodeBlock>();
        var windowSize = options.MinTokens;

        if (tokensWithLines.Count < windowSize)
            return results;

        for (int i = 0; i <= tokensWithLines.Count - windowSize; i++)
        {
            var windowTokens = tokensWithLines.Skip(i).Take(windowSize).ToList();
            var startLine = windowTokens.First().Line;
            var endLine = windowTokens.Last().Line;
            var lineCount = endLine - startLine + 1;

            if (lineCount < options.MinLines)
                continue;

            var tokens = windowTokens.Select(t => t.Token).ToList();
            results.Add(new GDCodeBlock
            {
                FilePath = filePath,
                MethodName = methodName,
                StartLine = startLine,
                EndLine = endLine,
                LineCount = lineCount,
                TokenCount = tokens.Count,
                Hash = ComputeHash(tokens),
                NormalizedTokens = tokens,
                TokensWithLines = windowTokens
            });
        }

        return results;
    }

    private List<GDDuplicateGroup> FindExactDuplicates(List<GDCodeBlock> blocks, GDDuplicateOptions options)
    {
        var groups = blocks
            .GroupBy(b => b.Hash)
            .Where(g => g.Count() >= options.MinInstances)
            .ToList();

        var result = new List<GDDuplicateGroup>();

        foreach (var group in groups)
        {
            var mergedInstances = MergeOverlappingInstances(group.ToList());
            if (mergedInstances.Count < options.MinInstances)
                continue;

            var firstBlock = group.First();
            result.Add(new GDDuplicateGroup
            {
                Hash = group.Key,
                Instances = mergedInstances.Select(b => new GDDuplicateInstance
                {
                    FilePath = b.FilePath,
                    MethodName = b.MethodName,
                    StartLine = b.StartLine,
                    EndLine = b.EndLine,
                    LineCount = b.LineCount,
                    TokenCount = b.TokenCount
                }).ToList(),
                InstanceCount = mergedInstances.Count,
                TokenCount = firstBlock.TokenCount,
                TotalDuplicateLines = mergedInstances.Skip(1).Sum(b => b.LineCount),
                SimilarityScore = 1.0
            });
        }

        return result;
    }

    private List<GDDuplicateGroup> FindSimilarDuplicates(List<GDCodeBlock> blocks, GDDuplicateOptions options)
    {
        var groups = new List<GDDuplicateGroup>();
        var processed = new HashSet<int>();

        for (int i = 0; i < blocks.Count; i++)
        {
            if (processed.Contains(i)) continue;

            var group = new List<(int Index, double Similarity)> { (i, 1.0) };

            for (int j = i + 1; j < blocks.Count; j++)
            {
                if (processed.Contains(j)) continue;

                var similarity = CalculateSimilarity(
                    blocks[i].NormalizedTokens,
                    blocks[j].NormalizedTokens);

                if (similarity >= options.SimilarityThreshold)
                {
                    group.Add((j, similarity));
                    processed.Add(j);
                }
            }

            if (group.Count >= options.MinInstances)
            {
                var avgSimilarity = group.Average(g => g.Similarity);
                var instanceBlocks = group.Select(g => blocks[g.Index]).ToList();
                var mergedInstances = MergeOverlappingInstances(instanceBlocks);

                if (mergedInstances.Count >= options.MinInstances)
                {
                    groups.Add(new GDDuplicateGroup
                    {
                        Hash = blocks[i].Hash,
                        Instances = mergedInstances.Select(b => new GDDuplicateInstance
                        {
                            FilePath = b.FilePath,
                            MethodName = b.MethodName,
                            StartLine = b.StartLine,
                            EndLine = b.EndLine,
                            LineCount = b.LineCount,
                            TokenCount = b.TokenCount
                        }).ToList(),
                        InstanceCount = mergedInstances.Count,
                        TokenCount = blocks[i].TokenCount,
                        TotalDuplicateLines = mergedInstances.Skip(1).Sum(b => b.LineCount),
                        SimilarityScore = Math.Round(avgSimilarity, 2)
                    });
                }
            }

            processed.Add(i);
        }

        return groups;
    }

    private List<GDCodeBlock> MergeOverlappingInstances(List<GDCodeBlock> blocks)
    {
        var result = new List<GDCodeBlock>();
        var groupedByFile = blocks.GroupBy(b => (b.FilePath, b.MethodName));

        foreach (var fileGroup in groupedByFile)
        {
            var sorted = fileGroup.OrderBy(b => b.StartLine).ToList();
            GDCodeBlock? current = null;

            foreach (var block in sorted)
            {
                if (current == null)
                {
                    current = block;
                }
                else if (block.StartLine <= current.EndLine + 1)
                {
                    current = new GDCodeBlock
                    {
                        FilePath = current.FilePath,
                        MethodName = current.MethodName,
                        StartLine = current.StartLine,
                        EndLine = Math.Max(current.EndLine, block.EndLine),
                        LineCount = Math.Max(current.EndLine, block.EndLine) - current.StartLine + 1,
                        TokenCount = current.TokenCount + block.TokenCount,
                        Hash = current.Hash,
                        NormalizedTokens = current.NormalizedTokens.Concat(block.NormalizedTokens).ToList(),
                        TokensWithLines = current.TokensWithLines.Concat(block.TokensWithLines).ToList()
                    };
                }
                else
                {
                    result.Add(current);
                    current = block;
                }
            }

            if (current != null)
                result.Add(current);
        }

        return result;
    }

    private double CalculateSimilarity(List<string> tokens1, List<string> tokens2)
    {
        if (tokens1.Count == 0 && tokens2.Count == 0) return 1.0;
        if (tokens1.Count == 0 || tokens2.Count == 0) return 0.0;

        var lcs = LongestCommonSubsequence(tokens1, tokens2);
        return 2.0 * lcs / (tokens1.Count + tokens2.Count);
    }

    private int LongestCommonSubsequence(List<string> a, List<string> b)
    {
        var m = a.Count;
        var n = b.Count;
        var dp = new int[m + 1, n + 1];

        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (a[i - 1] == b[j - 1])
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                else
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        return dp[m, n];
    }

    private void EnrichGroupWithPreview(GDDuplicateGroup group)
    {
        var firstInstance = group.Instances.FirstOrDefault();
        if (firstInstance == null) return;

        try
        {
            if (!File.Exists(firstInstance.FilePath)) return;

            var lines = File.ReadAllLines(firstInstance.FilePath);
            var snippetLines = lines
                .Skip(firstInstance.StartLine - 1)
                .Take(firstInstance.LineCount)
                .ToArray();

            group.CommonCode = string.Join("\n", snippetLines);

            foreach (var instance in group.Instances)
            {
                if (!File.Exists(instance.FilePath)) continue;

                var instanceLines = File.ReadAllLines(instance.FilePath);
                var previewLines = instanceLines
                    .Skip(instance.StartLine - 1)
                    .Take(Math.Min(5, instance.LineCount))
                    .ToArray();

                instance.CodeSnippet = string.Join("\n", previewLines);
            }
        }
        catch
        {
            // Ignore file reading errors
        }
    }

    private void SuggestRefactoring(GDDuplicateGroup group)
    {
        var filesInvolved = group.Instances.Select(i => i.FilePath).Distinct().ToList();
        var methodsInvolved = group.Instances.Select(i => i.MethodName).Distinct().ToList();

        if (filesInvolved.Count == 1)
        {
            if (methodsInvolved.Count > 1)
            {
                group.SuggestedRefactoring = GDRefactoringType.ExtractMethod;
                group.RefactoringHint = $"Extract common code to a new private method in {Path.GetFileName(filesInvolved[0])}";
            }
            else
            {
                group.SuggestedRefactoring = GDRefactoringType.ExtractMethod;
                group.RefactoringHint = $"Consider extracting repeated logic in {methodsInvolved[0]}";
            }
        }
        else if (filesInvolved.Count == 2)
        {
            group.SuggestedRefactoring = GDRefactoringType.ExtractToBase;
            group.RefactoringHint = $"Consider extracting common code to a shared base class or utility";
        }
        else
        {
            group.SuggestedRefactoring = GDRefactoringType.CreateUtilityFunc;
            group.RefactoringHint = $"Create a utility function and call from {filesInvolved.Count} files";
        }
    }

    private bool ShouldIgnoreFile(string filePath, GDDuplicateOptions options)
    {
        if (options.IgnorePatterns.Count == 0) return false;

        var fileName = Path.GetFileName(filePath);

        foreach (var pattern in options.IgnorePatterns)
        {
            if (MatchGlob(filePath, pattern) || MatchGlob(fileName, pattern))
                return true;
        }

        return false;
    }

    private static bool MatchGlob(string path, string pattern)
    {
        var normalized = path.Replace('\\', '/');
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(normalized, regexPattern, RegexOptions.IgnoreCase);
    }

    private static int CountLines(string filePath)
    {
        try
        {
            return File.ReadAllLines(filePath).Length;
        }
        catch
        {
            return 0;
        }
    }

    private string NormalizeToken(string? token, GDDuplicateOptions options)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "";

        if (token.StartsWith("#"))
            return "";

        if (options.NormalizeIdentifiers)
        {
            if (IsIdentifier(token) && !Keywords.Contains(token))
            {
                return "$ID";
            }
        }

        if (options.NormalizeLiterals)
        {
            if (IsStringLiteral(token))
                return "$STR";
            if (IsNumberLiteral(token))
                return "$NUM";
        }

        return token;
    }

    private static string ComputeHash(List<string> tokens)
    {
        var data = string.Join("|", tokens);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash[..16]);
    }

    private static bool IsIdentifier(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        return char.IsLetter(token[0]) || token[0] == '_';
    }

    private static bool IsStringLiteral(string token)
    {
        return token.StartsWith("\"") || token.StartsWith("'");
    }

    private static bool IsNumberLiteral(string token)
    {
        return token.Length > 0 && (char.IsDigit(token[0]) || token[0] == '.');
    }

    /// <summary>
    /// Internal representation of a code block during analysis.
    /// </summary>
    internal class GDCodeBlock
    {
        public string FilePath { get; set; } = "";
        public string MethodName { get; set; } = "";
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int LineCount { get; set; }
        public int TokenCount { get; set; }
        public string Hash { get; set; } = "";
        public List<string> NormalizedTokens { get; set; } = new();
        public List<(string Token, int Line)> TokensWithLines { get; set; } = new();
    }
}
