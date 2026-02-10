using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Creates a .gdshrapt.json configuration file with optional presets.
/// </summary>
public class GDInitCommand : IGDCommand
{
    private readonly string _path;
    private readonly string? _preset;
    private readonly bool _force;

    public string Name => "init";
    public string Description => "Create a .gdshrapt.json configuration file";

    public GDInitCommand(string path, string? preset, bool force)
    {
        _path = path;
        _preset = preset;
        _force = force;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetFullPath(_path);

            if (!Directory.Exists(directory))
            {
                Console.Error.WriteLine($"Error: Directory not found: {directory}");
                return Task.FromResult(2);
            }

            var configPath = Path.Combine(directory, GDConfigLoader.ConfigFileName);

            if (File.Exists(configPath) && !_force)
            {
                Console.Error.WriteLine($"Error: Configuration file already exists: {configPath}");
                Console.Error.WriteLine("Use --force to overwrite.");
                return Task.FromResult(2);
            }

            if (_preset != null && !IsValidPreset(_preset))
            {
                Console.Error.WriteLine($"Error: Unknown preset '{_preset}'. Valid presets: minimal, recommended, strict, relaxed");
                return Task.FromResult(2);
            }

            var config = CreateConfig(_preset);
            GDConfigLoader.SaveConfig(directory, config);

            var presetLabel = _preset ?? "minimal";
            Console.WriteLine($"Created {GDConfigLoader.ConfigFileName} in {directory} (preset: {presetLabel})");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return Task.FromResult(2);
        }
    }

    private static bool IsValidPreset(string preset)
    {
        return preset == "minimal" || preset == "recommended" || preset == "strict" || preset == "relaxed";
    }

    private static GDProjectConfig CreateConfig(string? preset)
    {
        return preset switch
        {
            "recommended" => CreateRecommendedConfig(),
            "strict" => CreateStrictConfig(),
            "relaxed" => CreateRelaxedConfig(),
            _ => new GDProjectConfig()
        };
    }

    private static GDProjectConfig CreateRecommendedConfig()
    {
        var config = new GDProjectConfig();

        config.Linting.MaxLineLength = 120;

        config.AdvancedLinting.WarnUnusedVariables = true;
        config.AdvancedLinting.WarnUnusedParameters = true;
        config.AdvancedLinting.WarnEmptyFunctions = true;
        config.AdvancedLinting.WarnVariableShadowing = true;
        config.AdvancedLinting.WarnAwaitInLoop = true;
        config.AdvancedLinting.WarnDuplicatedLoad = true;
        config.AdvancedLinting.MaxCyclomaticComplexity = 15;

        return config;
    }

    private static GDProjectConfig CreateStrictConfig()
    {
        var config = new GDProjectConfig();

        config.Cli.FailOnWarning = true;

        config.Linting.MaxLineLength = 100;

        config.AdvancedLinting.WarnUnusedVariables = true;
        config.AdvancedLinting.WarnUnusedParameters = true;
        config.AdvancedLinting.WarnUnusedSignals = true;
        config.AdvancedLinting.WarnEmptyFunctions = true;
        config.AdvancedLinting.WarnMagicNumbers = true;
        config.AdvancedLinting.WarnVariableShadowing = true;
        config.AdvancedLinting.WarnAwaitInLoop = true;
        config.AdvancedLinting.WarnNoElifReturn = true;
        config.AdvancedLinting.WarnNoElseReturn = true;
        config.AdvancedLinting.WarnPrivateMethodCall = true;
        config.AdvancedLinting.WarnDuplicatedLoad = true;
        config.AdvancedLinting.WarnExpressionNotAssigned = true;
        config.AdvancedLinting.WarnUselessAssignment = true;
        config.AdvancedLinting.WarnInconsistentReturn = true;
        config.AdvancedLinting.WarnNoLonelyIf = true;
        config.AdvancedLinting.MaxCyclomaticComplexity = 10;
        config.AdvancedLinting.MaxFunctionLength = 40;
        config.AdvancedLinting.MaxParameters = 4;
        config.AdvancedLinting.MaxNestingDepth = 3;
        config.AdvancedLinting.StrictTypingParameters = GDStrictTypingSeverity.Warning;
        config.AdvancedLinting.StrictTypingReturnTypes = GDStrictTypingSeverity.Warning;

        return config;
    }

    private static GDProjectConfig CreateRelaxedConfig()
    {
        var config = new GDProjectConfig();

        config.Linting.MaxLineLength = 200;

        config.AdvancedLinting.WarnUnusedVariables = false;
        config.AdvancedLinting.WarnUnusedParameters = false;
        config.AdvancedLinting.WarnEmptyFunctions = false;
        config.AdvancedLinting.WarnVariableShadowing = false;
        config.AdvancedLinting.WarnAwaitInLoop = false;
        config.AdvancedLinting.MaxCyclomaticComplexity = 25;
        config.AdvancedLinting.MaxFunctionLength = 100;
        config.AdvancedLinting.MaxParameters = 10;
        config.AdvancedLinting.MaxFileLines = 2000;
        config.AdvancedLinting.MaxNestingDepth = 8;
        config.AdvancedLinting.MaxLocalVariables = 30;

        return config;
    }
}
