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

    /// <summary>
    /// Context object for condition analysis, reducing parameter passing overhead.
    /// </summary>
    private sealed class ConditionAnalysisContext
    {
        public string ParamName { get; }
        public HashSet<string> TypeGuardTypes { get; }
        public bool HasNullCheck { get; set; }
        public bool IsNegated { get; set; }

        public ConditionAnalysisContext(string paramName, HashSet<string> typeGuardTypes)
        {
            ParamName = paramName;
            TypeGuardTypes = typeGuardTypes;
        }
    }

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
    /// <param name="excludeTypeGuards">When true, skips type guard analysis (is checks, typeof, match, assert).
    /// Use this when the flow analyzer already handles branch-aware narrowing.</param>
    public GDUnionType ComputeExpectedTypes(GDParameterDeclaration param, GDMethodDeclaration method, bool includeUsageConstraints = false, bool excludeTypeGuards = false)
    {
        var union = new GDUnionType();
        var paramName = param.Identifier?.Sequence;

        if (string.IsNullOrEmpty(paramName))
            return union;

        // 1. Explicit type annotation
        AddExplicitTypeAnnotation(param, union);

        // 2. Default value type
        AddDefaultValueType(param, union);

        // 3. Type guards from conditions (skip when flow analyzer handles narrowing)
        if (!excludeTypeGuards)
        {
            var context = new ConditionAnalysisContext(paramName, new HashSet<string>());

            AnalyzeIfStatements(method, context);
            AnalyzeMatchStatements(method, paramName, context.TypeGuardTypes);
            AnalyzeAssertStatements(method, paramName, context.TypeGuardTypes);

            foreach (var guardType in context.TypeGuardTypes)
            {
                union.AddTypeName(guardType, isHighConfidence: true);
            }

            if (context.HasNullCheck)
            {
                union.AddTypeName("null", isHighConfidence: true);
            }
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
                union.AddType(type, isHighConfidence: true);
            }
        }
        else if (!inferredType.TypeName.IsVariant)
        {
            union.AddType(inferredType.TypeName, isHighConfidence: true);
        }
    }

    private void AddExplicitTypeAnnotation(GDParameterDeclaration param, GDUnionType union)
    {
        var explicitType = param.Type?.BuildName();
        if (!string.IsNullOrEmpty(explicitType))
        {
            union.AddTypeName(explicitType, isHighConfidence: true);
        }
    }

    private void AddDefaultValueType(GDParameterDeclaration param, GDUnionType union)
    {
        if (param.DefaultValue != null && _typeEngine != null)
        {
            var defaultType = _typeEngine.InferSemanticType(param.DefaultValue);
            if (!defaultType.IsVariant)
            {
                union.AddType(defaultType, isHighConfidence: true);
            }
        }
    }

    private void AnalyzeIfStatements(GDMethodDeclaration method, ConditionAnalysisContext context)
    {
        foreach (var ifStmt in method.AllNodes.OfType<GDIfStatement>())
        {
            AnalyzeCondition(ifStmt.IfBranch?.Condition, context);

            if (ifStmt.ElifBranchesList != null)
            {
                foreach (var elif in ifStmt.ElifBranchesList)
                {
                    AnalyzeCondition(elif.Condition, context);
                }
            }
        }
    }

    /// <summary>
    /// Analyzes a condition expression to find type guards and null checks.
    /// Handles: is checks, typeof() checks, null comparisons, and/or, not, brackets.
    /// </summary>
    private void AnalyzeCondition(GDExpression? condition, ConditionAnalysisContext context)
    {
        if (condition == null)
            return;

        if (condition is GDDualOperatorExpression dualOp)
        {
            AnalyzeDualOperator(dualOp, context);
        }
        else if (condition is GDSingleOperatorExpression singleOp)
        {
            AnalyzeSingleOperator(singleOp, context);
        }
        else if (condition is GDBracketExpression bracketExpr)
        {
            AnalyzeCondition(bracketExpr.InnerExpression, context);
        }
    }

    private void AnalyzeDualOperator(GDDualOperatorExpression dualOp, ConditionAnalysisContext context)
    {
        var opType = dualOp.Operator?.OperatorType;

        // Handle 'param is Type' check
        if (opType == GDDualOperatorType.Is)
        {
            if (IsParameterExpression(dualOp.LeftExpression, context.ParamName))
            {
                var typeName = ExtractTypeNameFromExpression(dualOp.RightExpression);
                if (!string.IsNullOrEmpty(typeName))
                {
                    context.TypeGuardTypes.Add(typeName);
                }
            }
        }
        // Handle 'param == null' or 'typeof(param) == TYPE_*'
        else if (opType == GDDualOperatorType.Equal || opType == GDDualOperatorType.NotEqual)
        {
            // Null check
            if (IsParameterExpression(dualOp.LeftExpression, context.ParamName) && IsNullExpression(dualOp.RightExpression) ||
                IsParameterExpression(dualOp.RightExpression, context.ParamName) && IsNullExpression(dualOp.LeftExpression))
            {
                context.HasNullCheck = true;
            }

            // typeof() check
            var typeofType = TryExtractTypeofCheck(dualOp, context.ParamName);
            if (!string.IsNullOrEmpty(typeofType))
            {
                context.TypeGuardTypes.Add(typeofType);
            }
        }
        // Handle 'and' / 'or'
        else if (opType == GDDualOperatorType.And || opType == GDDualOperatorType.And2 ||
                 opType == GDDualOperatorType.Or || opType == GDDualOperatorType.Or2)
        {
            AnalyzeCondition(dualOp.LeftExpression, context);
            AnalyzeCondition(dualOp.RightExpression, context);
        }
    }

    private void AnalyzeSingleOperator(GDSingleOperatorExpression singleOp, ConditionAnalysisContext context)
    {
        var isNotOperator = singleOp.OperatorType == GDSingleOperatorType.Not ||
                            singleOp.OperatorType == GDSingleOperatorType.Not2;

        if (isNotOperator)
            context.IsNegated = !context.IsNegated;

        AnalyzeCondition(singleOp.TargetExpression, context);

        if (isNotOperator)
            context.IsNegated = !context.IsNegated; // Restore
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
                var typeName = ExtractTypeNameFromExpression(dualOp.RightExpression);
                if (!string.IsNullOrEmpty(typeName))
                {
                    typeGuardTypes.Add(typeName);
                }
            }
        }
    }

    /// <summary>
    /// Extracts a type name from an expression.
    /// Handles identifiers, member expressions, and falls back to ToString() for complex cases.
    /// </summary>
    private static string? ExtractTypeNameFromExpression(GDExpression? expr)
    {
        if (expr == null)
            return null;

        // Simple identifier (e.g., Node, String, int)
        if (expr is GDIdentifierExpression identExpr)
            return identExpr.Identifier?.Sequence;

        // Member access (e.g., MyClass.InnerType)
        if (expr is GDMemberOperatorExpression memberExpr)
        {
            var caller = ExtractTypeNameFromExpression(memberExpr.CallerExpression);
            var member = memberExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(caller) && !string.IsNullOrEmpty(member))
                return $"{caller}.{member}";
            return member;
        }

        // Fallback for complex expressions
        return expr.ToString();
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
            GDStringExpression => GDWellKnownTypes.Strings.String,
            GDBoolExpression => GDWellKnownTypes.Numeric.Bool,
            GDArrayInitializerExpression => GDWellKnownTypes.Containers.Array,
            GDDictionaryInitializerExpression => GDWellKnownTypes.Containers.Dictionary,
            GDIdentifierExpression idExpr when idExpr.Identifier?.Sequence == GDWellKnownTypes.Null => GDWellKnownTypes.Null,
            GDMatchCaseVariableExpression => null,
            GDMatchDefaultOperatorExpression => null,
            _ => null
        };
    }

    private static string InferNumberType(GDNumberExpression numExpr)
    {
        var numberType = numExpr.Number?.ResolveNumberType();
        return numberType == GDNumberType.Double ? GDWellKnownTypes.Numeric.Float : GDWellKnownTypes.Numeric.Int;
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
            "NIL" => GDWellKnownTypes.Null,
            "BOOL" => GDWellKnownTypes.Numeric.Bool,
            "INT" => GDWellKnownTypes.Numeric.Int,
            "FLOAT" => GDWellKnownTypes.Numeric.Float,
            "STRING" => GDWellKnownTypes.Strings.String,
            "STRING_NAME" => GDWellKnownTypes.Strings.StringName,
            "NODE_PATH" => GDWellKnownTypes.Other.NodePath,
            "OBJECT" => GDWellKnownTypes.Object,
            "RID" => GDWellKnownTypes.Other.RID,
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
