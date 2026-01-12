using Godot;
using GDShrapt.Reader;

namespace GDShrapt.Plugin;

/// <summary>
/// Orchestrates parsing and evaluation of REPL expressions.
/// </summary>
internal class ReplExecutor
{
    private readonly GDScriptReader _reader = new();
    private readonly ReplExpressionEvaluator _evaluator = new();

    /// <summary>
    /// Executes a GDScript expression on a target GodotObject.
    /// </summary>
    /// <param name="input">The GDScript expression to execute</param>
    /// <param name="target">The target node/object to execute on</param>
    /// <returns>The result of the execution</returns>
    public ReplResult Execute(string input, GodotObject target)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ReplResult.Error("Empty expression");

        if (target == null)
            return ReplResult.Error("No target node selected");

        // Parse the expression
        GDExpression expression;
        try
        {
            expression = _reader.ParseExpression(input);
        }
        catch (System.Exception ex)
        {
            return ReplResult.Error($"Parse error: {ex.Message}");
        }

        if (expression == null)
            return ReplResult.Error("Failed to parse expression");

        // Evaluate the expression
        return _evaluator.Evaluate(expression, target);
    }
}
