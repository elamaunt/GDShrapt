using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Resolves result types for GDScript binary and unary operators
    /// based on Godot's deterministic operator rules.
    /// </summary>
    public static class GDOperatorTypeResolver
    {
        #region Type Sets

        private static readonly HashSet<string> NumericTypes = new HashSet<string>
        {
            "int", "float"
        };

        private static readonly HashSet<string> VectorTypes = new HashSet<string>
        {
            "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i"
        };

        private static readonly HashSet<string> IntegerVectorTypes = new HashSet<string>
        {
            "Vector2i", "Vector3i", "Vector4i"
        };

        private static readonly HashSet<string> TransformTypes = new HashSet<string>
        {
            "Transform2D", "Transform3D"
        };

        #endregion

        #region Main Resolution

        /// <summary>
        /// Resolves the result type for a binary operator expression.
        /// </summary>
        /// <param name="op">The operator type</param>
        /// <param name="leftType">The type of the left operand</param>
        /// <param name="rightType">The type of the right operand</param>
        /// <returns>The result type, or null if types are incompatible</returns>
        public static string ResolveOperatorType(
            GDDualOperatorType op,
            string leftType,
            string rightType)
        {
            // FIRST: Operators with fixed result type (don't depend on operand types)
            // These must be checked BEFORE the null check to handle Variant parameters
            switch (op)
            {
                // Comparison operators - ALWAYS return bool
                case GDDualOperatorType.Equal:
                case GDDualOperatorType.NotEqual:
                case GDDualOperatorType.LessThan:
                case GDDualOperatorType.MoreThan:
                case GDDualOperatorType.LessThanOrEqual:
                case GDDualOperatorType.MoreThanOrEqual:
                case GDDualOperatorType.Is:
                case GDDualOperatorType.In:
                    return "bool";

                // Logical operators - ALWAYS return bool
                case GDDualOperatorType.And:
                case GDDualOperatorType.And2:
                case GDDualOperatorType.Or:
                case GDDualOperatorType.Or2:
                    return "bool";

                // As operator - returns right type (or Variant if unknown)
                case GDDualOperatorType.As:
                    return rightType ?? "Variant";
            }

            // For remaining operators, null types propagate as null (unknown)
            if (string.IsNullOrEmpty(leftType) || string.IsNullOrEmpty(rightType))
                return null;
            if (leftType == "Unknown" || rightType == "Unknown")
                return "Unknown";
            if (leftType == "Variant" || rightType == "Variant")
                return "Variant";

            switch (op)
            {
                // Arithmetic
                case GDDualOperatorType.Addition:
                    return ResolveAddition(leftType, rightType);

                case GDDualOperatorType.Subtraction:
                    return ResolveSubtraction(leftType, rightType);

                case GDDualOperatorType.Multiply:
                    return ResolveMultiplication(leftType, rightType);

                case GDDualOperatorType.Division:
                    return ResolveDivision(leftType, rightType);

                case GDDualOperatorType.Mod:
                    return ResolveMod(leftType, rightType);

                case GDDualOperatorType.Power:
                    // Power always returns float
                    if (IsNumeric(leftType) && IsNumeric(rightType))
                        return "float";
                    return null;

                // Bitwise - require int, return int
                case GDDualOperatorType.BitwiseAnd:
                case GDDualOperatorType.BitwiseOr:
                case GDDualOperatorType.Xor:
                case GDDualOperatorType.BitShiftLeft:
                case GDDualOperatorType.BitShiftRight:
                    if (leftType == "int" && rightType == "int")
                        return "int";
                    return null;

                // Assignment - returns left type
                case GDDualOperatorType.Assignment:
                    return leftType;

                // Compound assignments
                case GDDualOperatorType.AddAndAssign:
                case GDDualOperatorType.SubtractAndAssign:
                case GDDualOperatorType.MultiplyAndAssign:
                case GDDualOperatorType.DivideAndAssign:
                case GDDualOperatorType.ModAndAssign:
                case GDDualOperatorType.PowerAndAssign:
                case GDDualOperatorType.BitwiseAndAndAssign:
                case GDDualOperatorType.BitwiseOrAndAssign:
                case GDDualOperatorType.XorAndAssign:
                case GDDualOperatorType.BitShiftLeftAndAssign:
                case GDDualOperatorType.BitShiftRightAndAssign:
                    return leftType;

                default:
                    return null;
            }
        }

        #endregion

        #region Type Node Resolution

        /// <summary>
        /// Resolves the result type node for a binary operator expression.
        /// Returns the type node directly without string conversion.
        /// </summary>
        public static GDTypeNode? ResolveOperatorTypeNode(
            GDDualOperatorType op,
            GDTypeNode? leftTypeNode,
            GDTypeNode? rightTypeNode)
        {
            // Fixed-result operators - always return specific type
            switch (op)
            {
                case GDDualOperatorType.Equal:
                case GDDualOperatorType.NotEqual:
                case GDDualOperatorType.LessThan:
                case GDDualOperatorType.MoreThan:
                case GDDualOperatorType.LessThanOrEqual:
                case GDDualOperatorType.MoreThanOrEqual:
                case GDDualOperatorType.Is:
                case GDDualOperatorType.In:
                case GDDualOperatorType.And:
                case GDDualOperatorType.And2:
                case GDDualOperatorType.Or:
                case GDDualOperatorType.Or2:
                    return GDTypeNode.CreateSimple("bool");

                case GDDualOperatorType.As:
                    return rightTypeNode;
            }

            // For remaining operators, null types propagate as null
            if (leftTypeNode == null || rightTypeNode == null)
                return null;

            switch (op)
            {
                case GDDualOperatorType.Addition:
                    return ResolveAdditionTypeNode(leftTypeNode, rightTypeNode);

                case GDDualOperatorType.Subtraction:
                    return ResolveSubtractionTypeNode(leftTypeNode, rightTypeNode);

                case GDDualOperatorType.Multiply:
                    return ResolveMultiplicationTypeNode(leftTypeNode, rightTypeNode);

                case GDDualOperatorType.Division:
                    return ResolveDivisionTypeNode(leftTypeNode, rightTypeNode);

                case GDDualOperatorType.Mod:
                    return ResolveModTypeNode(leftTypeNode, rightTypeNode);

                case GDDualOperatorType.Power:
                    if (leftTypeNode.IsNumericType() && rightTypeNode.IsNumericType())
                        return GDTypeNode.CreateSimple("float");
                    return null;

                case GDDualOperatorType.BitwiseAnd:
                case GDDualOperatorType.BitwiseOr:
                case GDDualOperatorType.Xor:
                case GDDualOperatorType.BitShiftLeft:
                case GDDualOperatorType.BitShiftRight:
                    if (leftTypeNode.IsIntType() && rightTypeNode.IsIntType())
                        return GDTypeNode.CreateSimple("int");
                    return null;

                case GDDualOperatorType.Assignment:
                    return leftTypeNode;

                case GDDualOperatorType.AddAndAssign:
                case GDDualOperatorType.SubtractAndAssign:
                case GDDualOperatorType.MultiplyAndAssign:
                case GDDualOperatorType.DivideAndAssign:
                case GDDualOperatorType.ModAndAssign:
                case GDDualOperatorType.PowerAndAssign:
                case GDDualOperatorType.BitwiseAndAndAssign:
                case GDDualOperatorType.BitwiseOrAndAssign:
                case GDDualOperatorType.XorAndAssign:
                case GDDualOperatorType.BitShiftLeftAndAssign:
                case GDDualOperatorType.BitShiftRightAndAssign:
                    return leftTypeNode;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Resolves addition with proper GDTypeNode handling.
        /// </summary>
        private static GDTypeNode? ResolveAdditionTypeNode(GDTypeNode left, GDTypeNode right)
        {
            // String concatenation
            if (left.IsStringType() || right.IsStringType())
                return GDTypeNode.CreateSimple("String");

            // Numeric addition
            if (left.IsNumericType() && right.IsNumericType())
                return left.IsFloatType() || right.IsFloatType()
                    ? GDTypeNode.CreateSimple("float")
                    : GDTypeNode.CreateSimple("int");

            // Vector addition - same types only
            if (left.IsVectorType() && right.IsVectorType())
            {
                if (AreTypesEqual(left, right))
                    return left;
            }

            // Color addition
            if (left.IsColorType() && right.IsColorType())
                return GDTypeNode.CreateSimple("Color");

            // Array concatenation - preserve type info
            if (left is GDArrayTypeNode leftArray && right is GDArrayTypeNode rightArray)
            {
                return CombineArrayTypes(leftArray, rightArray);
            }

            // Mixed array types (one typed, one untyped or plain Array)
            if (left.IsArray && right.IsArray)
                return GDTypeNode.CreateArray(null);

            return null;
        }

        /// <summary>
        /// Resolves subtraction with proper GDTypeNode handling.
        /// </summary>
        private static GDTypeNode? ResolveSubtractionTypeNode(GDTypeNode left, GDTypeNode right)
        {
            // Numeric subtraction
            if (left.IsNumericType() && right.IsNumericType())
                return left.IsFloatType() || right.IsFloatType()
                    ? GDTypeNode.CreateSimple("float")
                    : GDTypeNode.CreateSimple("int");

            // Vector subtraction - same types only
            if (left.IsVectorType() && right.IsVectorType() && AreTypesEqual(left, right))
                return left;

            // Color subtraction
            if (left.IsColorType() && right.IsColorType())
                return GDTypeNode.CreateSimple("Color");

            return null;
        }

        /// <summary>
        /// Resolves multiplication with proper GDTypeNode handling.
        /// </summary>
        private static GDTypeNode? ResolveMultiplicationTypeNode(GDTypeNode left, GDTypeNode right)
        {
            // String repetition
            if ((left.IsStringType() && right.IsIntType()) ||
                (left.IsIntType() && right.IsStringType()))
                return GDTypeNode.CreateSimple("String");

            // Numeric multiplication
            if (left.IsNumericType() && right.IsNumericType())
                return left.IsFloatType() || right.IsFloatType()
                    ? GDTypeNode.CreateSimple("float")
                    : GDTypeNode.CreateSimple("int");

            // Vector * scalar or Vector * Vector
            if (left.IsVectorType())
            {
                if (right.IsNumericType() || right.IsVectorType())
                    return left;
            }
            if (right.IsVectorType() && left.IsNumericType())
                return right;

            // Color operations
            if (left.IsColorType() && (right.IsNumericType() || right.IsColorType()))
                return GDTypeNode.CreateSimple("Color");
            if (left.IsNumericType() && right.IsColorType())
                return GDTypeNode.CreateSimple("Color");

            return null;
        }

        /// <summary>
        /// Resolves division with proper GDTypeNode handling.
        /// </summary>
        private static GDTypeNode? ResolveDivisionTypeNode(GDTypeNode left, GDTypeNode right)
        {
            // Numeric division always returns float
            if (left.IsNumericType() && right.IsNumericType())
                return GDTypeNode.CreateSimple("float");

            // Vector / scalar or Vector / Vector
            if (left.IsVectorType() && (right.IsNumericType() || right.IsVectorType()))
                return left;

            // Color operations
            if (left.IsColorType() && (right.IsNumericType() || right.IsColorType()))
                return GDTypeNode.CreateSimple("Color");

            return null;
        }

        /// <summary>
        /// Resolves modulo with proper GDTypeNode handling.
        /// </summary>
        private static GDTypeNode? ResolveModTypeNode(GDTypeNode left, GDTypeNode right)
        {
            // int % int = int
            if (left.IsIntType() && right.IsIntType())
                return GDTypeNode.CreateSimple("int");

            // Any float involvement = float
            if (left.IsNumericType() && right.IsNumericType())
                return GDTypeNode.CreateSimple("float");

            // Vector % Vector or Vector % scalar
            if (left.IsVectorType() && (right.IsVectorType() || right.IsNumericType()))
                return left;

            return null;
        }

        /// <summary>
        /// Combines two typed arrays for concatenation.
        /// Rules:
        /// - Same element type → preserve
        /// - int + float → Array[float] (numeric widening)
        /// - Incompatible types → Array[A|B] (union type)
        /// - One untyped → untyped Array
        /// </summary>
        private static GDTypeNode CombineArrayTypes(GDArrayTypeNode left, GDArrayTypeNode right)
        {
            var leftInner = left.InnerType;
            var rightInner = right.InnerType;

            // Both untyped → untyped
            if (leftInner == null || rightInner == null)
                return GDTypeNode.CreateArray(null);

            // Same type → preserve
            if (AreTypesEqual(leftInner, rightInner))
                return GDTypeNode.CreateArray(leftInner);

            // Try numeric widening
            if (leftInner.IsNumericType() && rightInner.IsNumericType())
            {
                // int + float → float
                if (leftInner.IsFloatType() || rightInner.IsFloatType())
                    return GDTypeNode.CreateArray(GDTypeNode.CreateSimple("float"));
                // Both int → preserve int
                return GDTypeNode.CreateArray(leftInner);
            }

            // Incompatible types → return null to signal fallback to string-based resolver
            // The string-based resolver will return "Array[A|B]" union type
            return null;
        }

        /// <summary>
        /// Creates a union type name, handling nested unions.
        /// Example: ("int|String", "float") → "String|float|int"
        /// </summary>
        private static string CreateUnionTypeName(string type1, string type2)
        {
            var types = new HashSet<string>();
            types.UnionWith(ExtractUnionTypes(type1));
            types.UnionWith(ExtractUnionTypes(type2));

            if (types.Count == 1)
                return types.First();

            return string.Join("|", types.OrderBy(t => t));
        }

        /// <summary>
        /// Extracts individual types from a union type string.
        /// </summary>
        private static HashSet<string> ExtractUnionTypes(string typeName)
        {
            if (typeName.Contains("|"))
                return new HashSet<string>(typeName.Split('|').Select(t => t.Trim()));
            return new HashSet<string> { typeName };
        }

        /// <summary>
        /// Compares two type nodes structurally.
        /// </summary>
        private static bool AreTypesEqual(GDTypeNode left, GDTypeNode right)
        {
            if (left == null || right == null)
                return left == right;

            // Compare simple types
            if (left is GDSingleTypeNode leftSingle && right is GDSingleTypeNode rightSingle)
                return leftSingle.Type?.Sequence == rightSingle.Type?.Sequence;

            // Compare array types recursively
            if (left is GDArrayTypeNode leftArray && right is GDArrayTypeNode rightArray)
            {
                if (leftArray.InnerType == null && rightArray.InnerType == null)
                    return true;
                if (leftArray.InnerType == null || rightArray.InnerType == null)
                    return false;
                return AreTypesEqual(leftArray.InnerType, rightArray.InnerType);
            }

            // For other types, compare by name
            return left.BuildName() == right.BuildName();
        }

        #endregion

        #region Arithmetic Resolution

        /// <summary>
        /// Resolves the result type for addition operator.
        /// String concatenation takes priority over numeric addition.
        /// </summary>
        private static string ResolveAddition(string left, string right)
        {
            // String concatenation takes priority - any type + String or String + any type = String
            if (left == "String" || right == "String")
                return "String";

            // StringName concatenation
            if (left == "StringName" || right == "StringName")
                return "String";

            // Numeric addition
            if (IsNumeric(left) && IsNumeric(right))
                return (left == "float" || right == "float") ? "float" : "int";

            // Vector addition - same types only
            if (IsVector(left) && left == right)
                return left;

            // Color addition
            if (left == "Color" && right == "Color")
                return "Color";

            // Array concatenation
            if (left == "Array" && right == "Array")
                return "Array";

            // PackedArray concatenation (same types)
            if (IsPackedArray(left) && left == right)
                return left;

            return null; // Incompatible types
        }

        /// <summary>
        /// Resolves the result type for subtraction operator.
        /// </summary>
        private static string ResolveSubtraction(string left, string right)
        {
            // Numeric subtraction
            if (IsNumeric(left) && IsNumeric(right))
                return (left == "float" || right == "float") ? "float" : "int";

            // Vector subtraction - same types only
            if (IsVector(left) && left == right)
                return left;

            // Color subtraction
            if (left == "Color" && right == "Color")
                return "Color";

            return null;
        }

        /// <summary>
        /// Resolves the result type for multiplication operator.
        /// Supports numeric multiplication, string repetition, vector scaling,
        /// and transform composition.
        /// </summary>
        private static string ResolveMultiplication(string left, string right)
        {
            // String repetition: String * int or int * String
            if ((left == "String" && right == "int") ||
                (left == "int" && right == "String"))
                return "String";

            // Numeric multiplication
            if (IsNumeric(left) && IsNumeric(right))
                return (left == "float" || right == "float") ? "float" : "int";

            // Vector * scalar (scalar multiplication)
            if (IsVector(left) && IsNumeric(right))
            {
                // If multiplying integer vector by float, result is float vector
                if (IsIntegerVector(left) && right == "float")
                    return GetFloatVectorVersion(left);
                return left;
            }
            if (IsNumeric(left) && IsVector(right))
            {
                // If multiplying integer vector by float, result is float vector
                if (IsIntegerVector(right) && left == "float")
                    return GetFloatVectorVersion(right);
                return right;
            }

            // Vector * Vector (component-wise multiplication)
            if (IsVector(left) && left == right)
                return left;

            // Transform composition
            if (IsTransform(left) && left == right)
                return left;

            // Transform * Vector (applying transformation)
            if (left == "Transform2D" && (right == "Vector2" || right == "Vector2i"))
                return "Vector2";
            if (left == "Transform3D" && (right == "Vector3" || right == "Vector3i"))
                return "Vector3";

            // Basis operations
            if (left == "Basis" && right == "Basis")
                return "Basis";
            if (left == "Basis" && (right == "Vector3" || right == "Vector3i"))
                return "Vector3";

            // Quaternion operations
            if (left == "Quaternion" && right == "Quaternion")
                return "Quaternion";
            if (left == "Quaternion" && (right == "Vector3" || right == "Vector3i"))
                return "Vector3";

            // Projection * Vector4
            if (left == "Projection" && (right == "Vector4" || right == "Vector4i"))
                return "Vector4";

            // Color * scalar
            if (left == "Color" && IsNumeric(right))
                return "Color";
            if (IsNumeric(left) && right == "Color")
                return "Color";

            // Color * Color (component-wise)
            if (left == "Color" && right == "Color")
                return "Color";

            return null;
        }

        /// <summary>
        /// Resolves the result type for division operator.
        /// In GDScript, numeric division always returns float.
        /// </summary>
        private static string ResolveDivision(string left, string right)
        {
            // Numeric division ALWAYS returns float in GDScript
            if (IsNumeric(left) && IsNumeric(right))
                return "float";

            // Vector / scalar
            if (IsVector(left) && IsNumeric(right))
            {
                // Integer vectors divided by any number become float vectors
                if (IsIntegerVector(left))
                    return GetFloatVectorVersion(left);
                return left;
            }

            // Vector / Vector (component-wise)
            if (IsVector(left) && left == right)
            {
                // Integer vector division returns float vector
                if (IsIntegerVector(left))
                    return GetFloatVectorVersion(left);
                return left;
            }

            // Color / scalar
            if (left == "Color" && IsNumeric(right))
                return "Color";

            // Color / Color (component-wise)
            if (left == "Color" && right == "Color")
                return "Color";

            return null;
        }

        /// <summary>
        /// Resolves the result type for modulo operator.
        /// </summary>
        private static string ResolveMod(string left, string right)
        {
            // int % int = int
            if (left == "int" && right == "int")
                return "int";

            // Any float involvement = float
            if (IsNumeric(left) && IsNumeric(right))
                return "float";

            // Vector % Vector (component-wise)
            if (IsVector(left) && left == right)
                return left;

            // Vector % scalar
            if (IsVector(left) && IsNumeric(right))
                return left;

            return null;
        }

        #endregion

        #region Unary Operators

        /// <summary>
        /// Resolves the result type for a unary operator expression.
        /// </summary>
        /// <param name="op">The operator type</param>
        /// <param name="operandType">The type of the operand</param>
        /// <returns>The result type, or null if invalid</returns>
        public static string ResolveSingleOperatorType(
            GDSingleOperatorType op,
            string operandType)
        {
            if (string.IsNullOrEmpty(operandType))
                return null;
            if (operandType == "Unknown")
                return "Unknown";
            if (operandType == "Variant")
                return "Variant";

            switch (op)
            {
                case GDSingleOperatorType.Negate:
                    // Negation preserves type for numeric and vector types
                    if (IsNumeric(operandType) || IsVector(operandType) || operandType == "Color")
                        return operandType;
                    return null;

                case GDSingleOperatorType.Not:
                case GDSingleOperatorType.Not2:
                    // Logical not always returns bool
                    return "bool";

                case GDSingleOperatorType.BitwiseNegate:
                    // Bitwise negation requires int
                    if (operandType == "int")
                        return "int";
                    return null;

                default:
                    return null;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if a type is a numeric type (int or float).
        /// </summary>
        private static bool IsNumeric(string type)
            => type != null && NumericTypes.Contains(type);

        /// <summary>
        /// Checks if a type is a vector type.
        /// </summary>
        private static bool IsVector(string type)
            => type != null && VectorTypes.Contains(type);

        /// <summary>
        /// Checks if a type is an integer vector type.
        /// </summary>
        private static bool IsIntegerVector(string type)
            => type != null && IntegerVectorTypes.Contains(type);

        /// <summary>
        /// Checks if a type is a transform type.
        /// </summary>
        private static bool IsTransform(string type)
            => type != null && TransformTypes.Contains(type);

        /// <summary>
        /// Checks if a type is a packed array type.
        /// </summary>
        private static bool IsPackedArray(string type)
        {
            if (type == null) return false;
            return type == "PackedByteArray" ||
                   type == "PackedInt32Array" ||
                   type == "PackedInt64Array" ||
                   type == "PackedFloat32Array" ||
                   type == "PackedFloat64Array" ||
                   type == "PackedStringArray" ||
                   type == "PackedVector2Array" ||
                   type == "PackedVector3Array" ||
                   type == "PackedColorArray";
        }

        /// <summary>
        /// Gets the float version of an integer vector type.
        /// </summary>
        private static string GetFloatVectorVersion(string integerVectorType)
        {
            switch (integerVectorType)
            {
                case "Vector2i": return "Vector2";
                case "Vector3i": return "Vector3";
                case "Vector4i": return "Vector4";
                default: return integerVectorType;
            }
        }

        #endregion

        #region GDInferredType Resolution

        /// <summary>
        /// Resolves the result type for array addition operations.
        /// Returns GDInferredType which supports union types for incompatible element types.
        /// </summary>
        /// <param name="leftType">The inferred type of the left array</param>
        /// <param name="rightType">The inferred type of the right array</param>
        /// <returns>The combined array type with union element type if needed</returns>
        public static GDShrapt.Abstractions.GDInferredType? ResolveArrayAddition(
            GDShrapt.Abstractions.GDInferredType? leftType,
            GDShrapt.Abstractions.GDInferredType? rightType)
        {
            return GDShrapt.Abstractions.GDInferredType.CombineArrays(leftType, rightType);
        }

        /// <summary>
        /// Creates a GDInferredType from a GDTypeNode.
        /// </summary>
        public static GDShrapt.Abstractions.GDInferredType? ToInferredType(GDTypeNode? typeNode)
        {
            if (typeNode == null)
                return null;

            if (typeNode is GDArrayTypeNode arrayNode)
            {
                var result = GDShrapt.Abstractions.GDInferredType.Array();
                if (arrayNode.InnerType != null)
                {
                    var innerTypeName = arrayNode.InnerType.BuildName();
                    result.ElementType = new GDShrapt.Abstractions.GDUnionType();
                    result.ElementType.AddType(innerTypeName);
                }
                return result;
            }

            if (typeNode is GDDictionaryTypeNode dictNode)
            {
                var keyType = dictNode.KeyType?.BuildName();
                var valueType = dictNode.ValueType?.BuildName();
                return GDShrapt.Abstractions.GDInferredType.Dictionary(keyType, valueType);
            }

            return GDShrapt.Abstractions.GDInferredType.Simple(typeNode.BuildName());
        }

        #endregion
    }
}
