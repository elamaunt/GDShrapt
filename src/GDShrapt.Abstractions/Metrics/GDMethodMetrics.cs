namespace GDShrapt.Abstractions;

/// <summary>
/// Code metrics for a single method/function.
/// </summary>
public class GDMethodMetrics
{
    /// <summary>
    /// Method name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Start line number (1-based).
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// End line number (1-based).
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// Cyclomatic complexity (number of linearly independent paths).
    /// </summary>
    public int CyclomaticComplexity { get; set; }

    /// <summary>
    /// Cognitive complexity (human-perceived difficulty).
    /// </summary>
    public int CognitiveComplexity { get; set; }

    /// <summary>
    /// Maximum nesting depth of control structures.
    /// </summary>
    public int NestingDepth { get; set; }

    /// <summary>
    /// Number of parameters.
    /// </summary>
    public int ParameterCount { get; set; }

    /// <summary>
    /// Number of local variables declared in the method.
    /// </summary>
    public int LocalVariableCount { get; set; }

    /// <summary>
    /// Number of return statements.
    /// </summary>
    public int ReturnCount { get; set; }

    /// <summary>
    /// Maintainability index (0-100, higher is better).
    /// Based on Visual Studio formula.
    /// </summary>
    public double MaintainabilityIndex { get; set; }

    /// <summary>
    /// Lines of code in the method.
    /// </summary>
    public int LinesOfCode { get; set; }

    public GDMethodMetrics()
    {
    }

    public GDMethodMetrics(string name)
    {
        Name = name;
    }
}
