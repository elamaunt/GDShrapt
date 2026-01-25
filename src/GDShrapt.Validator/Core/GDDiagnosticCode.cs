namespace GDShrapt.Reader
{
    /// <summary>
    /// Diagnostic codes for validation errors and warnings.
    /// Codes are grouped by category:
    /// - GD1xxx: Syntax errors
    /// - GD2xxx: Scope errors
    /// - GD3xxx: Type errors (including indexers GD3013-3015, generics GD3016-3018)
    /// - GD4xxx: Call errors (including signal types GD4009-4010)
    /// - GD5xxx: Control flow errors
    /// - GD6xxx: Indentation errors
    /// - GD7xxx: Duck typing errors
    /// - GD8xxx: Abstract errors
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

        /// <summary>
        /// Unknown type in extends clause.
        /// </summary>
        UnknownBaseType = 3005,

        /// <summary>
        /// Unknown type in type annotation.
        /// </summary>
        UnknownType = 3006,

        /// <summary>
        /// Incompatible return type.
        /// </summary>
        IncompatibleReturnType = 3007,

        /// <summary>
        /// Cannot access member on type.
        /// </summary>
        MemberNotAccessible = 3008,

        /// <summary>
        /// Property not found on type.
        /// </summary>
        PropertyNotFound = 3009,

        /// <summary>
        /// Argument type mismatch in function call.
        /// </summary>
        ArgumentTypeMismatch = 3010,

        /// <summary>
        /// Accessing a member on a type that was excluded by flow analysis.
        /// For example, calling String.to_upper() after type guards excluded String.
        /// </summary>
        ImpossibleTypeAccess = 3011,

        /// <summary>
        /// Recommendation to add type annotation for better analysis.
        /// </summary>
        TypeAnnotationRecommended = 3012,

        /// <summary>
        /// Indexer key type does not match expected type.
        /// For example: arr["string"] where arr expects int key.
        /// </summary>
        IndexerKeyTypeMismatch = 3013,

        /// <summary>
        /// Attempting to use indexer on a non-indexable type.
        /// For example: int[0] - int is not indexable.
        /// </summary>
        NotIndexable = 3014,

        /// <summary>
        /// Static index is out of range for known-size collections.
        /// </summary>
        IndexOutOfRange = 3015,

        /// <summary>
        /// Wrong number of type parameters for generic type.
        /// For example: Array[int, String] instead of Array[int].
        /// </summary>
        WrongGenericParameterCount = 3016,

        /// <summary>
        /// Unknown or invalid type used as generic argument.
        /// For example: Array[UnknownType].
        /// </summary>
        InvalidGenericArgument = 3017,

        /// <summary>
        /// Dictionary key type is not hashable.
        /// For example: Dictionary[Array, int] - Array is not hashable.
        /// </summary>
        DictionaryKeyNotHashable = 3018,

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

        /// <summary>
        /// emit_signal called with wrong number of arguments.
        /// </summary>
        EmitSignalWrongArgCount = 4005,

        /// <summary>
        /// Signal not found on type.
        /// </summary>
        UndefinedSignalEmit = 4006,

        /// <summary>
        /// connect callback has wrong signature.
        /// </summary>
        ConnectCallbackMismatch = 4007,

        /// <summary>
        /// A resource path in load/preload does not exist.
        /// </summary>
        ResourceNotFound = 4008,

        /// <summary>
        /// emit_signal called with argument type that doesn't match signal parameter.
        /// </summary>
        EmitSignalTypeMismatch = 4009,

        /// <summary>
        /// connect callback signature doesn't match signal parameters.
        /// </summary>
        ConnectCallbackTypeMismatch = 4010,

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
        IndentationMismatch = 6005,

        // Duck typing errors (7xxx)
        /// <summary>
        /// Accessing a method on an untyped variable without type guard.
        /// </summary>
        UnguardedMethodAccess = 7001,

        /// <summary>
        /// Accessing a property on an untyped variable without type guard.
        /// </summary>
        UnguardedPropertyAccess = 7002,

        /// <summary>
        /// Calling a method on an untyped variable without type guard.
        /// </summary>
        UnguardedMethodCall = 7003,

        /// <summary>
        /// Member access on variable where no known type has that member.
        /// </summary>
        MemberNotGuaranteed = 7004,

        // Abstract errors (8xxx)
        /// <summary>
        /// Abstract method has an implementation body.
        /// </summary>
        AbstractMethodHasBody = 8001,

        /// <summary>
        /// Class contains abstract methods but is not marked @abstract.
        /// </summary>
        ClassNotAbstract = 8002,

        /// <summary>
        /// Non-abstract class does not implement inherited abstract method.
        /// </summary>
        AbstractMethodNotImplemented = 8003,

        /// <summary>
        /// Cannot call super() in an abstract method.
        /// </summary>
        SuperInAbstractMethod = 8004,

        /// <summary>
        /// Cannot instantiate abstract class.
        /// </summary>
        AbstractClassInstantiation = 8005
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
