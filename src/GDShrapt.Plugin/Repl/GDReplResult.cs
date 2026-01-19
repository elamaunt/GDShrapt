using Godot;

namespace GDShrapt.Plugin;

/// <summary>
/// Result of evaluating a REPL expression.
/// </summary>
internal class GDReplResult
{
    /// <summary>
    /// Whether the evaluation was successful.
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// The resulting value (if successful).
    /// </summary>
    public Variant Value { get; private set; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Whether the result has a value to display.
    /// Some expressions (like void method calls) may succeed without returning a value.
    /// </summary>
    public bool HasValue { get; private set; }

    private GDReplResult() { }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static GDReplResult WithValue(Variant value)
    {
        return new GDReplResult
        {
            Success = true,
            Value = value,
            HasValue = true
        };
    }

    /// <summary>
    /// Creates a successful result without a value (void).
    /// </summary>
    public static GDReplResult Void()
    {
        return new GDReplResult
        {
            Success = true,
            HasValue = false
        };
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static GDReplResult Error(string message)
    {
        return new GDReplResult
        {
            Success = false,
            ErrorMessage = message,
            HasValue = false
        };
    }

    /// <summary>
    /// Formats the result for display.
    /// Uses Godot's built-in VarToStr for pretty printing.
    /// </summary>
    public string FormatOutput()
    {
        if (!Success)
        {
            return $"Error: {ErrorMessage}";
        }

        if (!HasValue)
        {
            return "(void)";
        }

        // Use Godot's built-in formatting
        return GD.VarToStr(Value);
    }
}
