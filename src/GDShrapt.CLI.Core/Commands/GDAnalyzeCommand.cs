using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Analyzes a GDScript project and outputs diagnostics.
/// </summary>
public class GDAnalyzeCommand : IGDCommand
{
    private readonly string _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly GDProjectConfig? _config;

    public string Name => "analyze";
    public string Description => "Analyze a GDScript project and output diagnostics";

    public GDAnalyzeCommand(string projectPath, IGDOutputFormatter formatter, TextWriter? output = null, GDProjectConfig? config = null)
    {
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _config = config;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var projectRoot = GDProjectLoader.FindProjectRoot(_projectPath);
            if (projectRoot == null)
            {
                _formatter.WriteError(_output, $"Could not find project.godot in or above: {_projectPath}");
                return Task.FromResult(2);
            }

            // Load config from project or use provided
            var config = _config ?? GDConfigLoader.LoadConfig(projectRoot);

            using var project = GDProjectLoader.LoadProject(projectRoot);

            var result = BuildAnalysisResult(project, projectRoot, config);
            _formatter.WriteAnalysisResult(_output, result);

            // Determine exit code based on config
            if (result.TotalErrors > 0)
                return Task.FromResult(1);

            if (config.Cli.FailOnWarning && result.TotalWarnings > 0)
                return Task.FromResult(1);

            if (config.Cli.FailOnHint && result.TotalHints > 0)
                return Task.FromResult(1);

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
        }
    }

    private static GDAnalysisResult BuildAnalysisResult(GDScriptProject project, string projectRoot, GDProjectConfig config)
    {
        var result = new GDAnalysisResult
        {
            ProjectPath = projectRoot,
            TotalFiles = project.ScriptFiles.Count()
        };

        var filesWithErrors = 0;
        var totalErrors = 0;
        var totalWarnings = 0;
        var totalHints = 0;

        // Create linter with options from config
        var linterOptions = CreateLinterOptionsFromConfig(config);
        var linter = new GDLinter(linterOptions);

        // Create validator
        var validator = new GDValidator();
        var validationOptions = new GDValidationOptions
        {
            CheckSyntax = true,
            CheckScope = true,
            CheckTypes = true,
            CheckCalls = true,
            CheckControlFlow = true,
            CheckIndentation = config.Linting.FormattingLevel != GDFormattingLevel.Off
        };

        foreach (var script in project.ScriptFiles)
        {
            var relativePath = GetRelativePath(script.Reference.FullPath, projectRoot);

            // Check if file should be excluded
            if (GDConfigLoader.ShouldExclude(relativePath, config.Cli.Exclude))
                continue;

            var fileDiags = new GDFileDiagnostics
            {
                FilePath = relativePath
            };

            // Check for parse errors
            if (script.WasReadError)
            {
                fileDiags.Diagnostics.Add(new GDDiagnosticInfo
                {
                    Code = "GD0001",
                    Message = "Failed to parse file",
                    Severity = GDSeverity.Error,
                    Line = 1,
                    Column = 1
                });
                totalErrors++;
            }

            if (script.Class != null)
            {
                // Check for invalid tokens in AST
                var invalidTokens = script.Class.AllInvalidTokens;
                foreach (var token in invalidTokens)
                {
                    fileDiags.Diagnostics.Add(new GDDiagnosticInfo
                    {
                        Code = "GD0002",
                        Message = $"Invalid token: {token.ToString()?.Trim() ?? "unknown"}",
                        Severity = GDSeverity.Error,
                        Line = token.StartLine,
                        Column = token.StartColumn
                    });
                    totalErrors++;
                }

                // Run validator
                var validationResult = validator.Validate(script.Class, validationOptions);
                foreach (var diagnostic in validationResult.Diagnostics)
                {
                    var severity = MapValidatorSeverity(diagnostic.Severity);

                    // Check if rule is enabled in config
                    var ruleId = diagnostic.CodeString;
                    if (!IsRuleEnabled(config, ruleId, severity))
                        continue;

                    fileDiags.Diagnostics.Add(new GDDiagnosticInfo
                    {
                        Code = ruleId,
                        Message = diagnostic.Message,
                        Severity = GetConfiguredSeverity(config, ruleId, severity),
                        Line = diagnostic.StartLine,
                        Column = diagnostic.StartColumn,
                        EndLine = diagnostic.EndLine,
                        EndColumn = diagnostic.EndColumn
                    });

                    UpdateCounts(ref totalErrors, ref totalWarnings, ref totalHints, severity);
                }

                // Run linter
                if (config.Linting.Enabled)
                {
                    var lintResult = linter.Lint(script.Class);
                    foreach (var issue in lintResult.Issues)
                    {
                        var severity = MapLinterSeverity(issue.Severity);

                        // Check if rule is enabled in config
                        if (!IsRuleEnabled(config, issue.RuleId, severity))
                            continue;

                        fileDiags.Diagnostics.Add(new GDDiagnosticInfo
                        {
                            Code = issue.RuleId,
                            Message = issue.Message,
                            Severity = GetConfiguredSeverity(config, issue.RuleId, severity),
                            Line = issue.StartLine,
                            Column = issue.StartColumn,
                            EndLine = issue.EndLine,
                            EndColumn = issue.EndColumn
                        });

                        UpdateCounts(ref totalErrors, ref totalWarnings, ref totalHints, severity);
                    }
                }
            }

            if (fileDiags.Diagnostics.Count > 0)
            {
                filesWithErrors++;
                result.Files.Add(fileDiags);
            }
        }

        result.FilesWithErrors = filesWithErrors;
        result.TotalErrors = totalErrors;
        result.TotalWarnings = totalWarnings;
        result.TotalHints = totalHints;

        return result;
    }

    /// <summary>
    /// Creates linter options from project configuration.
    /// </summary>
    public static GDLinterOptions CreateLinterOptionsFromConfig(GDProjectConfig config)
    {
        var advanced = config.AdvancedLinting;

        return new GDLinterOptions
        {
            // Naming conventions
            ClassNameCase = MapNamingCase(advanced.ClassNameCase),
            FunctionNameCase = MapNamingCase(advanced.FunctionNameCase),
            VariableNameCase = MapNamingCase(advanced.VariableNameCase),
            ConstantNameCase = MapNamingCase(advanced.ConstantNameCase),
            SignalNameCase = MapNamingCase(advanced.SignalNameCase),
            EnumNameCase = MapNamingCase(advanced.EnumNameCase),
            EnumValueCase = MapNamingCase(advanced.EnumValueCase),
            RequireUnderscoreForPrivate = advanced.RequireUnderscoreForPrivate,

            // Best practices
            WarnUnusedVariables = advanced.WarnUnusedVariables,
            WarnUnusedParameters = advanced.WarnUnusedParameters,
            WarnUnusedSignals = advanced.WarnUnusedSignals,
            WarnEmptyFunctions = advanced.WarnEmptyFunctions,
            WarnMagicNumbers = advanced.WarnMagicNumbers,
            WarnVariableShadowing = advanced.WarnVariableShadowing,
            WarnAwaitInLoop = advanced.WarnAwaitInLoop,

            // Limits
            MaxParameters = advanced.MaxParameters,
            MaxFunctionLength = advanced.MaxFunctionLength,
            MaxCyclomaticComplexity = advanced.MaxCyclomaticComplexity,
            MaxLineLength = config.Linting.MaxLineLength,

            // Strict typing
            StrictTypingClassVariables = MapStrictTypingSeverity(advanced.StrictTypingClassVariables),
            StrictTypingLocalVariables = MapStrictTypingSeverity(advanced.StrictTypingLocalVariables),
            StrictTypingParameters = MapStrictTypingSeverity(advanced.StrictTypingParameters),
            StrictTypingReturnTypes = MapStrictTypingSeverity(advanced.StrictTypingReturnTypes),

            // Comment suppression
            EnableCommentSuppression = advanced.EnableCommentSuppression
        };
    }

    private static NamingCase MapNamingCase(GDNamingCase namingCase)
    {
        return namingCase switch
        {
            GDNamingCase.SnakeCase => NamingCase.SnakeCase,
            GDNamingCase.PascalCase => NamingCase.PascalCase,
            GDNamingCase.CamelCase => NamingCase.CamelCase,
            GDNamingCase.ScreamingSnakeCase => NamingCase.ScreamingSnakeCase,
            GDNamingCase.Any => NamingCase.Any,
            _ => NamingCase.SnakeCase
        };
    }

    private static GDLintSeverity? MapStrictTypingSeverity(GDStrictTypingSeverity? severity)
    {
        if (!severity.HasValue)
            return null;

        return severity.Value switch
        {
            GDStrictTypingSeverity.Warning => GDLintSeverity.Warning,
            GDStrictTypingSeverity.Error => GDLintSeverity.Error,
            _ => GDLintSeverity.Warning
        };
    }

    private static GDSeverity MapValidatorSeverity(Reader.GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            Reader.GDDiagnosticSeverity.Error => GDSeverity.Error,
            Reader.GDDiagnosticSeverity.Warning => GDSeverity.Warning,
            Reader.GDDiagnosticSeverity.Hint => GDSeverity.Hint,
            _ => GDSeverity.Information
        };
    }

    private static GDSeverity MapLinterSeverity(GDLintSeverity severity)
    {
        return severity switch
        {
            GDLintSeverity.Error => GDSeverity.Error,
            GDLintSeverity.Warning => GDSeverity.Warning,
            GDLintSeverity.Info => GDSeverity.Information,
            GDLintSeverity.Hint => GDSeverity.Hint,
            _ => GDSeverity.Information
        };
    }

    private static bool IsRuleEnabled(GDProjectConfig config, string ruleId, GDSeverity defaultSeverity)
    {
        if (config.Linting.Rules.TryGetValue(ruleId, out var ruleConfig))
        {
            return ruleConfig.Enabled;
        }
        return true; // Enabled by default
    }

    private static GDSeverity GetConfiguredSeverity(GDProjectConfig config, string ruleId, GDSeverity defaultSeverity)
    {
        if (config.Linting.Rules.TryGetValue(ruleId, out var ruleConfig) && ruleConfig.Severity.HasValue)
        {
            return ruleConfig.Severity.Value switch
            {
                Semantics.GDDiagnosticSeverity.Error => GDSeverity.Error,
                Semantics.GDDiagnosticSeverity.Warning => GDSeverity.Warning,
                Semantics.GDDiagnosticSeverity.Info => GDSeverity.Information,
                Semantics.GDDiagnosticSeverity.Hint => GDSeverity.Hint,
                _ => defaultSeverity
            };
        }
        return defaultSeverity;
    }

    private static void UpdateCounts(ref int errors, ref int warnings, ref int hints, GDSeverity severity)
    {
        switch (severity)
        {
            case GDSeverity.Error:
                errors++;
                break;
            case GDSeverity.Warning:
                warnings++;
                break;
            case GDSeverity.Hint:
            case GDSeverity.Information:
                hints++;
                break;
        }
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        try
        {
            return Path.GetRelativePath(basePath, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }
}
