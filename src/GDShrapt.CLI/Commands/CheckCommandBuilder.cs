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
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> globalQuietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("check", "Quick project health check with exit codes for CI/CD pipelines.\n\nExamples:\n  gdshrapt check                           Check current directory\n  gdshrapt check --silent                  Only return exit code\n  gdshrapt check --fail-on warning         Fail on warnings too");

        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var projectOption = new Option<string?>(
            new[] { "--project", "-p" },
            "Path to the Godot project (alternative to positional argument)");
        var silentOption = new Option<bool>(
            new[] { "--silent", "-s" },
            "Suppress all output, only return exit code");
        var failOnOption = new Option<string?>(
            new[] { "--fail-on" },
            "Fail threshold: error (default), warning, or hint");

        command.AddArgument(pathArg);
        command.AddOption(projectOption);
        command.AddOption(silentOption);
        command.AddOption(failOnOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForOption(projectOption)
                ?? context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var silent = context.ParseResult.GetValueForOption(silentOption);
            var failOn = context.ParseResult.GetValueForOption(failOnOption);

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
                }
            }

            var formatter = CommandHelpers.GetFormatter(format);
            var cmd = new GDCheckCommand(projectPath, formatter, silent: silent, config: config);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
