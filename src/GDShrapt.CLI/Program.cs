using System;
using System.CommandLine;
using System.Reflection;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Handle --version before System.CommandLine parsing
        if (args.Length == 1 && (args[0] == "--version" || args[0] == "-V"))
        {
            var version = typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "6.0.0";
            Console.WriteLine($"gdshrapt {version}");
            return 0;
        }

        var rootCommand = new RootCommand("GDShrapt - GDScript analysis and refactoring CLI");

        var formatOption = new Option<string>(
            aliases: new[] { "--format", "-f" },
            getDefaultValue: () => "text",
            description: "Output format (text, json)");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Shorthand for --log-level verbose");

        var debugOption = new Option<bool>(
            aliases: new[] { "--debug" },
            description: "Shorthand for --log-level debug (includes timestamps)");

        var quietOption = new Option<bool>(
            aliases: new[] { "--quiet", "-q" },
            description: "Shorthand for --log-level error");

        var maxParallelismOption = new Option<int?>(
            aliases: new[] { "--max-parallelism" },
            description: "Maximum parallelism (-1 = auto, 0 = sequential)");

        var timeoutSecondsOption = new Option<int?>(
            aliases: new[] { "--timeout-seconds" },
            description: "Per-file timeout in seconds (default: 30)");

        var logLevelOption = new Option<string?>(
            aliases: new[] { "--log-level" },
            description: "Log level: verbose, debug, info, warning, error, silent");

        var excludeOption = new Option<string[]>(
            aliases: new[] { "--exclude" },
            description: "Exclude patterns (glob, can be used multiple times)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var colorOption = new Option<string?>(
            aliases: new[] { "--color" },
            description: "Color output: auto (default), always, never");

        rootCommand.AddGlobalOption(formatOption);
        rootCommand.AddGlobalOption(verboseOption);
        rootCommand.AddGlobalOption(debugOption);
        rootCommand.AddGlobalOption(quietOption);
        rootCommand.AddGlobalOption(maxParallelismOption);
        rootCommand.AddGlobalOption(timeoutSecondsOption);
        rootCommand.AddGlobalOption(logLevelOption);
        rootCommand.AddGlobalOption(excludeOption);
        rootCommand.AddGlobalOption(colorOption);

        // Validate mutually exclusive verbosity options (pre-parse, since beta4
        // AddValidator doesn't run for subcommands with global options)
        var validationError = ValidateVerbosityFlags(args);
        if (validationError != null)
        {
            Console.Error.WriteLine($"Error: {validationError}");
            return 1;
        }

        rootCommand.AddCommand(AnalyzeCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(CheckCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(LintCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(ValidateCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(FormatCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(SymbolsCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(FindRefsCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(RenameCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(ParseCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(ExtractStyleCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));

        rootCommand.AddCommand(MetricsCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(DeadCodeCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(DepsCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(TypeCoverageCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));
        rootCommand.AddCommand(StatsCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));

        rootCommand.AddCommand(InitCommandBuilder.Build());
        rootCommand.AddCommand(WatchCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption, logLevelOption));

        // Pre-parse: --color must apply before command execution
        var colorArg = GetOptionValue(args, "--color");
        GDAnsiColors.Configure(colorArg);

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Extracts a simple option value from args (for pre-parse configuration).
    /// </summary>
    private static string? GetOptionValue(string[] args, string optionName)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == optionName)
                return args[i + 1];
        }
        return null;
    }

    private static bool HasFlag(string[] args, params string[] flags)
    {
        foreach (var arg in args)
        {
            foreach (var flag in flags)
            {
                if (arg == flag)
                    return true;
            }
        }
        return false;
    }

    private static string? ValidateVerbosityFlags(string[] args)
    {
        var hasLogLevel = HasFlag(args, "--log-level");
        var hasQuiet = HasFlag(args, "--quiet", "-q");
        var hasVerbose = HasFlag(args, "--verbose", "-v");
        var hasDebug = HasFlag(args, "--debug");

        if (hasLogLevel && (hasQuiet || hasVerbose || hasDebug))
            return "--log-level cannot be combined with --quiet, --verbose, or --debug.";

        var flagCount = 0;
        if (hasQuiet) flagCount++;
        if (hasVerbose) flagCount++;
        if (hasDebug) flagCount++;
        if (flagCount > 1)
            return "Options --verbose, --debug, and --quiet are mutually exclusive.";

        return null;
    }
}
