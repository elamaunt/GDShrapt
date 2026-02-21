using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Validates a .gdshrapt.json configuration file for errors, range violations, and conflicts.
/// </summary>
public class GDConfigValidateCommand : IGDCommand
{
    private readonly string _path;
    private readonly bool _explain;
    private readonly TextWriter _output;

    public string Name => "config validate";
    public string Description => "Check configuration for errors";

    public GDConfigValidateCommand(string path, bool explain, TextWriter? output = null)
    {
        _path = path;
        _explain = explain;
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

            if (!File.Exists(configPath))
            {
                Console.Error.WriteLine($"Error: Configuration file not found: {configPath}");
                Console.Error.WriteLine("Use 'gdshrapt config init' to create one.");
                return Task.FromResult(GDExitCode.Fatal);
            }

            var result = GDConfigValidator.Validate(configPath, _explain);

            if (result.IsValid && !result.HasWarnings)
            {
                _output.WriteLine("Configuration is valid.");
                return Task.FromResult(GDExitCode.Success);
            }

            // Show errors
            var errors = result.Errors.Where(e => e.Severity == "error").ToList();
            var warnings = result.Errors.Where(e => e.Severity == "warning").ToList();

            if (errors.Count > 0)
            {
                _output.WriteLine($"Configuration errors ({errors.Count}):");
                foreach (var error in errors)
                {
                    _output.WriteLine($"  error: {error.Message}");
                    if (error.Explanation != null)
                        _output.WriteLine($"         {error.Explanation}");
                }
                _output.WriteLine();
            }

            if (warnings.Count > 0)
            {
                _output.WriteLine($"Configuration warnings ({warnings.Count}):");
                foreach (var warning in warnings)
                {
                    _output.WriteLine($"  warning: {warning.Message}");
                    if (warning.Explanation != null)
                        _output.WriteLine($"           {warning.Explanation}");
                }
            }

            return Task.FromResult(result.IsValid ? GDExitCode.Success : GDExitCode.Errors);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return Task.FromResult(GDExitCode.Fatal);
        }
    }
}
