using GDShrapt.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Resolves parameter types by merging:
/// 1. Duck typing constraints (from usage analysis)
/// 2. Type narrowing (from 'is' checks)
/// </summary>
internal class GDParameterTypeResolver
{
    private readonly IGDRuntimeProvider _runtimeProvider;
    private readonly GDTypeConfidence _minDuckTypeConfidence;
    private HashSet<string>? _cachedCommonArrayMethods;

    // Types that are iterable in GDScript
    private static readonly HashSet<string> IterableTypes = new()
    {
        GDWellKnownTypes.Containers.Array, GDWellKnownTypes.Containers.Dictionary, GDWellKnownTypes.Strings.String,
        GDWellKnownTypes.PackedArrays.PackedByteArray, GDWellKnownTypes.PackedArrays.PackedInt32Array, GDWellKnownTypes.PackedArrays.PackedInt64Array,
        GDWellKnownTypes.PackedArrays.PackedFloat32Array, GDWellKnownTypes.PackedArrays.PackedFloat64Array, GDWellKnownTypes.PackedArrays.PackedStringArray,
        GDWellKnownTypes.PackedArrays.PackedVector2Array, GDWellKnownTypes.PackedArrays.PackedVector3Array, GDWellKnownTypes.PackedArrays.PackedColorArray
    };

    // Types that support indexing
    private static readonly HashSet<string> IndexableTypes = new()
    {
        GDWellKnownTypes.Containers.Array, GDWellKnownTypes.Containers.Dictionary, GDWellKnownTypes.Strings.String,
        GDWellKnownTypes.PackedArrays.PackedByteArray, GDWellKnownTypes.PackedArrays.PackedInt32Array, GDWellKnownTypes.PackedArrays.PackedInt64Array,
        GDWellKnownTypes.PackedArrays.PackedFloat32Array, GDWellKnownTypes.PackedArrays.PackedFloat64Array, GDWellKnownTypes.PackedArrays.PackedStringArray,
        GDWellKnownTypes.PackedArrays.PackedVector2Array, GDWellKnownTypes.PackedArrays.PackedVector3Array, GDWellKnownTypes.PackedArrays.PackedColorArray,
        GDWellKnownTypes.Vectors.Vector2, GDWellKnownTypes.Vectors.Vector2i, GDWellKnownTypes.Vectors.Vector3, GDWellKnownTypes.Vectors.Vector3i, GDWellKnownTypes.Vectors.Vector4, GDWellKnownTypes.Vectors.Vector4i,
        GDWellKnownTypes.Other.Color, GDWellKnownTypes.Geometry.Basis, GDWellKnownTypes.Geometry.Transform2D, GDWellKnownTypes.Geometry.Transform3D, GDWellKnownTypes.Geometry.Projection
    };

    /// <summary>
    /// Creates a new parameter type resolver.
    /// </summary>
    public GDParameterTypeResolver(
        IGDRuntimeProvider runtimeProvider,
        GDTypeConfidence minDuckTypeConfidence = GDTypeConfidence.Medium)
    {
        _runtimeProvider = runtimeProvider;
        _minDuckTypeConfidence = minDuckTypeConfidence;
    }

    /// <summary>
    /// Resolves parameter type from constraints.
    /// </summary>
    public GDInferredParameterType ResolveFromConstraints(GDParameterConstraints constraints)
    {
        if (!constraints.HasConstraints)
            return GDInferredParameterType.Unknown(constraints.ParameterName);

        var candidates = new List<(string Type, GDTypeConfidence Confidence, string Reason)>();

        // Duck typing -> find matching types with per-type confidence
        var duckMatches = FindDuckTypeMatches(constraints);
        foreach (var (type, confidence) in duckMatches)
        {
            candidates.Add((type, confidence, "duck typing from usage"));
        }

        // Type checks - these are high confidence
        foreach (var possibleType in constraints.PossibleTypes)
        {
            candidates.Add((possibleType.DisplayName, GDTypeConfidence.High, "type check"));
        }

        return MergeCandidates(constraints.ParameterName, candidates, constraints.ExcludedTypes, constraints);
    }

    /// <summary>
    /// Finds types that match the duck typing constraints using TypesMap data.
    /// Returns types with per-member confidence scores.
    /// </summary>
    private List<(string Type, GDTypeConfidence Confidence)> FindDuckTypeMatches(GDParameterConstraints constraints)
    {
        var matches = new List<(string Type, GDTypeConfidence Confidence)>();

        // If we have structural constraints only, return generic types
        if (constraints.RequiredMethods.Count == 0 && constraints.RequiredProperties.Count == 0)
        {
            if (constraints.IsIterable && constraints.IsIndexable)
                matches.Add((GDWellKnownTypes.Containers.Array, GDTypeConfidence.Medium));
            else if (constraints.IsIterable)
                matches.Add((GDWellKnownTypes.Containers.Array, GDTypeConfidence.Medium));
            else if (constraints.IsIndexable)
                matches.Add((GDWellKnownTypes.Containers.Array, GDTypeConfidence.Medium));
            return matches;
        }

        // Try to use TypesMap for method-based duck typing
        GDGodotTypesProvider? typesProvider = null;
        GDProjectTypesProvider? projectProvider = null;

        if (_runtimeProvider is GDGodotTypesProvider godotProvider)
        {
            typesProvider = godotProvider;
        }
        else if (_runtimeProvider is GDCompositeRuntimeProvider compositeProvider)
        {
            typesProvider = compositeProvider.GodotTypesProvider;
            projectProvider = compositeProvider.ProjectTypesProvider;
        }

        if (typesProvider != null && constraints.RequiredMethods.Count > 0)
        {
            var methodMatches = FindTypesMatchingAllMethods(typesProvider, projectProvider, constraints.RequiredMethods);
            if (methodMatches.Count > 0)
            {
                // Score each candidate and filter by minConfidence
                var scored = methodMatches
                    .Select(t => (Type: t, Confidence: ScoreCandidateConfidence(typesProvider, t, constraints)))
                    .ToList();

                var filtered = scored
                    .Where(s => s.Confidence <= _minDuckTypeConfidence)
                    .ToList();

                // Fallback: if filtering removes ALL types, keep originals with their scores
                if (filtered.Count == 0)
                    filtered = scored;

                filtered = DeduplicatePackedArrays(typesProvider, filtered, constraints);

                matches.AddRange(filtered.Select(s => (s.Type, s.Confidence)).Distinct());
                return matches;
            }
        }

        // Fallback to hardcoded patterns if TypesMap lookup fails or is unavailable
        if (constraints.RequiredMethods.Contains("get"))
            matches.Add((GDWellKnownTypes.Containers.Dictionary, GDTypeConfidence.Medium));

        if (constraints.RequiredMethods.Contains("append") ||
            constraints.RequiredMethods.Contains("push_back") ||
            constraints.RequiredMethods.Contains("pop_back"))
            matches.Add((GDWellKnownTypes.Containers.Array, GDTypeConfidence.Medium));

        if (constraints.RequiredMethods.Contains("substr") ||
            constraints.RequiredMethods.Contains("find") ||
            constraints.RequiredMethods.Contains("split"))
            matches.Add((GDWellKnownTypes.Strings.String, GDTypeConfidence.Medium));

        // Check for Node methods
        if (constraints.RequiredMethods.Contains("get_node") ||
            constraints.RequiredMethods.Contains("add_child") ||
            constraints.RequiredMethods.Contains("get_children"))
            matches.Add((GDWellKnownTypes.Node, GDTypeConfidence.Medium));

        // Check for common properties
        if (constraints.RequiredProperties.Contains("x") &&
            constraints.RequiredProperties.Contains("y"))
        {
            if (constraints.RequiredProperties.Contains("z"))
                matches.Add((GDWellKnownTypes.Vectors.Vector3, GDTypeConfidence.Medium));
            else
                matches.Add((GDWellKnownTypes.Vectors.Vector2, GDTypeConfidence.Medium));
        }

        if (constraints.RequiredProperties.Contains("position") ||
            constraints.RequiredProperties.Contains("rotation"))
            matches.Add((GDWellKnownTypes.Nodes.Node2D, GDTypeConfidence.Medium));

        // If still no matches but has structural constraints, fall back
        if (matches.Count == 0)
        {
            if (constraints.IsIterable)
                matches.Add((GDWellKnownTypes.Containers.Array, GDTypeConfidence.Medium));
            else if (constraints.IsIndexable)
                matches.Add((GDWellKnownTypes.Containers.Array, GDTypeConfidence.Medium));
        }

        return matches;
    }

    /// <summary>
    /// Finds types that have ALL required methods using TypesMap data.
    /// Intersects the sets of types for each method to find common types.
    /// Checks both Godot types and project types.
    /// </summary>
    private static List<string> FindTypesMatchingAllMethods(
        GDGodotTypesProvider? godotProvider,
        GDProjectTypesProvider? projectProvider,
        HashSet<string> requiredMethods)
    {
        if (requiredMethods.Count == 0)
            return new List<string>();

        HashSet<string>? intersection = null;

        foreach (var methodName in requiredMethods)
        {
            var typesWithMethod = new HashSet<string>();

            // Check Godot types
            if (godotProvider != null)
            {
                foreach (var t in godotProvider.FindTypesWithMethod(methodName))
                    typesWithMethod.Add(t);
            }

            // Check project types
            if (projectProvider != null)
            {
                foreach (var t in projectProvider.FindTypesWithMethod(methodName))
                    typesWithMethod.Add(t);
            }

            if (typesWithMethod.Count == 0)
            {
                // If any method has no types, intersection is empty
                return new List<string>();
            }

            if (intersection == null)
            {
                intersection = typesWithMethod;
            }
            else
            {
                intersection.IntersectWith(typesWithMethod);
                if (intersection.Count == 0)
                    return new List<string>();
            }
        }

        return intersection?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Merges candidate types into a final result.
    /// </summary>
    private GDInferredParameterType MergeCandidates(
        string paramName,
        List<(string Type, GDTypeConfidence Confidence, string Reason)> candidates,
        HashSet<GDSemanticType> excluded,
        GDParameterConstraints? sourceConstraints)
    {
        // Filter out excluded types (compare by DisplayName since candidates use string)
        var excludedNames = new HashSet<string>(excluded.Select(e => e.DisplayName));
        candidates = candidates
            .Where(c => !excludedNames.Contains(c.Type))
            .ToList();

        if (candidates.Count == 0)
            return GDInferredParameterType.Unknown(paramName);

        // Group by type
        var grouped = candidates.GroupBy(c => c.Type).ToList();

        // Build union members with detailed info
        var unionMembers = grouped.Select(g => BuildUnionMember(g.Key, g.First().Confidence, sourceConstraints)).ToList();

        // Format types with per-type constraints if available
        var formattedTypes = unionMembers.Select(m => m.FormattedType).ToList();

        if (formattedTypes.Count == 1)
        {
            // Single type - use highest confidence
            var best = candidates.OrderBy(c => c.Confidence).First();

            // Still use UnionWithMembers to preserve derivability info
            return GDInferredParameterType.UnionWithMembers(paramName, unionMembers, best.Confidence, sourceConstraints);
        }

        // Multiple types - return Union with members
        var minConfidence = candidates.Min(c => c.Confidence);
        return GDInferredParameterType.UnionWithMembers(paramName, unionMembers, minConfidence, sourceConstraints);
    }

    /// <summary>
    /// Builds a union member with detailed type info, sources, and derivability markers.
    /// </summary>
    private GDUnionTypeMember BuildUnionMember(string baseType, GDTypeConfidence confidence, GDParameterConstraints? constraints)
    {
        if (constraints == null)
        {
            return new GDUnionTypeMember
            {
                BaseType = baseType,
                FormattedType = baseType,
                Confidence = confidence
            };
        }

        // Try to get per-type constraints (look up by DisplayName)
        GDTypeSpecificConstraints? typeSpecific = null;
        var semanticKey = constraints.TypeConstraints.Keys.FirstOrDefault(k => k.DisplayName == baseType);
        if (semanticKey != null)
            constraints.TypeConstraints.TryGetValue(semanticKey, out typeSpecific);

        var source = typeSpecific?.InferenceSources.FirstOrDefault();

        // Build key and value slots
        GDGenericTypeSlot? keySlot = null;
        GDGenericTypeSlot? valueSlot = null;

        if (baseType == GDWellKnownTypes.Containers.Dictionary)
        {
            keySlot = BuildKeySlot(typeSpecific, constraints);
            valueSlot = BuildValueSlot(typeSpecific, constraints);
        }
        else if (baseType == GDWellKnownTypes.Containers.Array)
        {
            valueSlot = BuildValueSlot(typeSpecific, constraints);
        }

        // Format the type
        var formattedType = typeSpecific?.FormatFullType() ?? FormatTypeWithElements(baseType, constraints);

        return new GDUnionTypeMember
        {
            BaseType = baseType,
            FormattedType = formattedType,
            Source = source,
            KeyType = keySlot,
            ValueType = valueSlot,
            Confidence = confidence
        };
    }

    /// <summary>
    /// Builds a key type slot with derivability info.
    /// </summary>
    private GDGenericTypeSlot BuildKeySlot(GDTypeSpecificConstraints? typeSpecific, GDParameterConstraints constraints)
    {
        var keyTypes = typeSpecific?.KeyTypes ?? constraints.KeyTypes;
        var sources = typeSpecific?.KeyTypeSources ?? new List<GDTypeInferenceSource>();

        if (keyTypes.Count == 0)
        {
            if (typeSpecific?.KeyIsDerivable == true)
            {
                return GDGenericTypeSlot.Derivable(
                    typeSpecific.KeyDerivableNode,
                    typeSpecific.KeyDerivableReason);
            }
            return GDGenericTypeSlot.Variant();
        }

        var typeStr = keyTypes.Count == 1
            ? keyTypes.First().DisplayName
            : string.Join(" | ", keyTypes.Select(t => t.DisplayName).OrderBy(t => t));

        return new GDGenericTypeSlot
        {
            TypeName = typeStr,
            IsDerivable = typeSpecific?.KeyIsDerivable ?? false,
            DerivableSourceNode = typeSpecific?.KeyDerivableNode,
            DerivableReason = typeSpecific?.KeyDerivableReason,
            Sources = sources.ToList(),
            Confidence = GDTypeConfidence.Medium
        };
    }

    /// <summary>
    /// Builds a value type slot with derivability info.
    /// </summary>
    private GDGenericTypeSlot BuildValueSlot(GDTypeSpecificConstraints? typeSpecific, GDParameterConstraints constraints)
    {
        var elemTypes = typeSpecific?.ElementTypes ?? constraints.ElementTypes;
        var sources = typeSpecific?.ElementTypeSources ?? new List<GDTypeInferenceSource>();

        if (elemTypes.Count == 0)
        {
            if (typeSpecific?.ValueIsDerivable == true)
            {
                return GDGenericTypeSlot.Derivable(
                    typeSpecific.ValueDerivableNode,
                    typeSpecific.ValueDerivableReason);
            }
            return GDGenericTypeSlot.Variant();
        }

        var typeStr = elemTypes.Count == 1
            ? elemTypes.First().DisplayName
            : string.Join(" | ", elemTypes.Select(t => t.DisplayName).OrderBy(t => t));

        return new GDGenericTypeSlot
        {
            TypeName = typeStr,
            IsDerivable = typeSpecific?.ValueIsDerivable ?? false,
            DerivableSourceNode = typeSpecific?.ValueDerivableNode,
            DerivableReason = typeSpecific?.ValueDerivableReason,
            Sources = sources.ToList(),
            Confidence = GDTypeConfidence.Medium
        };
    }

    /// <summary>
    /// Formats a type using per-type constraints if available, falling back to global constraints.
    /// This ensures Dictionary gets its specific key types and Array gets its specific element types.
    /// </summary>
    private string FormatTypeWithPerTypeConstraints(string baseType, GDParameterConstraints? constraints)
    {
        if (constraints == null)
            return baseType;

        // Try to use per-type constraints first (look up by DisplayName)
        var semanticKey = constraints.TypeConstraints.Keys.FirstOrDefault(k => k.DisplayName == baseType);
        if (semanticKey != null && constraints.TypeConstraints.TryGetValue(semanticKey, out var typeSpecific))
        {
            return typeSpecific.FormatFullType();
        }

        // Fall back to global constraints (legacy behavior)
        return FormatTypeWithElements(baseType, constraints);
    }

    /// <summary>
    /// Formats a container type with element/key types if available from constraints.
    /// E.g., "Array" -> "Array[int | String]", "Dictionary" -> "Dictionary[String, Variant]"
    /// </summary>
    private string FormatTypeWithElements(string baseType, GDParameterConstraints? constraints)
    {
        if (constraints == null)
            return baseType;

        // Build element type string
        string? elementTypeString = null;
        if (constraints.ElementTypes.Count > 0)
        {
            elementTypeString = constraints.ElementTypes.Count == 1
                ? constraints.ElementTypes.First().DisplayName
                : string.Join(" | ", constraints.ElementTypes.Select(t => t.DisplayName).OrderBy(t => t));
        }

        // Build key type string
        string? keyTypeString = null;
        if (constraints.KeyTypes.Count > 0)
        {
            keyTypeString = constraints.KeyTypes.Count == 1
                ? constraints.KeyTypes.First().DisplayName
                : string.Join(" | ", constraints.KeyTypes.Select(t => t.DisplayName).OrderBy(t => t));
        }

        // Format based on type
        if (baseType == GDWellKnownTypes.Containers.Array)
        {
            if (!string.IsNullOrEmpty(elementTypeString))
                return $"{GDWellKnownTypes.Containers.Array}[{elementTypeString}]";
            return baseType;
        }

        if (baseType == GDWellKnownTypes.Containers.Dictionary)
        {
            var key = keyTypeString ?? GDWellKnownTypes.Variant;
            var val = elementTypeString ?? GDWellKnownTypes.Variant;
            if (keyTypeString != null || elementTypeString != null)
                return $"{GDWellKnownTypes.Containers.Dictionary}[{key}, {val}]";
            return baseType;
        }

        // Other types - no element formatting
        return baseType;
    }

    #region Confidence Scoring

    // Godot singletons that are never passed as parameters
    private static readonly HashSet<string> SingletonTypes = new()
    {
        "TextServer", "RenderingServer", "PhysicsServer2D", "PhysicsServer3D",
        "DisplayServer", "AudioServer", "NavigationServer2D", "NavigationServer3D",
        "XRServer", "CameraServer", "ClassDB", "Engine", "IP", "OS",
        "Performance", "ProjectSettings", "TranslationServer", "Time",
        "Input", "InputMap", "ThemeDB", "GDExtensionManager",
        "WorkerThreadPool", "JavaClassWrapper", "JavaScriptBridge",
        "EditorInterface", "EngineDebugger", "NativeMenu"
    };

    // Internal types that are never passed as parameters
    private static readonly HashSet<string> InternalTypes = new()
    {
        "PackedDataContainer", "PackedDataContainerRef", "ScriptBacktrace"
    };

    private static bool IsSingletonOrInternal(string typeName) =>
        SingletonTypes.Contains(typeName) || InternalTypes.Contains(typeName);

    // Methods typically associated with containers
    private static readonly HashSet<string> ContainerMethodNames = new()
    {
        "has", "size", "keys", "values", "is_empty", "slice", "append",
        "map", "filter", "reduce", "any", "all", "pop_back", "push_back",
        "pop_front", "push_front", "insert", "erase", "clear", "sort"
    };

    // Non-container types that shouldn't appear in container-method unions
    private static readonly HashSet<string> NonContainerFPTypes = new()
    {
        "Image", "XMLParser", "TileMapPattern", "NodePath",
        "ScriptBacktrace", "PackedDataContainer", "PackedDataContainerRef"
    };

    /// <summary>
    /// Scores a candidate type's confidence based on signature compatibility,
    /// singleton/internal status, and container affinity.
    /// </summary>
    private GDTypeConfidence ScoreCandidateConfidence(
        GDGodotTypesProvider typesProvider,
        string candidateType,
        GDParameterConstraints constraints)
    {
        // Rule 2: Singleton & Internal -> always Low
        if (IsSingletonOrInternal(candidateType))
            return GDTypeConfidence.Low;

        // Rule 3: Container Affinity violation -> Low
        if (IsContainerAffinityViolation(candidateType, constraints))
            return GDTypeConfidence.Low;

        // Rule 1: Check method signatures
        int compatible = 0;
        int checkable = 0;

        foreach (var methodName in constraints.RequiredMethods)
        {
            var memberInfo = typesProvider.GetMember(candidateType, methodName);
            if (memberInfo?.Parameters == null || memberInfo.Parameters.Count == 0)
                continue;

            checkable++;
            if (IsMethodSignatureCompatible(typesProvider, candidateType, methodName, memberInfo, constraints))
                compatible++;
        }

        if (checkable == 0)
            return GDTypeConfidence.Medium;

        if (compatible == checkable)
            return GDTypeConfidence.High;

        if (compatible > 0)
            return GDTypeConfidence.Medium;

        return GDTypeConfidence.Low;
    }

    /// <summary>
    /// Checks if a candidate type's method signature is compatible with actual usage.
    /// </summary>
    private static bool IsMethodSignatureCompatible(
        GDGodotTypesProvider typesProvider,
        string candidateType,
        string methodName,
        GDRuntimeMemberInfo memberInfo,
        GDParameterConstraints constraints)
    {
        if (memberInfo.Parameters == null || memberInfo.Parameters.Count == 0)
            return true;

        // Primary: check tracked argument info from actual calls
        if (constraints.MethodCallArgTypes.TryGetValue(methodName, out var argInfoLists))
        {
            foreach (var argInfos in argInfoLists)
            {
                for (int i = 0; i < Math.Min(argInfos.Length, memberInfo.Parameters.Count); i++)
                {
                    var argInfo = argInfos[i];
                    var paramType = memberInfo.Parameters[i].Type;

                    if (string.IsNullOrEmpty(paramType) || paramType == "Variant")
                        continue;

                    // Parameter reference: apply property-access heuristic
                    if (argInfo.IsParameterRef)
                    {
                        if (i == 0 && IsPropertyAccessMethod(methodName) && IsIntegerType(paramType))
                            return false;
                        continue;
                    }

                    // Unknown arg: skip check
                    if (argInfo.IsUnknown)
                        continue;

                    // Resolved type: strict compatibility check
                    var argType = argInfo.ResolvedType!.DisplayName;
                    if (argType == "Variant")
                        continue;

                    if (!typesProvider.IsAssignableTo(argType, paramType))
                        return false;
                }
            }
            return true;
        }

        // Fallback: check KeyTypes for .has/.get
        var firstParamType = memberInfo.Parameters[0].Type;
        if (string.IsNullOrEmpty(firstParamType) || firstParamType == "Variant")
            return true;

        if (constraints.KeyTypes.Count > 0 && (methodName == "has" || methodName == "get"))
        {
            return constraints.KeyTypes.Any(kt => typesProvider.IsAssignableTo(kt.DisplayName, firstParamType));
        }

        // Fallback: check .set(String, ...) pattern
        if (methodName == "set" && memberInfo.Parameters.Count >= 2)
        {
            if (firstParamType != GDWellKnownTypes.Variant && firstParamType != GDWellKnownTypes.Strings.String && firstParamType != GDWellKnownTypes.Strings.StringName)
            {
                if (constraints.RequiredProperties.Count > 0 || constraints.KeyTypes.Any(kt => kt.DisplayName == GDWellKnownTypes.Strings.String))
                    return false;
            }
        }

        return true;
    }

    private static bool IsPropertyAccessMethod(string methodName) =>
        methodName == "set" || methodName == "get" || methodName == "has";

    private static bool IsIntegerType(string? typeName) =>
        typeName == GDWellKnownTypes.Numeric.Int || typeName == GDWellKnownTypes.Numeric.Float;

    /// <summary>
    /// Removes individual PackedArray types when Array is already present and
    /// all required methods are common across Array and all PackedArray types.
    /// </summary>
    private List<(string Type, GDTypeConfidence Confidence)> DeduplicatePackedArrays(
        GDGodotTypesProvider typesProvider,
        List<(string Type, GDTypeConfidence Confidence)> candidates,
        GDParameterConstraints constraints)
    {
        var hasArray = candidates.Any(c => c.Type == GDWellKnownTypes.Containers.Array);
        if (!hasArray) return candidates;

        var packedCount = candidates.Count(c => typesProvider.IsPackedArrayType(c.Type));
        if (packedCount < 2) return candidates;

        var commonMethods = GetCommonArrayPackedMethods(typesProvider);
        var allCommon = constraints.RequiredMethods.All(m => commonMethods.Contains(m));
        if (!allCommon) return candidates;

        return candidates.Where(c => !typesProvider.IsPackedArrayType(c.Type)).ToList();
    }

    /// <summary>
    /// Computes the intersection of method names shared by Array and all PackedArray types.
    /// Result is cached for the lifetime of this resolver instance.
    /// </summary>
    private HashSet<string> GetCommonArrayPackedMethods(GDGodotTypesProvider typesProvider)
    {
        if (_cachedCommonArrayMethods != null)
            return _cachedCommonArrayMethods;

        var packedTypes = typesProvider.GetAllTypes()
            .Where(t => typesProvider.IsPackedArrayType(t))
            .ToList();

        var arrayTypeInfo = typesProvider.GetTypeInfo(GDWellKnownTypes.Containers.Array);
        if (arrayTypeInfo?.Members == null || packedTypes.Count == 0)
        {
            _cachedCommonArrayMethods = new HashSet<string>();
            return _cachedCommonArrayMethods;
        }

        var commonMethods = new HashSet<string>(
            arrayTypeInfo.Members
                .Where(m => m.Kind == GDRuntimeMemberKind.Method)
                .Select(m => m.Name));

        foreach (var packedType in packedTypes)
        {
            var packedInfo = typesProvider.GetTypeInfo(packedType);
            if (packedInfo?.Members == null) continue;

            var packedMethods = new HashSet<string>(
                packedInfo.Members
                    .Where(m => m.Kind == GDRuntimeMemberKind.Method)
                    .Select(m => m.Name));

            commonMethods.IntersectWith(packedMethods);
        }

        _cachedCommonArrayMethods = commonMethods;
        return _cachedCommonArrayMethods;
    }

    /// <summary>
    /// Checks if a type violates container affinity (non-container type in container-method context).
    /// </summary>
    private static bool IsContainerAffinityViolation(string typeName, GDParameterConstraints constraints)
    {
        if (!NonContainerFPTypes.Contains(typeName))
            return false;

        var containerMethodCount = constraints.RequiredMethods.Count(m => ContainerMethodNames.Contains(m));
        return containerMethodCount > 0 && containerMethodCount >= constraints.RequiredMethods.Count / 2;
    }

    #endregion
}
