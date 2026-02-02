using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the find-refs command.
/// </summary>
public static class FindRefsCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption)
    {
        var command = new Command("find-refs", "Find references to a symbol");

        var symbolArg = new Argument<string>("symbol", "Symbol name to find references for");
        var projectOption = new Option<string>(
            new[] { "--project", "-p" },
            getDefaultValue: () => ".",
            description: "Path to the Godot project");
        var fileOption = new Option<string?>(
            new[] { "--file" },
            "Limit search to a specific file");

        command.AddArgument(symbolArg);
        command.AddOption(projectOption);
        command.AddOption(fileOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var symbol = context.ParseResult.GetValueForArgument(symbolArg);
            var projectPath = context.ParseResult.GetValueForOption(projectOption)!;
            var filePath = context.ParseResult.GetValueForOption(fileOption);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";

            var formatter = CommandHelpers.GetFormatter(format);
            var cmd = new GDFindRefsCommand(symbol, projectPath, filePath, formatter);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
