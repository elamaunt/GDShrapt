using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.InteropServices;
using System.Threading;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

public static class WatchCommandBuilder
{
    public static Command Build(
        Option<string> globalFormatOption,
        Option<bool> verboseOption,
        Option<bool> debugOption,
        Option<bool> quietOption,
        Option<string?> logLevelOption)
    {
        var command = new Command("watch",
            "[Experimental] Watch for file changes and report diagnostics in real-time.\n\n" +
            "Examples:\n" +
            "  gdshrapt watch                           Watch current directory\n" +
            "  gdshrapt watch ./my-project              Watch specific project\n" +
            "  gdshrapt watch --format json             Output as JSON");

        var pathArg = new Argument<string>("project-path", "Path to the Godot project") { Arity = ArgumentArity.ZeroOrOne };
        var projectOption = new Option<string?>(
            new[] { "--project", "-p" },
            "Path to the Godot project (alternative to positional argument)");

        command.AddArgument(pathArg);
        command.AddOption(projectOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var projectPath = context.ParseResult.GetValueForOption(projectOption)
                ?? context.ParseResult.GetValueForArgument(pathArg);
            var format = context.ParseResult.GetValueForOption(globalFormatOption) ?? "text";
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var debug = context.ParseResult.GetValueForOption(debugOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);

            var logLevel = context.ParseResult.GetValueForOption(logLevelOption);
            var logger = GDCliLogger.FromFlags(quiet, verbose, debug, logLevel);

            var formatter = CommandHelpers.GetFormatter(format);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(context.GetCancellationToken());

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // Handle SIGTERM on Unix platforms (Docker, systemd, etc.)
            IDisposable? sigTermRegistration = null;
            if (!OperatingSystem.IsWindows())
            {
                sigTermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx =>
                {
                    ctx.Cancel = true;
                    cts.Cancel();
                });
            }

            try
            {
                var cmd = new GDWatchCommand(projectPath, formatter, logger: logger);
                Environment.ExitCode = await cmd.ExecuteAsync(cts.Token);
            }
            finally
            {
                sigTermRegistration?.Dispose();
            }
        });

        return command;
    }
}
