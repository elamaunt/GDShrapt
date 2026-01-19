using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Abstractions;

/// <summary>
/// Type of control flow termination.
/// </summary>
public enum TerminationType
{
    /// <summary>Return statement (exits method).</summary>
    Return,
    /// <summary>Break statement (exits loop).</summary>
    Break,
    /// <summary>Continue statement (skips to next iteration).</summary>
    Continue
}

/// <summary>
/// Represents the type state of all variables at a specific program point.
/// Supports branching and merging for control flow analysis.
/// </summary>
public class GDFlowState
{
    private readonly Dictionary<string, GDFlowVariableType> _variables = new();
    private readonly GDFlowState? _parent;

    /// <summary>
    /// Indicates that this branch terminates (return, break, continue).
    /// Terminated branches don't participate in Union merging.
    /// </summary>
    public bool IsTerminated { get; set; }

    /// <summary>
    /// The type of termination (for debugging/tracking).
    /// </summary>
    public TerminationType? Termination { get; set; }

    /// <summary>
    /// Creates a new flow state with optional parent for scope chaining.
    /// </summary>
    public GDFlowState(GDFlowState? parent = null)
    {
        _parent = parent;
    }

    /// <summary>
    /// Gets the parent state (for scope chaining).
    /// </summary>
    public GDFlowState? Parent => _parent;

    /// <summary>
    /// Gets all variable names in this state (excluding parent).
    /// </summary>
    public IEnumerable<string> LocalVariables => _variables.Keys;

    /// <summary>
    /// Gets the current type of a variable.
    /// Searches up the parent chain if not found locally.
    /// </summary>
    public GDFlowVariableType? GetVariableType(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (_variables.TryGetValue(name, out var type))
            return type;
        return _parent?.GetVariableType(name);
    }

    /// <summary>
    /// Sets the type of a variable (for assignments).
    /// Resets narrowing and replaces current type with new one.
    /// </summary>
    public void SetVariableType(string name, string typeName, GDNode? assignmentNode = null)
    {
        if (string.IsNullOrEmpty(name))
            return;

        var existing = GetVariableType(name) ?? new GDFlowVariableType();
        var newType = existing.Clone();

        // Reset narrowing on assignment
        newType.IsNarrowed = false;
        newType.NarrowedFromType = null;

        // Replace current type with new one
        newType.CurrentType = new GDUnionType();
        if (!string.IsNullOrEmpty(typeName))
            newType.CurrentType.AddType(typeName);
        newType.LastAssignmentNode = assignmentNode;

        _variables[name] = newType;
    }

    /// <summary>
    /// Declares a variable with optional explicit type.
    /// </summary>
    public void DeclareVariable(string name, string? declaredType, string? initType = null)
    {
        if (string.IsNullOrEmpty(name))
            return;

        var flowType = new GDFlowVariableType { DeclaredType = declaredType };

        if (!string.IsNullOrEmpty(initType) && initType != "Variant")
            flowType.CurrentType.AddType(initType);
        else if (!string.IsNullOrEmpty(declaredType) && declaredType != "Variant")
            flowType.CurrentType.AddType(declaredType);

        _variables[name] = flowType;
    }

    /// <summary>
    /// Applies type narrowing (from 'is' check).
    /// Does not modify parent state.
    /// </summary>
    public void NarrowType(string name, string toType)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(toType))
            return;

        var existing = GetVariableType(name);
        if (existing == null)
        {
            // Variable not known - create with narrowed type
            var newType = new GDFlowVariableType
            {
                IsNarrowed = true,
                NarrowedFromType = toType
            };
            newType.CurrentType.AddType(toType);
            _variables[name] = newType;
            return;
        }

        var narrowed = existing.Clone();
        narrowed.IsNarrowed = true;
        narrowed.NarrowedFromType = toType;
        _variables[name] = narrowed;
    }

    /// <summary>
    /// Creates a child state for a new scope/branch.
    /// </summary>
    public GDFlowState CreateChild() => new(this);

    /// <summary>
    /// Marks this state as terminated (return/break/continue).
    /// </summary>
    public void MarkTerminated(TerminationType terminationType)
    {
        IsTerminated = true;
        Termination = terminationType;
    }

    /// <summary>
    /// Merges two branch states into a Union.
    /// Variables modified in either branch get merged types.
    /// Terminated branches don't contribute to the merge (their types don't flow forward).
    /// </summary>
    public static GDFlowState MergeBranches(GDFlowState? ifBranch, GDFlowState? elseBranch, GDFlowState parent)
    {
        // Handle terminated branches:
        // If a branch terminates (return/break), code after if/else only runs if the OTHER branch runs
        var ifTerminated = ifBranch?.IsTerminated ?? false;
        var elseTerminated = elseBranch?.IsTerminated ?? false;

        // If both branches terminate, the merged state is also terminated
        // (code after this point is unreachable)
        if (ifTerminated && elseTerminated)
        {
            var unreachable = new GDFlowState(parent._parent) { IsTerminated = true };
            // Copy parent variables (for reference purposes)
            foreach (var varName in parent.LocalVariables)
            {
                var parentType = parent.GetVariableType(varName);
                if (parentType != null)
                    unreachable._variables[varName] = parentType.Clone();
            }
            return unreachable;
        }

        // If only if-branch terminates, use else-branch (or parent if no else)
        if (ifTerminated)
        {
            return CloneState(elseBranch ?? parent, parent._parent);
        }

        // If only else-branch terminates, use if-branch
        if (elseTerminated)
        {
            return CloneState(ifBranch ?? parent, parent._parent);
        }

        // Neither branch terminates - standard merge
        // Merged state continues from parent's parent (exits the if/else scope)
        var merged = new GDFlowState(parent._parent);

        // Collect all variable names from both branches and parent
        var allVars = new HashSet<string>();
        CollectVariableNames(ifBranch, allVars);
        CollectVariableNames(elseBranch, allVars);
        CollectVariableNames(parent, allVars);

        foreach (var varName in allVars)
        {
            var ifType = ifBranch?.GetVariableType(varName);
            var elseType = elseBranch?.GetVariableType(varName);
            var parentType = parent.GetVariableType(varName);

            // Check if variable was modified in either branch
            var ifModified = ifBranch != null && ifBranch._variables.ContainsKey(varName);
            var elseModified = elseBranch != null && elseBranch._variables.ContainsKey(varName);

            // If modified in neither branch, no merge needed (keep parent type)
            if (!ifModified && !elseModified)
            {
                if (parentType != null)
                    merged._variables[varName] = parentType.Clone();
                continue;
            }

            // Use parent type if branch didn't modify
            ifType ??= parentType;
            elseType ??= parentType;

            if (ifType == null && elseType == null)
                continue;

            var mergedType = MergeVariableTypes(ifType, elseType);
            if (mergedType != null)
                merged._variables[varName] = mergedType;
        }

        return merged;
    }

    /// <summary>
    /// Creates a clone of a state with a new parent.
    /// </summary>
    private static GDFlowState CloneState(GDFlowState source, GDFlowState? newParent)
    {
        var clone = new GDFlowState(newParent)
        {
            IsTerminated = source.IsTerminated,
            Termination = source.Termination
        };

        // Copy all variables from source (flattening parent chain)
        var allVars = new HashSet<string>();
        CollectVariableNames(source, allVars);

        foreach (var varName in allVars)
        {
            var varType = source.GetVariableType(varName);
            if (varType != null)
                clone._variables[varName] = varType.Clone();
        }

        return clone;
    }

    /// <summary>
    /// Collects all variable names in a state and its parent chain.
    /// </summary>
    private static void CollectVariableNames(GDFlowState? state, HashSet<string> names)
    {
        while (state != null)
        {
            foreach (var name in state._variables.Keys)
                names.Add(name);
            state = state._parent;
        }
    }

    /// <summary>
    /// Merges two variable types into a Union.
    /// </summary>
    private static GDFlowVariableType? MergeVariableTypes(GDFlowVariableType? a, GDFlowVariableType? b)
    {
        if (a == null) return b?.Clone();
        if (b == null) return a.Clone();

        var merged = new GDFlowVariableType
        {
            DeclaredType = a.DeclaredType ?? b.DeclaredType,
            IsNarrowed = false,
            NarrowedFromType = null
        };

        // Merge current types into Union
        merged.CurrentType.MergeWith(a.CurrentType);
        merged.CurrentType.MergeWith(b.CurrentType);

        // Also consider narrowed types if they differ from current
        if (a.IsNarrowed && !string.IsNullOrEmpty(a.NarrowedFromType))
            merged.CurrentType.AddType(a.NarrowedFromType);
        if (b.IsNarrowed && !string.IsNullOrEmpty(b.NarrowedFromType))
            merged.CurrentType.AddType(b.NarrowedFromType);

        return merged;
    }

    public override string ToString()
    {
        var vars = string.Join(", ", _variables.Select(kv => $"{kv.Key}: {kv.Value.EffectiveTypeFormatted}"));
        var parentInfo = _parent != null ? " (has parent)" : "";
        return $"FlowState[{vars}]{parentInfo}";
    }

    #region Fixed-Point Iteration Support

    /// <summary>
    /// Checks if this state's variable types are a subset of another state.
    /// Used for fixed-point iteration: if merging doesn't add new types, we've reached a fixed point.
    /// </summary>
    /// <param name="other">The state to compare against.</param>
    /// <returns>True if all types in this state are already present in the other state.</returns>
    public bool IsSubsetOf(GDFlowState? other)
    {
        if (other == null)
            return _variables.Count == 0;

        foreach (var kvp in _variables)
        {
            var name = kvp.Key;
            var varType = kvp.Value;

            var otherType = other.GetVariableType(name);
            if (otherType == null)
                return false;

            // Check if all types in this variable are in the other
            foreach (var type in varType.CurrentType.Types)
            {
                if (!otherType.CurrentType.Types.Contains(type))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Merges another state into this state (union of all types).
    /// Used for fixed-point iteration to accumulate loop iterations.
    /// </summary>
    /// <param name="other">The state to merge in.</param>
    /// <returns>True if any new types were added.</returns>
    public bool MergeInto(GDFlowState? other)
    {
        if (other == null)
            return false;

        bool changed = false;

        // Collect all variable names from other state
        var otherVars = new HashSet<string>();
        CollectVariableNames(other, otherVars);

        foreach (var varName in otherVars)
        {
            var otherType = other.GetVariableType(varName);
            if (otherType == null)
                continue;

            var myType = GetVariableType(varName);
            if (myType == null)
            {
                // New variable - add it
                _variables[varName] = otherType.Clone();
                changed = true;
            }
            else
            {
                // Existing variable - merge types
                var beforeCount = myType.CurrentType.Types.Count;
                myType.CurrentType.MergeWith(otherType.CurrentType);
                if (myType.CurrentType.Types.Count > beforeCount)
                    changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// Creates a snapshot of this state for fixed-point comparison.
    /// Only includes local variables, not parent chain.
    /// </summary>
    public Dictionary<string, HashSet<string>> GetTypeSnapshot()
    {
        var snapshot = new Dictionary<string, HashSet<string>>();
        foreach (var kvp in _variables)
        {
            snapshot[kvp.Key] = new HashSet<string>(kvp.Value.CurrentType.Types);
        }
        return snapshot;
    }

    /// <summary>
    /// Checks if current state matches a previous snapshot.
    /// Used for fixed-point detection.
    /// </summary>
    public bool MatchesSnapshot(Dictionary<string, HashSet<string>> snapshot)
    {
        if (snapshot.Count != _variables.Count)
            return false;

        foreach (var kvp in snapshot)
        {
            if (!_variables.TryGetValue(kvp.Key, out var varType))
                return false;

            if (!kvp.Value.SetEquals(varType.CurrentType.Types))
                return false;
        }

        return true;
    }

    #endregion
}
