using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.Plugin;

/// <summary>
/// Builds a type flow graph for a given symbol.
/// The graph shows how types flow through the code - where they come from (inflows)
/// and where they are used (outflows).
/// Supports multi-level graphs, union type expansion, and duck type constraints.
/// Supports unlimited depth traversal with lazy loading.
/// </summary>
internal class GDTypeFlowGraphBuilder
{
    private readonly GDScriptProject _project;
    private int _nodeIdCounter;

    /// <summary>
    /// Registry of all nodes in the graph, keyed by ID.
    /// Allows lazy loading of connected nodes and prevents duplicates.
    /// </summary>
    private readonly Dictionary<string, GDTypeFlowNode> _nodeRegistry = new();

    /// <summary>
    /// Maximum number of inflow levels to build in initial graph.
    /// Set to int.MaxValue for unlimited depth.
    /// </summary>
    public int MaxInflowLevels { get; set; } = int.MaxValue;

    /// <summary>
    /// Maximum number of outflow levels to build in initial graph.
    /// Set to int.MaxValue for unlimited depth.
    /// </summary>
    public int MaxOutflowLevels { get; set; } = int.MaxValue;

    /// <summary>
    /// Maximum number of nodes to build per level (to prevent explosion).
    /// </summary>
    public int MaxNodesPerLevel { get; set; } = 20;

    /// <summary>
    /// Whether to expand union types into separate source nodes.
    /// </summary>
    public bool ExpandUnionTypes { get; set; } = true;

    /// <summary>
    /// Whether to include duck type constraints.
    /// </summary>
    public bool IncludeDuckConstraints { get; set; } = true;

    public GDTypeFlowGraphBuilder(GDScriptProject project)
    {
        _project = project;
    }

    /// <summary>
    /// Gets a node from the registry by ID.
    /// </summary>
    public GDTypeFlowNode GetNodeById(string nodeId)
    {
        return _nodeRegistry.TryGetValue(nodeId, out var node) ? node : null;
    }

    /// <summary>
    /// Clears the node registry (call when starting a new graph).
    /// </summary>
    public void ClearRegistry()
    {
        _nodeRegistry.Clear();
        _nodeIdCounter = 0;
    }

    /// <summary>
    /// Loads inflows for a node that hasn't had its inflows loaded yet.
    /// This enables lazy loading for deep graph traversal.
    /// </summary>
    public void LoadInflowsFor(GDTypeFlowNode node)
    {
        if (node == null || node.AreInflowsLoaded)
            return;

        var script = node.SourceScript;
        if (script == null)
            return;

        var analyzer = EnsureAnalyzer(script);
        if (analyzer == null)
            return;

        var symbol = analyzer.FindSymbol(node.Label);
        if (symbol != null)
        {
            BuildMultiLevelInflows(node, symbol, script, analyzer, 1);
        }

        node.AreInflowsLoaded = true;
    }

    /// <summary>
    /// Loads outflows for a node that hasn't had its outflows loaded yet.
    /// This enables lazy loading for deep graph traversal.
    /// </summary>
    public void LoadOutflowsFor(GDTypeFlowNode node)
    {
        if (node == null || node.AreOutflowsLoaded)
            return;

        var script = node.SourceScript;
        if (script == null)
            return;

        var analyzer = EnsureAnalyzer(script);
        if (analyzer == null)
            return;

        var symbol = analyzer.FindSymbol(node.Label);
        if (symbol != null)
        {
            BuildMultiLevelOutflows(node, symbol, script, analyzer, 1);
        }

        node.AreOutflowsLoaded = true;
    }

    /// <summary>
    /// Builds a type flow graph for the specified symbol.
    /// </summary>
    /// <param name="symbolName">The name of the symbol to analyze.</param>
    /// <param name="script">The script containing the symbol.</param>
    /// <returns>The root node of the graph, or null if symbol not found.</returns>
    public GDTypeFlowNode BuildGraph(string symbolName, GDScriptFile script)
    {
        if (script == null || string.IsNullOrEmpty(symbolName))
            return null;

        // Clear registry for new graph
        ClearRegistry();

        var analyzer = EnsureAnalyzer(script);
        if (analyzer == null)
            return null;

        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol == null)
            return null;

        // Create the root node for the symbol
        var rootNode = CreateNodeFromSymbol(symbol, script, analyzer);
        if (rootNode == null)
            return null;

        // Register the root node
        RegisterNode(rootNode);

        // Add union type info if applicable
        AddUnionTypeInfo(rootNode, symbolName, analyzer);

        // Add duck type info if applicable
        AddDuckTypeInfo(rootNode, symbolName, analyzer);

        // Build multi-level inflows
        BuildMultiLevelInflows(rootNode, symbol, script, analyzer, 1);
        rootNode.AreInflowsLoaded = true;

        // Build multi-level outflows
        BuildMultiLevelOutflows(rootNode, symbol, script, analyzer, 1);
        rootNode.AreOutflowsLoaded = true;

        // Expand union types if enabled
        if (ExpandUnionTypes)
        {
            ExpandUnionTypeNodes(rootNode, script, analyzer);
        }

        // Create edge objects from Inflows/Outflows relationships
        CreateEdges(rootNode);

        return rootNode;
    }

    /// <summary>
    /// Registers a node in the registry.
    /// </summary>
    private void RegisterNode(GDTypeFlowNode node)
    {
        if (node != null && !string.IsNullOrEmpty(node.Id))
        {
            _nodeRegistry[node.Id] = node;
        }
    }

    /// <summary>
    /// Creates GDTypeFlowEdge objects from the Inflows/Outflows relationships.
    /// </summary>
    private void CreateEdges(GDTypeFlowNode rootNode)
    {
        var visited = new HashSet<string>();
        var edgeIdCounter = 0;
        CreateEdgesRecursive(rootNode, visited, ref edgeIdCounter);
    }

    /// <summary>
    /// Recursively creates edges for all nodes in the graph.
    /// </summary>
    private void CreateEdgesRecursive(GDTypeFlowNode node, HashSet<string> visited, ref int edgeIdCounter)
    {
        if (node == null || string.IsNullOrEmpty(node.Id) || visited.Contains(node.Id))
            return;

        visited.Add(node.Id);

        // Create edges for inflows (source → this node)
        foreach (var inflow in node.Inflows)
        {
            if (inflow == null)
                continue;

            var edge = new GDTypeFlowEdge
            {
                Id = $"edge_{edgeIdCounter++}",
                Source = inflow,
                Target = node,
                Kind = DetermineEdgeKind(inflow, node),
                Confidence = Math.Min(inflow.Confidence, node.Confidence)
            };

            // Add duck type constraints if applicable
            if (inflow.HasDuckConstraints && inflow.DuckTypeInfo != null)
            {
                edge.Constraints = GDEdgeConstraints.FromDuckType(inflow.DuckTypeInfo);
            }

            inflow.OutgoingEdges.Add(edge);
            node.IncomingEdges.Add(edge);

            // Recurse into inflow
            CreateEdgesRecursive(inflow, visited, ref edgeIdCounter);
        }

        // Create edges for outflows (this node → target)
        foreach (var outflow in node.Outflows)
        {
            if (outflow == null)
                continue;

            var edge = new GDTypeFlowEdge
            {
                Id = $"edge_{edgeIdCounter++}",
                Source = node,
                Target = outflow,
                Kind = DetermineEdgeKind(node, outflow),
                Confidence = Math.Min(node.Confidence, outflow.Confidence)
            };

            // Add duck type constraints if applicable
            if (node.HasDuckConstraints && node.DuckTypeInfo != null)
            {
                edge.Constraints = GDEdgeConstraints.FromDuckType(node.DuckTypeInfo);
            }

            node.OutgoingEdges.Add(edge);
            outflow.IncomingEdges.Add(edge);

            // Recurse into outflow
            CreateEdgesRecursive(outflow, visited, ref edgeIdCounter);
        }
    }

    /// <summary>
    /// Determines the edge kind based on source and target node kinds.
    /// </summary>
    private GDTypeFlowEdgeKind DetermineEdgeKind(GDTypeFlowNode source, GDTypeFlowNode target)
    {
        // Type annotation edges
        if (source.Kind == GDTypeFlowNodeKind.TypeAnnotation)
            return GDTypeFlowEdgeKind.TypeFlow;

        // Return value edges
        if (source.Kind == GDTypeFlowNodeKind.ReturnValue || target.Kind == GDTypeFlowNodeKind.ReturnValue)
            return GDTypeFlowEdgeKind.Return;

        // Assignment edges
        if (source.Kind == GDTypeFlowNodeKind.Assignment || target.Kind == GDTypeFlowNodeKind.Assignment)
            return GDTypeFlowEdgeKind.Assignment;

        // Union member edges
        if (source.Description?.Contains("Union member") == true)
            return GDTypeFlowEdgeKind.UnionMember;

        // Duck constraint edges
        if (source.HasDuckConstraints || target.HasDuckConstraints)
            return GDTypeFlowEdgeKind.DuckConstraint;

        // Default to type flow
        return GDTypeFlowEdgeKind.TypeFlow;
    }

    /// <summary>
    /// Adds union type information to a node.
    /// </summary>
    private void AddUnionTypeInfo(GDTypeFlowNode node, string symbolName, GDScriptAnalyzer analyzer)
    {
        var semanticModel = analyzer.SemanticModel;
        if (semanticModel == null)
            return;

        var unionType = semanticModel.GetUnionType(symbolName);
        if (unionType != null && unionType.IsUnion)
        {
            node.IsUnionType = true;
            node.UnionTypeInfo = unionType;
            node.Type = string.Join("|", unionType.Types.Take(3));
            if (unionType.Types.Count > 3)
                node.Type += "...";
        }
    }

    /// <summary>
    /// Adds duck type information to a node.
    /// </summary>
    private void AddDuckTypeInfo(GDTypeFlowNode node, string symbolName, GDScriptAnalyzer analyzer)
    {
        if (!IncludeDuckConstraints)
            return;

        var duckType = analyzer.GetDuckType(symbolName);
        if (duckType != null && duckType.HasRequirements)
        {
            node.HasDuckConstraints = true;
            node.DuckTypeInfo = duckType;
        }
    }

    /// <summary>
    /// Builds multi-level inflows recursively.
    /// </summary>
    private void BuildMultiLevelInflows(GDTypeFlowNode targetNode, GDSymbolInfo symbol, GDScriptFile script, GDScriptAnalyzer analyzer, int currentLevel)
    {
        if (currentLevel > MaxInflowLevels)
            return;

        var decl = symbol?.DeclarationNode;
        if (decl == null)
            return;

        // Check for explicit type annotation
        if (HasExplicitTypeAnnotation(decl, out var annotationType))
        {
            var annotationNode = new GDTypeFlowNode
            {
                Id = GenerateNodeId(),
                Label = "Type annotation",
                Type = annotationType,
                Kind = GDTypeFlowNodeKind.TypeAnnotation,
                Confidence = 1.0f,
                Description = "Explicitly declared type",
                Location = GDSourceLocation.FromNode(decl, script.FullPath),
                SourceScript = script
            };
            targetNode.Inflows.Add(annotationNode);
        }

        // For parameters, check for default value
        if (decl is GDParameterDeclaration param && param.DefaultValue != null)
        {
            var defaultNode = CreateInflowFromExpression(param.DefaultValue, script, analyzer, "Default value", currentLevel);
            if (defaultNode != null)
            {
                targetNode.Inflows.Add(defaultNode);
                // Recurse into default value expression
                BuildInflowsFromExpression(defaultNode, param.DefaultValue, script, analyzer, currentLevel + 1);
            }
        }

        // For variables, check initialization
        if (decl is GDVariableDeclaration variable && variable.Initializer != null)
        {
            var initNode = CreateInflowFromExpression(variable.Initializer, script, analyzer, "Initialization", currentLevel);
            if (initNode != null)
            {
                targetNode.Inflows.Add(initNode);
                // Recurse into initializer
                BuildInflowsFromExpression(initNode, variable.Initializer, script, analyzer, currentLevel + 1);
            }
        }

        // Check assignments to this symbol
        var assignments = FindAssignmentsTo(symbol.Name, script, analyzer);
        foreach (var assignment in assignments.Take(5))
        {
            if (assignment.RightExpression != null)
            {
                var assignNode = CreateInflowFromExpression(assignment.RightExpression, script, analyzer, "Assignment", currentLevel);
                if (assignNode != null)
                {
                    targetNode.Inflows.Add(assignNode);
                    // Recurse into assignment
                    BuildInflowsFromExpression(assignNode, assignment.RightExpression, script, analyzer, currentLevel + 1);
                }
            }
        }
    }

    /// <summary>
    /// Builds inflows from an expression (for recursive multi-level).
    /// </summary>
    private void BuildInflowsFromExpression(GDTypeFlowNode targetNode, GDNode expression, GDScriptFile script, GDScriptAnalyzer analyzer, int currentLevel)
    {
        if (currentLevel > MaxInflowLevels)
            return;

        // For call expressions, trace the method return type
        if (expression is GDCallExpression call)
        {
            // Try to find the method being called
            if (call.CallerExpression is GDIdentifierExpression idExpr)
            {
                var methodSymbol = analyzer.FindSymbol(idExpr.Identifier?.Sequence);
                if (methodSymbol != null)
                {
                    // Create node for the method return
                    var methodNode = CreateNodeFromSymbol(methodSymbol, script, analyzer);
                    if (methodNode != null)
                    {
                        methodNode.Description = "Method definition";
                        targetNode.Inflows.Add(methodNode);
                    }
                }
            }
        }
        // For identifier expressions, trace the variable
        else if (expression is GDIdentifierExpression idExpr)
        {
            var varSymbol = analyzer.FindSymbol(idExpr.Identifier?.Sequence);
            if (varSymbol != null)
            {
                var varNode = CreateNodeFromSymbol(varSymbol, script, analyzer);
                if (varNode != null)
                {
                    targetNode.Inflows.Add(varNode);
                    // Recurse into this variable's sources
                    BuildMultiLevelInflows(varNode, varSymbol, script, analyzer, currentLevel + 1);
                }
            }
        }
        // For member access, trace the member
        else if (expression is GDMemberOperatorExpression memberAccess)
        {
            var memberNode = CreateNodeFromMemberAccess(memberAccess, script, analyzer);
            if (memberNode != null)
            {
                targetNode.Inflows.Add(memberNode);
            }
        }
    }

    /// <summary>
    /// Builds multi-level outflows recursively.
    /// </summary>
    private void BuildMultiLevelOutflows(GDTypeFlowNode sourceNode, GDSymbolInfo symbol, GDScriptFile script, GDScriptAnalyzer analyzer, int currentLevel)
    {
        if (currentLevel > MaxOutflowLevels)
            return;

        var refs = analyzer.GetReferencesTo(symbol);
        var processedLocations = new HashSet<(int, int)>();

        foreach (var reference in refs.Take(10))
        {
            if (reference.ReferenceNode == null)
                continue;

            var loc = (reference.ReferenceNode.StartLine, reference.ReferenceNode.StartColumn);
            if (!processedLocations.Add(loc))
                continue;

            var parent = reference.ReferenceNode.Parent;

            // Handle different parent expression types
            if (parent is GDCallExpression call)
            {
                var callNode = CreateNodeFromCall(call, script, analyzer, reference);
                if (callNode != null)
                {
                    AddDuckTypeInfo(callNode, symbol.Name, analyzer);
                    sourceNode.Outflows.Add(callNode);
                    // Recurse into call usages
                    BuildOutflowsFromCall(callNode, call, script, analyzer, currentLevel + 1);
                }
            }
            else if (parent is GDIndexerExpression indexer)
            {
                // Indexer access: symbol[key]
                var indexerNode = CreateNodeFromIndexer(indexer, script, analyzer);
                if (indexerNode != null)
                {
                    sourceNode.Outflows.Add(indexerNode);
                }
            }
            else if (parent is GDMemberOperatorExpression memberAccess)
            {
                var accessNode = CreateNodeFromMemberAccess(memberAccess, script, analyzer);
                if (accessNode != null)
                {
                    sourceNode.Outflows.Add(accessNode);
                }
            }
            else if (parent is GDDualOperatorExpression dualOp)
            {
                // Type check, null check, comparison
                var opNode = CreateNodeFromDualOperator(dualOp, script, analyzer);
                if (opNode != null)
                {
                    sourceNode.Outflows.Add(opNode);
                }
            }
            else if (parent is GDReturnExpression ret)
            {
                // Return statement
                var returnNode = CreateNodeFromReturn(ret, script, analyzer);
                if (returnNode != null)
                {
                    sourceNode.Outflows.Add(returnNode);
                }
            }
            else
            {
                // Find parent dual operator or return up the tree
                var foundNode = FindAndCreateParentContextNode(reference.ReferenceNode, script, analyzer);
                if (foundNode != null)
                {
                    sourceNode.Outflows.Add(foundNode);
                }
                else
                {
                    // Generic usage node
                    var usageNode = CreateGenericUsageNode(reference, script);
                    sourceNode.Outflows.Add(usageNode);
                }
            }
        }
    }

    /// <summary>
    /// Finds parent context (return, dual operator) and creates a node.
    /// </summary>
    private GDTypeFlowNode FindAndCreateParentContextNode(GDNode node, GDScriptFile script, GDScriptAnalyzer analyzer)
    {
        var current = node.Parent;
        int depth = 0;

        while (current != null && depth < 5)
        {
            if (current is GDReturnExpression ret)
            {
                return CreateNodeFromReturn(ret, script, analyzer);
            }
            if (current is GDDualOperatorExpression dualOp)
            {
                return CreateNodeFromDualOperator(dualOp, script, analyzer);
            }
            if (current is GDIndexerExpression indexer)
            {
                return CreateNodeFromIndexer(indexer, script, analyzer);
            }
            current = current.Parent;
            depth++;
        }
        return null;
    }

    /// <summary>
    /// Creates a generic usage node for unclassified references.
    /// </summary>
    private GDTypeFlowNode CreateGenericUsageNode(GDReference reference, GDScriptFile script)
    {
        var node = reference.ReferenceNode;
        var kind = GetNodeKindFromExpression(node);
        var label = GetLabelFromExpression(node);

        return new GDTypeFlowNode
        {
            Id = GenerateNodeId(),
            Label = label,
            Type = reference.InferredType ?? "Variant",
            Kind = kind,
            Confidence = reference.Confidence == GDReferenceConfidence.Strict ? 0.9f : 0.5f,
            Description = $"Line {node.StartLine + 1}",
            Location = GDSourceLocation.FromNode(node, script.FullPath),
            SourceScript = script,
            AstNode = node
        };
    }

    /// <summary>
    /// Creates a node from an indexer expression.
    /// </summary>
    private GDTypeFlowNode CreateNodeFromIndexer(GDIndexerExpression indexer, GDScriptFile script, GDScriptAnalyzer analyzer)
    {
        var sourceObjectName = GetRootIdentifierName(indexer.CallerExpression);
        var sourceType = GetSourceType(indexer.CallerExpression, analyzer);
        var resultType = analyzer.GetTypeForNode(indexer) ?? "Variant";
        var label = GetIndexerLabel(indexer);
        var description = GetDetailedDescription(indexer, GDTypeFlowNodeKind.IndexerAccess, null, analyzer);

        return new GDTypeFlowNode
        {
            Id = GenerateNodeId(),
            Label = label,
            Type = resultType,
            Kind = GDTypeFlowNodeKind.IndexerAccess,
            Confidence = !string.IsNullOrEmpty(sourceType) ? 0.8f : 0.5f,
            Description = description,
            SourceType = sourceType,
            SourceObjectName = sourceObjectName,
            Location = GDSourceLocation.FromNode(indexer, script.FullPath),
            SourceScript = script,
            AstNode = indexer
        };
    }

    /// <summary>
    /// Creates a node from a dual operator expression (type check, null check, comparison).
    /// </summary>
    private GDTypeFlowNode CreateNodeFromDualOperator(GDDualOperatorExpression dualOp, GDScriptFile script, GDScriptAnalyzer analyzer)
    {
        var kind = GetKindForDualOperator(dualOp);
        var label = GetDualOperatorLabel(dualOp);
        var resultType = InferImprovedType(dualOp, analyzer);
        var description = GetDetailedDescription(dualOp, kind, null, analyzer);

        return new GDTypeFlowNode
        {
            Id = GenerateNodeId(),
            Label = label,
            Type = resultType,
            Kind = kind,
            Confidence = 0.9f, // Type checks and comparisons have high confidence
            Description = description,
            Location = GDSourceLocation.FromNode(dualOp, script.FullPath),
            SourceScript = script,
            AstNode = dualOp
        };
    }

    /// <summary>
    /// Creates a node from a return expression.
    /// </summary>
    private GDTypeFlowNode CreateNodeFromReturn(GDReturnExpression ret, GDScriptFile script, GDScriptAnalyzer analyzer)
    {
        var label = GetReturnLabel(ret);
        var returnedType = ret.Expression != null
            ? (analyzer.GetTypeForNode(ret.Expression) ?? "Variant")
            : "void";
        var description = GetDetailedDescription(ret, GDTypeFlowNodeKind.ReturnValue, null, analyzer);

        return new GDTypeFlowNode
        {
            Id = GenerateNodeId(),
            Label = label,
            Type = returnedType,
            Kind = GDTypeFlowNodeKind.ReturnValue,
            Confidence = 0.9f,
            Description = description,
            Location = GDSourceLocation.FromNode(ret, script.FullPath),
            SourceScript = script,
            AstNode = ret
        };
    }

    /// <summary>
    /// Builds outflows from a call expression.
    /// </summary>
    private void BuildOutflowsFromCall(GDTypeFlowNode sourceNode, GDCallExpression call, GDScriptFile script, GDScriptAnalyzer analyzer, int currentLevel)
    {
        if (currentLevel > MaxOutflowLevels)
            return;

        // Find where the call result is used
        var parent = call.Parent;
        if (parent is GDDualOperatorExpression assignment && assignment.LeftExpression != null)
        {
            // Result assigned to variable
            if (assignment.LeftExpression is GDIdentifierExpression idExpr)
            {
                var varSymbol = analyzer.FindSymbol(idExpr.Identifier?.Sequence);
                if (varSymbol != null)
                {
                    var varNode = CreateNodeFromSymbol(varSymbol, script, analyzer);
                    if (varNode != null)
                    {
                        varNode.Description = "Receives result";
                        sourceNode.Outflows.Add(varNode);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Expands union type nodes by creating separate inflow nodes for each type source.
    /// </summary>
    private void ExpandUnionTypeNodes(GDTypeFlowNode rootNode, GDScriptFile script, GDScriptAnalyzer analyzer)
    {
        if (!rootNode.IsUnionType || rootNode.UnionTypeInfo == null)
            return;

        var unionTypes = rootNode.UnionTypeInfo.Types;
        var assignments = FindAssignmentsTo(rootNode.Label, script, analyzer).ToList();

        // Try to trace each union type to its source
        foreach (var typeName in unionTypes.Take(5))
        {
            // Find assignment that produces this type
            foreach (var assignment in assignments)
            {
                if (assignment.RightExpression == null)
                    continue;

                var exprType = analyzer.GetTypeForNode(assignment.RightExpression);
                if (exprType == typeName)
                {
                    var unionSourceNode = new GDTypeFlowNode
                    {
                        Id = GenerateNodeId(),
                        Label = GetLabelFromExpression(assignment.RightExpression),
                        Type = typeName,
                        Kind = GetNodeKindFromExpression(assignment.RightExpression),
                        Confidence = 0.8f,
                        Description = $"Union member: {typeName}",
                        Location = GDSourceLocation.FromNode(assignment.RightExpression, script.FullPath),
                        SourceScript = script,
                        AstNode = assignment.RightExpression
                    };
                    rootNode.UnionSources.Add(unionSourceNode);
                    // Also add to inflows so they appear in the graph
                    if (!rootNode.Inflows.Any(n => n.Type == typeName))
                    {
                        rootNode.Inflows.Add(unionSourceNode);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Creates a node from a symbol.
    /// </summary>
    private GDTypeFlowNode CreateNodeFromSymbol(GDSymbolInfo symbol, GDScriptFile script, GDScriptAnalyzer analyzer)
    {
        var decl = symbol.DeclarationNode;
        if (decl == null)
            return null;

        var typeStr = analyzer.GetTypeForNode(decl) ?? "Variant";
        var kind = GetNodeKindFromSymbol(symbol);
        var confidence = CalculateConfidence(symbol, typeStr, analyzer);

        var node = new GDTypeFlowNode
        {
            Id = GenerateNodeId(),
            Label = symbol.Name,
            Type = typeStr,
            Kind = kind,
            Confidence = confidence,
            Description = GetDescriptionForKind(kind),
            Location = GDSourceLocation.FromNode(decl, script.FullPath),
            SourceScript = script,
            AstNode = decl
        };

        return node;
    }

    /// <summary>
    /// Creates an inflow node from an expression.
    /// </summary>
    private GDTypeFlowNode CreateInflowFromExpression(GDNode expression, GDScriptFile script, GDScriptAnalyzer analyzer, string context, int level)
    {
        if (expression == null)
            return null;

        var exprType = InferImprovedType(expression, analyzer);
        var kind = GetNodeKindFromExpression(expression);
        var label = GetLabelFromExpression(expression);
        var description = GetDetailedDescription(expression, kind, context, analyzer);

        string sourceType = null;
        string sourceObjectName = null;

        // Fill SourceType and SourceObjectName for applicable expression types
        if (expression is GDCallExpression call)
        {
            if (call.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                sourceObjectName = GetRootIdentifierName(memberOp.CallerExpression);
                sourceType = GetSourceType(memberOp.CallerExpression, analyzer);
            }
        }
        else if (expression is GDIndexerExpression indexer)
        {
            sourceObjectName = GetRootIdentifierName(indexer.CallerExpression);
            sourceType = GetSourceType(indexer.CallerExpression, analyzer);
        }
        else if (expression is GDMemberOperatorExpression memberAccess)
        {
            sourceObjectName = GetRootIdentifierName(memberAccess.CallerExpression);
            sourceType = GetSourceType(memberAccess.CallerExpression, analyzer);
        }

        return new GDTypeFlowNode
        {
            Id = GenerateNodeId(),
            Label = label,
            Type = exprType,
            Kind = kind,
            Confidence = CalculateExpressionConfidence(kind, sourceType),
            Description = description,
            SourceType = sourceType,
            SourceObjectName = sourceObjectName,
            Location = GDSourceLocation.FromNode(expression, script.FullPath),
            SourceScript = script,
            AstNode = expression
        };
    }

    /// <summary>
    /// Calculates confidence based on expression kind and source type availability.
    /// </summary>
    private float CalculateExpressionConfidence(GDTypeFlowNodeKind kind, string sourceType)
    {
        // Higher confidence if we know the source type
        if (!string.IsNullOrEmpty(sourceType))
            return 0.8f;

        return kind switch
        {
            GDTypeFlowNodeKind.TypeCheck or GDTypeFlowNodeKind.NullCheck or GDTypeFlowNodeKind.Comparison => 0.9f,
            GDTypeFlowNodeKind.Literal => 0.95f,
            GDTypeFlowNodeKind.MethodCall => 0.7f,
            _ => 0.6f
        };
    }

    /// <summary>
    /// Creates a node from a call expression.
    /// </summary>
    private GDTypeFlowNode CreateNodeFromCall(GDCallExpression call, GDScriptFile script, GDScriptAnalyzer analyzer, GDReference reference)
    {
        string sourceObjectName = null;
        string sourceType = null;

        if (call.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            sourceObjectName = GetRootIdentifierName(memberOp.CallerExpression);
            sourceType = GetSourceType(memberOp.CallerExpression, analyzer);
        }

        var returnType = InferImprovedType(call, analyzer);
        var label = GetCallLabel(call);
        var description = GetDetailedDescription(call, GDTypeFlowNodeKind.MethodCall, null, analyzer);

        return new GDTypeFlowNode
        {
            Id = GenerateNodeId(),
            Label = label,
            Type = returnType,
            Kind = GDTypeFlowNodeKind.MethodCall,
            Confidence = !string.IsNullOrEmpty(sourceType) ? 0.8f :
                         (reference.Confidence == GDReferenceConfidence.Strict ? 0.7f : 0.5f),
            Description = description,
            SourceType = sourceType,
            SourceObjectName = sourceObjectName,
            Location = GDSourceLocation.FromNode(call, script.FullPath),
            SourceScript = script,
            AstNode = call
        };
    }

    /// <summary>
    /// Creates a node from a member access expression.
    /// </summary>
    private GDTypeFlowNode CreateNodeFromMemberAccess(GDMemberOperatorExpression memberAccess, GDScriptFile script, GDScriptAnalyzer analyzer)
    {
        var sourceObjectName = GetRootIdentifierName(memberAccess.CallerExpression);
        var sourceType = GetSourceType(memberAccess.CallerExpression, analyzer);
        var memberType = analyzer.GetTypeForNode(memberAccess) ?? "Variant";
        var label = GetMemberAccessLabel(memberAccess);
        var description = GetDetailedDescription(memberAccess, GDTypeFlowNodeKind.PropertyAccess, null, analyzer);

        return new GDTypeFlowNode
        {
            Id = GenerateNodeId(),
            Label = label,
            Type = memberType,
            Kind = GDTypeFlowNodeKind.PropertyAccess,
            Confidence = !string.IsNullOrEmpty(sourceType) ? 0.8f : 0.6f,
            Description = description,
            SourceType = sourceType,
            SourceObjectName = sourceObjectName,
            Location = GDSourceLocation.FromNode(memberAccess, script.FullPath),
            SourceScript = script,
            AstNode = memberAccess
        };
    }

    /// <summary>
    /// Finds assignment statements to a variable.
    /// </summary>
    private IEnumerable<GDDualOperatorExpression> FindAssignmentsTo(string variableName, GDScriptFile script, GDScriptAnalyzer analyzer)
    {
        if (script.Class == null)
            yield break;

        foreach (var expr in script.Class.AllNodes.OfType<GDDualOperatorExpression>())
        {
            if (expr.OperatorType != GDDualOperatorType.Assignment &&
                expr.OperatorType != GDDualOperatorType.AddAndAssign &&
                expr.OperatorType != GDDualOperatorType.SubtractAndAssign &&
                expr.OperatorType != GDDualOperatorType.MultiplyAndAssign &&
                expr.OperatorType != GDDualOperatorType.DivideAndAssign)
                continue;

            var leftStr = expr.LeftExpression?.ToString();
            if (leftStr == variableName)
            {
                yield return expr;
            }
        }
    }

    /// <summary>
    /// Checks if a declaration has an explicit type annotation.
    /// </summary>
    private bool HasExplicitTypeAnnotation(GDNode decl, out string annotationType)
    {
        annotationType = null;

        if (decl is GDVariableDeclaration variable && variable.Type != null)
        {
            annotationType = variable.Type.ToString();
            return true;
        }

        if (decl is GDParameterDeclaration param && param.Type != null)
        {
            annotationType = param.Type.ToString();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates confidence based on symbol and type information.
    /// </summary>
    private float CalculateConfidence(GDSymbolInfo symbol, string type, GDScriptAnalyzer analyzer)
    {
        if (type == "Variant" || string.IsNullOrEmpty(type))
            return 0.2f;

        if (HasExplicitTypeAnnotation(symbol.DeclarationNode, out _))
            return 1.0f;

        if (IsBuiltinType(type))
            return 0.9f;

        return 0.6f;
    }

    /// <summary>
    /// Checks if a type is a known built-in type.
    /// </summary>
    private bool IsBuiltinType(string type)
    {
        var builtins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "int", "float", "bool", "String", "Vector2", "Vector3", "Vector4",
            "Rect2", "Transform2D", "Transform3D", "Color", "NodePath", "RID",
            "Object", "Dictionary", "Array", "PackedByteArray", "PackedInt32Array",
            "PackedFloat32Array", "PackedStringArray", "PackedVector2Array",
            "PackedVector3Array", "PackedColorArray", "Callable", "Signal"
        };
        return builtins.Contains(type);
    }

    /// <summary>
    /// Gets the node kind from a symbol.
    /// </summary>
    private GDTypeFlowNodeKind GetNodeKindFromSymbol(GDSymbolInfo symbol)
    {
        return symbol.Kind switch
        {
            GDSymbolKind.Parameter => GDTypeFlowNodeKind.Parameter,
            GDSymbolKind.Variable => GDTypeFlowNodeKind.LocalVariable,
            GDSymbolKind.Method => GDTypeFlowNodeKind.MethodCall,
            GDSymbolKind.Property => GDTypeFlowNodeKind.MemberVariable,
            _ => GDTypeFlowNodeKind.Unknown
        };
    }

    /// <summary>
    /// Gets the node kind from an expression.
    /// </summary>
    private GDTypeFlowNodeKind GetNodeKindFromExpression(GDNode expression)
    {
        return expression switch
        {
            // Calls and indexing
            GDCallExpression => GDTypeFlowNodeKind.MethodCall,
            GDIndexerExpression => GDTypeFlowNodeKind.IndexerAccess,

            // Literals
            GDNumberExpression or GDStringExpression or GDBoolExpression => GDTypeFlowNodeKind.Literal,

            // Identifiers - determine by symbol if possible
            GDIdentifierExpression => GDTypeFlowNodeKind.LocalVariable,

            // Member access - distinguish property from method (method handled by CallExpression)
            GDMemberOperatorExpression => GDTypeFlowNodeKind.PropertyAccess,

            // Operators
            GDDualOperatorExpression dualOp => GetKindForDualOperator(dualOp),

            // Return statement
            GDReturnExpression => GDTypeFlowNodeKind.ReturnValue,

            _ => GDTypeFlowNodeKind.Unknown
        };
    }

    /// <summary>
    /// Gets the node kind for a dual operator expression.
    /// </summary>
    private GDTypeFlowNodeKind GetKindForDualOperator(GDDualOperatorExpression dualOp)
    {
        var opType = dualOp.OperatorType;

        // Type checks: x is Type
        if (opType == GDDualOperatorType.Is)
            return GDTypeFlowNodeKind.TypeCheck;

        // Null checks: x == null, x != null
        if ((opType == GDDualOperatorType.Equal || opType == GDDualOperatorType.NotEqual) &&
            IsNullLiteral(dualOp.RightExpression))
            return GDTypeFlowNodeKind.NullCheck;

        // Comparisons
        if (IsComparisonOperator(opType))
            return GDTypeFlowNodeKind.Comparison;

        return GDTypeFlowNodeKind.Unknown;
    }

    /// <summary>
    /// Checks if an expression is a null literal.
    /// </summary>
    private bool IsNullLiteral(GDExpression expression)
    {
        if (expression is GDIdentifierExpression idExpr)
        {
            var name = idExpr.Identifier?.Sequence;
            return string.Equals(name, "null", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// Checks if an operator type is a comparison operator.
    /// </summary>
    private bool IsComparisonOperator(GDDualOperatorType opType)
    {
        return opType switch
        {
            GDDualOperatorType.Equal => true,
            GDDualOperatorType.NotEqual => true,
            GDDualOperatorType.LessThan => true,
            GDDualOperatorType.MoreThan => true,
            GDDualOperatorType.LessThanOrEqual => true,
            GDDualOperatorType.MoreThanOrEqual => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets a label from an expression.
    /// </summary>
    private string GetLabelFromExpression(GDNode expression)
    {
        return expression switch
        {
            // Call: result.get("tag") or method()
            GDCallExpression call => GetCallLabel(call),

            // Indexer: result["key"] or array[i]
            GDIndexerExpression indexer => GetIndexerLabel(indexer),

            // Member: result.property
            GDMemberOperatorExpression ma => GetMemberAccessLabel(ma),

            // Return: return current
            GDReturnExpression ret => GetReturnLabel(ret),

            // Operators: current == null, current is Dictionary
            GDDualOperatorExpression dualOp => GetDualOperatorLabel(dualOp),

            // Identifier
            GDIdentifierExpression id => id.Identifier?.Sequence ?? "var",

            // Literals
            GDNumberExpression num => num.ToString(),
            GDStringExpression str => TruncateText(str.ToString(), 20),

            _ => TruncateText(expression.ToString(), 25)
        };
    }

    /// <summary>
    /// Gets label for a call expression like result.get("tag").
    /// </summary>
    private string GetCallLabel(GDCallExpression call)
    {
        if (call.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            var objName = GetRootIdentifierName(memberOp.CallerExpression) ?? "?";
            var methodName = memberOp.Identifier?.Sequence ?? "method";
            return TruncateText($"{objName}.{methodName}()", 30);
        }
        return TruncateText((call.CallerExpression?.ToString() ?? "call") + "()", 25);
    }

    /// <summary>
    /// Gets label for an indexer expression like result["key"].
    /// </summary>
    private string GetIndexerLabel(GDIndexerExpression indexer)
    {
        var objName = GetRootIdentifierName(indexer.CallerExpression) ?? "?";
        var keyStr = indexer.InnerExpression switch
        {
            GDStringExpression str => TruncateText(str.ToString(), 15),
            GDNumberExpression num => num.ToString(),
            GDIdentifierExpression id => id.Identifier?.Sequence ?? "?",
            _ => "..."
        };
        return TruncateText($"{objName}[{keyStr}]", 30);
    }

    /// <summary>
    /// Gets label for a member access expression like result.property.
    /// </summary>
    private string GetMemberAccessLabel(GDMemberOperatorExpression memberAccess)
    {
        var objName = GetRootIdentifierName(memberAccess.CallerExpression) ?? "?";
        var memberName = memberAccess.Identifier?.Sequence ?? "member";
        return TruncateText($"{objName}.{memberName}", 30);
    }

    /// <summary>
    /// Gets label for a return expression.
    /// </summary>
    private string GetReturnLabel(GDReturnExpression ret)
    {
        var exprName = ret.Expression != null ? GetRootIdentifierName(ret.Expression) : null;
        if (!string.IsNullOrEmpty(exprName))
            return TruncateText($"return {exprName}", 25);
        return "return";
    }

    /// <summary>
    /// Gets label for a dual operator expression like current == null or current is Dictionary.
    /// </summary>
    private string GetDualOperatorLabel(GDDualOperatorExpression dualOp)
    {
        var left = GetRootIdentifierName(dualOp.LeftExpression) ?? dualOp.LeftExpression?.ToString() ?? "?";
        var right = dualOp.RightExpression?.ToString() ?? "?";
        var op = dualOp.OperatorType switch
        {
            GDDualOperatorType.Is => "is",
            GDDualOperatorType.Equal => "==",
            GDDualOperatorType.NotEqual => "!=",
            GDDualOperatorType.LessThan => "<",
            GDDualOperatorType.MoreThan => ">",
            GDDualOperatorType.LessThanOrEqual => "<=",
            GDDualOperatorType.MoreThanOrEqual => ">=",
            _ => dualOp.OperatorType.ToString()
        };
        return TruncateText($"{left} {op} {right}", 30);
    }

    /// <summary>
    /// Gets the root identifier name from an expression (e.g., "result" from "result.get()").
    /// </summary>
    private string GetRootIdentifierName(GDNode expression)
    {
        return expression switch
        {
            GDIdentifierExpression id => id.Identifier?.Sequence,
            GDMemberOperatorExpression ma => GetRootIdentifierName(ma.CallerExpression),
            GDCallExpression call => GetRootIdentifierName(call.CallerExpression),
            GDIndexerExpression indexer => GetRootIdentifierName(indexer.CallerExpression),
            _ => null
        };
    }

    /// <summary>
    /// Gets a description for a node kind.
    /// </summary>
    private string GetDescriptionForKind(GDTypeFlowNodeKind kind)
    {
        return kind switch
        {
            GDTypeFlowNodeKind.Parameter => "Function parameter",
            GDTypeFlowNodeKind.LocalVariable => "Local variable",
            GDTypeFlowNodeKind.MemberVariable => "Class member",
            GDTypeFlowNodeKind.MethodCall => "Method call",
            GDTypeFlowNodeKind.ReturnValue => "Return statement",
            GDTypeFlowNodeKind.Assignment => "Assignment",
            GDTypeFlowNodeKind.TypeAnnotation => "Type annotation",
            GDTypeFlowNodeKind.InheritedMember => "Inherited",
            GDTypeFlowNodeKind.BuiltinType => "Built-in type",
            GDTypeFlowNodeKind.Literal => "Literal value",
            GDTypeFlowNodeKind.IndexerAccess => "Index access",
            GDTypeFlowNodeKind.PropertyAccess => "Property access",
            GDTypeFlowNodeKind.TypeCheck => "Type guard",
            GDTypeFlowNodeKind.NullCheck => "Null check",
            GDTypeFlowNodeKind.Comparison => "Comparison",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets a detailed description for a node, including line number and context.
    /// </summary>
    private string GetDetailedDescription(GDNode node, GDTypeFlowNodeKind kind, string context, GDScriptAnalyzer analyzer)
    {
        var basePart = kind switch
        {
            GDTypeFlowNodeKind.MethodCall => GetMethodCallDescription(node, analyzer),
            GDTypeFlowNodeKind.IndexerAccess => GetIndexerDescription(node, analyzer),
            GDTypeFlowNodeKind.ReturnValue => "Return statement",
            GDTypeFlowNodeKind.TypeCheck => "Type guard",
            GDTypeFlowNodeKind.NullCheck => "Null check",
            GDTypeFlowNodeKind.Comparison => "Comparison",
            GDTypeFlowNodeKind.PropertyAccess => "Property access",
            _ => GetDescriptionForKind(kind)
        };

        if (!string.IsNullOrEmpty(context))
            basePart = $"{context}: {basePart}";

        var lineNumber = node?.StartLine ?? 0;
        if (lineNumber > 0)
            basePart += $", Line {lineNumber + 1}";

        return basePart;
    }

    /// <summary>
    /// Gets description for a method call including source type.
    /// </summary>
    private string GetMethodCallDescription(GDNode node, GDScriptAnalyzer analyzer)
    {
        if (node is GDCallExpression call &&
            call.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            var callerType = analyzer?.GetTypeForNode(memberOp.CallerExpression);
            var methodName = memberOp.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant")
                return $"{callerType}.{methodName}() call";
            return $"{methodName}() call";
        }
        return "Method call";
    }

    /// <summary>
    /// Gets description for an indexer access including source type.
    /// </summary>
    private string GetIndexerDescription(GDNode node, GDScriptAnalyzer analyzer)
    {
        if (node is GDIndexerExpression indexer)
        {
            var callerType = analyzer?.GetTypeForNode(indexer.CallerExpression);
            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant")
                return $"{callerType} index access";
        }
        return "Index access";
    }

    /// <summary>
    /// Gets the source type for an expression.
    /// </summary>
    private string GetSourceType(GDExpression callerExpr, GDScriptAnalyzer analyzer)
    {
        var sourceType = analyzer?.GetTypeForNode(callerExpr);
        if (!string.IsNullOrEmpty(sourceType) && sourceType != "Variant")
            return sourceType;
        return null;
    }

    /// <summary>
    /// Infers an improved type for an expression, using knowledge of common methods.
    /// </summary>
    private string InferImprovedType(GDNode expression, GDScriptAnalyzer analyzer)
    {
        var type = analyzer?.GetTypeForNode(expression);
        if (!string.IsNullOrEmpty(type) && type != "Variant")
            return type;

        return expression switch
        {
            // is, ==, != → bool
            GDDualOperatorExpression dualOp when dualOp.OperatorType == GDDualOperatorType.Is => "bool",
            GDDualOperatorExpression dualOp when IsComparisonOperator(dualOp.OperatorType) => "bool",

            // Known methods
            GDCallExpression call when IsKnownMethodCall(call, "size") => "int",
            GDCallExpression call when IsKnownMethodCall(call, "keys") => "Array",
            GDCallExpression call when IsKnownMethodCall(call, "values") => "Array",
            GDCallExpression call when IsKnownMethodCall(call, "has") => "bool",
            GDCallExpression call when IsKnownMethodCall(call, "contains") => "bool",
            GDCallExpression call when IsKnownMethodCall(call, "is_empty") => "bool",
            GDCallExpression call when IsKnownMethodCall(call, "get_class") => "String",
            GDCallExpression call when IsKnownMethodCall(call, "to_string") => "String",
            GDCallExpression call when IsKnownMethodCall(call, "length") => "int",
            GDCallExpression call when IsKnownMethodCall(call, "abs") => "float",

            _ => type ?? "Variant"
        };
    }

    /// <summary>
    /// Checks if a call expression is a call to a known method.
    /// </summary>
    private bool IsKnownMethodCall(GDCallExpression call, string methodName)
    {
        if (call.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            return string.Equals(memberOp.Identifier?.Sequence, methodName, StringComparison.OrdinalIgnoreCase);
        }
        if (call.CallerExpression is GDIdentifierExpression idExpr)
        {
            return string.Equals(idExpr.Identifier?.Sequence, methodName, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// Ensures the script has an analyzer.
    /// </summary>
    private GDScriptAnalyzer EnsureAnalyzer(GDScriptFile script)
    {
        if (script.Analyzer != null)
            return script.Analyzer;

        try
        {
            var runtimeProvider = _project?.CreateRuntimeProvider();
            script.Analyze(runtimeProvider);
            return script.Analyzer;
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to analyze script: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates a unique node ID.
    /// </summary>
    private string GenerateNodeId()
    {
        return $"node_{_nodeIdCounter++}";
    }

    /// <summary>
    /// Truncates text to a maximum length.
    /// </summary>
    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
    }
}
