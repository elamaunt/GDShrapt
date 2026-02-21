using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

/// <summary>
/// Builds the 'config' parent command with init, show, and validate subcommands.
/// </summary>
public static class ConfigCommandBuilder
{
    public static Command Build()
    {
        var command = new Command("config",
            "Manage .gdshrapt.json configuration.\n\n" +
            "Subcommands:\n" +
            "  init       Create a new configuration file\n" +
            "  show       Display current configuration\n" +
            "  validate   Check configuration for errors");

        command.AddCommand(BuildInitSubcommand());
        command.AddCommand(BuildShowSubcommand());
        command.AddCommand(BuildValidateSubcommand());

        return command;
    }

    private static Command BuildInitSubcommand()
    {
        var command = new Command("init",
            "Create a .gdshrapt.json configuration file with optional preset.\n\n" +
            "Available presets: minimal, recommended, strict, relaxed, ci, local, team\n\n" +
            "Examples:\n" +
            "  gdshrapt config init                          Create config with defaults\n" +
            "  gdshrapt config init --preset ci              CI-optimized config (fail-fast)\n" +
            "  gdshrapt config init --preset team            Team conventions (balanced)\n" +
            "  gdshrapt config init ./project --force        Overwrite existing config");

        var pathArg = new Argument<string>("project-path", () => ".", "Directory to create config in");
        var presetOption = new Option<string?>(
            new[] { "--preset" },
            "Apply a built-in preset (minimal, recommended, strict, relaxed, ci, local, team)");
        var forceOption = new Option<bool>(
            new[] { "--force" },
            "Overwrite existing configuration file");

        command.AddArgument(pathArg);
        command.AddOption(presetOption);
        command.AddOption(forceOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var path = context.ParseResult.GetValueForArgument(pathArg);
            var preset = context.ParseResult.GetValueForOption(presetOption);
            var force = context.ParseResult.GetValueForOption(forceOption);

            var cmd = new GDConfigInitCommand(path, preset, force);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }

    private static Command BuildShowSubcommand()
    {
        var command = new Command("show",
            "Display current project configuration.\n\n" +
            "By default shows the raw .gdshrapt.json content.\n" +
            "Use --effective to see resolved config (defaults + file).\n\n" +
            "Examples:\n" +
            "  gdshrapt config show                          Show raw config\n" +
            "  gdshrapt config show --effective              Show resolved config with defaults\n" +
            "  gdshrapt config show --format json            Output as JSON\n" +
            "  gdshrapt config show ./project --effective    Show effective config for project");

        var pathArg = new Argument<string>("project-path", () => ".", "Directory containing configuration");
        var effectiveOption = new Option<bool>(
            new[] { "--effective" },
            "Show effective configuration (defaults merged with file)");
        var formatOption = new Option<string>(
            new[] { "--format" },
            () => "text",
            "Output format: text (default) or json");

        command.AddArgument(pathArg);
        command.AddOption(effectiveOption);
        command.AddOption(formatOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var path = context.ParseResult.GetValueForArgument(pathArg);
            var effective = context.ParseResult.GetValueForOption(effectiveOption);
            var format = context.ParseResult.GetValueForOption(formatOption) ?? "text";

            var cmd = new GDConfigShowCommand(path, effective, format);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }

    private static Command BuildValidateSubcommand()
    {
        var command = new Command("validate",
            "Check .gdshrapt.json for errors (schema, ranges, conflicts).\n\n" +
            "Examples:\n" +
            "  gdshrapt config validate                      Validate current config\n" +
            "  gdshrapt config validate --explain            Show detailed explanations\n" +
            "  gdshrapt config validate ./project            Validate config in project dir");

        var pathArg = new Argument<string>("project-path", () => ".", "Directory containing configuration");
        var explainOption = new Option<bool>(
            new[] { "--explain" },
            "Show detailed explanations for each issue");

        command.AddArgument(pathArg);
        command.AddOption(explainOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var path = context.ParseResult.GetValueForArgument(pathArg);
            var explain = context.ParseResult.GetValueForOption(explainOption);

            var cmd = new GDConfigValidateCommand(path, explain);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
