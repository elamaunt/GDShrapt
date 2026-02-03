using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

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
    /// Whether this container is (or can be) an Array.
    /// For Union types (Array | Dictionary), both IsArray and IsDictionary are true.
    /// </summary>
    public bool IsArray { get; set; } = true;

    /// <summary>
    /// Whether this container is a Union of Array and Dictionary.
    /// When true, both IsArray and IsDictionary should be true.
    /// </summary>
    public bool IsUnion { get; set; }

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

    /// <summary>
    /// Creates a deep clone of this profile.
    /// </summary>
    public GDContainerUsageProfile Clone()
    {
        var clone = new GDContainerUsageProfile(VariableName)
        {
            IsDictionary = IsDictionary,
            IsArray = IsArray,
            IsUnion = IsUnion,
            DeclarationLine = DeclarationLine,
            DeclarationColumn = DeclarationColumn
        };

        clone.ValueUsages.AddRange(ValueUsages);
        clone.KeyUsages.AddRange(KeyUsages);

        return clone;
    }

    /// <summary>
    /// Adds a value usage observation.
    /// </summary>
    public void AddValueUsage(string? inferredType, GDContainerUsageKind kind, GDNode? node)
    {
        if (string.IsNullOrEmpty(inferredType))
            return;

        ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = kind,
            InferredType = inferredType,
            IsHighConfidence = !string.IsNullOrEmpty(inferredType) && inferredType != "Variant",
            Node = node,
            Line = node?.AllTokens.FirstOrDefault()?.StartLine ?? 0,
            Column = node?.AllTokens.FirstOrDefault()?.StartColumn ?? 0
        });
    }

    /// <summary>
    /// Adds a key usage observation (for dictionaries).
    /// </summary>
    public void AddKeyUsage(string? inferredType, GDContainerUsageKind kind, GDNode? node)
    {
        if (string.IsNullOrEmpty(inferredType))
            return;

        KeyUsages.Add(new GDContainerUsageObservation
        {
            Kind = kind,
            InferredType = inferredType,
            IsHighConfidence = !string.IsNullOrEmpty(inferredType) && inferredType != "Variant",
            Node = node,
            Line = node?.AllTokens.FirstOrDefault()?.StartLine ?? 0,
            Column = node?.AllTokens.FirstOrDefault()?.StartColumn ?? 0
        });
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

    /// <summary>
    /// Source file path for cross-file analysis (optional).
    /// </summary>
    public string? SourceFilePath { get; set; }

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
    GetWithDefault,

    /// <summary>
    /// arr.push_back(value)
    /// </summary>
    PushBack,

    /// <summary>
    /// arr.append_array(array)
    /// </summary>
    AppendArray,

    /// <summary>
    /// dict[key] - tracking key type for dictionary access
    /// </summary>
    KeyAssignment,

    /// <summary>
    /// dict.get(key) - tracking dictionary get access
    /// </summary>
    DictionaryGet,

    /// <summary>
    /// arr[index] = value - tracking index assignment
    /// </summary>
    IndexAssignment,

    /// <summary>
    /// arr.merge(other_array) - merging arrays
    /// </summary>
    Merge,

    /// <summary>
    /// Unknown usage pattern
    /// </summary>
    Unknown
}
