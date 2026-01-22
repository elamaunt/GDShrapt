using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for TypeFlow navigation.
/// Manages navigation state and provides basic graph building.
/// For advanced graph features (multi-level, union expansion), see Plugin's GDTypeFlowGraphBuilder.
/// </summary>
public class GDTypeFlowHandler : IGDTypeFlowHandler
{
    protected readonly GDScriptProject _project;
    private readonly Stack<GDTypeFlowNavigationEntry> _navigationStack = new();
    private readonly HashSet<string> _visitedNodesInSession = new();
    private readonly Dictionary<string, GDTypeFlowNode> _nodeRegistry = new();
    private int _nodeIdCounter;

    public GDTypeFlowHandler(GDScriptProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
    }

    /// <inheritdoc />
    public GDTypeFlowNode? CurrentNode { get; private set; }

    /// <inheritdoc />
    public GDTypeFlowNode? RootNode { get; private set; }

    /// <inheritdoc />
    public bool CanGoBack => _navigationStack.Count > 1;

    /// <inheritdoc />
    public string? CurrentSymbolName => CurrentNode?.Label;

    /// <summary>
    /// List of inflow nodes for the current node.
    /// </summary>
    public IReadOnlyList<GDTypeFlowNode> Inflows => CurrentNode?.Inflows ?? new List<GDTypeFlowNode>();

    /// <summary>
    /// List of outflow nodes for the current node.
    /// </summary>
    public IReadOnlyList<GDTypeFlowNode> Outflows => CurrentNode?.Outflows ?? new List<GDTypeFlowNode>();

    /// <inheritdoc />
    public virtual GDTypeFlowNode? ShowForSymbol(string symbolName, GDScriptFile script)
    {
        if (string.IsNullOrEmpty(symbolName) || script == null)
            return null;

        var node = BuildGraph(symbolName, script);
        if (node == null)
            return null;

        // Clear previous session
        _navigationStack.Clear();
        _visitedNodesInSession.Clear();

        // Set as root and current
        RootNode = node;
        CurrentNode = node;

        // Add to navigation stack
        _navigationStack.Push(new GDTypeFlowNavigationEntry(node, GDTypeFlowNavigationAction.Initial));
        _visitedNodesInSession.Add(node.Id);

        return node;
    }

    /// <inheritdoc />
    public virtual GDTypeFlowNode? ShowForPosition(string filePath, int line, int column)
    {
        var script = _project.GetScript(filePath);
        if (script?.SemanticModel == null || script.Class == null)
            return null;

        // Find the node at the position
        var finder = new GDPositionFinder(script.Class);
        var astNode = finder.FindNodeAtPosition(line, column);
        if (astNode == null)
            return null;

        // Get the symbol for this node
        var symbol = script.SemanticModel.GetSymbolForNode(astNode);
        if (symbol == null)
            return null;

        return ShowForSymbol(symbol.Name, script);
    }

    /// <inheritdoc />
    public virtual bool NavigateToNode(GDTypeFlowNode node)
    {
        if (node == null)
            return false;

        // Ensure the node has its inflows and outflows loaded
        EnsureNodeLoaded(node);

        // Track visited nodes for cycle detection
        _visitedNodesInSession.Add(node.Id);

        // Push to navigation stack
        _navigationStack.Push(new GDTypeFlowNavigationEntry(node, GDTypeFlowNavigationAction.ClickedNode));
        CurrentNode = node;

        return true;
    }

    /// <inheritdoc />
    public bool NavigateToInflow(int index)
    {
        if (index < 0 || index >= Inflows.Count)
            return false;

        return NavigateToNode(Inflows[index]);
    }

    /// <inheritdoc />
    public bool NavigateToOutflow(int index)
    {
        if (index < 0 || index >= Outflows.Count)
            return false;

        return NavigateToNode(Outflows[index]);
    }

    /// <inheritdoc />
    public bool NavigateToLabel(string label)
    {
        var node = GetNodeByLabel(label);
        if (node == null)
            return false;

        return NavigateToNode(node);
    }

    /// <inheritdoc />
    public bool GoBack()
    {
        if (!CanGoBack)
            return false;

        // Pop current
        _navigationStack.Pop();

        // Get previous
        var previous = _navigationStack.Peek();
        CurrentNode = previous.Node;

        return true;
    }

    /// <inheritdoc />
    public bool WouldCreateCycle(GDTypeFlowNode node)
    {
        return node != null && _visitedNodesInSession.Contains(node.Id);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _navigationStack.Clear();
        _visitedNodesInSession.Clear();
        _nodeRegistry.Clear();
        _nodeIdCounter = 0;
        CurrentNode = null;
        RootNode = null;
    }

    /// <summary>
    /// Finds a node by its label in both inflows and outflows.
    /// </summary>
    protected GDTypeFlowNode? GetNodeByLabel(string label)
    {
        if (CurrentNode == null || string.IsNullOrEmpty(label))
            return null;

        // Search in inflows
        var inflow = Inflows.FirstOrDefault(n =>
            n.Label != null && n.Label.Contains(label, StringComparison.OrdinalIgnoreCase));
        if (inflow != null)
            return inflow;

        // Search in outflows
        return Outflows.FirstOrDefault(n =>
            n.Label != null && n.Label.Contains(label, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a node from the registry by ID.
    /// </summary>
    public GDTypeFlowNode? GetNodeById(string nodeId)
    {
        return _nodeRegistry.TryGetValue(nodeId, out var node) ? node : null;
    }

    /// <summary>
    /// Ensures a node has its inflows and outflows loaded.
    /// </summary>
    protected virtual void EnsureNodeLoaded(GDTypeFlowNode node)
    {
        if (node == null)
            return;

        if (!node.AreInflowsLoaded && node.SourceScript != null)
        {
            LoadInflowsFor(node);
            node.AreInflowsLoaded = true;
        }

        if (!node.AreOutflowsLoaded && node.SourceScript != null)
        {
            LoadOutflowsFor(node);
            node.AreOutflowsLoaded = true;
        }
    }

    /// <summary>
    /// Loads inflows for a node.
    /// </summary>
    protected virtual void LoadInflowsFor(GDTypeFlowNode node)
    {
        // Base implementation: minimal loading
        // Advanced loading is done by Plugin's GDTypeFlowGraphBuilder
    }

    /// <summary>
    /// Loads outflows for a node.
    /// </summary>
    protected virtual void LoadOutflowsFor(GDTypeFlowNode node)
    {
        // Base implementation: minimal loading
        // Advanced loading is done by Plugin's GDTypeFlowGraphBuilder
    }

    /// <summary>
    /// Builds a basic type flow graph for a symbol.
    /// </summary>
    protected virtual GDTypeFlowNode? BuildGraph(string symbolName, GDScriptFile script)
    {
        if (script?.SemanticModel == null || string.IsNullOrEmpty(symbolName))
            return null;

        // Clear registry for new graph
        _nodeRegistry.Clear();
        _nodeIdCounter = 0;

        var semanticModel = script.SemanticModel;
        var symbol = semanticModel.FindSymbol(symbolName);
        if (symbol == null)
            return null;

        // Create the root node
        var rootNode = CreateNodeFromSymbol(symbol, script, semanticModel);
        if (rootNode == null)
            return null;

        RegisterNode(rootNode);

        // Add union type info
        var unionType = semanticModel.GetUnionType(symbolName);
        if (unionType?.IsUnion == true)
        {
            rootNode.IsUnionType = true;
            rootNode.UnionTypeInfo = unionType;
        }

        // Add duck type info
        var duckType = semanticModel.GetDuckType(symbolName);
        if (duckType?.HasRequirements == true)
        {
            rootNode.HasDuckConstraints = true;
            rootNode.DuckTypeInfo = duckType;
        }

        // Build basic inflows (type annotation)
        BuildBasicInflows(rootNode, symbol, script, semanticModel);
        rootNode.AreInflowsLoaded = true;

        // Build basic outflows (references)
        BuildBasicOutflows(rootNode, symbol, script, semanticModel);
        rootNode.AreOutflowsLoaded = true;

        return rootNode;
    }

    /// <summary>
    /// Creates a node from a symbol.
    /// </summary>
    protected virtual GDTypeFlowNode CreateNodeFromSymbol(
        Semantics.GDSymbolInfo symbol,
        GDScriptFile script,
        GDSemanticModel semanticModel)
    {
        var type = semanticModel.GetTypeForNode(symbol.DeclarationNode) ?? symbol.TypeName ?? "Variant";
        var kind = MapSymbolKind(symbol.Kind);
        var confidence = CalculateConfidence(symbol, type);

        return new GDTypeFlowNode
        {
            Id = GenerateNodeId(),
            Label = symbol.Name,
            Type = type,
            Kind = kind,
            Confidence = confidence,
            Description = GetSymbolDescription(symbol),
            Location = GDSourceLocation.FromNode(symbol.DeclarationNode, script.FullPath),
            SourceScript = script,
            AstNode = symbol.DeclarationNode
        };
    }

    /// <summary>
    /// Maps GDSymbolKind to GDTypeFlowNodeKind.
    /// </summary>
    protected static GDTypeFlowNodeKind MapSymbolKind(GDSymbolKind kind)
    {
        return kind switch
        {
            GDSymbolKind.Parameter => GDTypeFlowNodeKind.Parameter,
            GDSymbolKind.Variable => GDTypeFlowNodeKind.LocalVariable,
            GDSymbolKind.Iterator => GDTypeFlowNodeKind.LocalVariable,
            GDSymbolKind.Property => GDTypeFlowNodeKind.MemberVariable,
            GDSymbolKind.Method => GDTypeFlowNodeKind.MethodCall,
            GDSymbolKind.Signal => GDTypeFlowNodeKind.MemberVariable,
            GDSymbolKind.Constant => GDTypeFlowNodeKind.MemberVariable,
            GDSymbolKind.Class => GDTypeFlowNodeKind.BuiltinType,
            GDSymbolKind.Enum => GDTypeFlowNodeKind.BuiltinType,
            GDSymbolKind.EnumValue => GDTypeFlowNodeKind.Literal,
            GDSymbolKind.MatchCaseBinding => GDTypeFlowNodeKind.LocalVariable,
            _ => GDTypeFlowNodeKind.Unknown
        };
    }

    /// <summary>
    /// Calculates confidence for a symbol's type.
    /// </summary>
    protected virtual float CalculateConfidence(Semantics.GDSymbolInfo symbol, string type)
    {
        if (type == "Variant" || string.IsNullOrEmpty(type))
            return 0.2f;

        // Check for explicit type annotation
        if (symbol.TypeName != null)
            return 1.0f;

        // Check if it's a known type via RuntimeProvider
        var runtimeProvider = _project.CreateRuntimeProvider();
        if (runtimeProvider != null && runtimeProvider.IsKnownType(type))
            return 0.9f;

        return 0.6f;
    }

    /// <summary>
    /// Gets a description for a symbol.
    /// </summary>
    protected static string GetSymbolDescription(Semantics.GDSymbolInfo symbol)
    {
        var parts = new List<string>();

        parts.Add(symbol.Kind.ToString());

        if (symbol.DeclaringTypeName != null)
            parts.Add($"in {symbol.DeclaringTypeName}");

        if (symbol.IsInherited)
            parts.Add("(inherited)");

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Builds basic inflows (type annotation only).
    /// </summary>
    protected virtual void BuildBasicInflows(
        GDTypeFlowNode node,
        Semantics.GDSymbolInfo symbol,
        GDScriptFile script,
        GDSemanticModel semanticModel)
    {
        // Get type source from explicit annotation
        if (symbol.TypeName != null)
        {
            var annotationNode = new GDTypeFlowNode
            {
                Id = GenerateNodeId(),
                Label = $": {symbol.TypeName}",
                Type = symbol.TypeName,
                Kind = GDTypeFlowNodeKind.TypeAnnotation,
                Confidence = 1.0f,
                Description = "Explicit type annotation",
                Level = -1,
                SourceScript = script
            };
            RegisterNode(annotationNode);
            node.Inflows.Add(annotationNode);
            node.InflowNodeIds.Add(annotationNode.Id);
        }
    }

    /// <summary>
    /// Builds basic outflows (references to this symbol).
    /// </summary>
    protected virtual void BuildBasicOutflows(
        GDTypeFlowNode node,
        Semantics.GDSymbolInfo symbol,
        GDScriptFile script,
        GDSemanticModel semanticModel)
    {
        // Get references to this symbol
        var references = semanticModel.GetReferencesTo(symbol);
        foreach (var reference in references.Take(20))
        {
            if (reference.ReferenceNode == null)
                continue;

            var usageContext = GetUsageContext(reference);
            var usageType = semanticModel.GetTypeForNode(reference.ReferenceNode) ?? node.Type;
            var usageNode = new GDTypeFlowNode
            {
                Id = GenerateNodeId(),
                Label = GetUsageLabel(reference, usageContext),
                Type = usageType,
                Kind = GetUsageKind(usageContext),
                Confidence = CalculateExpressionConfidence(usageType),
                Description = usageContext,
                Level = 1,
                SourceScript = script,
                AstNode = reference.ReferenceNode,
                Location = GDSourceLocation.FromNode(reference.ReferenceNode, script.FullPath)
            };
            RegisterNode(usageNode);
            node.Outflows.Add(usageNode);
            node.OutflowNodeIds.Add(usageNode.Id);
        }
    }

    /// <summary>
    /// Gets the usage context description.
    /// </summary>
    protected static string GetUsageContext(GDReference reference)
    {
        var node = reference.ReferenceNode;
        var parent = node?.Parent;

        return parent switch
        {
            GDCallExpression => "Method call",
            GDMemberOperatorExpression => "Property access",
            GDIndexerExpression => "Indexer access",
            GDReturnExpression => "Return value",
            GDIfBranch => "Condition",
            GDMatchCaseDeclaration => "Match pattern",
            _ => reference.IsWrite ? "Assignment" : "Usage"
        };
    }

    /// <summary>
    /// Gets a label for a usage.
    /// </summary>
    protected static string GetUsageLabel(GDReference reference, string context)
    {
        var node = reference.ReferenceNode;
        var parent = node?.Parent;

        return parent switch
        {
            GDCallExpression call => $"{GetCallerName(call)}()",
            GDMemberOperatorExpression member => $".{member.Identifier}",
            GDIndexerExpression => "[...]",
            GDReturnExpression => "return",
            _ => context.ToLowerInvariant()
        };
    }

    /// <summary>
    /// Gets the caller name from a call expression.
    /// </summary>
    protected static string GetCallerName(GDCallExpression call)
    {
        return call.CallerExpression switch
        {
            GDIdentifierExpression id => id.Identifier?.ToString() ?? "call",
            GDMemberOperatorExpression member => member.Identifier?.ToString() ?? "method",
            _ => "call"
        };
    }

    /// <summary>
    /// Gets the kind for a usage node.
    /// </summary>
    protected static GDTypeFlowNodeKind GetUsageKind(string context)
    {
        return context switch
        {
            "Method call" => GDTypeFlowNodeKind.MethodCall,
            "Property access" => GDTypeFlowNodeKind.PropertyAccess,
            "Indexer access" => GDTypeFlowNodeKind.IndexerAccess,
            "Return value" => GDTypeFlowNodeKind.ReturnValue,
            "Condition" => GDTypeFlowNodeKind.Comparison,
            "Assignment" => GDTypeFlowNodeKind.Assignment,
            _ => GDTypeFlowNodeKind.Unknown
        };
    }

    /// <summary>
    /// Calculates confidence for an expression type.
    /// </summary>
    protected float CalculateExpressionConfidence(string type)
    {
        if (type == "Variant" || string.IsNullOrEmpty(type))
            return 0.2f;

        var runtimeProvider = _project.CreateRuntimeProvider();
        if (runtimeProvider != null && runtimeProvider.IsKnownType(type))
            return 0.9f;

        return 0.6f;
    }

    /// <summary>
    /// Generates a unique node ID.
    /// </summary>
    protected string GenerateNodeId()
    {
        return $"node_{_nodeIdCounter++}";
    }

    /// <summary>
    /// Registers a node in the registry.
    /// </summary>
    protected void RegisterNode(GDTypeFlowNode node)
    {
        if (node != null && !string.IsNullOrEmpty(node.Id))
        {
            _nodeRegistry[node.Id] = node;
        }
    }

    /// <inheritdoc />
    public virtual GDUnionType? ResolveUnionType(string symbolName, string filePath)
    {
        if (string.IsNullOrEmpty(symbolName) || string.IsNullOrEmpty(filePath))
            return null;

        var script = _project.GetScript(filePath);
        return script?.SemanticModel?.GetUnionType(symbolName);
    }

    /// <inheritdoc />
    public virtual GDDuckType? ResolveDuckType(string symbolName, string filePath)
    {
        if (string.IsNullOrEmpty(symbolName) || string.IsNullOrEmpty(filePath))
            return null;

        var script = _project.GetScript(filePath);
        return script?.SemanticModel?.GetDuckType(symbolName);
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDTypeFlowNode>? GetInflowNodes(string symbolName, string filePath)
    {
        if (string.IsNullOrEmpty(symbolName) || string.IsNullOrEmpty(filePath))
            return null;

        var script = _project.GetScript(filePath);
        if (script?.SemanticModel == null)
            return null;

        var node = ShowForSymbol(symbolName, script);
        return node?.Inflows;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDTypeFlowNode>? GetOutflowNodes(string symbolName, string filePath)
    {
        if (string.IsNullOrEmpty(symbolName) || string.IsNullOrEmpty(filePath))
            return null;

        var script = _project.GetScript(filePath);
        if (script?.SemanticModel == null)
            return null;

        var node = ShowForSymbol(symbolName, script);
        return node?.Outflows;
    }

    /// <inheritdoc />
    public virtual Semantics.GDSymbolInfo? FindSymbol(string symbolName, string filePath)
    {
        if (string.IsNullOrEmpty(symbolName) || string.IsNullOrEmpty(filePath))
            return null;

        var script = _project.GetScript(filePath);
        return script?.SemanticModel?.FindSymbol(symbolName);
    }

    /// <inheritdoc />
    public virtual string? ResolveTypeAtPosition(string filePath, int line, int column)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        var script = _project.GetScript(filePath);
        if (script?.SemanticModel == null || script.Class == null)
            return null;

        var finder = new GDPositionFinder(script.Class);
        var astNode = finder.FindNodeAtPosition(line, column);
        if (astNode == null)
            return null;

        return script.SemanticModel.GetTypeForNode(astNode);
    }
}
