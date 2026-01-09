using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Commands;

/// <summary>
/// Builder for the rename command.
/// </summary>
public static class RenameCommandBuilder
{
    public static Command Build(Option<string> globalFormatOption)
    {
        var command = new Command("rename", "Rename a symbol across the project");

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
