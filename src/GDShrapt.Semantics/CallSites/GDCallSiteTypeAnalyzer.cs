using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Analyzes argument types at call sites to infer parameter types.
/// Uses flow-sensitive type analysis when available to get precise argument types.
/// </summary>
public class GDCallSiteTypeAnalyzer
{
    private readonly GDCallSiteRegistry _callSiteRegistry;
    private readonly Func<GDScriptFile, GDSemanticModel?> _getSemanticModel;

    /// <summary>
    /// Creates a new call site type analyzer.
    /// </summary>
    /// <param name="callSiteRegistry">The call site registry to query.</param>
    /// <param name="getSemanticModel">Function to get semantic model for a file.</param>
    public GDCallSiteTypeAnalyzer(
        GDCallSiteRegistry callSiteRegistry,
        Func<GDScriptFile, GDSemanticModel?> getSemanticModel)
    {
        _callSiteRegistry = callSiteRegistry ?? throw new ArgumentNullException(nameof(callSiteRegistry));
        _getSemanticModel = getSemanticModel ?? throw new ArgumentNullException(nameof(getSemanticModel));
    }

    /// <summary>
    /// Result of analyzing argument types at call sites.
    /// </summary>
    public class ParameterTypeFromCallSites
    {
        /// <summary>
        /// The parameter name.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// Parameter index (0-based).
        /// </summary>
        public int ParameterIndex { get; }

        /// <summary>
        /// Union of all argument types passed at call sites.
        /// </summary>
        public GDUnionType ArgumentTypes { get; } = new();

        /// <summary>
        /// Sources of each type for navigation.
        /// </summary>
        public List<GDCallSiteArgumentSource> Sources { get; } = new();

        /// <summary>
        /// Number of call sites analyzed.
        /// </summary>
        public int CallSiteCount { get; set; }

        /// <summary>
        /// Number of call sites where argument type was unknown.
        /// </summary>
        public int UnknownTypeCount { get; set; }

        public ParameterTypeFromCallSites(string parameterName, int parameterIndex)
        {
            ParameterName = parameterName;
            ParameterIndex = parameterIndex;
        }

        /// <summary>
        /// Gets the effective type (single type or union).
        /// </summary>
        public string EffectiveType => ArgumentTypes.IsEmpty ? "Variant" : ArgumentTypes.EffectiveType;

        /// <summary>
        /// Gets confidence based on coverage.
        /// </summary>
        public GDTypeConfidence GetConfidence()
        {
            if (CallSiteCount == 0 || ArgumentTypes.IsEmpty)
                return GDTypeConfidence.Unknown;

            // If all call sites have known types - high confidence
            if (UnknownTypeCount == 0)
                return GDTypeConfidence.High;

            // If most call sites have known types - medium confidence
            var knownRatio = (double)(CallSiteCount - UnknownTypeCount) / CallSiteCount;
            if (knownRatio >= 0.7)
                return GDTypeConfidence.Medium;

            // Low coverage - low confidence
            return GDTypeConfidence.Low;
        }
    }

    /// <summary>
    /// Source information for an argument type at a call site.
    /// </summary>
    public class GDCallSiteArgumentSource
    {
        /// <summary>
        /// The inferred type at this call site.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// The call site entry.
        /// </summary>
        public GDCallSiteEntry CallSite { get; }

        /// <summary>
        /// The argument expression AST node.
        /// </summary>
        public GDExpression? ArgumentExpression { get; }

        public GDCallSiteArgumentSource(string typeName, GDCallSiteEntry callSite, GDExpression? argumentExpression)
        {
            TypeName = typeName;
            CallSite = callSite;
            ArgumentExpression = argumentExpression;
        }

        public override string ToString() =>
            $"{TypeName} from {CallSite.SourceFilePath}:{CallSite.Line}";
    }

    /// <summary>
    /// Analyzes all call sites for a method and returns inferred parameter types.
    /// </summary>
    /// <param name="targetClassName">The class containing the method.</param>
    /// <param name="targetMethodName">The method name.</param>
    /// <param name="parameterNames">Names of the parameters (in order).</param>
    /// <param name="fileByPath">Function to get file by path.</param>
    /// <returns>Dictionary of parameter name to inferred type info.</returns>
    public IReadOnlyDictionary<string, ParameterTypeFromCallSites> AnalyzeCallSites(
        string targetClassName,
        string targetMethodName,
        IReadOnlyList<string> parameterNames,
        Func<string, GDScriptFile?> fileByPath)
    {
        var result = new Dictionary<string, ParameterTypeFromCallSites>();

        // Initialize result for each parameter
        for (int i = 0; i < parameterNames.Count; i++)
        {
            result[parameterNames[i]] = new ParameterTypeFromCallSites(parameterNames[i], i);
        }

        // Get all call sites targeting this method
        var callSites = _callSiteRegistry.GetCallersOf(targetClassName, targetMethodName);
        if (callSites.Count == 0)
            return result;

        foreach (var callSite in callSites)
        {
            AnalyzeCallSite(callSite, parameterNames, fileByPath, result);
        }

        return result;
    }

    /// <summary>
    /// Analyzes a single call site and adds argument types to the results.
    /// </summary>
    private void AnalyzeCallSite(
        GDCallSiteEntry callSite,
        IReadOnlyList<string> parameterNames,
        Func<string, GDScriptFile?> fileByPath,
        Dictionary<string, ParameterTypeFromCallSites> result)
    {
        // Get the call expression
        var callExpr = callSite.CallExpression;
        if (callExpr == null)
            return;

        // Get the arguments
        var arguments = callExpr.Parameters?.ToList() ?? new List<GDExpression>();

        // Get the semantic model for the source file
        var sourceFile = fileByPath(callSite.SourceFilePath);
        var semanticModel = sourceFile != null ? _getSemanticModel(sourceFile) : null;

        // Analyze each argument
        for (int i = 0; i < parameterNames.Count && i < arguments.Count; i++)
        {
            var paramName = parameterNames[i];
            var argExpr = arguments[i];
            var paramResult = result[paramName];

            paramResult.CallSiteCount++;

            // Try to get the type of the argument using flow-sensitive analysis
            var argType = GetArgumentType(argExpr, semanticModel, callSite);

            if (string.IsNullOrEmpty(argType) || argType == "Variant")
            {
                paramResult.UnknownTypeCount++;
            }
            else
            {
                paramResult.ArgumentTypes.AddType(argType);
                paramResult.Sources.Add(new GDCallSiteArgumentSource(argType, callSite, argExpr));
            }
        }
    }

    /// <summary>
    /// Gets the type of an argument expression using flow-sensitive analysis if available.
    /// </summary>
    private string? GetArgumentType(GDExpression argExpr, GDSemanticModel? semanticModel, GDCallSiteEntry callSite)
    {
        if (argExpr == null)
            return null;

        // First try using the semantic model (which includes flow-sensitive types)
        if (semanticModel != null)
        {
            var type = semanticModel.GetExpressionType(argExpr);
            if (!string.IsNullOrEmpty(type) && type != "Variant")
                return type;
        }

        // Fall back to simple type inference
        return InferSimpleType(argExpr);
    }

    /// <summary>
    /// Simple type inference for literal expressions.
    /// </summary>
    private string? InferSimpleType(GDExpression? expr)
    {
        return expr switch
        {
            GDNumberExpression num => InferNumberType(num),
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            GDArrayInitializerExpression => "Array",
            GDDictionaryInitializerExpression => "Dictionary",
            GDIdentifierExpression ident => InferIdentifierType(ident),
            // For method calls, we can't easily infer return type here
            _ => null
        };
    }

    /// <summary>
    /// Infers type from an identifier expression (handles null, self, true, false).
    /// </summary>
    private static string? InferIdentifierType(GDIdentifierExpression ident)
    {
        var name = ident.Identifier?.Sequence;
        return name switch
        {
            "null" => "null",
            "self" => "self",
            "true" => "bool",
            "false" => "bool",
            _ => null
        };
    }

    /// <summary>
    /// Infers type from a number expression.
    /// </summary>
    private static string InferNumberType(GDNumberExpression num)
    {
        if (num.Number == null)
            return "int";

        return num.Number.ResolveNumberType() switch
        {
            GDNumberType.LongDecimal or GDNumberType.LongBinary or GDNumberType.LongHexadecimal => "int",
            GDNumberType.Double => "float",
            _ => "int"
        };
    }

    /// <summary>
    /// Creates an inferred parameter type from call site analysis results.
    /// </summary>
    public static GDInferredParameterType ToInferredParameterType(ParameterTypeFromCallSites callSiteResult)
    {
        if (callSiteResult.ArgumentTypes.IsEmpty)
            return GDInferredParameterType.Unknown(callSiteResult.ParameterName);

        var confidence = callSiteResult.GetConfidence();
        var types = callSiteResult.ArgumentTypes.Types.ToList();

        // Format source description
        var sourceDesc = callSiteResult.Sources.Count > 0
            ? $"inferred from {callSiteResult.CallSiteCount} call site(s)"
            : "no call sites found";

        if (types.Count == 1)
        {
            return GDInferredParameterType.Create(
                callSiteResult.ParameterName,
                types[0],
                confidence,
                sourceDesc);
        }

        // Union type from multiple call sites
        return GDInferredParameterType.Union(
            callSiteResult.ParameterName,
            types,
            confidence,
            $"{sourceDesc} with {types.Count} different types");
    }
}
