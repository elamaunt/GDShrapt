using System.CommandLine;
using System.Threading.Tasks;
using GDShrapt.CLI.Commands;

namespace GDShrapt.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("GDShrapt - GDScript analysis and refactoring CLI");

        // Global options
        var formatOption = new Option<string>(
            aliases: new[] { "--format", "-f" },
            getDefaultValue: () => "text",
            description: "Output format (text, json)");

        rootCommand.AddGlobalOption(formatOption);

        // Add all commands using builders
        rootCommand.AddCommand(AnalyzeCommandBuilder.Build(formatOption));
        rootCommand.AddCommand(CheckCommandBuilder.Build(formatOption));
        rootCommand.AddCommand(LintCommandBuilder.Build(formatOption));
        rootCommand.AddCommand(ValidateCommandBuilder.Build(formatOption));
        rootCommand.AddCommand(FormatCommandBuilder.Build(formatOption));
        rootCommand.AddCommand(SymbolsCommandBuilder.Build(formatOption));
        rootCommand.AddCommand(FindRefsCommandBuilder.Build(formatOption));
        rootCommand.AddCommand(RenameCommandBuilder.Build(formatOption));
        rootCommand.AddCommand(ParseCommandBuilder.Build(formatOption));
        rootCommand.AddCommand(ExtractStyleCommandBuilder.Build(formatOption));

        return await rootCommand.InvokeAsync(args);
    }
}
