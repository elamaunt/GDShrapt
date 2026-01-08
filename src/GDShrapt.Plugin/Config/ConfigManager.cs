using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GDShrapt.Plugin.Config;

/// <summary>
/// Manages loading, saving, and watching project configuration.
/// Configuration is stored in .gdshrapt.json in project root for team sharing.
/// </summary>
internal class ConfigManager : IDisposable
{
    private const string ConfigFileName = ".gdshrapt.json";

    private readonly string _projectPath;
    private readonly string _configFilePath;
    private FileSystemWatcher? _watcher;
    private ProjectConfig _config;
    private bool _disposedValue;
    private DateTime _lastLoadTime;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Gets the current project configuration.
    /// </summary>
    public ProjectConfig Config => _config;

    /// <summary>
    /// Event fired when configuration changes (either by reload or external modification).
    /// </summary>
    public event Action<ProjectConfig>? OnConfigChanged;

    /// <summary>
    /// Creates a new ConfigManager for the specified project path.
    /// </summary>
    public ConfigManager(string projectPath)
    {
        _projectPath = projectPath;
        _configFilePath = Path.Combine(projectPath, ConfigFileName);
        _config = new ProjectConfig();

        LoadConfig();
        SetupWatcher();
    }

    /// <summary>
    /// Gets the full path to the config file.
    /// </summary>
    public string ConfigFilePath => _configFilePath;

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
                var loaded = JsonSerializer.Deserialize<ProjectConfig>(json, JsonOptions);

                if (loaded != null)
                {
                    _config = loaded;
                    _lastLoadTime = DateTime.UtcNow;
                    Logger.Info($"Loaded project config from: {_configFilePath}");
                    OnConfigChanged?.Invoke(_config);
                    return;
                }
            }

            // No config file exists, use defaults
            _config = new ProjectConfig();
            Logger.Info("Using default project configuration (no .gdshrapt.json found)");
        }
        catch (JsonException ex)
        {
            Logger.Error($"Error parsing config file: {ex.Message}");
            Logger.Warning("Using default configuration due to parse error");
            _config = new ProjectConfig();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading config: {ex.Message}");
            _config = new ProjectConfig();
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
            Logger.Info($"Saved project config to: {_configFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error saving config: {ex.Message}");
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
            Logger.Warning("Config file already exists, not overwriting");
            return false;
        }

        _config = new ProjectConfig();
        return SaveConfig();
    }

    /// <summary>
    /// Resets configuration to defaults and saves.
    /// </summary>
    public void ResetToDefaults()
    {
        _config = new ProjectConfig();
        SaveConfig();
        OnConfigChanged?.Invoke(_config);
    }

    /// <summary>
    /// Gets a rule configuration, returning defaults if not specified.
    /// </summary>
    public RuleConfig GetRuleConfig(string ruleId, DiagnosticSeverity defaultSeverity)
    {
        if (_config.Linting.Rules.TryGetValue(ruleId, out var ruleConfig))
        {
            return ruleConfig;
        }

        // Return default config
        return new RuleConfig
        {
            Enabled = true,
            Severity = defaultSeverity
        };
    }

    /// <summary>
    /// Checks if a rule is enabled based on configuration.
    /// </summary>
    public bool IsRuleEnabled(string ruleId, DiagnosticCategory category)
    {
        // Check global linting enabled
        if (!_config.Linting.Enabled)
            return false;

        // Check formatting level for formatting rules
        if (category == DiagnosticCategory.Formatting)
        {
            if (_config.Linting.FormattingLevel == FormattingLevel.Off)
                return false;
        }

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
    public DiagnosticSeverity GetRuleSeverity(string ruleId, DiagnosticSeverity defaultSeverity)
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

            Logger.Debug("Config file watcher initialized");
        }
        catch (Exception ex)
        {
            Logger.Warning($"Could not setup config file watcher: {ex.Message}");
        }
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce - ignore if we just saved
        if ((DateTime.UtcNow - _lastLoadTime).TotalSeconds < 1)
            return;

        Logger.Info("Config file changed externally, reloading...");

        // Delay slightly to allow file write to complete
        System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
        {
            LoadConfig();
        });
    }

    private void OnConfigFileDeleted(object sender, FileSystemEventArgs e)
    {
        Logger.Info("Config file deleted, reverting to defaults");
        _config = new ProjectConfig();
        OnConfigChanged?.Invoke(_config);
    }

    /// <summary>
    /// Gets a serialized JSON representation of the current config (for display/editing).
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
            var newConfig = JsonSerializer.Deserialize<ProjectConfig>(json, JsonOptions);
            if (newConfig != null)
            {
                _config = newConfig;
                OnConfigChanged?.Invoke(_config);
                return true;
            }
        }
        catch (JsonException ex)
        {
            Logger.Error($"Invalid config JSON: {ex.Message}");
        }

        return false;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _watcher?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Diagnostic category for rule classification.
/// </summary>
internal enum DiagnosticCategory
{
    /// <summary>
    /// Syntax errors from parser.
    /// </summary>
    Syntax,

    /// <summary>
    /// Formatting issues (whitespace, indentation).
    /// </summary>
    Formatting,

    /// <summary>
    /// Style issues (naming conventions).
    /// </summary>
    Style,

    /// <summary>
    /// Best practice recommendations.
    /// </summary>
    BestPractice,

    /// <summary>
    /// Performance-related suggestions.
    /// </summary>
    Performance,

    /// <summary>
    /// Potential correctness issues.
    /// </summary>
    Correctness
}
