using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

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
        var failOnOption = new Option<string?>(
            new[] { "--fail-on" },
            "Fail threshold: error (default), warning, or hint");

        command.AddArgument(pathArg);
        command.AddOption(quietOption);
        command.AddOption(failOnOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var failOn = context.ParseResult.GetValueForOption(failOnOption);

            // Build config with fail-on overrides
            GDProjectConfig? config = null;
            if (failOn != null)
            {
                config = new GDProjectConfig();
                switch (failOn.ToLowerInvariant())
                {
                    case "warning":
                        config.Cli.FailOnWarning = true;
                        break;
                    case "hint":
                        config.Cli.FailOnWarning = true;
                        config.Cli.FailOnHint = true;
                        break;
                    // "error" is the default, no changes needed
                }
            }

            var formatter = CommandHelpers.GetFormatter(format);
            var cmd = new GDCheckCommand(projectPath, formatter, quiet: quiet, config: config);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
