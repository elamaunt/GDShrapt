using GDShrapt.Abstractions;

namespace GDShrapt.Semantics;

/// <summary>
/// Static helper methods for loop flow analysis.
/// Provides fixed-point iteration and iterator type inference.
/// </summary>
internal static class GDLoopFlowHelpers
{
    /// <summary>
    /// Maximum iterations for fixed-point loop analysis.
    /// </summary>
    public const int MaxFixedPointIterations = 10;

    /// <summary>
    /// Computes the fixed-point for loop type analysis.
    /// Iterates until types stabilize or max iterations reached.
    /// </summary>
    public static GDFlowState ComputeLoopFixedPoint(
        GDFlowState preLoopState,
        GDFlowState firstIterationState,
        string? iteratorName,
        string? iteratorType)
    {
        // Start with the result of the first iteration
        var currentState = firstIterationState;

        // Get initial snapshot
        var previousSnapshot = currentState.GetTypeSnapshot();

        // Iterate until fixed point or max iterations
        for (int i = 0; i < MaxFixedPointIterations; i++)
        {
            // Simulate another iteration: loop body starts with types from either before the loop or after previous iteration
            var mergedEntry = GDFlowState.MergeBranches(currentState, preLoopState, preLoopState);

            var iterationState = mergedEntry.CreateChild();

            // Re-declare iterator if present
            if (!string.IsNullOrEmpty(iteratorName))
            {
                iterationState.DeclareVariable(iteratorName, null, GDSemanticType.FromRuntimeTypeName(iteratorType));
            }

            // Merge the new iteration state into current state
            // This accumulates types across iterations
            var changed = currentState.MergeInto(iterationState);

            if (!changed)
            {
                break;
            }

            // Also check via snapshot comparison
            var newSnapshot = currentState.GetTypeSnapshot();
            if (currentState.MatchesSnapshot(previousSnapshot))
            {
                break;
            }

            previousSnapshot = newSnapshot;
        }

        // Final merge: loop may execute 0+ times
        // So the result is the union of pre-loop state (0 iterations)
        // and the fixed-point state (1+ iterations)
        return GDFlowState.MergeBranches(currentState, preLoopState, preLoopState);
    }

    /// <summary>
    /// Infers the element type from a collection type for for-loop iteration.
    /// </summary>
    public static string? InferIteratorElementType(string? collectionType)
    {
        if (string.IsNullOrEmpty(collectionType))
            return "Variant";

        // Handle typed arrays: Array[Type] -> Type
        var arrayElementType = GDGenericTypeHelper.ExtractArrayElementType(collectionType);
        if (arrayElementType != null)
            return arrayElementType;

        // Handle range() -> int
        if (collectionType == GDWellKnownTypes.Other.Range || collectionType == GDWellKnownTypes.Numeric.Int)
            return GDWellKnownTypes.Numeric.Int;

        // Handle String -> String (iterating chars)
        if (collectionType == GDWellKnownTypes.Strings.String)
            return GDWellKnownTypes.Strings.String;

        // Handle Dictionary -> Variant (iterating keys)
        if (GDGenericTypeHelper.IsDictionaryType(collectionType))
            return GDWellKnownTypes.Variant;

        // Handle PackedArray types
        var packedElement = GDPackedArrayTypes.GetElementType(collectionType);
        if (packedElement != null)
            return packedElement;

        return GDWellKnownTypes.Variant;
    }
}
