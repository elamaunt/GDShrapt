using GDShrapt.Reader;

namespace GDShrapt.CLI.Core;

/// <summary>
/// CLI overrides for linter options. Null values mean "use config file value".
/// </summary>
public class GDLinterOptionsOverrides
{
    // Filtering
    public string? Rules { get; set; }
    public string? Category { get; set; }

    // Naming conventions
    public NamingCase? ClassNameCase { get; set; }
    public NamingCase? FunctionNameCase { get; set; }
    public NamingCase? VariableNameCase { get; set; }
    public NamingCase? ConstantNameCase { get; set; }
    public NamingCase? SignalNameCase { get; set; }
    public NamingCase? EnumNameCase { get; set; }
    public NamingCase? EnumValueCase { get; set; }
    public NamingCase? InnerClassNameCase { get; set; }
    public bool? RequireUnderscoreForPrivate { get; set; }

    // Limits
    public int? MaxLineLength { get; set; }
    public int? MaxFileLines { get; set; }
    public int? MaxParameters { get; set; }
    public int? MaxFunctionLength { get; set; }
    public int? MaxCyclomaticComplexity { get; set; }

    // Complexity limits (new rules)
    public int? MaxPublicMethods { get; set; }
    public int? MaxReturns { get; set; }
    public int? MaxNestingDepth { get; set; }
    public int? MaxLocalVariables { get; set; }
    public int? MaxClassVariables { get; set; }
    public int? MaxBranches { get; set; }
    public int? MaxBooleanExpressions { get; set; }
    public int? MaxInnerClasses { get; set; }

    // Warnings
    public bool? WarnUnusedVariables { get; set; }
    public bool? WarnUnusedParameters { get; set; }
    public bool? WarnUnusedSignals { get; set; }
    public bool? WarnEmptyFunctions { get; set; }
    public bool? WarnMagicNumbers { get; set; }
    public bool? WarnVariableShadowing { get; set; }
    public bool? WarnAwaitInLoop { get; set; }
    public bool? WarnNoElifReturn { get; set; }
    public bool? WarnNoElseReturn { get; set; }
    public bool? WarnPrivateMethodCall { get; set; }
    public bool? WarnDuplicatedLoad { get; set; }

    // New warnings (new rules)
    public bool? WarnExpressionNotAssigned { get; set; }
    public bool? WarnUselessAssignment { get; set; }
    public bool? WarnInconsistentReturn { get; set; }
    public bool? WarnNoLonelyIf { get; set; }

    // Strict typing
    public GDLintSeverity? StrictTypingClassVariables { get; set; }
    public GDLintSeverity? StrictTypingLocalVariables { get; set; }
    public GDLintSeverity? StrictTypingParameters { get; set; }
    public GDLintSeverity? StrictTypingReturnTypes { get; set; }

    // Comment suppression
    public bool? EnableCommentSuppression { get; set; }

    // Member ordering
    public string? AbstractMethodPosition { get; set; }
    public string? PrivateMethodPosition { get; set; }
    public string? StaticMethodPosition { get; set; }

    /// <summary>
    /// Applies overrides to the given linter options.
    /// </summary>
    public void ApplyTo(GDLinterOptions options)
    {
        // Naming conventions
        if (ClassNameCase.HasValue)
            options.ClassNameCase = ClassNameCase.Value;
        if (FunctionNameCase.HasValue)
            options.FunctionNameCase = FunctionNameCase.Value;
        if (VariableNameCase.HasValue)
            options.VariableNameCase = VariableNameCase.Value;
        if (ConstantNameCase.HasValue)
            options.ConstantNameCase = ConstantNameCase.Value;
        if (SignalNameCase.HasValue)
            options.SignalNameCase = SignalNameCase.Value;
        if (EnumNameCase.HasValue)
            options.EnumNameCase = EnumNameCase.Value;
        if (EnumValueCase.HasValue)
            options.EnumValueCase = EnumValueCase.Value;
        if (InnerClassNameCase.HasValue)
            options.InnerClassNameCase = InnerClassNameCase.Value;
        if (RequireUnderscoreForPrivate.HasValue)
            options.RequireUnderscoreForPrivate = RequireUnderscoreForPrivate.Value;

        // Limits
        if (MaxLineLength.HasValue)
            options.MaxLineLength = MaxLineLength.Value;
        if (MaxFileLines.HasValue)
            options.MaxFileLines = MaxFileLines.Value;
        if (MaxParameters.HasValue)
            options.MaxParameters = MaxParameters.Value;
        if (MaxFunctionLength.HasValue)
            options.MaxFunctionLength = MaxFunctionLength.Value;
        if (MaxCyclomaticComplexity.HasValue)
            options.MaxCyclomaticComplexity = MaxCyclomaticComplexity.Value;

        // Complexity limits (new rules)
        if (MaxPublicMethods.HasValue)
            options.MaxPublicMethods = MaxPublicMethods.Value;
        if (MaxReturns.HasValue)
            options.MaxReturns = MaxReturns.Value;
        if (MaxNestingDepth.HasValue)
            options.MaxNestingDepth = MaxNestingDepth.Value;
        if (MaxLocalVariables.HasValue)
            options.MaxLocalVariables = MaxLocalVariables.Value;
        if (MaxClassVariables.HasValue)
            options.MaxClassVariables = MaxClassVariables.Value;
        if (MaxBranches.HasValue)
            options.MaxBranches = MaxBranches.Value;
        if (MaxBooleanExpressions.HasValue)
            options.MaxBooleanExpressions = MaxBooleanExpressions.Value;
        if (MaxInnerClasses.HasValue)
            options.MaxInnerClasses = MaxInnerClasses.Value;

        // Warnings
        if (WarnUnusedVariables.HasValue)
            options.WarnUnusedVariables = WarnUnusedVariables.Value;
        if (WarnUnusedParameters.HasValue)
            options.WarnUnusedParameters = WarnUnusedParameters.Value;
        if (WarnUnusedSignals.HasValue)
            options.WarnUnusedSignals = WarnUnusedSignals.Value;
        if (WarnEmptyFunctions.HasValue)
            options.WarnEmptyFunctions = WarnEmptyFunctions.Value;
        if (WarnMagicNumbers.HasValue)
            options.WarnMagicNumbers = WarnMagicNumbers.Value;
        if (WarnVariableShadowing.HasValue)
            options.WarnVariableShadowing = WarnVariableShadowing.Value;
        if (WarnAwaitInLoop.HasValue)
            options.WarnAwaitInLoop = WarnAwaitInLoop.Value;
        if (WarnNoElifReturn.HasValue)
            options.WarnNoElifReturn = WarnNoElifReturn.Value;
        if (WarnNoElseReturn.HasValue)
            options.WarnNoElseReturn = WarnNoElseReturn.Value;
        if (WarnPrivateMethodCall.HasValue)
            options.WarnPrivateMethodCall = WarnPrivateMethodCall.Value;
        if (WarnDuplicatedLoad.HasValue)
            options.WarnDuplicatedLoad = WarnDuplicatedLoad.Value;

        // New warnings (new rules)
        if (WarnExpressionNotAssigned.HasValue)
            options.WarnExpressionNotAssigned = WarnExpressionNotAssigned.Value;
        if (WarnUselessAssignment.HasValue)
            options.WarnUselessAssignment = WarnUselessAssignment.Value;
        if (WarnInconsistentReturn.HasValue)
            options.WarnInconsistentReturn = WarnInconsistentReturn.Value;
        if (WarnNoLonelyIf.HasValue)
            options.WarnNoLonelyIf = WarnNoLonelyIf.Value;

        // Strict typing
        if (StrictTypingClassVariables.HasValue)
            options.StrictTypingClassVariables = StrictTypingClassVariables.Value;
        if (StrictTypingLocalVariables.HasValue)
            options.StrictTypingLocalVariables = StrictTypingLocalVariables.Value;
        if (StrictTypingParameters.HasValue)
            options.StrictTypingParameters = StrictTypingParameters.Value;
        if (StrictTypingReturnTypes.HasValue)
            options.StrictTypingReturnTypes = StrictTypingReturnTypes.Value;

        // Comment suppression
        if (EnableCommentSuppression.HasValue)
            options.EnableCommentSuppression = EnableCommentSuppression.Value;

        // Member ordering
        if (AbstractMethodPosition != null)
            options.AbstractMethodPosition = AbstractMethodPosition;
        if (PrivateMethodPosition != null)
            options.PrivateMethodPosition = PrivateMethodPosition;
        if (StaticMethodPosition != null)
            options.StaticMethodPosition = StaticMethodPosition;
    }
}
