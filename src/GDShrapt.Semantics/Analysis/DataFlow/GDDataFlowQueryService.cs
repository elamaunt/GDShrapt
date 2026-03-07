using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Implementation of IGDDataFlowQuery.
/// Composes data flow information from flow analyzers and symbol resolution.
/// </summary>
internal sealed class GDDataFlowQueryService : IGDDataFlowQuery
{
    private readonly GDFlowAnalysisRegistry _flowRegistry;
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly Func<GDNode, GDFlowAnalyzer?> _getOrCreateAnalyzer;
    private readonly Func<GDNodeHandle, GDNode?>? _resolveNode;

    public GDDataFlowQueryService(
        GDFlowAnalysisRegistry flowRegistry,
        IGDRuntimeProvider? runtimeProvider,
        Func<GDNode, GDFlowAnalyzer?> getOrCreateAnalyzer,
        Func<GDNodeHandle, GDNode?>? resolveNode = null)
    {
        _flowRegistry = flowRegistry ?? throw new ArgumentNullException(nameof(flowRegistry));
        _runtimeProvider = runtimeProvider;
        _getOrCreateAnalyzer = getOrCreateAnalyzer ?? throw new ArgumentNullException(nameof(getOrCreateAnalyzer));
        _resolveNode = resolveNode;
    }

    public GDDataFlowInfo? GetDataFlowAt(GDSymbolInfo symbol, int line, int column)
    {
        if (symbol == null)
            return null;

        var varType = FindFlowVariableType(symbol, line, column);
        if (varType == null)
            return null;

        var objectState = FindObjectState(varType);

        return new GDDataFlowInfo(
            flowType: varType.CurrentType,
            effectiveType: varType.EffectiveType,
            activeNarrowings: varType.ActiveNarrowings,
            duckType: varType.DuckType,
            isGuaranteedNonNull: varType.IsGuaranteedNonNull,
            isPotentiallyNull: varType.IsPotentiallyNull,
            escapePoints: varType.EscapePoints,
            objectState: objectState);
    }

    public IReadOnlyList<GDTypeConflict> GetTypeConflicts(GDSymbolInfo symbol, int line, int column)
    {
        if (symbol == null)
            return Array.Empty<GDTypeConflict>();

        var varType = FindFlowVariableType(symbol, line, column);
        if (varType == null)
            return Array.Empty<GDTypeConflict>();

        return DetectConflicts(varType);
    }

    public IReadOnlyList<GDTypeOrigin> GetUnknownSources(GDSymbolInfo symbol)
    {
        if (symbol == null)
            return Array.Empty<GDTypeOrigin>();

        var varType = FindFlowVariableTypeFromDeclaration(symbol);
        if (varType == null)
            return Array.Empty<GDTypeOrigin>();

        var unknowns = new List<GDTypeOrigin>();
        foreach (var type in varType.CurrentType.Types)
        {
            if (type.IsVariant)
            {
                foreach (var origin in varType.CurrentType.GetOrigins(type))
                    unknowns.Add(origin);
            }
        }

        if (unknowns.Count == 0 && !varType.CurrentType.HasOrigins && varType.CurrentType.IsEmpty)
        {
            unknowns.Add(new GDTypeOrigin(
                GDTypeOriginKind.Unknown,
                GDTypeOriginConfidence.Heuristic,
                default,
                description: "No type information available"));
        }

        return unknowns;
    }

    public IReadOnlyList<GDEscapePoint> GetEscapePoints(GDSymbolInfo symbol)
    {
        if (symbol == null)
            return Array.Empty<GDEscapePoint>();

        var varType = FindFlowVariableTypeFromDeclaration(symbol);
        if (varType == null)
            return Array.Empty<GDEscapePoint>();

        return varType.EscapePoints;
    }

    public GDObjectState? GetObjectState(GDSymbolInfo symbol, int line, int column)
    {
        if (symbol == null)
            return null;

        var varType = FindFlowVariableType(symbol, line, column);
        if (varType == null)
            return null;

        return FindObjectState(varType);
    }

    // ========================================
    // Internal helpers
    // ========================================

    private GDNode? ResolveDeclarationNode(GDSymbolInfo? symbol)
    {
        if (symbol == null)
            return null;

        if (symbol.Symbol != null && _resolveNode != null)
            return _resolveNode(symbol.Symbol.DeclarationNode);

        return symbol.DeclarationNode;
    }

    private GDFlowVariableType? FindFlowVariableType(GDSymbolInfo symbol, int line, int column)
    {
        if (symbol.Kind != GDSymbolKind.Variable
            && symbol.Kind != GDSymbolKind.Parameter
            && symbol.Kind != GDSymbolKind.Iterator
            && symbol.Kind != GDSymbolKind.MatchCaseBinding)
        {
            return null;
        }

        var resolvedDeclNode = ResolveDeclarationNode(symbol);
        var scopeNode = symbol.DeclaringScopeNode ?? resolvedDeclNode?.GetContainingMethodScope();
        if (scopeNode == null)
            return null;

        var analyzer = _getOrCreateAnalyzer(scopeNode);
        if (analyzer == null)
            return null;

        var atNode = FindNodeAtPosition(scopeNode, line, column);
        if (atNode == null)
            return null;

        return analyzer.GetVariableTypeAtLocation(symbol.Name, atNode);
    }

    private GDFlowVariableType? FindFlowVariableTypeFromDeclaration(GDSymbolInfo symbol)
    {
        if (symbol.Kind != GDSymbolKind.Variable
            && symbol.Kind != GDSymbolKind.Parameter
            && symbol.Kind != GDSymbolKind.Iterator
            && symbol.Kind != GDSymbolKind.MatchCaseBinding)
        {
            return null;
        }

        var declarationNode = ResolveDeclarationNode(symbol);
        if (declarationNode == null)
            return null;

        var scopeNode = symbol.DeclaringScopeNode ?? declarationNode.GetContainingMethodScope();
        if (scopeNode == null)
            return null;

        var analyzer = _getOrCreateAnalyzer(scopeNode);
        if (analyzer == null)
            return null;

        var state = analyzer.GetStateAtLocation(declarationNode);
        return state?.GetVariableType(symbol.Name);
    }

    private static GDObjectState? FindObjectState(GDFlowVariableType varType)
    {
        if (!varType.CurrentType.HasOrigins)
            return null;

        foreach (var tracked in varType.CurrentType.AllTrackedTypes)
        {
            if (tracked.Origin.ObjectState != null)
                return tracked.Origin.ObjectState;
        }

        return null;
    }

    private List<GDTypeConflict> DetectConflicts(GDFlowVariableType varType)
    {
        var conflicts = new List<GDTypeConflict>();

        if (varType.DeclaredType == null || varType.CurrentType.IsEmpty)
            return conflicts;

        var declaredName = varType.DeclaredType.DisplayName;

        foreach (var (type, origins) in varType.CurrentType.GetAllOrigins())
        {
            var typeName = type.DisplayName;

            if (type.Equals(varType.DeclaredType))
                continue;

            if (type is GDNullSemanticType)
            {
                foreach (var origin in origins)
                {
                    conflicts.Add(new GDTypeConflict(
                        GDTypeConflictKind.PotentialNull,
                        type,
                        varType.DeclaredType,
                        origin,
                        $"Potential null assigned to {declaredName}"));
                }
                continue;
            }

            bool isAssignable = false;
            bool isWider = false;

            if (_runtimeProvider != null)
            {
                isAssignable = _runtimeProvider.IsAssignableTo(typeName, declaredName);
                isWider = _runtimeProvider.IsAssignableTo(declaredName, typeName);
            }

            foreach (var origin in origins)
            {
                if (isAssignable)
                    continue;

                if (isWider)
                {
                    conflicts.Add(new GDTypeConflict(
                        GDTypeConflictKind.Widening,
                        type,
                        varType.DeclaredType,
                        origin,
                        $"Assignment widens '{declaredName}' to '{typeName}' (from {origin.Description ?? origin.Kind.ToString()})"));
                }
                else
                {
                    conflicts.Add(new GDTypeConflict(
                        GDTypeConflictKind.IncompatibleAssignment,
                        type,
                        varType.DeclaredType,
                        origin,
                        $"'{typeName}' is not assignable to '{declaredName}' (from {origin.Description ?? origin.Kind.ToString()})"));
                }
            }
        }

        // Check for removed node access via object state
        var objectState = FindObjectState(varType);
        if (objectState != null)
        {
            foreach (var mutation in objectState.GetMutationHistory())
            {
                if (mutation.Kind == GDStateMutationKind.RemoveChild
                    || mutation.Kind == GDStateMutationKind.QueueFree)
                {
                    var removedPath = mutation.NodePath ?? "unknown";
                    conflicts.Add(new GDTypeConflict(
                        GDTypeConflictKind.RemovedNodeAccess,
                        varType.EffectiveType,
                        varType.DeclaredType,
                        new GDTypeOrigin(
                            GDTypeOriginKind.Unknown,
                            GDTypeOriginConfidence.Inferred,
                            mutation.Location,
                            description: $"Node '{removedPath}' removed"),
                        $"Node '{removedPath}' may be removed at {mutation.Location}"));
                }
            }
        }

        // Check for collision layer mismatches
        if (objectState != null)
        {
            var collisionLayers = objectState.GetCurrentCollisionLayers();
            if (collisionLayers != null && objectState.HasConflictingMutations("collision_layer"))
            {
                conflicts.Add(new GDTypeConflict(
                    GDTypeConflictKind.CollisionLayerMismatch,
                    varType.EffectiveType,
                    varType.DeclaredType,
                    new GDTypeOrigin(
                        GDTypeOriginKind.Unknown,
                        GDTypeOriginConfidence.Inferred,
                        default,
                        description: "Conflicting collision layer values from branches"),
                    "Collision layer values conflict between branches"));
            }
        }

        return conflicts;
    }

    private static GDNode? FindNodeAtPosition(GDNode scope, int line, int column)
    {
        GDNode? best = null;
        int bestDistance = int.MaxValue;

        foreach (var node in scope.AllNodes)
        {
            var firstToken = node.FirstLeafToken;
            if (firstToken == null)
                continue;

            var nodeLine = firstToken.StartLine;
            var nodeCol = firstToken.StartColumn;

            if (nodeLine == line && nodeCol == column)
                return node;

            if (nodeLine == line)
            {
                var dist = Math.Abs(nodeCol - column);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = node;
                }
            }
            else if (nodeLine < line)
            {
                var dist = (line - nodeLine) * 1000 + Math.Abs(nodeCol - column);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = node;
                }
            }
        }

        return best;
    }
}
