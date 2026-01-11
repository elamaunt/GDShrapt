using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the analyze command.
/// </summary>
public static class AnalyzeCommandBuilder
{
    public static Command Build(Option<string> globalFormatOption)
    {
        var command = new Command("analyze", "Analyze a GDScript project and output diagnostics");

        var pathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");
        command.AddArgument(pathArg);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";

            var formatter = CommandHelpers.GetFormatter(format);
            var cmd = new GDAnalyzeCommand(projectPath, formatter);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
