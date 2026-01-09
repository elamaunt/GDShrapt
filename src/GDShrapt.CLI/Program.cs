using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;
using GDShrapt.Reader;

namespace GDShrapt.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("GDShrapt - GDScript analysis and refactoring CLI");

        // Global options
        var formatOption = new Option<string>(
            aliases: new[] { "--format", "-f" },
            getDefaultValue: () => "text",
            description: "Output format (text, json)");

        rootCommand.AddGlobalOption(formatOption);

        // analyze command
        var analyzeCommand = new Command("analyze", "Analyze a GDScript project and output diagnostics");
        var analyzePathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");
        analyzeCommand.AddArgument(analyzePathArg);
        analyzeCommand.SetHandler(async (string projectPath, string format) =>
        {
            var formatter = GetFormatter(format);
            var command = new GDAnalyzeCommand(projectPath, formatter);
            Environment.ExitCode = await command.ExecuteAsync();
        }, analyzePathArg, formatOption);
        rootCommand.AddCommand(analyzeCommand);

        // check command
        var checkCommand = new Command("check", "Check a GDScript project for errors (for CI/CD)");
        var checkPathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");
        var quietOption = new Option<bool>(
            aliases: new[] { "--quiet", "-q" },
            description: "Suppress output, only return exit code");
        checkCommand.AddArgument(checkPathArg);
        checkCommand.AddOption(quietOption);
        checkCommand.SetHandler(async (string projectPath, string format, bool quiet) =>
        {
            var formatter = GetFormatter(format);
            var command = new GDCheckCommand(projectPath, formatter, quiet: quiet);
            Environment.ExitCode = await command.ExecuteAsync();
        }, checkPathArg, formatOption, quietOption);
        rootCommand.AddCommand(checkCommand);

        // symbols command
        var symbolsCommand = new Command("symbols", "List symbols in a GDScript file");
        var symbolsFileArg = new Argument<string>("file", "Path to the GDScript file");
        symbolsCommand.AddArgument(symbolsFileArg);
        symbolsCommand.SetHandler(async (string filePath, string format) =>
        {
            var formatter = GetFormatter(format);
            var command = new GDSymbolsCommand(filePath, formatter);
            Environment.ExitCode = await command.ExecuteAsync();
        }, symbolsFileArg, formatOption);
        rootCommand.AddCommand(symbolsCommand);

        // find-refs command
        var findRefsCommand = new Command("find-refs", "Find references to a symbol");
        var symbolArg = new Argument<string>("symbol", "Symbol name to find references for");
        var findRefsProjectOption = new Option<string>(
            aliases: new[] { "--project", "-p" },
            getDefaultValue: () => ".",
            description: "Path to the Godot project");
        var findRefsFileOption = new Option<string?>(
            aliases: new[] { "--file" },
            description: "Limit search to a specific file");
        findRefsCommand.AddArgument(symbolArg);
        findRefsCommand.AddOption(findRefsProjectOption);
        findRefsCommand.AddOption(findRefsFileOption);
        findRefsCommand.SetHandler(async (string symbol, string projectPath, string? filePath, string format) =>
        {
            var formatter = GetFormatter(format);
            var command = new GDFindRefsCommand(symbol, projectPath, filePath, formatter);
            Environment.ExitCode = await command.ExecuteAsync();
        }, symbolArg, findRefsProjectOption, findRefsFileOption, formatOption);
        rootCommand.AddCommand(findRefsCommand);

        // rename command
        var renameCommand = new Command("rename", "Rename a symbol across the project");
        var oldNameArg = new Argument<string>("old-name", "Current symbol name");
        var newNameArg = new Argument<string>("new-name", "New symbol name");
        var renameProjectOption = new Option<string>(
            aliases: new[] { "--project", "-p" },
            getDefaultValue: () => ".",
            description: "Path to the Godot project");
        var renameFileOption = new Option<string?>(
            aliases: new[] { "--file" },
            description: "Limit rename to a specific file");
        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run", "-n" },
            description: "Show what would be changed without making changes");
        renameCommand.AddArgument(oldNameArg);
        renameCommand.AddArgument(newNameArg);
        renameCommand.AddOption(renameProjectOption);
        renameCommand.AddOption(renameFileOption);
        renameCommand.AddOption(dryRunOption);
        renameCommand.SetHandler(async (string oldName, string newName, string projectPath, string? filePath, bool dryRun, string format) =>
        {
            var formatter = GetFormatter(format);
            var command = new GDRenameCommand(oldName, newName, projectPath, filePath, formatter, dryRun: dryRun);
            Environment.ExitCode = await command.ExecuteAsync();
        }, oldNameArg, newNameArg, renameProjectOption, renameFileOption, dryRunOption, formatOption);
        rootCommand.AddCommand(renameCommand);

        // format command
        var formatCommand = new Command("format", "Format GDScript files");
        var formatPathArg = new Argument<string>("path", () => ".", "Path to file or directory");
        var formatDryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run", "-n" },
            description: "Show what would be formatted without making changes");
        var formatCheckOption = new Option<bool>(
            aliases: new[] { "--check", "-c" },
            description: "Check if files are formatted (exit 1 if not)");
        formatCommand.AddArgument(formatPathArg);
        formatCommand.AddOption(formatDryRunOption);
        formatCommand.AddOption(formatCheckOption);
        formatCommand.SetHandler(async (string path, bool dryRun, bool check, string format) =>
        {
            var formatter = GetFormatter(format);
            var command = new GDFormatCommand(path, formatter, dryRun: dryRun, checkOnly: check);
            Environment.ExitCode = await command.ExecuteAsync();
        }, formatPathArg, formatDryRunOption, formatCheckOption, formatOption);
        rootCommand.AddCommand(formatCommand);

        // lint command
        var lintCommand = new Command("lint", "Lint GDScript files for style and best practices");
        var lintPathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");
        var lintRulesOption = new Option<string?>(
            aliases: new[] { "--rules", "-r" },
            description: "Only run specific rules (comma-separated, e.g., GDL001,GDL003)");
        var lintCategoryOption = new Option<string?>(
            aliases: new[] { "--category", "-c" },
            description: "Only run rules in category (naming, style, best-practices, organization, documentation)");
        lintCommand.AddArgument(lintPathArg);
        lintCommand.AddOption(lintRulesOption);
        lintCommand.AddOption(lintCategoryOption);
        lintCommand.SetHandler(async (string projectPath, string? rules, string? category, string format) =>
        {
            var formatter = GetFormatter(format);
            var onlyRules = rules?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var categories = ParseCategories(category);
            var command = new GDLintCommand(projectPath, formatter, onlyRules: onlyRules, categories: categories);
            Environment.ExitCode = await command.ExecuteAsync();
        }, lintPathArg, lintRulesOption, lintCategoryOption, formatOption);
        rootCommand.AddCommand(lintCommand);

        // validate command
        var validateCommand = new Command("validate", "Validate GDScript syntax and semantics");
        var validatePathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");
        var validateChecksOption = new Option<string?>(
            aliases: new[] { "--checks" },
            description: "Checks to run (syntax,scope,types,calls,controlflow,indentation or 'all')");
        var validateStrictOption = new Option<bool>(
            aliases: new[] { "--strict" },
            description: "Treat all issues as errors");
        validateCommand.AddArgument(validatePathArg);
        validateCommand.AddOption(validateChecksOption);
        validateCommand.AddOption(validateStrictOption);
        validateCommand.SetHandler(async (string projectPath, string? checks, bool strict, string format) =>
        {
            var formatter = GetFormatter(format);
            var validationChecks = ParseValidationChecks(checks);
            var command = new GDValidateCommand(projectPath, formatter, checks: validationChecks, strict: strict);
            Environment.ExitCode = await command.ExecuteAsync();
        }, validatePathArg, validateChecksOption, validateStrictOption, formatOption);
        rootCommand.AddCommand(validateCommand);

        // parse command
        var parseCommand = new Command("parse", "Parse a GDScript file and output its AST structure");
        var parseFileArg = new Argument<string>("file", "Path to the GDScript file");
        var parseFormatOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            getDefaultValue: () => "tree",
            description: "Output format (tree, json, tokens)");
        var parsePositionsOption = new Option<bool>(
            aliases: new[] { "--positions", "-p" },
            description: "Show position information in output");
        parseCommand.AddArgument(parseFileArg);
        parseCommand.AddOption(parseFormatOption);
        parseCommand.AddOption(parsePositionsOption);
        parseCommand.SetHandler(async (string filePath, string outputFormat, bool showPositions, string format) =>
        {
            var formatter = GetFormatter(format);
            var parseOutputFormat = ParseOutputFormat(outputFormat);
            var command = new GDParseCommand(filePath, formatter, outputFormat: parseOutputFormat, showPositions: showPositions);
            Environment.ExitCode = await command.ExecuteAsync();
        }, parseFileArg, parseFormatOption, parsePositionsOption, formatOption);
        rootCommand.AddCommand(parseCommand);

        // extract-style command
        var extractStyleCommand = new Command("extract-style", "Extract formatting style from sample GDScript code");
        var extractFileArg = new Argument<string>("file", "Path to the sample GDScript file");
        var extractFormatOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            getDefaultValue: () => "toml",
            description: "Output format (toml, json, text)");
        extractStyleCommand.AddArgument(extractFileArg);
        extractStyleCommand.AddOption(extractFormatOption);
        extractStyleCommand.SetHandler(async (string filePath, string outputFormat, string format) =>
        {
            var formatter = GetFormatter(format);
            var styleOutputFormat = ParseStyleOutputFormat(outputFormat);
            var command = new GDExtractStyleCommand(filePath, formatter, outputFormat: styleOutputFormat);
            Environment.ExitCode = await command.ExecuteAsync();
        }, extractFileArg, extractFormatOption, formatOption);
        rootCommand.AddCommand(extractStyleCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static IGDOutputFormatter GetFormatter(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => new GDJsonFormatter(),
            "text" => new GDTextFormatter(),
            _ => new GDTextFormatter()
        };
    }

    private static GDLintCategory[]? ParseCategories(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return null;

        var categories = category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return categories.Select(c => c.ToLowerInvariant() switch
        {
            "naming" => GDLintCategory.Naming,
            "style" => GDLintCategory.Style,
            "best-practices" or "bestpractices" => GDLintCategory.BestPractices,
            "organization" => GDLintCategory.Organization,
            "documentation" => GDLintCategory.Documentation,
            _ => GDLintCategory.Naming // default
        }).ToArray();
    }

    private static GDValidationChecks ParseValidationChecks(string? checks)
    {
        if (string.IsNullOrWhiteSpace(checks) || checks.Equals("all", StringComparison.OrdinalIgnoreCase))
            return GDValidationChecks.All;

        var result = GDValidationChecks.None;
        var parts = checks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            result |= part.ToLowerInvariant() switch
            {
                "syntax" => GDValidationChecks.Syntax,
                "scope" => GDValidationChecks.Scope,
                "types" => GDValidationChecks.Types,
                "calls" => GDValidationChecks.Calls,
                "controlflow" or "control-flow" => GDValidationChecks.ControlFlow,
                "indentation" => GDValidationChecks.Indentation,
                _ => GDValidationChecks.None
            };
        }

        return result == GDValidationChecks.None ? GDValidationChecks.All : result;
    }

    private static GDParseOutputFormat ParseOutputFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "tree" => GDParseOutputFormat.Tree,
            "json" => GDParseOutputFormat.Json,
            "tokens" => GDParseOutputFormat.Tokens,
            _ => GDParseOutputFormat.Tree
        };
    }

    private static GDExtractStyleOutputFormat ParseStyleOutputFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "toml" => GDExtractStyleOutputFormat.Toml,
            "json" => GDExtractStyleOutputFormat.Json,
            "text" => GDExtractStyleOutputFormat.Text,
            _ => GDExtractStyleOutputFormat.Toml
        };
    }
}
