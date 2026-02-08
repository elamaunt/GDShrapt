using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Collects .call() and .callv() call sites from GDScript code.
/// Tracks assignments to identify which Callable definition is being called.
/// </summary>
internal class GDCallableCallSiteCollector : GDVisitor
{
    private readonly List<GDCallableCallSiteInfo> _callSites = new();
    private readonly GDCallableTracker _tracker;
    private readonly GDScriptFile? _sourceFile;
    private readonly Func<GDExpression, GDSemanticType?>? _typeInferrer;

    public GDCallableCallSiteCollector(
        GDScriptFile? sourceFile = null,
        Func<GDExpression, GDSemanticType?>? typeInferrer = null)
    {
        _sourceFile = sourceFile;
        _typeInferrer = typeInferrer;
        _tracker = new GDCallableTracker(sourceFile, typeInferrer);
    }

    /// <summary>
    /// All collected call sites.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> CallSites => _callSites;

    /// <summary>
    /// The Callable tracker with assignment information.
    /// </summary>
    public GDCallableTracker Tracker => _tracker;

    /// <summary>
    /// Collects call sites from a class declaration.
    /// </summary>
    public void Collect(GDClassDeclaration classDecl)
    {
        if (classDecl == null)
            return;

        classDecl.WalkIn(this);
        ResolveCallSiteDefinitions();
    }

    /// <summary>
    /// Collects call sites from a single method.
    /// </summary>
    public void Collect(GDMethodDeclaration methodDecl)
    {
        if (methodDecl == null)
            return;

        methodDecl.WalkIn(this);
        ResolveCallSiteDefinitions();
    }

    public override void Visit(GDVariableDeclarationStatement varStmt)
    {
        // Track: var cb = func(x): ...
        if (varStmt.Initializer != null)
        {
            var varName = varStmt.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                _tracker.ProcessAssignmentByName(varName, varStmt.Initializer, false);
            }
        }
    }

    public override void Visit(GDVariableDeclaration varDecl)
    {
        // Track class variable: var _callback = func(x): ...
        if (varDecl.Initializer != null)
        {
            var varName = varDecl.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                _tracker.ProcessAssignmentByName(varName, varDecl.Initializer, true);
            }
        }
    }

    public override void Visit(GDExpressionStatement exprStmt)
    {
        // Track assignments: cb = func(x): ...
        if (exprStmt.Expression is GDDualOperatorExpression dualOp &&
            dualOp.OperatorType == GDDualOperatorType.Assignment)
        {
            _tracker.ProcessAssignment(dualOp.LeftExpression, dualOp.RightExpression, false);
        }
    }

    public override void Visit(GDCallExpression callExpr)
    {
        // Check for .call() or .callv()
        var callSite = GDCallableCallSiteInfo.TryCreate(callExpr, _sourceFile, _typeInferrer);
        if (callSite != null)
        {
            _callSites.Add(callSite);
        }
    }

    /// <summary>
    /// Resolves call sites to their Callable definitions after collection is complete.
    /// </summary>
    private void ResolveCallSiteDefinitions()
    {
        foreach (var callSite in _callSites)
        {
            var definitions = _tracker.ResolveExpression(callSite.CallableExpression);
            if (definitions.Count == 1)
            {
                callSite.ResolvedDefinition = definitions[0];
            }
            else if (definitions.Count > 1)
            {
                // Multiple possible definitions - pick the first for now
                // In the future, could use flow analysis to narrow down
                callSite.ResolvedDefinition = definitions[0];
            }
        }
    }

    /// <summary>
    /// Gets call sites for a specific Callable definition.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSitesFor(GDCallableDefinition definition)
    {
        return _callSites
            .Where(cs => cs.ResolvedDefinition?.UniqueId == definition.UniqueId)
            .ToList();
    }

    /// <summary>
    /// Gets call sites for a specific lambda expression.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSitesFor(GDMethodExpression lambda)
    {
        var definition = GDCallableDefinition.FromLambda(lambda, _sourceFile);
        return GetCallSitesFor(definition);
    }

    /// <summary>
    /// Gets call sites for a specific variable name.
    /// </summary>
    public IReadOnlyList<GDCallableCallSiteInfo> GetCallSitesForVariable(string variableName)
    {
        return _callSites
            .Where(cs => cs.CallableVariableName == variableName)
            .ToList();
    }

    /// <summary>
    /// Builds a map from lambda definitions to their call sites.
    /// </summary>
    public Dictionary<GDCallableDefinition, List<GDCallableCallSiteInfo>> BuildDefinitionToCallSitesMap()
    {
        var map = new Dictionary<GDCallableDefinition, List<GDCallableCallSiteInfo>>();

        foreach (var callSite in _callSites)
        {
            if (callSite.ResolvedDefinition == null)
                continue;

            if (!map.TryGetValue(callSite.ResolvedDefinition, out var sites))
            {
                sites = new List<GDCallableCallSiteInfo>();
                map[callSite.ResolvedDefinition] = sites;
            }

            sites.Add(callSite);
        }

        return map;
    }

    /// <summary>
    /// Clears all collected data.
    /// </summary>
    public void Clear()
    {
        _callSites.Clear();
        _tracker.Clear();
    }
}
