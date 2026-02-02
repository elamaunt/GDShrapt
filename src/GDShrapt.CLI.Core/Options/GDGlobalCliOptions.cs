using System.Collections.Generic;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Global CLI options shared across all commands.
/// </summary>
public class GDGlobalCliOptions
{
    /// <summary>
    /// Maximum parallelism. -1 = auto (use all CPUs), 0 = sequential, N = use N threads.
    /// </summary>
    public int? MaxParallelism { get; set; }

    /// <summary>
    /// Per-file timeout in seconds.
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Log level override: verbose, debug, info, warning, error, silent.
    /// </summary>
    public string? LogLevel { get; set; }

    /// <summary>
    /// Exclude patterns (glob patterns).
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();

    /// <summary>
    /// Gets the effective parallelism value.
    /// </summary>
    public int GetEffectiveParallelism()
    {
        if (!MaxParallelism.HasValue || MaxParallelism.Value < 0)
            return -1; // Auto
        return MaxParallelism.Value;
    }

    /// <summary>
    /// Gets the effective timeout in seconds.
    /// </summary>
    public int GetEffectiveTimeoutSeconds()
    {
        return TimeoutSeconds ?? 30;
    }
}
