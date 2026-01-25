using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Analyzes parameter types from various sources:
/// - Explicit type annotations
/// - Type guards (is checks)
/// - typeof() checks
/// - match statement patterns
/// - assert statements
/// - null checks
/// - Default values
/// </summary>
internal sealed class GDParameterTypeAnalyzer
{
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDTypeInferenceEngine? _typeEngine;

    public GDParameterTypeAnalyzer(IGDRuntimeProvider? runtimeProvider, GDTypeInferenceEngine? typeEngine)
    {
        _runtimeProvider = runtimeProvider;
        _typeEngine = typeEngine;
    }

    /// <summary>
    /// Computes the expected union type for a parameter based on all analysis sources.
    /// </summary>
    /// <param name="param">The parameter declaration.</param>
    /// <param name="method">The method containing the parameter.</param>
    /// <param name="includeUsageConstraints">Whether to include usage constraints analysis (method call patterns, property accesses, etc.).</param>
    public GDUnionType ComputeExpectedTypes(GDParameterDeclaration param, GDMethodDeclaration method, bool includeUsageConstraints = false)
    {
        var union = new GDUnionType();
        var paramName = param.Identifier?.Sequence;

        if (string.IsNullOrEmpty(paramName))
            return union;

        // 1. Explicit type annotation
        AddExplicitTypeAnnotation(param, union);

        // 2. Default value type
        AddDefaultValueType(param, union);

        // 3. Type guards from conditions
        var hasNullCheck = false;
        var typeGuardTypes = new HashSet<string>();

        AnalyzeIfStatements(method, paramName, ref hasNullCheck, typeGuardTypes);
        AnalyzeMatchStatements(method, paramName, typeGuardTypes);
        AnalyzeAssertStatements(method, paramName, typeGuardTypes);

        foreach (var guardType in typeGuardTypes)
        {
            union.AddType(guardType, isHighConfidence: true);
        }

        if (hasNullCheck)
        {
            union.AddType("null", isHighConfidence: true);
        }

        // 4. Usage constraints (method calls, property accesses, etc.)
        if (includeUsageConstraints && _runtimeProvider != null)
        {
            AddUsageConstraints(method, paramName, union);
        }

        return union;
    }

    private void AddUsageConstraints(GDMethodDeclaration method, string paramName, GDUnionType union)
    {
        var constraints = GDParameterUsageAnalyzer.AnalyzeMethod(method, _runtimeProvider);
        if (!constraints.TryGetValue(paramName, out var paramConstraints) || !paramConstraints.HasConstraints)
            return;

        var resolver = new GDParameterTypeResolver(_runtimeProvider ?? new GDGodotTypesProvider());
        var inferredType = resolver.ResolveFromConstraints(paramConstraints);

        if (inferredType.Confidence == GDTypeConfidence.Unknown)
            return;

        // Use individual union types if available, otherwise use TypeName
        if (inferredType.UnionTypes != null && inferredType.UnionTypes.Count > 0)
        {
            foreach (var type in inferredType.UnionTypes)
            {
                if (!string.IsNullOrEmpty(type))
                {
                    union.AddType(type, isHighConfidence: true);
                }
            }
        }
        else if (!string.IsNullOrEmpty(inferredType.TypeName))
        {
            union.AddType(inferredType.TypeName, isHighConfidence: true);
        }
    }

    private void AddExplicitTypeAnnotation(GDParameterDeclaration param, GDUnionType union)
    {
        var explicitType = param.Type?.BuildName();
        if (!string.IsNullOrEmpty(explicitType))
        {
            union.AddType(explicitType, isHighConfidence: true);
        }
    }

    private void AddDefaultValueType(GDParameterDeclaration param, GDUnionType union)
    {
        if (param.DefaultValue != null && _typeEngine != null)
        {
            var defaultType = _typeEngine.InferType(param.DefaultValue);
            if (!string.IsNullOrEmpty(defaultType))
            {
                union.AddType(defaultType, isHighConfidence: true);
            }
        }
    }

    private void AnalyzeIfStatements(
        GDMethodDeclaration method,
        string paramName,
        ref bool hasNullCheck,
        HashSet<string> typeGuardTypes)
    {
        foreach (var ifStmt in method.AllNodes.OfType<GDIfStatement>())
        {
            AnalyzeCondition(ifStmt.IfBranch?.Condition, paramName, ref hasNullCheck, typeGuardTypes);

            if (ifStmt.ElifBranchesList != null)
            {
                foreach (var elif in ifStmt.ElifBranchesList)
                {
                    AnalyzeCondition(elif.Condition, paramName, ref hasNullCheck, typeGuardTypes);
                }
            }
        }
    }

    /// <summary>
    /// Analyzes a condition expression to find type guards and null checks.
    /// Handles: is checks, typeof() checks, null comparisons, and/or, not, brackets.
    /// </summary>
    private void AnalyzeCondition(
        GDExpression? condition,
        string paramName,
        ref bool hasNullCheck,
        HashSet<string> typeGuardTypes,
        bool isNegated = false)
    {
        if (condition == null)
            return;

        if (condition is GDDualOperatorExpression dualOp)
        {
            AnalyzeDualOperator(dualOp, paramName, ref hasNullCheck, typeGuardTypes, isNegated);
        }
        else if (condition is GDSingleOperatorExpression singleOp)
        {
            AnalyzeSingleOperator(singleOp, paramName, ref hasNullCheck, typeGuardTypes, isNegated);
        }
        else if (condition is GDBracketExpression bracketExpr)
        {
            AnalyzeCondition(bracketExpr.InnerExpression, paramName, ref hasNullCheck, typeGuardTypes, isNegated);
        }
    }

    private void AnalyzeDualOperator(
        GDDualOperatorExpression dualOp,
        string paramName,
        ref bool hasNullCheck,
        HashSet<string> typeGuardTypes,
        bool isNegated)
    {
        var opType = dualOp.Operator?.OperatorType;

        // Handle 'param is Type' check
        if (opType == GDDualOperatorType.Is)
        {
            if (IsParameterExpression(dualOp.LeftExpression, paramName))
            {
                var typeName = dualOp.RightExpression?.ToString();
                if (!string.IsNullOrEmpty(typeName))
                {
                    typeGuardTypes.Add(typeName);
                }
            }
        }
        // Handle 'param == null' or 'typeof(param) == TYPE_*'
        else if (opType == GDDualOperatorType.Equal || opType == GDDualOperatorType.NotEqual)
        {
            // Null check
            if (IsParameterExpression(dualOp.LeftExpression, paramName) && IsNullExpression(dualOp.RightExpression) ||
                IsParameterExpression(dualOp.RightExpression, paramName) && IsNullExpression(dualOp.LeftExpression))
            {
                hasNullCheck = true;
            }

            // typeof() check
            var typeofType = TryExtractTypeofCheck(dualOp, paramName);
            if (!string.IsNullOrEmpty(typeofType))
            {
                typeGuardTypes.Add(typeofType);
            }
        }
        // Handle 'and' / 'or'
        else if (opType == GDDualOperatorType.And || opType == GDDualOperatorType.And2 ||
                 opType == GDDualOperatorType.Or || opType == GDDualOperatorType.Or2)
        {
            AnalyzeCondition(dualOp.LeftExpression, paramName, ref hasNullCheck, typeGuardTypes, isNegated);
            AnalyzeCondition(dualOp.RightExpression, paramName, ref hasNullCheck, typeGuardTypes, isNegated);
        }
    }

    private void AnalyzeSingleOperator(
        GDSingleOperatorExpression singleOp,
        string paramName,
        ref bool hasNullCheck,
        HashSet<string> typeGuardTypes,
        bool isNegated)
    {
        var isNotOperator = singleOp.OperatorType == GDSingleOperatorType.Not ||
                            singleOp.OperatorType == GDSingleOperatorType.Not2;

        var newIsNegated = isNotOperator ? !isNegated : isNegated;
        AnalyzeCondition(singleOp.TargetExpression, paramName, ref hasNullCheck, typeGuardTypes, newIsNegated);
    }

    /// <summary>
    /// Analyzes match statements for pattern-based type inference.
    /// </summary>
    private void AnalyzeMatchStatements(
        GDMethodDeclaration method,
        string paramName,
        HashSet<string> inferredTypes)
    {
        foreach (var matchStmt in method.AllNodes.OfType<GDMatchStatement>())
        {
            if (!IsParameterExpression(matchStmt.Value, paramName))
                continue;

            var cases = matchStmt.Cases;
            if (cases == null)
                continue;

            foreach (var matchCase in cases.OfType<GDMatchCaseDeclaration>())
            {
                var conditions = matchCase.Conditions;
                if (conditions == null)
                    continue;

                foreach (var pattern in conditions)
                {
                    var patternType = InferTypeFromMatchPattern(pattern);
                    if (!string.IsNullOrEmpty(patternType))
                    {
                        inferredTypes.Add(patternType);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Analyzes assert statements for type inference.
    /// </summary>
    private void AnalyzeAssertStatements(
        GDMethodDeclaration method,
        string paramName,
        HashSet<string> typeGuardTypes)
    {
        foreach (var call in method.AllNodes.OfType<GDCallExpression>())
        {
            if (!call.IsAssert())
                continue;

            var arg = call.Parameters?.FirstOrDefault();
            if (arg is GDDualOperatorExpression dualOp &&
                dualOp.Operator?.OperatorType == GDDualOperatorType.Is &&
                IsParameterExpression(dualOp.LeftExpression, paramName))
            {
                var typeName = dualOp.RightExpression?.ToString();
                if (!string.IsNullOrEmpty(typeName))
                {
                    typeGuardTypes.Add(typeName);
                }
            }
        }
    }

    #region typeof() Analysis

    private string? TryExtractTypeofCheck(GDDualOperatorExpression dualOp, string paramName)
    {
        var leftExpr = dualOp.LeftExpression;
        var rightExpr = dualOp.RightExpression;

        if (IsTypeofCallOnParam(leftExpr, paramName))
        {
            return ExtractTypeFromTypeConstant(rightExpr);
        }

        if (IsTypeofCallOnParam(rightExpr, paramName))
        {
            return ExtractTypeFromTypeConstant(leftExpr);
        }

        return null;
    }

    private static bool IsTypeofCallOnParam(GDExpression? expr, string paramName)
    {
        return expr is GDCallExpression call &&
               call.IsCallTo(GDExpressionHelper.Typeof) &&
               IsParameterExpression(call.Parameters?.FirstOrDefault(), paramName);
    }

    private static string? ExtractTypeFromTypeConstant(GDExpression? expr)
    {
        if (expr is GDIdentifierExpression typeId)
        {
            var constantName = typeId.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(constantName))
            {
                return MapTypeConstantToTypeName(constantName);
            }
        }
        return null;
    }

    #endregion

    #region Match Pattern Analysis

    private static string? InferTypeFromMatchPattern(GDExpression? pattern)
    {
        return pattern switch
        {
            GDNumberExpression numExpr => InferNumberType(numExpr),
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            GDArrayInitializerExpression => "Array",
            GDDictionaryInitializerExpression => "Dictionary",
            GDIdentifierExpression idExpr when idExpr.Identifier?.Sequence == "null" => "null",
            GDMatchCaseVariableExpression => null,
            GDMatchDefaultOperatorExpression => null,
            _ => null
        };
    }

    private static string InferNumberType(GDNumberExpression numExpr)
    {
        var numberType = numExpr.Number?.ResolveNumberType();
        return numberType == GDNumberType.Double ? "float" : "int";
    }

    #endregion

    #region Helper Methods

    private static bool IsParameterExpression(GDExpression? expr, string paramName)
    {
        return expr is GDIdentifierExpression idExpr &&
               idExpr.Identifier?.Sequence == paramName;
    }

    private static bool IsNullExpression(GDExpression? expr)
    {
        return expr is GDIdentifierExpression idExpr &&
               (idExpr.Identifier?.Sequence == "null" || idExpr.Identifier?.Sequence == "nil");
    }

    #endregion

    #region TYPE_* Constant Mapping

    /// <summary>
    /// Maps TYPE_* constant to GDScript type name.
    /// Supports all Godot TYPE_* constants including Vector2I, Vector3I, PackedArrays, etc.
    /// </summary>
    internal static string? MapTypeConstantToTypeName(string typeConstant)
    {
        if (string.IsNullOrEmpty(typeConstant) || !typeConstant.StartsWith("TYPE_"))
            return null;

        var suffix = typeConstant.Substring(5);

        return suffix switch
        {
            "NIL" => "null",
            "BOOL" => "bool",
            "INT" => "int",
            "FLOAT" => "float",
            "STRING" => "String",
            "STRING_NAME" => "StringName",
            "NODE_PATH" => "NodePath",
            "OBJECT" => "Object",
            "RID" => "Rid",
            "MAX" => null,
            var s when s.StartsWith("PACKED_") => ConvertPackedArrayName(s),
            _ => ConvertSnakeToPascalCase(suffix)
        };
    }

    private static string ConvertSnakeToPascalCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase))
            return snakeCase;

        var parts = snakeCase.Split('_');
        var result = new System.Text.StringBuilder();

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
                continue;

            var i = 0;
            while (i < part.Length && char.IsLetter(part[i]))
                i++;

            if (i > 0)
            {
                result.Append(char.ToUpper(part[0]));
                if (i > 1)
                    result.Append(part.Substring(1, i - 1).ToLower());
            }

            if (i < part.Length)
                result.Append(part.Substring(i).ToUpper());
        }

        return result.ToString();
    }

    private static string ConvertPackedArrayName(string name)
    {
        var withoutPacked = name.Substring(7);
        return "Packed" + ConvertSnakeToPascalCase(withoutPacked);
    }

    #endregion
}
