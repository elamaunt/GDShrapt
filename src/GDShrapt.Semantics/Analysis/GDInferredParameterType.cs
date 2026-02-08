using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Result of parameter type inference.
/// Extends GDInferredType with parameter-specific information like union types
/// and inference sources.
/// </summary>
public class GDInferredParameterType
{
    /// <summary>
    /// The parameter name.
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// The inferred semantic type. For union types, this is the combined type.
    /// </summary>
    public GDSemanticType TypeName { get; }

    /// <summary>
    /// Confidence level of the inference.
    /// </summary>
    public GDTypeConfidence Confidence { get; }

    /// <summary>
    /// Human-readable reason for the inference.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Individual types if this is a union type, null otherwise.
    /// </summary>
    public IReadOnlyList<GDSemanticType>? UnionTypes { get; private init; }

    /// <summary>
    /// Detailed union members with sources and derivability info.
    /// </summary>
    internal IReadOnlyList<GDUnionTypeMember>? UnionMembers { get; private init; }

    /// <summary>
    /// Whether the inferred type is a union of multiple types.
    /// </summary>
    public bool IsUnion => UnionTypes != null && UnionTypes.Count > 1;

    /// <summary>
    /// Whether the type is completely unknown (Variant).
    /// </summary>
    public bool IsUnknown => Confidence == GDTypeConfidence.Unknown;

    /// <summary>
    /// Whether the type inference was derived from duck typing (usage analysis).
    /// </summary>
    public bool IsDuckTyped { get; private init; }

    /// <summary>
    /// The constraints used to infer this type (if duck typed).
    /// </summary>
    public GDParameterConstraints? SourceConstraints { get; private init; }

    /// <summary>
    /// Whether any part of the type can be inferred further.
    /// </summary>
    public bool HasDerivableParts => UnionMembers?.Any(m =>
        m.KeyType?.IsDerivable == true ||
        m.ValueType?.IsDerivable == true) ?? false;

    /// <summary>
    /// Internal constructor for factory methods.
    /// </summary>
    protected GDInferredParameterType(
        string paramName,
        GDSemanticType typeName,
        GDTypeConfidence confidence,
        string? reason = null)
    {
        ParameterName = paramName;
        TypeName = typeName;
        Confidence = confidence;
        Reason = reason;
    }

    #region Factory Methods

    /// <summary>
    /// Creates a parameter type with specified confidence and reason.
    /// </summary>
    internal static GDInferredParameterType Create(
        string paramName,
        string typeName,
        GDTypeConfidence confidence,
        string? reason = null)
        => new(paramName, GDSemanticType.FromRuntimeTypeName(typeName), confidence, reason);

    /// <summary>
    /// Creates an unknown parameter type (Variant fallback).
    /// </summary>
    public static GDInferredParameterType Unknown(string paramName)
        => new(paramName, GDVariantSemanticType.Instance, GDTypeConfidence.Unknown);

    /// <summary>
    /// Creates a parameter type from an explicit type annotation.
    /// </summary>
    public static GDInferredParameterType Declared(string paramName, string typeName)
        => new(paramName, GDSemanticType.FromRuntimeTypeName(typeName), GDTypeConfidence.Certain, "explicit type annotation");

    /// <summary>
    /// Creates a parameter type from duck typing analysis.
    /// </summary>
    public static GDInferredParameterType FromDuckTyping(
        string paramName,
        string typeName,
        GDParameterConstraints? constraints = null)
        => new(paramName, GDSemanticType.FromRuntimeTypeName(typeName), GDTypeConfidence.Medium, "inferred from usage (duck typing)")
        {
            IsDuckTyped = true,
            SourceConstraints = constraints
        };

    /// <summary>
    /// Creates a parameter type from call site data flow.
    /// </summary>
    public static GDInferredParameterType FromCallSite(
        string paramName,
        string typeName,
        string sourceLocation)
        => new(paramName, GDSemanticType.FromRuntimeTypeName(typeName), GDTypeConfidence.High, $"passed from {sourceLocation}");

    /// <summary>
    /// Creates a parameter type from a type check (is operator).
    /// </summary>
    public static GDInferredParameterType FromTypeCheck(
        string paramName,
        string typeName)
        => new(paramName, GDSemanticType.FromRuntimeTypeName(typeName), GDTypeConfidence.High, "narrowed by type check");

    /// <summary>
    /// Creates a union type from multiple candidate types.
    /// </summary>
    public static GDInferredParameterType Union(
        string paramName,
        List<string> types,
        GDTypeConfidence confidence,
        string? reason = null)
    {
        if (types == null || types.Count == 0)
            return Unknown(paramName);

        var semanticTypes = types.Select(t => GDSemanticType.FromRuntimeTypeName(t)).ToList();

        if (semanticTypes.Count == 1)
            return new GDInferredParameterType(paramName, semanticTypes[0], confidence, reason);

        var effectiveType = new GDUnionSemanticType(semanticTypes);
        return new GDInferredParameterType(paramName, effectiveType, confidence, reason ?? "multiple types detected")
        {
            UnionTypes = semanticTypes
        };
    }

    /// <summary>
    /// Creates a union type with detailed member info including sources and derivability.
    /// </summary>
    internal static GDInferredParameterType UnionWithMembers(
        string paramName,
        List<GDUnionTypeMember> members,
        GDTypeConfidence confidence,
        GDParameterConstraints? constraints = null)
    {
        if (members == null || members.Count == 0)
            return Unknown(paramName);

        var semanticTypes = members.Select(m => GDSemanticType.FromRuntimeTypeName(m.FormattedType)).ToList();

        if (members.Count == 1)
        {
            return new GDInferredParameterType(paramName, semanticTypes[0], confidence, "single type from constraints")
            {
                UnionMembers = members,
                UnionTypes = semanticTypes,
                IsDuckTyped = true,
                SourceConstraints = constraints
            };
        }

        var effectiveType = (GDSemanticType)new GDUnionSemanticType(semanticTypes);
        return new GDInferredParameterType(paramName, effectiveType, confidence, "union from type checks")
        {
            UnionTypes = semanticTypes,
            UnionMembers = members,
            IsDuckTyped = true,
            SourceConstraints = constraints
        };
    }

    #endregion

    #region Derivable Navigation

    /// <summary>
    /// Gets all derivable slots with their source nodes for navigation.
    /// </summary>
    public IEnumerable<(string SlotDescription, GDNode? SourceNode, string? Reason)> GetDerivableSlots()
    {
        if (UnionMembers == null)
            yield break;

        foreach (var member in UnionMembers)
        {
            if (member.KeyType?.IsDerivable == true)
            {
                yield return (
                    $"{member.BaseType} key type",
                    member.KeyType.DerivableSourceNode,
                    member.KeyType.DerivableReason);
            }

            if (member.ValueType?.IsDerivable == true)
            {
                yield return (
                    $"{member.BaseType} value type",
                    member.ValueType.DerivableSourceNode,
                    member.ValueType.DerivableReason);
            }
        }
    }

    /// <summary>
    /// Gets the source node for a specific union member.
    /// </summary>
    public GDNode? GetSourceNodeForMember(int memberIndex)
    {
        if (UnionMembers == null || memberIndex < 0 || memberIndex >= UnionMembers.Count)
            return null;

        return UnionMembers[memberIndex].Source?.SourceNode;
    }

    #endregion

    /// <summary>
    /// Converts to the general GDInferredType.
    /// </summary>
    public GDInferredType ToInferredType()
        => GDInferredType.FromType(TypeName.DisplayName, Confidence, Reason);

    /// <summary>
    /// Returns a compact string representation for debugging/display.
    /// </summary>
    public override string ToString()
    {
        var conf = Confidence switch
        {
            GDTypeConfidence.Unknown => "?",
            GDTypeConfidence.Low => "~",
            GDTypeConfidence.Medium => "→",
            GDTypeConfidence.High => "::",
            GDTypeConfidence.Certain => ":",
            _ => ""
        };
        return $"{ParameterName}{conf}{TypeName.DisplayName}";
    }
}
