using System.CommandLine;
using System.Threading.Tasks;

namespace GDShrapt.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("GDShrapt - GDScript analysis and refactoring CLI");

        // Global options - Output format
        var formatOption = new Option<string>(
            aliases: new[] { "--format", "-f" },
            getDefaultValue: () => "text",
            description: "Output format (text, json)");

        // Global options - Verbosity (mutually exclusive by priority: quiet > debug > verbose)
        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Show detailed output (includes debug-level messages)");

        var debugOption = new Option<bool>(
            aliases: new[] { "--debug" },
            description: "Show all diagnostic output with timestamps");

        var quietOption = new Option<bool>(
            aliases: new[] { "--quiet", "-q" },
            description: "Only show errors");

        // Global options - Performance and execution
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

        rootCommand.AddGlobalOption(formatOption);
        rootCommand.AddGlobalOption(verboseOption);
        rootCommand.AddGlobalOption(debugOption);
        rootCommand.AddGlobalOption(quietOption);
        rootCommand.AddGlobalOption(maxParallelismOption);
        rootCommand.AddGlobalOption(timeoutSecondsOption);
        rootCommand.AddGlobalOption(logLevelOption);
        rootCommand.AddGlobalOption(excludeOption);

        // Add all commands using builders
        rootCommand.AddCommand(AnalyzeCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(CheckCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(LintCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(ValidateCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(FormatCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(SymbolsCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(FindRefsCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(RenameCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(ParseCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(ExtractStyleCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));

        // New analysis commands
        rootCommand.AddCommand(MetricsCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(DeadCodeCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(DepsCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(TypeCoverageCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));
        rootCommand.AddCommand(StatsCommandBuilder.Build(formatOption, verboseOption, debugOption, quietOption));

        return await rootCommand.InvokeAsync(args);
    }
}
