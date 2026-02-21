using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

public static class InitCommandBuilder
{
    public static Command Build()
    {
        var command = new Command("init",
            "Create a .gdshrapt.json configuration file.\n\n" +
            "Available presets: minimal, recommended, strict, relaxed, ci, local, team\n\n" +
            "Examples:\n" +
            "  gdshrapt init                             Create config in current directory\n" +
            "  gdshrapt init --preset strict             Create strict config\n" +
            "  gdshrapt init --preset ci                 CI-optimized config (fail-fast)\n" +
            "  gdshrapt init ./my-project --force        Overwrite existing config");

        var pathArg = new Argument<string>("path", () => ".", "Directory to create config in");
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

            var cmd = new GDInitCommand(path, preset, force);
            Environment.ExitCode = await cmd.ExecuteAsync();
        });

        return command;
    }
}
