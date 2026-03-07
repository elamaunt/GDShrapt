using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Immutable persistent object state. Each mutation creates a new node in a DAG.
/// Branching (if/else) forks from a shared parent. Merge creates a node referencing both branches.
/// No copying — structural sharing via references.
/// </summary>
public sealed class GDObjectState
{
    /// <summary>
    /// Previous state (null = initial/root state).
    /// </summary>
    public GDObjectState? Previous { get; }

    /// <summary>
    /// Second parent for merge points (null if not a merge).
    /// </summary>
    public GDObjectState? MergedFrom { get; }

    /// <summary>
    /// Mutation from Previous (null = initial state with data in base fields).
    /// </summary>
    public GDStateMutation? Mutation { get; }

    // --- Initial state (set only on root) ---

    public GDSceneSnapshot? SceneSnapshot { get; }
    public IReadOnlyDictionary<string, GDAbstractValue>? InitialProperties { get; }
    public GDCollisionLayerState? CollisionLayers { get; }

    /// <summary>
    /// Creates a root (initial) state.
    /// </summary>
    public GDObjectState(
        GDSceneSnapshot? sceneSnapshot = null,
        IReadOnlyDictionary<string, GDAbstractValue>? initialProperties = null,
        GDCollisionLayerState? collisionLayers = null)
    {
        SceneSnapshot = sceneSnapshot;
        InitialProperties = initialProperties;
        CollisionLayers = collisionLayers;
    }

    private GDObjectState(
        GDObjectState? previous,
        GDObjectState? mergedFrom,
        GDStateMutation? mutation)
    {
        Previous = previous;
        MergedFrom = mergedFrom;
        Mutation = mutation;
    }

    /// <summary>
    /// Creates a new state with a mutation applied. Does not modify this.
    /// </summary>
    public GDObjectState WithMutation(GDStateMutation mutation)
        => new GDObjectState(this, null, mutation);

    /// <summary>
    /// Merges two branch-end states into a single state.
    /// </summary>
    public static GDObjectState Merge(GDObjectState? branch1, GDObjectState? branch2)
        => new GDObjectState(branch1, branch2, null);

    /// <summary>
    /// Walks the DAG to collect all mutations in chronological order.
    /// </summary>
    public IEnumerable<GDStateMutation> GetMutationHistory()
    {
        var mutations = new List<GDStateMutation>();
        CollectMutations(this, mutations, new HashSet<GDObjectState>());
        return mutations;
    }

    /// <summary>
    /// Gets the latest value set for a property by walking the DAG.
    /// Returns null if the property was never set or has conflicting values from merge.
    /// </summary>
    public GDAbstractValue? GetPropertyValue(string propertyName)
    {
        return FindPropertyValue(this, propertyName, new HashSet<GDObjectState>());
    }

    /// <summary>
    /// Gets the current collision layers by walking the DAG for the latest change.
    /// </summary>
    public GDCollisionLayerState? GetCurrentCollisionLayers()
    {
        return FindCollisionLayers(this, new HashSet<GDObjectState>());
    }

    /// <summary>
    /// Checks if merged branches set different values for a property.
    /// </summary>
    public bool HasConflictingMutations(string propertyName)
    {
        if (MergedFrom == null)
            return false;

        var val1 = FindPropertyValue(Previous, propertyName, new HashSet<GDObjectState>());
        var val2 = FindPropertyValue(MergedFrom, propertyName, new HashSet<GDObjectState>());

        if (val1 == null && val2 == null)
            return false;
        if (val1 == null || val2 == null)
            return true;

        return val1.DisplayValue != val2.DisplayValue;
    }

    /// <summary>
    /// Gets the root scene snapshot (walks to the root of the DAG).
    /// </summary>
    public GDSceneSnapshot? GetRootSceneSnapshot()
    {
        if (SceneSnapshot != null)
            return SceneSnapshot;
        return Previous?.GetRootSceneSnapshot() ?? MergedFrom?.GetRootSceneSnapshot();
    }

    private static void CollectMutations(GDObjectState? state, List<GDStateMutation> result, HashSet<GDObjectState> visited)
    {
        if (state == null || !visited.Add(state))
            return;

        CollectMutations(state.Previous, result, visited);
        CollectMutations(state.MergedFrom, result, visited);

        if (state.Mutation != null)
            result.Add(state.Mutation);
    }

    private static GDAbstractValue? FindPropertyValue(GDObjectState? state, string propertyName, HashSet<GDObjectState> visited)
    {
        if (state == null || !visited.Add(state))
            return null;

        if (state.Mutation != null
            && state.Mutation.Kind == GDStateMutationKind.PropertySet
            && state.Mutation.PropertyName == propertyName)
        {
            return state.Mutation.NewValue;
        }

        var fromPrevious = FindPropertyValue(state.Previous, propertyName, visited);
        if (fromPrevious != null)
            return fromPrevious;

        var fromMerged = FindPropertyValue(state.MergedFrom, propertyName, visited);
        if (fromMerged != null)
            return fromMerged;

        if (state.InitialProperties != null && state.InitialProperties.TryGetValue(propertyName, out var initialValue))
            return initialValue;

        return null;
    }

    private static GDCollisionLayerState? FindCollisionLayers(GDObjectState? state, HashSet<GDObjectState> visited)
    {
        if (state == null || !visited.Add(state))
            return null;

        if (state.Mutation != null
            && (state.Mutation.Kind == GDStateMutationKind.CollisionLayerChange
                || state.Mutation.Kind == GDStateMutationKind.CollisionMaskChange))
        {
            return state.Mutation.NewCollisionState;
        }

        var fromPrevious = FindCollisionLayers(state.Previous, visited);
        if (fromPrevious != null)
            return fromPrevious;

        var fromMerged = FindCollisionLayers(state.MergedFrom, visited);
        if (fromMerged != null)
            return fromMerged;

        return state.CollisionLayers;
    }

    public override string ToString()
    {
        if (Mutation != null) return $"State({Mutation})";
        if (MergedFrom != null) return "State(merged)";
        if (SceneSnapshot != null) return $"State(root: {SceneSnapshot.ScenePath})";
        return "State(empty)";
    }
}
