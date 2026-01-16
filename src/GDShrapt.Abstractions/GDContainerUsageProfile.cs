using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Profile of a local container variable's usage.
/// Tracks all values added to infer element types.
/// </summary>
public class GDContainerUsageProfile
{
    /// <summary>
    /// Variable name of the container.
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// Whether this container is a Dictionary (vs Array).
    /// </summary>
    public bool IsDictionary { get; set; }

    /// <summary>
    /// All value usages (elements added to the container).
    /// </summary>
    public List<GDContainerUsageObservation> ValueUsages { get; } = new();

    /// <summary>
    /// All key usages (for Dictionary only).
    /// </summary>
    public List<GDContainerUsageObservation> KeyUsages { get; } = new();

    /// <summary>
    /// Line where the container was declared.
    /// </summary>
    public int DeclarationLine { get; set; }

    /// <summary>
    /// Column where the container was declared.
    /// </summary>
    public int DeclarationColumn { get; set; }

    public GDContainerUsageProfile(string variableName)
    {
        VariableName = variableName;
    }

    /// <summary>
    /// Computes the inferred container element type using GDUnionType.
    /// </summary>
    public GDContainerElementType ComputeInferredType()
    {
        var result = new GDContainerElementType { IsDictionary = IsDictionary };

        // Fill ElementUnionType from value usages
        foreach (var usage in ValueUsages)
        {
            if (!string.IsNullOrEmpty(usage.InferredType))
            {
                result.ElementUnionType.AddType(usage.InferredType, usage.IsHighConfidence);
            }
        }

        // Fill KeyUnionType for Dictionary
        if (IsDictionary)
        {
            result.KeyUnionType = new GDUnionType();
            foreach (var usage in KeyUsages)
            {
                if (!string.IsNullOrEmpty(usage.InferredType))
                {
                    result.KeyUnionType.AddType(usage.InferredType, usage.IsHighConfidence);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Number of value usages.
    /// </summary>
    public int ValueUsageCount => ValueUsages.Count;

    /// <summary>
    /// Number of key usages (Dictionary only).
    /// </summary>
    public int KeyUsageCount => KeyUsages.Count;

    public override string ToString()
    {
        var inferredType = ComputeInferredType();
        return $"Container '{VariableName}': {inferredType} ({ValueUsageCount} values)";
    }
}

/// <summary>
/// A single usage of a container (element added).
/// </summary>
public class GDContainerUsageObservation
{
    /// <summary>
    /// Kind of usage (append, insert, index assign, etc.).
    /// </summary>
    public GDContainerUsageKind Kind { get; set; }

    /// <summary>
    /// Inferred type of the value/key.
    /// </summary>
    public string? InferredType { get; set; }

    /// <summary>
    /// Whether this type was inferred with high confidence.
    /// </summary>
    public bool IsHighConfidence { get; set; }

    /// <summary>
    /// AST node of the usage (for navigation).
    /// </summary>
    public GDNode? Node { get; set; }

    /// <summary>
    /// Line number of the usage (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Column number of the usage (1-based).
    /// </summary>
    public int Column { get; set; }

    public override string ToString()
    {
        var confidence = IsHighConfidence ? "High" : "Low";
        return $"[Line {Line}] {Kind}: {InferredType ?? "unknown"} ({confidence})";
    }
}

/// <summary>
/// Kind of container usage.
/// </summary>
public enum GDContainerUsageKind
{
    /// <summary>
    /// Array initialization: [1, 2, 3]
    /// </summary>
    Initialization,

    /// <summary>
    /// arr.append(value), arr.push_back(value)
    /// </summary>
    Append,

    /// <summary>
    /// arr.push_front(value)
    /// </summary>
    PushFront,

    /// <summary>
    /// arr.insert(index, value)
    /// </summary>
    Insert,

    /// <summary>
    /// arr[index] = value, dict[key] = value
    /// </summary>
    IndexAssign,

    /// <summary>
    /// arr.fill(value)
    /// </summary>
    Fill,

    /// <summary>
    /// dict.get(key, default) - infers from default value
    /// </summary>
    GetWithDefault
}
