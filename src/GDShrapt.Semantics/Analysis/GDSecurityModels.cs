using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Security vulnerability report.
/// </summary>
public class GDSecurityReport
{
    public List<GDSecurityIssue> Issues { get; set; } = new();
    public int TotalIssues { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public bool RequiresLicense { get; init; }

    public bool HasCritical => CriticalCount > 0;
    public bool HasHigh => HighCount > 0;

    public static GDSecurityReport LicenseRequired() => new()
    {
        RequiresLicense = true
    };
}

/// <summary>
/// Single security issue.
/// </summary>
public class GDSecurityIssue
{
    public GDSecurityCategory Category { get; set; }
    public GDSecuritySeverity Severity { get; set; }
    public string FilePath { get; set; } = "";
    public int Line { get; set; }
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Recommendation { get; set; }
}

/// <summary>
/// Security issue categories.
/// </summary>
public enum GDSecurityCategory
{
    HardcodedSecret,
    UnsafePattern,
    PathTraversal,
    InsecureNetwork,
    Other
}

/// <summary>
/// Security severity levels.
/// </summary>
public enum GDSecuritySeverity
{
    Low,
    Medium,
    High,
    Critical
}
