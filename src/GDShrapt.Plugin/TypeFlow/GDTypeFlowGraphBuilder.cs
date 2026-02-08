using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using GDSymbolInfo = GDShrapt.Semantics.GDSymbolInfo;

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
    private readonly IGDTypeFlowHandler _typeFlowHandler;
    private readonly IGDSymbolsHandler _symbolsHandler;
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

    public GDTypeFlowGraphBuilder(GDScriptProject project, IGDTypeFlowHandler typeFlowHandler, IGDSymbolsHandler symbolsHandler)
    {
        _project = project;
        _typeFlowHandler = typeFlowHandler;
        _symbolsHandler = symbolsHandler;
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
    /// Delegates to CLI.Core handler.
    /// </summary>
    public void LoadInflowsFor(GDTypeFlowNode node)
    {
        if (node == null || node.AreInflowsLoaded)
            return;

        var script = node.SourceScript;
        if (script == null || string.IsNullOrEmpty(script.FullPath))
            return;

        // Use handler to get inflows from CLI.Core
        var inflows = _typeFlowHandler.GetInflowNodes(node.Label, script.FullPath);
        if (inflows != null)
        {
            foreach (var inflow in inflows)
            {
                if (!node.Inflows.Any(n => n.Id == inflow.Id))
                {
                    node.Inflows.Add(inflow);
                    RegisterNode(inflow);
                }
            }
        }

        node.AreInflowsLoaded = true;
    }

    /// <summary>
    /// Loads outflows for a node that hasn't had its outflows loaded yet.
    /// This enables lazy loading for deep graph traversal.
    /// Delegates to CLI.Core handler.
    /// </summary>
    public void LoadOutflowsFor(GDTypeFlowNode node)
    {
        if (node == null || node.AreOutflowsLoaded)
            return;

        var script = node.SourceScript;
        if (script == null || string.IsNullOrEmpty(script.FullPath))
            return;

        // Use handler to get outflows from CLI.Core
        var outflows = _typeFlowHandler.GetOutflowNodes(node.Label, script.FullPath);
        if (outflows != null)
        {
            foreach (var outflow in outflows)
            {
                if (!node.Outflows.Any(n => n.Id == outflow.Id))
                {
                    node.Outflows.Add(outflow);
                    RegisterNode(outflow);
                }
            }
        }

        node.AreOutflowsLoaded = true;
    }

    /// <summary>
    /// Builds a type flow graph for the specified symbol.
    /// Delegates to IGDTypeFlowHandler for core graph building, then enhances with UI-specific features.
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

        // Delegate to CLI.Core handler for core graph building
        // This ensures all type flow logic is centralized in CLI.Core
        var rootNode = _typeFlowHandler.ShowForSymbol(symbolName, script);
        if (rootNode == null)
            return null;

        // Register the root node
        RegisterNode(rootNode);

        // Register all inflow and outflow nodes
        foreach (var inflow in rootNode.Inflows)
            RegisterNode(inflow);
        foreach (var outflow in rootNode.Outflows)
            RegisterNode(outflow);

        var semanticModel = script.SemanticModel;

        // Add union type info if applicable (UI enhancement)
        if (semanticModel != null)
        {
            AddUnionTypeInfo(rootNode, symbolName, semanticModel);

            // Add duck type info if applicable (UI enhancement)
            AddDuckTypeInfo(rootNode, symbolName, semanticModel);

            // Expand union types if enabled (UI feature)
            if (ExpandUnionTypes)
            {
                ExpandUnionTypeNodes(rootNode, script, semanticModel);
            }
        }

        // Create edge objects from Inflows/Outflows relationships (UI feature)
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
    private void AddUnionTypeInfo(GDTypeFlowNode node, string symbolName, GDSemanticModel semanticModel)
    {
        if (semanticModel == null)
            return;

        var unionType = semanticModel.TypeSystem.GetUnionType(symbolName);
        if (unionType != null && unionType.IsUnion)
        {
            node.IsUnionType = true;
            node.UnionTypeInfo = unionType;
            node.Type = string.Join("|", unionType.Types.Take(3).Select(t => t.DisplayName));
            if (unionType.Types.Count > 3)
                node.Type += "...";
        }
    }

    /// <summary>
    /// Adds duck type information to a node.
    /// Delegates suppression logic to GDSemanticModel.ShouldSuppressDuckConstraints().
    /// </summary>
    private void AddDuckTypeInfo(GDTypeFlowNode node, string symbolName, GDSemanticModel semanticModel)
    {
        if (!IncludeDuckConstraints)
            return;

        // Delegate suppression decision to semantic model
        if (semanticModel.ShouldSuppressDuckConstraints(symbolName))
            return;

        var duckType = semanticModel.TypeSystem.GetDuckType(symbolName);
        if (duckType == null || !duckType.HasRequirements)
            return;

        node.HasDuckConstraints = true;
        node.DuckTypeInfo = duckType;
    }

    /// <summary>
    /// Builds multi-level inflows recursively.
    /// </summary>
    private void BuildMultiLevelInflows(GDTypeFlowNode targetNode, GDSymbolInfo symbol, GDScriptFile script, GDSemanticModel semanticModel, int currentLevel)
    {
        if (currentLevel > MaxInflowLevels)
            return;

        var decl = symbol?.DeclarationNode;
        if (decl == null)
            return;

        // For methods, inflows are the parameters
        if (decl is GDMethodDeclaration method)
        {
            BuildMethodInflows(targetNode, method, script, semanticModel, currentLevel);
            return;
        }

        // Check for explicit type annotation via GDSymbolInfo.TypeName
        if (symbol.TypeName != null)
        {
            var annotationNode = new GDTypeFlowNode
            {
                Id = GenerateNodeId(),
                Label = "Type annotation",
                Type = symbol.TypeName,
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
            var defaultNode = CreateInflowFromExpression(param.DefaultValue, script, semanticModel, "Default value", currentLevel);
            if (defaultNode != null)
            {
                targetNode.Inflows.Add(defaultNode);
                // Recurse into default value expression
                BuildInflowsFromExpression(defaultNode, param.DefaultValue, script, semanticModel, currentLevel + 1);
            }
        }

        // For variables, check initialization
        if (decl is GDVariableDeclaration variable && variable.Initializer != null)
        {
            var initNode = CreateInflowFromExpression(variable.Initializer, script, semanticModel, "Initialization", currentLevel);
            if (initNode != null)
            {
                targetNode.Inflows.Add(initNode);
                // Recurse into initializer
                BuildInflowsFromExpression(initNode, variable.Initializer, script, semanticModel, currentLevel + 1);
            }
        }

        // Check assignments to this symbol
        var assignments = FindAssignmentsTo(symbol.Name, script, semanticModel);
        foreach (var assignment in assignments.Take(5))
        {
            if (assignment.RightExpression != null)
            {
                var assignNode = CreateInflowFromExpression(assignment.RightExpression, script, semanticModel, "Assignment", currentLevel);
                if (assignNode != null)
                {
                    targetNode.Inflows.Add(assignNode);
                    // Recurse into assignment
                    BuildInflowsFromExpression(assignNode, assignment.RightExpression, script, semanticModel, currentLevel + 1);
                }
            }
        }
    }

    /// <summary>
    /// Builds inflows for a method (parameters are the inputs).
    /// </summary>
    private void BuildMethodInflows(GDTypeFlowNode targetNode, GDMethodDeclaration method, GDScriptFile script, GDSemanticModel semanticModel, int currentLevel)
    {
        // Add parameters as inflows (they are inputs to the method)
        if (method.Parameters != null)
        {
            foreach (var param in method.Parameters)
            {
                if (param == null)
                    continue;

                var paramName = param.Identifier?.Sequence;
                if (string.IsNullOrEmpty(paramName))
                    continue;

                var paramTypeInfo = semanticModel.TypeSystem.GetType(param);
                var paramType = param.Type?.BuildName() ?? (paramTypeInfo.IsVariant ? "Variant" : paramTypeInfo.DisplayName);
                var paramNode = new GDTypeFlowNode
                {
                    Id = GenerateNodeId(),
                    Label = paramName,
                    Type = paramType,
                    Kind = GDTypeFlowNodeKind.Parameter,
                    Confidence = param.Type != null ? 1.0f : 0.5f,
                    Description = $"Parameter: {paramName}",
                    Location = GDSourceLocation.FromNode(param, script.FullPath),
                    SourceScript = script,
                    AstNode = param
                };
                targetNode.Inflows.Add(paramNode);
            }
        }

        // Add return type annotation if present
        if (method.ReturnType != null)
        {
            var returnType = method.ReturnType.BuildName();
            var returnAnnotationNode = new GDTypeFlowNode
            {
                Id = GenerateNodeId(),
                Label = "Return type",
                Type = returnType,
                Kind = GDTypeFlowNodeKind.TypeAnnotation,
                Confidence = 1.0f,
                Description = $"Declared return type: {returnType}",
                Location = GDSourceLocation.FromNode(method.ReturnType, script.FullPath),
                SourceScript = script,
                AstNode = method.ReturnType
            };
            targetNode.Inflows.Add(returnAnnotationNode);
        }
    }

    /// <summary>
    /// Builds inflows from an expression (for recursive multi-level).
    /// </summary>
    private void BuildInflowsFromExpression(GDTypeFlowNode targetNode, GDNode expression, GDScriptFile script, GDSemanticModel semanticModel, int currentLevel)
    {
        if (currentLevel > MaxInflowLevels)
            return;

        // For call expressions, trace the method return type
        if (expression is GDCallExpression call)
        {
            // Try to find the method being called
            if (call.CallerExpression is GDIdentifierExpression idExpr)
            {
                var methodSymbol = semanticModel.FindSymbol(idExpr.Identifier?.Sequence);
                if (methodSymbol != null)
                {
                    // Create node for the method return
                    var methodNode = CreateNodeFromSymbol(methodSymbol, script, semanticModel);
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
            var varSymbol = semanticModel.FindSymbol(idExpr.Identifier?.Sequence);
            if (varSymbol != null)
            {
                var varNode = CreateNodeFromSymbol(varSymbol, script, semanticModel);
                if (varNode != null)
                {
                    targetNode.Inflows.Add(varNode);
                    // Recurse into this variable's sources
                    BuildMultiLevelInflows(varNode, varSymbol, script, semanticModel, currentLevel + 1);
                }
            }
        }
        // For member access, trace the member
        else if (expression is GDMemberOperatorExpression memberAccess)
        {
            var memberNode = CreateNodeFromMemberAccess(memberAccess, script, semanticModel);
            if (memberNode != null)
            {
                targetNode.Inflows.Add(memberNode);
            }
        }
    }

    /// <summary>
    /// Builds multi-level outflows recursively.
    /// </summary>
    private void BuildMultiLevelOutflows(GDTypeFlowNode sourceNode, GDSymbolInfo symbol, GDScriptFile script, GDSemanticModel semanticModel, int currentLevel)
    {
        if (currentLevel > MaxOutflowLevels)
            return;

        // For methods, outflows are handled by CLI.Core handler
        // This method is now only used for non-method symbols
        if (symbol.DeclarationNode is GDMethodDeclaration)
        {
            // Methods are handled via _typeFlowHandler.ShowForSymbol() which builds outflows in CLI.Core
            return;
        }

        var refs = semanticModel.GetReferencesTo(symbol);
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
                var callNode = CreateNodeFromCall(call, script, semanticModel, reference);
                if (callNode != null)
                {
                    AddDuckTypeInfo(callNode, symbol.Name, semanticModel);
                    sourceNode.Outflows.Add(callNode);
                    // Recurse into call usages
                    BuildOutflowsFromCall(callNode, call, script, semanticModel, currentLevel + 1);
                }
            }
            else if (parent is GDIndexerExpression indexer)
            {
                // Indexer access: symbol[key]
                var indexerNode = CreateNodeFromIndexer(indexer, script, semanticModel);
                if (indexerNode != null)
                {
                    sourceNode.Outflows.Add(indexerNode);
                }
            }
            else if (parent is GDMemberOperatorExpression memberAccess)
            {
                var accessNode = CreateNodeFromMemberAccess(memberAccess, script, semanticModel);
                if (accessNode != null)
                {
                    sourceNode.Outflows.Add(accessNode);
                }
            }
            else if (parent is GDDualOperatorExpression dualOp)
            {
                // Type check, null check, comparison
                var opNode = CreateNodeFromDualOperator(dualOp, script, semanticModel);
                if (opNode != null)
                {
                    sourceNode.Outflows.Add(opNode);
                }
            }
            else if (parent is GDReturnExpression ret)
            {
                // Return statement
                var returnNode = CreateNodeFromReturn(ret, script, semanticModel);
                if (returnNode != null)
                {
                    sourceNode.Outflows.Add(returnNode);
                }
            }
            else
            {
                // Find parent dual operator or return up the tree
                var foundNode = FindAndCreateParentContextNode(reference.ReferenceNode, script, semanticModel);
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
    private GDTypeFlowNode FindAndCreateParentContextNode(GDNode node, GDScriptFile script, GDSemanticModel semanticModel)
    {
        var current = node.Parent;
        int depth = 0;

        while (current != null && depth < 5)
        {
            if (current is GDReturnExpression ret)
            {
                return CreateNodeFromReturn(ret, script, semanticModel);
            }
            if (current is GDDualOperatorExpression dualOp)
            {
                return CreateNodeFromDualOperator(dualOp, script, semanticModel);
            }
            if (current is GDIndexerExpression indexer)
            {
                return CreateNodeFromIndexer(indexer, script, semanticModel);
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
            Type = reference.InferredType?.DisplayName ?? "Variant",
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
    private GDTypeFlowNode CreateNodeFromIndexer(GDIndexerExpression indexer, GDScriptFile script, GDSemanticModel semanticModel)
    {
        var sourceObjectName = GetRootIdentifierName(indexer.CallerExpression);
        var sourceType = GetSourceType(indexer.CallerExpression, semanticModel);
        var indexerTypeInfo = semanticModel.TypeSystem.GetType(indexer);
        var resultType = indexerTypeInfo.IsVariant ? "Variant" : indexerTypeInfo.DisplayName;
        var label = GetIndexerLabel(indexer);
        var description = GetDetailedDescription(indexer, GDTypeFlowNodeKind.IndexerAccess, null, semanticModel);

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
    private GDTypeFlowNode CreateNodeFromDualOperator(GDDualOperatorExpression dualOp, GDScriptFile script, GDSemanticModel semanticModel)
    {
        var kind = GetKindForDualOperator(dualOp);
        var label = GetDualOperatorLabel(dualOp);
        var resultType = InferImprovedType(dualOp, semanticModel);
        var description = GetDetailedDescription(dualOp, kind, null, semanticModel);

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
    private GDTypeFlowNode CreateNodeFromReturn(GDReturnExpression ret, GDScriptFile script, GDSemanticModel semanticModel)
    {
        var label = GetReturnLabel(ret);
        string returnedType;
        if (ret.Expression != null)
        {
            var retTypeInfo = semanticModel.TypeSystem.GetType(ret.Expression);
            returnedType = retTypeInfo.IsVariant ? "Variant" : retTypeInfo.DisplayName;
        }
        else
        {
            returnedType = "void";
        }
        var description = GetDetailedDescription(ret, GDTypeFlowNodeKind.ReturnValue, null, semanticModel);

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
    private void BuildOutflowsFromCall(GDTypeFlowNode sourceNode, GDCallExpression call, GDScriptFile script, GDSemanticModel semanticModel, int currentLevel)
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
                var varSymbol = semanticModel.FindSymbol(idExpr.Identifier?.Sequence);
                if (varSymbol != null)
                {
                    var varNode = CreateNodeFromSymbol(varSymbol, script, semanticModel);
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
    private void ExpandUnionTypeNodes(GDTypeFlowNode rootNode, GDScriptFile script, GDSemanticModel semanticModel)
    {
        if (!rootNode.IsUnionType || rootNode.UnionTypeInfo == null)
            return;

        var unionTypes = rootNode.UnionTypeInfo.Types;
        var assignments = FindAssignmentsTo(rootNode.Label, script, semanticModel).ToList();

        // Try to trace each union type to its source
        foreach (var typeName in unionTypes.Take(5))
        {
            // Find assignment that produces this type
            foreach (var assignment in assignments)
            {
                if (assignment.RightExpression == null)
                    continue;

                var exprTypeInfo = semanticModel.TypeSystem.GetType(assignment.RightExpression);
                var exprType = exprTypeInfo.IsVariant ? null : exprTypeInfo.DisplayName;
                if (exprType == typeName.DisplayName)
                {
                    var unionSourceNode = new GDTypeFlowNode
                    {
                        Id = GenerateNodeId(),
                        Label = GetLabelFromExpression(assignment.RightExpression),
                        Type = typeName.DisplayName,
                        Kind = GetNodeKindFromExpression(assignment.RightExpression),
                        Confidence = 0.8f,
                        Description = $"Union member: {typeName}",
                        Location = GDSourceLocation.FromNode(assignment.RightExpression, script.FullPath),
                        SourceScript = script,
                        AstNode = assignment.RightExpression
                    };
                    rootNode.UnionSources.Add(unionSourceNode);
                    // Also add to inflows so they appear in the graph
                    if (!rootNode.Inflows.Any(n => n.Type == typeName.DisplayName))
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
    private GDTypeFlowNode CreateNodeFromSymbol(GDSymbolInfo symbol, GDScriptFile script, GDSemanticModel semanticModel)
    {
        var decl = symbol.DeclarationNode;
        if (decl == null)
            return null;

        var typeInfoDecl = semanticModel.TypeSystem.GetType(decl);
        var typeStr = typeInfoDecl.IsVariant ? "Variant" : typeInfoDecl.DisplayName;
        var kind = GetNodeKindFromSymbol(symbol);
        var confidence = CalculateConfidence(symbol, typeStr, semanticModel);

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
    private GDTypeFlowNode CreateInflowFromExpression(GDNode expression, GDScriptFile script, GDSemanticModel semanticModel, string context, int level)
    {
        if (expression == null)
            return null;

        var exprType = InferImprovedType(expression, semanticModel);
        var kind = GetNodeKindFromExpression(expression);
        var label = GetLabelFromExpression(expression);
        var description = GetDetailedDescription(expression, kind, context, semanticModel);

        string sourceType = null;
        string sourceObjectName = null;

        // Fill SourceType and SourceObjectName for applicable expression types
        if (expression is GDCallExpression call)
        {
            if (call.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                sourceObjectName = GetRootIdentifierName(memberOp.CallerExpression);
                sourceType = GetSourceType(memberOp.CallerExpression, semanticModel);
            }
        }
        else if (expression is GDIndexerExpression indexer)
        {
            sourceObjectName = GetRootIdentifierName(indexer.CallerExpression);
            sourceType = GetSourceType(indexer.CallerExpression, semanticModel);
        }
        else if (expression is GDMemberOperatorExpression memberAccess)
        {
            sourceObjectName = GetRootIdentifierName(memberAccess.CallerExpression);
            sourceType = GetSourceType(memberAccess.CallerExpression, semanticModel);
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
    private GDTypeFlowNode CreateNodeFromCall(GDCallExpression call, GDScriptFile script, GDSemanticModel semanticModel, GDReference reference)
    {
        string sourceObjectName = null;
        string sourceType = null;

        if (call.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            sourceObjectName = GetRootIdentifierName(memberOp.CallerExpression);
            sourceType = GetSourceType(memberOp.CallerExpression, semanticModel);
        }

        var returnType = InferImprovedType(call, semanticModel);
        var label = GetCallLabel(call);
        var description = GetDetailedDescription(call, GDTypeFlowNodeKind.MethodCall, null, semanticModel);

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
    private GDTypeFlowNode CreateNodeFromMemberAccess(GDMemberOperatorExpression memberAccess, GDScriptFile script, GDSemanticModel semanticModel)
    {
        var sourceObjectName = GetRootIdentifierName(memberAccess.CallerExpression);
        var sourceType = GetSourceType(memberAccess.CallerExpression, semanticModel);
        var memberTypeInfo = semanticModel.TypeSystem.GetType(memberAccess);
        var memberType = memberTypeInfo.IsVariant ? "Variant" : memberTypeInfo.DisplayName;
        var label = GetMemberAccessLabel(memberAccess);
        var description = GetDetailedDescription(memberAccess, GDTypeFlowNodeKind.PropertyAccess, null, semanticModel);

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
    private IEnumerable<GDDualOperatorExpression> FindAssignmentsTo(string variableName, GDScriptFile script, GDSemanticModel semanticModel)
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
    /// Calculates confidence based on symbol and type information.
    /// Uses GDSymbolInfo.TypeName for explicit annotations and RuntimeProvider for known types.
    /// </summary>
    private float CalculateConfidence(GDSymbolInfo symbol, string type, GDSemanticModel semanticModel)
    {
        if (type == "Variant" || string.IsNullOrEmpty(type))
            return 0.2f;

        // Check for explicit type annotation via GDSymbolInfo.TypeName
        if (symbol.TypeName != null)
            return 1.0f;

        // Check if it's a known type via RuntimeProvider (no hardcoded list)
        var runtimeProvider = _project?.CreateRuntimeProvider();
        if (runtimeProvider != null && runtimeProvider.IsKnownType(type))
            return 0.9f;

        return 0.6f;
    }

    /// <summary>
    /// Gets the node kind from a symbol.
    /// Matches GDTypeFlowHandler.MapSymbolKind() for consistency.
    /// </summary>
    private GDTypeFlowNodeKind GetNodeKindFromSymbol(GDSymbolInfo symbol)
    {
        return symbol.Kind switch
        {
            GDSymbolKind.Parameter => GDTypeFlowNodeKind.Parameter,
            GDSymbolKind.Variable => GDTypeFlowNodeKind.LocalVariable,
            GDSymbolKind.Iterator => GDTypeFlowNodeKind.LocalVariable,
            GDSymbolKind.Method => GDTypeFlowNodeKind.MethodCall,
            GDSymbolKind.Property => GDTypeFlowNodeKind.MemberVariable,
            GDSymbolKind.Signal => GDTypeFlowNodeKind.MemberVariable,
            GDSymbolKind.Constant => GDTypeFlowNodeKind.MemberVariable,
            GDSymbolKind.EnumValue => GDTypeFlowNodeKind.Literal,
            GDSymbolKind.MatchCaseBinding => GDTypeFlowNodeKind.LocalVariable,
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
    private string GetDetailedDescription(GDNode node, GDTypeFlowNodeKind kind, string context, GDSemanticModel semanticModel)
    {
        var basePart = kind switch
        {
            GDTypeFlowNodeKind.MethodCall => GetMethodCallDescription(node, semanticModel),
            GDTypeFlowNodeKind.IndexerAccess => GetIndexerDescription(node, semanticModel),
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
    private string GetMethodCallDescription(GDNode node, GDSemanticModel semanticModel)
    {
        if (node is GDCallExpression call &&
            call.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            var callerTypeInfo = semanticModel?.TypeSystem.GetType(memberOp.CallerExpression);
            var callerType = callerTypeInfo?.IsVariant == true ? null : callerTypeInfo?.DisplayName;
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
    private string GetIndexerDescription(GDNode node, GDSemanticModel semanticModel)
    {
        if (node is GDIndexerExpression indexer)
        {
            var callerTypeInfo = semanticModel?.TypeSystem.GetType(indexer.CallerExpression);
            var callerType = callerTypeInfo?.IsVariant == true ? null : callerTypeInfo?.DisplayName;
            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant")
                return $"{callerType} index access";
        }
        return "Index access";
    }

    /// <summary>
    /// Gets the source type for an expression.
    /// </summary>
    private string GetSourceType(GDExpression callerExpr, GDSemanticModel semanticModel)
    {
        var srcTypeInfo = semanticModel?.TypeSystem.GetType(callerExpr);
        var sourceType = srcTypeInfo?.IsVariant == true ? null : srcTypeInfo?.DisplayName;
        if (!string.IsNullOrEmpty(sourceType) && sourceType != "Variant")
            return sourceType;
        return null;
    }

    /// <summary>
    /// Infers an improved type for an expression by delegating to Semantics.
    /// All type knowledge is centralized in GDTypeInferenceEngine via GDGodotTypesProvider.
    /// </summary>
    private string InferImprovedType(GDNode expression, GDSemanticModel semanticModel)
    {
        // Delegate all type inference to Semantics - no hardcoded types here
        var typeInfo = semanticModel?.TypeSystem.GetType(expression);
        return typeInfo?.IsVariant == true ? "Variant" : (typeInfo?.DisplayName ?? "Variant");
    }

    /// <summary>
    /// Ensures the script has a semantic model.
    /// </summary>
    private GDSemanticModel EnsureSemanticModel(GDScriptFile script)
    {
        if (script.SemanticModel != null)
            return script.SemanticModel;

        try
        {
            var runtimeProvider = _project?.CreateRuntimeProvider();
            script.Analyze(runtimeProvider);
            return script.SemanticModel;
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
