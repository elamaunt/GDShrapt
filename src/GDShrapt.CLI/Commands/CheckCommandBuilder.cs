using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builder for the check command.
/// </summary>
public static class CheckCommandBuilder
{
    public static Command Build(Option<string> globalFormatOption)
    {
        var command = new Command("check", "Check a GDScript project for errors (for CI/CD)");

        var pathArg = new Argument<string>("project-path", () => ".", "Path to the Godot project");
        var quietOption = new Option<bool>(
            new[] { "--quiet", "-q" },
            "Suppress output, only return exit code");

        command.AddArgument(pathArg);
        command.AddOption(quietOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var quiet = context.ParseResult.GetValueForOption(quietOption);

            var formatter = CommandHelpers.GetFormatter(format);
            var cmd = new GDCheckCommand(projectPath, formatter, quiet: quiet);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
