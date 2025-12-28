namespace GDShrapt.Reader
{
    /// <summary>
    /// Diagnostic codes for validation errors and warnings.
    /// Codes are grouped by category:
    /// - GD1xxx: Syntax errors
    /// - GD2xxx: Scope errors
    /// - GD3xxx: Type errors
    /// - GD4xxx: Call errors
    /// - GD5xxx: Control flow errors
    /// </summary>
    public enum GDDiagnosticCode
    {
        // Syntax errors (1xxx)
        /// <summary>
        /// An invalid token was found during parsing.
        /// </summary>
        InvalidToken = 1001,

        /// <summary>
        /// A required semicolon is missing.
        /// </summary>
        MissingSemicolon = 1002,

        /// <summary>
        /// An unexpected token was encountered.
        /// </summary>
        UnexpectedToken = 1003,

        /// <summary>
        /// A required colon is missing.
        /// </summary>
        MissingColon = 1004,

        /// <summary>
        /// A required bracket is missing.
        /// </summary>
        MissingBracket = 1005,

        // Scope errors (2xxx)
        /// <summary>
        /// A variable is used but not defined.
        /// </summary>
        UndefinedVariable = 2001,

        /// <summary>
        /// A function is called but not defined.
        /// </summary>
        UndefinedFunction = 2002,

        /// <summary>
        /// A symbol is declared more than once in the same scope.
        /// </summary>
        DuplicateDeclaration = 2003,

        /// <summary>
        /// A variable is used before it is declared.
        /// </summary>
        VariableUsedBeforeDeclaration = 2004,

        /// <summary>
        /// A signal is referenced but not defined.
        /// </summary>
        UndefinedSignal = 2005,

        /// <summary>
        /// An enum value is referenced but not defined.
        /// </summary>
        UndefinedEnumValue = 2006,

        // Type errors (3xxx)
        /// <summary>
        /// Types are incompatible in an operation.
        /// </summary>
        TypeMismatch = 3001,

        /// <summary>
        /// An invalid operand type for an operator.
        /// </summary>
        InvalidOperandType = 3002,

        /// <summary>
        /// Cannot assign a value of one type to another.
        /// </summary>
        InvalidAssignment = 3003,

        /// <summary>
        /// A type annotation doesn't match the value type.
        /// </summary>
        TypeAnnotationMismatch = 3004,

        // Call errors (4xxx)
        /// <summary>
        /// Wrong number of arguments in a function call.
        /// </summary>
        WrongArgumentCount = 4001,

        /// <summary>
        /// A method is not found on the target type.
        /// </summary>
        MethodNotFound = 4002,

        /// <summary>
        /// A signal connection has wrong parameters.
        /// </summary>
        InvalidSignalConnection = 4003,

        /// <summary>
        /// Calling a non-callable expression.
        /// </summary>
        NotCallable = 4004,

        // Control flow errors (5xxx)
        /// <summary>
        /// A break statement is used outside of a loop.
        /// </summary>
        BreakOutsideLoop = 5001,

        /// <summary>
        /// A continue statement is used outside of a loop.
        /// </summary>
        ContinueOutsideLoop = 5002,

        /// <summary>
        /// A return statement is used outside of a function.
        /// </summary>
        ReturnOutsideFunction = 5003,

        /// <summary>
        /// Code that will never be executed.
        /// </summary>
        UnreachableCode = 5004,

        /// <summary>
        /// A yield statement is used outside of a function.
        /// </summary>
        YieldOutsideFunction = 5005,

        /// <summary>
        /// An await statement is used outside of a function.
        /// </summary>
        AwaitOutsideFunction = 5006,

        /// <summary>
        /// Await is used on a non-awaitable expression (not a signal or coroutine).
        /// </summary>
        AwaitOnNonAwaitable = 5007,

        /// <summary>
        /// Super is used outside of a method.
        /// </summary>
        SuperOutsideMethod = 5008,

        /// <summary>
        /// Super is used in a static method.
        /// </summary>
        SuperInStaticMethod = 5009,

        /// <summary>
        /// Assignment to a constant variable.
        /// </summary>
        ConstantReassignment = 5010,

        // Indentation errors (6xxx)
        /// <summary>
        /// Inconsistent indentation style (mixing tabs and spaces).
        /// </summary>
        InconsistentIndentation = 6001,

        /// <summary>
        /// Unexpected indentation increase.
        /// </summary>
        UnexpectedIndent = 6002,

        /// <summary>
        /// Expected indentation but none found.
        /// </summary>
        ExpectedIndent = 6003,

        /// <summary>
        /// Indentation level decreased unexpectedly.
        /// </summary>
        UnexpectedDedent = 6004,

        /// <summary>
        /// Indentation amount is not consistent with previous levels.
        /// </summary>
        IndentationMismatch = 6005
    }

    /// <summary>
    /// Extension methods for GDDiagnosticCode.
    /// </summary>
    public static class GDDiagnosticCodeExtensions
    {
        /// <summary>
        /// Gets the formatted code string (e.g., "GD1001", "GD2003").
        /// </summary>
        public static string ToCodeString(this GDDiagnosticCode code)
        {
            return $"GD{(int)code:D4}";
        }
    }
}
