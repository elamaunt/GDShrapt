using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

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
}
