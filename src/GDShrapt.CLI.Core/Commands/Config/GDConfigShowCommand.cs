using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Displays the current project configuration.
/// Supports raw file content or effective (resolved) config, in text or JSON format.
/// </summary>
public class GDConfigShowCommand : IGDCommand
{
    private readonly string _path;
    private readonly bool _effective;
    private readonly string _format;
    private readonly TextWriter _output;

    public string Name => "config show";
    public string Description => "Display current configuration";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public GDConfigShowCommand(string path, bool effective, string format, TextWriter? output = null)
    {
        _path = path;
        _effective = effective;
        _format = format;
        _output = output ?? Console.Out;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetFullPath(_path);

            if (!Directory.Exists(directory))
            {
                Console.Error.WriteLine($"Error: Directory not found: {directory}");
                return Task.FromResult(GDExitCode.Fatal);
            }

            var configPath = Path.Combine(directory, GDConfigLoader.ConfigFileName);

            if (_effective)
            {
                // Effective mode: load through config loader (defaults + file)
                var config = GDConfigLoader.LoadConfig(directory);
                WriteConfig(config, configPath, directory);
            }
            else
            {
                // Raw mode: file must exist
                if (!File.Exists(configPath))
                {
                    Console.Error.WriteLine($"Error: Configuration file not found: {configPath}");
                    Console.Error.WriteLine("Use --effective to see defaults, or 'gdshrapt config init' to create one.");
                    return Task.FromResult(GDExitCode.Fatal);
                }

                var config = GDConfigLoader.LoadConfig(directory);
                WriteConfig(config, configPath, directory);
            }

            return Task.FromResult(GDExitCode.Success);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return Task.FromResult(GDExitCode.Fatal);
        }
    }

    private void WriteConfig(GDProjectConfig config, string configPath, string directory)
    {
        if (string.Equals(_format, "json", StringComparison.OrdinalIgnoreCase))
        {
            WriteJsonOutput(config);
        }
        else
        {
            WriteTextOutput(config, configPath, directory);
        }
    }

    private void WriteJsonOutput(GDProjectConfig config)
    {
        var json = JsonSerializer.Serialize(config, SerializerOptions);
        _output.WriteLine(json);
    }

    private void WriteTextOutput(GDProjectConfig config, string configPath, string directory)
    {
        var mode = _effective ? "effective" : "raw";
        _output.WriteLine($"Configuration ({mode}):");

        if (File.Exists(configPath))
            _output.WriteLine($"Location: {configPath}");
        else
            _output.WriteLine("Location: (using defaults, no file found)");

        _output.WriteLine();

        // Linting section
        _output.WriteLine("Linting:");
        _output.WriteLine($"  Enabled:          {config.Linting.Enabled}");
        _output.WriteLine($"  MaxLineLength:    {config.Linting.MaxLineLength}");
        _output.WriteLine($"  FormattingLevel:  {config.Linting.FormattingLevel}");
        _output.WriteLine($"  IndentationStyle: {config.Linting.IndentationStyle}");
        _output.WriteLine($"  TabWidth:         {config.Linting.TabWidth}");
        _output.WriteLine();

        // Advanced Linting section
        _output.WriteLine("Advanced Linting:");
        _output.WriteLine($"  WarnUnusedVariables:    {config.AdvancedLinting.WarnUnusedVariables}");
        _output.WriteLine($"  WarnUnusedParameters:   {config.AdvancedLinting.WarnUnusedParameters}");
        _output.WriteLine($"  WarnUnusedSignals:      {config.AdvancedLinting.WarnUnusedSignals}");
        _output.WriteLine($"  WarnEmptyFunctions:     {config.AdvancedLinting.WarnEmptyFunctions}");
        _output.WriteLine($"  WarnMagicNumbers:       {config.AdvancedLinting.WarnMagicNumbers}");
        _output.WriteLine($"  WarnVariableShadowing:  {config.AdvancedLinting.WarnVariableShadowing}");
        _output.WriteLine($"  MaxCyclomaticComplexity: {config.AdvancedLinting.MaxCyclomaticComplexity}");
        _output.WriteLine($"  MaxFunctionLength:      {config.AdvancedLinting.MaxFunctionLength}");
        _output.WriteLine($"  MaxParameters:          {config.AdvancedLinting.MaxParameters}");
        _output.WriteLine($"  MaxNestingDepth:        {config.AdvancedLinting.MaxNestingDepth}");
        _output.WriteLine();

        // Formatter section
        _output.WriteLine("Formatter:");
        _output.WriteLine($"  IndentStyle:      {config.Formatter.IndentStyle}");
        _output.WriteLine($"  IndentSize:       {config.Formatter.IndentSize}");
        _output.WriteLine($"  MaxLineLength:    {config.Formatter.MaxLineLength}");
        _output.WriteLine();

        // CLI section
        _output.WriteLine("CLI:");
        _output.WriteLine($"  FailOnWarning:    {config.Cli.FailOnWarning}");
        _output.WriteLine($"  FailOnHint:       {config.Cli.FailOnHint}");
        _output.WriteLine($"  Exclude:          {string.Join(", ", config.Cli.Exclude)}");
        _output.WriteLine();

        // Validation section
        _output.WriteLine("Validation:");
        _output.WriteLine($"  NullableStrictness: {config.Validation.NullableStrictness}");
    }
}
