namespace GDShrapt.Semantics.StressTests.Infrastructure;

/// <summary>
/// Defines performance thresholds for stress tests.
/// Tests fail if operations exceed these limits.
/// Thresholds should be calibrated against CI hardware.
/// </summary>
public static class PerformanceThresholds
{
    // Project analysis time limits
    public static readonly TimeSpan Project100Files = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan Project500Files = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan Project1000Files = TimeSpan.FromSeconds(90);

    // Type inference limits
    public static readonly TimeSpan DeepInheritance15Levels = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan LongMethod1000Lines = TimeSpan.FromSeconds(1);

    // Reference collection limits
    public static readonly TimeSpan FindReferences100 = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan FindReferences1000 = TimeSpan.FromSeconds(1);

    // Memory limits (bytes)
    public static readonly long MaxMemoryProject100Files = 100 * 1024 * 1024;   // 100 MB
    public static readonly long MaxMemoryProject500Files = 400 * 1024 * 1024;   // 400 MB
    public static readonly long MaxMemoryProject1000Files = 800 * 1024 * 1024;  // 800 MB

    // Scaling factor: allows CI to adjust based on hardware
    private static double _scaleFactor = 1.0;

    /// <summary>
    /// Sets a scale factor for all time thresholds.
    /// Use values > 1.0 for slower CI hardware.
    /// </summary>
    public static void SetScaleFactor(double factor)
    {
        if (factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "Scale factor must be positive");
        _scaleFactor = factor;
    }

    /// <summary>
    /// Gets the current scale factor.
    /// </summary>
    public static double ScaleFactor => _scaleFactor;

    /// <summary>
    /// Scales a time threshold by the current scale factor.
    /// </summary>
    public static TimeSpan Scale(TimeSpan threshold)
    {
        return TimeSpan.FromTicks((long)(threshold.Ticks * _scaleFactor));
    }

    /// <summary>
    /// Gets the threshold for a given file count (interpolates between known values).
    /// </summary>
    public static TimeSpan GetProjectAnalysisThreshold(int fileCount)
    {
        if (fileCount <= 100)
            return Scale(Project100Files);
        if (fileCount <= 500)
        {
            var ratio = (fileCount - 100) / 400.0;
            var ticks = Project100Files.Ticks + (long)((Project500Files.Ticks - Project100Files.Ticks) * ratio);
            return Scale(TimeSpan.FromTicks(ticks));
        }
        if (fileCount <= 1000)
        {
            var ratio = (fileCount - 500) / 500.0;
            var ticks = Project500Files.Ticks + (long)((Project1000Files.Ticks - Project500Files.Ticks) * ratio);
            return Scale(TimeSpan.FromTicks(ticks));
        }

        // Beyond 1000 files, extrapolate linearly
        var extraRatio = fileCount / 1000.0;
        return Scale(TimeSpan.FromTicks((long)(Project1000Files.Ticks * extraRatio)));
    }

    /// <summary>
    /// Gets the memory threshold for a given file count.
    /// </summary>
    public static long GetMemoryThreshold(int fileCount)
    {
        if (fileCount <= 100)
            return MaxMemoryProject100Files;
        if (fileCount <= 500)
        {
            var ratio = (fileCount - 100) / 400.0;
            return MaxMemoryProject100Files + (long)((MaxMemoryProject500Files - MaxMemoryProject100Files) * ratio);
        }
        if (fileCount <= 1000)
        {
            var ratio = (fileCount - 500) / 500.0;
            return MaxMemoryProject500Files + (long)((MaxMemoryProject1000Files - MaxMemoryProject500Files) * ratio);
        }

        // Beyond 1000 files, extrapolate linearly
        var extraRatio = fileCount / 1000.0;
        return (long)(MaxMemoryProject1000Files * extraRatio);
    }
}
