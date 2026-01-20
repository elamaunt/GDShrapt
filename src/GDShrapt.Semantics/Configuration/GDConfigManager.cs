using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Manages loading, saving, and watching project configuration.
/// Used by CLI, LSP, and Plugin for unified config management.
/// Configuration is stored in .gdshrapt.json in project root for team sharing.
/// </summary>
public class GDConfigManager : IDisposable
{
    /// <summary>
    /// Default configuration file name.
    /// </summary>
    public const string ConfigFileName = ".gdshrapt.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _projectPath;
    private readonly string _configFilePath;
    private readonly bool _watchForChanges;
    private readonly IGDSemanticLogger? _logger;
    private FileSystemWatcher? _watcher;
    private GDProjectConfig _config;
    private DateTime _lastLoadTime;
    private bool _disposed;

    /// <summary>
    /// Gets the current project configuration.
    /// </summary>
    public GDProjectConfig Config => _config;

    /// <summary>
    /// Gets the full path to the config file.
    /// </summary>
    public string ConfigFilePath => _configFilePath;

    /// <summary>
    /// Event fired when configuration changes (either by reload or external modification).
    /// </summary>
    public event Action<GDProjectConfig>? OnConfigChanged;

    /// <summary>
    /// Creates a new ConfigManager for the specified project path.
    /// </summary>
    /// <param name="projectPath">Path to the project root directory.</param>
    /// <param name="watchForChanges">If true, watches for external config file changes.</param>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    public GDConfigManager(string projectPath, bool watchForChanges = false, IGDSemanticLogger? logger = null)
    {
        _projectPath = projectPath;
        _configFilePath = Path.Combine(projectPath, ConfigFileName);
        _watchForChanges = watchForChanges;
        _logger = logger;
        _config = new GDProjectConfig();

        LoadConfig();

        if (_watchForChanges)
        {
            SetupWatcher();
        }
    }

    /// <summary>
    /// Loads configuration from file, or creates default if not exists.
    /// </summary>
    public void LoadConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var loaded = JsonSerializer.Deserialize<GDProjectConfig>(json, JsonOptions);

                if (loaded != null)
                {
                    _config = loaded;
                    _lastLoadTime = DateTime.UtcNow;
                    _logger?.Info($"Loaded project config from: {_configFilePath}");
                    OnConfigChanged?.Invoke(_config);
                    return;
                }
            }

            // No config file exists, use defaults
            _config = new GDProjectConfig();
            _logger?.Info("Using default project configuration (no .gdshrapt.json found)");
        }
        catch (JsonException ex)
        {
            _logger?.Error($"Error parsing config file: {ex.Message}");
            _logger?.Warning("Using default configuration due to parse error");
            _config = new GDProjectConfig();
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error loading config: {ex.Message}");
            _config = new GDProjectConfig();
        }
    }

    /// <summary>
    /// Saves the current configuration to file.
    /// </summary>
    public bool SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(_config, JsonOptions);
            File.WriteAllText(_configFilePath, json);
            _lastLoadTime = DateTime.UtcNow;
            _logger?.Info($"Saved project config to: {_configFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error saving config: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a default configuration file if one doesn't exist.
    /// </summary>
    public bool CreateDefaultConfig()
    {
        if (File.Exists(_configFilePath))
        {
            _logger?.Warning("Config file already exists, not overwriting");
            return false;
        }

        _config = new GDProjectConfig();
        return SaveConfig();
    }

    /// <summary>
    /// Resets configuration to defaults and saves.
    /// </summary>
    public void ResetToDefaults()
    {
        _config = new GDProjectConfig();
        SaveConfig();
        OnConfigChanged?.Invoke(_config);
    }

    /// <summary>
    /// Gets a rule configuration, returning defaults if not specified.
    /// </summary>
    public GDRuleConfig GetRuleConfig(string ruleId, GDDiagnosticSeverity defaultSeverity)
    {
        if (_config.Linting.Rules.TryGetValue(ruleId, out var ruleConfig))
        {
            return ruleConfig;
        }

        // Return default config
        return new GDRuleConfig
        {
            Enabled = true,
            Severity = defaultSeverity
        };
    }

    /// <summary>
    /// Checks if a rule is enabled based on configuration.
    /// </summary>
    public bool IsRuleEnabled(string ruleId)
    {
        // Check global linting enabled
        if (!_config.Linting.Enabled)
            return false;

        // Check per-rule override
        if (_config.Linting.Rules.TryGetValue(ruleId, out var ruleConfig))
        {
            return ruleConfig.Enabled;
        }

        return true; // Enabled by default
    }

    /// <summary>
    /// Gets the effective severity for a rule.
    /// </summary>
    public GDDiagnosticSeverity GetRuleSeverity(string ruleId, GDDiagnosticSeverity defaultSeverity)
    {
        if (_config.Linting.Rules.TryGetValue(ruleId, out var ruleConfig) && ruleConfig.Severity.HasValue)
        {
            return ruleConfig.Severity.Value;
        }

        return defaultSeverity;
    }

    private void SetupWatcher()
    {
        try
        {
            if (!Directory.Exists(_projectPath))
                return;

            _watcher = new FileSystemWatcher(_projectPath, ConfigFileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnConfigFileChanged;
            _watcher.Created += OnConfigFileChanged;
            _watcher.Deleted += OnConfigFileDeleted;

            _logger?.Debug("Config file watcher initialized");
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Could not setup config file watcher: {ex.Message}");
        }
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce - ignore if we just saved
        if ((DateTime.UtcNow - _lastLoadTime).TotalSeconds < 1)
            return;

        _logger?.Info("Config file changed externally, reloading...");

        // Delay slightly to allow file write to complete
        System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
        {
            LoadConfig();
        });
    }

    private void OnConfigFileDeleted(object sender, FileSystemEventArgs e)
    {
        _logger?.Info("Config file deleted, reverting to defaults");
        _config = new GDProjectConfig();
        OnConfigChanged?.Invoke(_config);
    }

    /// <summary>
    /// Gets a serialized JSON representation of the current config.
    /// </summary>
    public string GetConfigJson()
    {
        return JsonSerializer.Serialize(_config, JsonOptions);
    }

    /// <summary>
    /// Updates config from JSON string.
    /// </summary>
    public bool UpdateFromJson(string json)
    {
        try
        {
            var newConfig = JsonSerializer.Deserialize<GDProjectConfig>(json, JsonOptions);
            if (newConfig != null)
            {
                _config = newConfig;
                OnConfigChanged?.Invoke(_config);
                return true;
            }
        }
        catch (JsonException ex)
        {
            _logger?.Error($"Invalid config JSON: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Disposes the ConfigManager and stops watching for file changes.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _watcher?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    #region Static Methods (for simple CLI usage without watching)

    /// <summary>
    /// Loads configuration from the project directory (no file watching).
    /// Returns default config if file doesn't exist.
    /// Falls back to .gdlintrc if .gdshrapt.json doesn't exist (gdtoolkit compatibility).
    /// </summary>
    /// <param name="projectPath">Path to the project directory.</param>
    /// <returns>Loaded or default configuration.</returns>
    public static GDProjectConfig LoadConfigStatic(string projectPath)
    {
        var configPath = Path.Combine(projectPath, ConfigFileName);

        // First, try .gdshrapt.json
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<GDProjectConfig>(json, JsonOptions) ?? new GDProjectConfig();
            }
            catch (JsonException)
            {
                // Return default config if JSON is invalid
                return new GDProjectConfig();
            }
        }

        // Fall back to .gdlintrc (gdtoolkit compatibility)
        var gdlintrcPath = Path.Combine(projectPath, GDGdlintConfigParser.ConfigFileName);
        if (File.Exists(gdlintrcPath))
        {
            var config = GDGdlintConfigParser.Parse(gdlintrcPath);
            if (config != null)
                return config;
        }

        // Fall back to .gdlint.cfg (alternative gdtoolkit filename)
        var gdlintCfgPath = Path.Combine(projectPath, GDGdlintConfigParser.AltConfigFileName);
        if (File.Exists(gdlintCfgPath))
        {
            var config = GDGdlintConfigParser.Parse(gdlintCfgPath);
            if (config != null)
                return config;
        }

        return new GDProjectConfig();
    }

    /// <summary>
    /// Saves configuration to the project directory.
    /// </summary>
    /// <param name="projectPath">Path to the project directory.</param>
    /// <param name="config">Configuration to save.</param>
    public static void SaveConfigStatic(string projectPath, GDProjectConfig config)
    {
        var configPath = Path.Combine(projectPath, ConfigFileName);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(configPath, json);
    }

    /// <summary>
    /// Checks if a file path matches any of the exclude patterns.
    /// </summary>
    /// <param name="relativePath">Relative path from project root.</param>
    /// <param name="excludePatterns">List of glob patterns to exclude.</param>
    /// <returns>True if the file should be excluded.</returns>
    public static bool ShouldExclude(string relativePath, IEnumerable<string> excludePatterns)
    {
        // Normalize path separators
        var normalizedPath = relativePath.Replace('\\', '/');

        foreach (var pattern in excludePatterns)
        {
            if (MatchesGlobPattern(normalizedPath, pattern.Replace('\\', '/')))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesGlobPattern(string path, string pattern)
    {
        // Simple glob matching for common patterns
        // Supports: **, *, ?

        if (pattern == "**")
            return true;

        if (pattern.StartsWith("**/"))
        {
            // Match any path containing this suffix
            var suffix = pattern.Substring(3);
            return path.Contains("/" + suffix) || path.StartsWith(suffix) || path.EndsWith("/" + suffix.TrimEnd('/'));
        }

        if (pattern.EndsWith("/**"))
        {
            // Match any path starting with this prefix
            var prefix = pattern.Substring(0, pattern.Length - 3);
            return path.StartsWith(prefix + "/") || path == prefix;
        }

        if (pattern.Contains("**"))
        {
            // Split by ** and match prefix/suffix
            var parts = pattern.Split(new[] { "**" }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                return path.StartsWith(parts[0]) && path.EndsWith(parts[1]);
            }
        }

        // Simple wildcard matching
        if (pattern.Contains("*") || pattern.Contains("?"))
        {
            return WildcardMatch(path, pattern);
        }

        // Exact match
        return path == pattern || path.StartsWith(pattern + "/");
    }

    private static bool WildcardMatch(string input, string pattern)
    {
        int inputIndex = 0, patternIndex = 0;
        int inputMark = -1, patternMark = -1;

        while (inputIndex < input.Length)
        {
            if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' || pattern[patternIndex] == input[inputIndex]))
            {
                inputIndex++;
                patternIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                patternMark = patternIndex++;
                inputMark = inputIndex;
            }
            else if (patternMark != -1)
            {
                patternIndex = patternMark + 1;
                inputIndex = ++inputMark;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }

    #endregion
}
