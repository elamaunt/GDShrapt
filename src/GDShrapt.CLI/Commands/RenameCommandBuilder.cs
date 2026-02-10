using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the rename command.
/// </summary>
public static class RenameCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("rename", "Safely rename a variable, function, signal, or class across all files.\n\nExamples:\n  gdshrapt rename old_name new_name        Rename in current project\n  gdshrapt rename old new --dry-run        Preview changes only\n  gdshrapt rename old new --file player.gd Rename in one file");

        var oldNameArg = new Argument<string>("old-name", "Current symbol name");
        var newNameArg = new Argument<string>("new-name", "New symbol name");
        var projectOption = new Option<string>(
            new[] { "--project", "-p" },
            getDefaultValue: () => ".",
            description: "Path to the Godot project");
        var fileOption = new Option<string?>(
            new[] { "--file" },
            "Limit rename to a specific file");
        var dryRunOption = new Option<bool>(
            new[] { "--dry-run", "-n" },
            "Show what would be changed without making changes");

        command.AddArgument(oldNameArg);
        command.AddArgument(newNameArg);
        command.AddOption(projectOption);
        command.AddOption(fileOption);
        command.AddOption(dryRunOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var oldName = context.ParseResult.GetValueForArgument(oldNameArg);
            var newName = context.ParseResult.GetValueForArgument(newNameArg);
            var projectPath = context.ParseResult.GetValueForOption(projectOption)!;
            var filePath = context.ParseResult.GetValueForOption(fileOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";

            var formatter = CommandHelpers.GetFormatter(format);
            var cmd = new GDRenameCommand(oldName, newName, projectPath, filePath, formatter, dryRun: dryRun);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
