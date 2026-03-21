using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GDShrapt.Semantics;

/// <summary>
/// Detection granularity for duplicate detection.
/// </summary>
public enum GDDuplicateGranularity
{
    /// <summary>
    /// Compare entire methods (original behavior).
    /// </summary>
    Method,

    /// <summary>
    /// Find duplicated code blocks within or across methods.
    /// </summary>
    Block
}

/// <summary>
/// Suggested refactoring type for duplicate code.
/// </summary>
public enum GDRefactoringType
{
    None,
    ExtractMethod,
    ExtractToBase,
    CreateUtilityFunc
}

/// <summary>
/// Options for duplicate detection.
/// </summary>
public class GDDuplicateOptions
{
    public int MinTokens { get; set; } = 50;
    public int MinLines { get; set; } = 5;
    public int MinInstances { get; set; } = 2;
    public bool NormalizeIdentifiers { get; set; } = true;
    public bool NormalizeLiterals { get; set; } = true;
    public double SimilarityThreshold { get; set; } = 1.0;
    public GDDuplicateGranularity Granularity { get; set; } = GDDuplicateGranularity.Block;
    public List<string> IgnorePatterns { get; set; } = new();
    public bool IncludeCodePreview { get; set; } = false;
}

/// <summary>
/// Duplicate detection report.
/// </summary>
public class GDDuplicateReport
{
    public List<GDDuplicateGroup> Duplicates { get; set; } = new();
    public int TotalBlocksAnalyzed { get; set; }
    public int TotalDuplicateGroups { get; set; }
    public int TotalDuplicateLines { get; set; }
    public double DuplicationPercentage { get; set; }
    public int TotalProjectLines { get; set; }
    public int NewDuplicateGroups { get; set; }
    public int FixedDuplicateGroups { get; set; }
    public bool RequiresLicense { get; set; }

    public static GDDuplicateReport LicenseRequired() => new()
    {
        RequiresLicense = true
    };
}

/// <summary>
/// Group of duplicate code instances.
/// </summary>
public class GDDuplicateGroup
{
    public string Hash { get; set; } = "";
    public List<GDDuplicateInstance> Instances { get; set; } = new();
    public int InstanceCount { get; set; }
    public int TokenCount { get; set; }
    public int TotalDuplicateLines { get; set; }
    public double SimilarityScore { get; set; } = 1.0;
    public string CommonCode { get; set; } = "";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GDRefactoringType SuggestedRefactoring { get; set; } = GDRefactoringType.None;

    public string RefactoringHint { get; set; } = "";
}

/// <summary>
/// Single instance of duplicated code.
/// </summary>
public class GDDuplicateInstance
{
    public string FilePath { get; set; } = "";
    public string MethodName { get; set; } = "";
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public int LineCount { get; set; }
    public int TokenCount { get; set; }
    public string CodeSnippet { get; set; } = "";
}
