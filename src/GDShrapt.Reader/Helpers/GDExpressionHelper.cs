namespace GDShrapt.Reader
{
    /// <summary>
    /// Helper class for identifying and working with GDScript built-in functions and special calls
    /// </summary>
    public static class GDExpressionHelper
    {
        // Built-in functions
        public const string Preload = "preload";
        public const string Load = "load";
        public const string Assert = "assert";
        public const string Super = "super";
        public const string Print = "print";
        public const string PrintS = "prints";
        public const string PrintT = "printt";
        public const string PrintRaw = "print_raw";
        public const string PrintRich = "print_rich";
        public const string PrintDebug = "print_debug";
        public const string PushError = "push_error";
        public const string PushWarning = "push_warning";
        public const string Str = "str";
        public const string Range = "range";
        public const string Len = "len";
        public const string Typeof = "typeof";
        public const string InstanceFromId = "instance_from_id";
        public const string IsInstanceValid = "is_instance_valid";
        public const string IsInstanceIdValid = "is_instance_id_valid";
        public const string GetStack = "get_stack";
        public const string Weakref = "weakref";
        public const string TypeString = "type_string";
        public const string VarToStr = "var_to_str";
        public const string StrToVar = "str_to_var";
        public const string VarToBytes = "var_to_bytes";
        public const string VarToBytesWithObjects = "var_to_bytes_with_objects";
        public const string BytesToVar = "bytes_to_var";
        public const string BytesToVarWithObjects = "bytes_to_var_with_objects";

        // Math functions
        public const string Abs = "abs";
        public const string Floor = "floor";
        public const string Ceil = "ceil";
        public const string Round = "round";
        public const string Clamp = "clamp";
        public const string Lerp = "lerp";
        public const string Min = "min";
        public const string Max = "max";
        public const string Sign = "sign";
        public const string Sqrt = "sqrt";
        public const string Pow = "pow";
        public const string Sin = "sin";
        public const string Cos = "cos";
        public const string Tan = "tan";
        public const string Asin = "asin";
        public const string Acos = "acos";
        public const string Atan = "atan";
        public const string Atan2 = "atan2";
        public const string Deg2Rad = "deg_to_rad";
        public const string Rad2Deg = "rad_to_deg";
        public const string Randi = "randi";
        public const string Randf = "randf";
        public const string RandRange = "randf_range";
        public const string RandSeed = "seed";
        public const string Randomize = "randomize";

        /// <summary>
        /// Checks if a call expression is a call to a specific function name
        /// </summary>
        public static bool IsCallTo(this GDCallExpression call, string functionName)
        {
            if (call?.CallerExpression == null)
                return false;

            if (call.CallerExpression is GDIdentifierExpression identExpr)
                return identExpr.Identifier?.Sequence == functionName;

            return false;
        }

        /// <summary>
        /// Gets the function name from a simple call expression (e.g., "print()", "preload()")
        /// Returns null if the caller is not a simple identifier
        /// </summary>
        public static string GetSimpleCallName(this GDCallExpression call)
        {
            if (call?.CallerExpression is GDIdentifierExpression identExpr)
                return identExpr.Identifier?.Sequence;

            return null;
        }

        /// <summary>
        /// Checks if the call is to preload()
        /// </summary>
        public static bool IsPreload(this GDCallExpression call)
        {
            return call.IsCallTo(Preload);
        }

        /// <summary>
        /// Checks if the call is to load()
        /// </summary>
        public static bool IsLoad(this GDCallExpression call)
        {
            return call.IsCallTo(Load);
        }

        /// <summary>
        /// Checks if the call is to assert()
        /// </summary>
        public static bool IsAssert(this GDCallExpression call)
        {
            return call.IsCallTo(Assert);
        }

        /// <summary>
        /// Checks if the call is to super()
        /// </summary>
        public static bool IsSuper(this GDCallExpression call)
        {
            return call.IsCallTo(Super);
        }

        /// <summary>
        /// Checks if the call is any print function
        /// </summary>
        public static bool IsPrint(this GDCallExpression call)
        {
            var name = call.GetSimpleCallName();
            if (name == null)
                return false;

            return name == Print ||
                   name == PrintS ||
                   name == PrintT ||
                   name == PrintRaw ||
                   name == PrintRich ||
                   name == PrintDebug;
        }

        /// <summary>
        /// Checks if the call is push_error() or push_warning()
        /// </summary>
        public static bool IsErrorLogging(this GDCallExpression call)
        {
            var name = call.GetSimpleCallName();
            return name == PushError || name == PushWarning;
        }

        /// <summary>
        /// Checks if the call is to range()
        /// </summary>
        public static bool IsRange(this GDCallExpression call)
        {
            return call.IsCallTo(Range);
        }

        /// <summary>
        /// Checks if the call is a math function
        /// </summary>
        public static bool IsMathFunction(this GDCallExpression call)
        {
            var name = call.GetSimpleCallName();
            if (name == null)
                return false;

            return name == Abs ||
                   name == Floor ||
                   name == Ceil ||
                   name == Round ||
                   name == Clamp ||
                   name == Lerp ||
                   name == Min ||
                   name == Max ||
                   name == Sign ||
                   name == Sqrt ||
                   name == Pow ||
                   name == Sin ||
                   name == Cos ||
                   name == Tan ||
                   name == Asin ||
                   name == Acos ||
                   name == Atan ||
                   name == Atan2 ||
                   name == Deg2Rad ||
                   name == Rad2Deg;
        }

        /// <summary>
        /// Checks if the call is a random function
        /// </summary>
        public static bool IsRandomFunction(this GDCallExpression call)
        {
            var name = call.GetSimpleCallName();
            if (name == null)
                return false;

            return name == Randi ||
                   name == Randf ||
                   name == RandRange ||
                   name == RandSeed ||
                   name == Randomize;
        }

        /// <summary>
        /// Checks if the expression is a GetUniqueNode expression (%NodeName)
        /// </summary>
        public static bool IsGetUniqueNode(this GDExpression expression)
        {
            return expression is GDGetUniqueNodeExpression;
        }

        /// <summary>
        /// Checks if the expression is a GetNode expression ($NodePath)
        /// </summary>
        public static bool IsGetNode(this GDExpression expression)
        {
            return expression is GDGetNodeExpression;
        }

        /// <summary>
        /// Checks if the expression is a lambda/method expression
        /// </summary>
        public static bool IsLambda(this GDExpression expression)
        {
            return expression is GDMethodExpression;
        }

        /// <summary>
        /// Checks if the expression is an await expression
        /// </summary>
        public static bool IsAwait(this GDExpression expression)
        {
            return expression is GDAwaitExpression;
        }

        /// <summary>
        /// Checks if the expression is a member access (object.member)
        /// </summary>
        public static bool IsMemberAccess(this GDExpression expression)
        {
            return expression is GDMemberOperatorExpression;
        }

        /// <summary>
        /// Checks if the expression is an indexer access (object[index])
        /// </summary>
        public static bool IsIndexer(this GDExpression expression)
        {
            return expression is GDIndexerExpression;
        }

        /// <summary>
        /// Checks if the call is a method call on an object (object.method())
        /// </summary>
        public static bool IsMethodCall(this GDCallExpression call)
        {
            return call?.CallerExpression is GDMemberOperatorExpression;
        }

        /// <summary>
        /// Gets the method name from a method call (object.method())
        /// Returns null if not a method call
        /// </summary>
        public static string GetMethodCallName(this GDCallExpression call)
        {
            if (call?.CallerExpression is GDMemberOperatorExpression memberExpr)
            {
                return memberExpr.Identifier?.Sequence;
            }

            return null;
        }

        /// <summary>
        /// Checks if the expression is an array initializer []
        /// </summary>
        public static bool IsArrayInitializer(this GDExpression expression)
        {
            return expression is GDArrayInitializerExpression;
        }

        /// <summary>
        /// Checks if the expression is a dictionary initializer {}
        /// </summary>
        public static bool IsDictionaryInitializer(this GDExpression expression)
        {
            return expression is GDDictionaryInitializerExpression;
        }

        /// <summary>
        /// Checks if the expression is a ternary if expression (value if condition else other)
        /// </summary>
        public static bool IsTernaryIf(this GDExpression expression)
        {
            return expression is GDIfExpression;
        }

        /// <summary>
        /// Checks if the function name is a known GDScript built-in function
        /// </summary>
        public static bool IsBuiltInFunction(string functionName)
        {
            if (functionName == null)
                return false;

            return functionName == Preload ||
                   functionName == Load ||
                   functionName == Assert ||
                   functionName == Print ||
                   functionName == PrintS ||
                   functionName == PrintT ||
                   functionName == PrintRaw ||
                   functionName == PrintRich ||
                   functionName == PrintDebug ||
                   functionName == PushError ||
                   functionName == PushWarning ||
                   functionName == Str ||
                   functionName == Range ||
                   functionName == Len ||
                   functionName == Typeof ||
                   functionName == InstanceFromId ||
                   functionName == IsInstanceValid ||
                   functionName == IsInstanceIdValid ||
                   functionName == GetStack ||
                   functionName == Weakref ||
                   functionName == TypeString ||
                   functionName == VarToStr ||
                   functionName == StrToVar ||
                   functionName == VarToBytes ||
                   functionName == VarToBytesWithObjects ||
                   functionName == BytesToVar ||
                   functionName == BytesToVarWithObjects ||
                   functionName == Abs ||
                   functionName == Floor ||
                   functionName == Ceil ||
                   functionName == Round ||
                   functionName == Clamp ||
                   functionName == Lerp ||
                   functionName == Min ||
                   functionName == Max ||
                   functionName == Sign ||
                   functionName == Sqrt ||
                   functionName == Pow ||
                   functionName == Sin ||
                   functionName == Cos ||
                   functionName == Tan ||
                   functionName == Asin ||
                   functionName == Acos ||
                   functionName == Atan ||
                   functionName == Atan2 ||
                   functionName == Deg2Rad ||
                   functionName == Rad2Deg ||
                   functionName == Randi ||
                   functionName == Randf ||
                   functionName == RandRange ||
                   functionName == RandSeed ||
                   functionName == Randomize;
        }
    }
}
