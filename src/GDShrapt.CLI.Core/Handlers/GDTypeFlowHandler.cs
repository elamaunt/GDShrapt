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
        var astNode = script.SemanticModel.GetNodeAtPosition(line, column);
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
        var unionType = semanticModel.TypeSystem.GetUnionType(symbolName);
        if (unionType?.IsUnion == true)
        {
            rootNode.IsUnionType = true;
            rootNode.UnionTypeInfo = unionType;
            // Update Type to show union types (e.g., "int|String|null")
            rootNode.Type = string.Join("|", unionType.Types.Take(3));
            if (unionType.Types.Count > 3)
                rootNode.Type += "...";
        }

        // Add duck type info (only if not suppressed for known types)
        if (!semanticModel.ShouldSuppressDuckConstraints(symbolName))
        {
            var duckType = semanticModel.TypeSystem.GetDuckType(symbolName);
            if (duckType?.HasRequirements == true)
            {
                rootNode.HasDuckConstraints = true;
                rootNode.DuckTypeInfo = duckType;
            }
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
        var typeInfo = semanticModel.TypeSystem.GetType(symbol.DeclarationNode);
        var type = typeInfo.IsVariant ? (symbol.TypeName ?? "Variant") : typeInfo.DisplayName;
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
    /// Builds basic inflows (type annotation, initializers, assignments, parameters for methods).
    /// </summary>
    protected virtual void BuildBasicInflows(
        GDTypeFlowNode node,
        Semantics.GDSymbolInfo symbol,
        GDScriptFile script,
        GDSemanticModel semanticModel)
    {
        // For methods, add parameters as inflows and return type annotation
        if (symbol.DeclarationNode is GDMethodDeclaration method)
        {
            BuildMethodInflows(node, method, script, semanticModel);
            return;
        }

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

        // For parameters with default values
        if (symbol.DeclarationNode is GDParameterDeclaration param && param.DefaultValue != null)
        {
            var defaultTypeInfo = semanticModel.TypeSystem.GetType(param.DefaultValue);
            var defaultType = defaultTypeInfo.IsVariant ? "Variant" : defaultTypeInfo.DisplayName;
            var defaultNode = new GDTypeFlowNode
            {
                Id = GenerateNodeId(),
                Label = "Default value",
                Type = defaultType,
                Kind = GDTypeFlowNodeKind.Literal,
                Confidence = 0.9f,
                Description = $"Default value: {GetExpressionLabel(param.DefaultValue)}",
                Level = -1,
                SourceScript = script,
                AstNode = param.DefaultValue,
                Location = GDSourceLocation.FromNode(param.DefaultValue, script.FullPath)
            };
            RegisterNode(defaultNode);
            node.Inflows.Add(defaultNode);
            node.InflowNodeIds.Add(defaultNode.Id);
        }

        // For variables with initializers
        if (symbol.DeclarationNode is GDVariableDeclaration variable && variable.Initializer != null)
        {
            var initTypeInfo = semanticModel.TypeSystem.GetType(variable.Initializer);
            var initType = initTypeInfo.IsVariant ? "Variant" : initTypeInfo.DisplayName;
            var initNode = new GDTypeFlowNode
            {
                Id = GenerateNodeId(),
                Label = "Initialization",
                Type = initType,
                Kind = GetExpressionKind(variable.Initializer),
                Confidence = 0.9f,
                Description = $"Initialized with: {GetExpressionLabel(variable.Initializer)}",
                Level = -1,
                SourceScript = script,
                AstNode = variable.Initializer,
                Location = GDSourceLocation.FromNode(variable.Initializer, script.FullPath)
            };
            RegisterNode(initNode);
            node.Inflows.Add(initNode);
            node.InflowNodeIds.Add(initNode.Id);
        }

        // For local variable declarations with initializers
        if (symbol.DeclarationNode is GDVariableDeclarationStatement varStmt && varStmt.Initializer != null)
        {
            var varInitTypeInfo = semanticModel.TypeSystem.GetType(varStmt.Initializer);
            var initType = varInitTypeInfo.IsVariant ? "Variant" : varInitTypeInfo.DisplayName;
            var initNode = new GDTypeFlowNode
            {
                Id = GenerateNodeId(),
                Label = "Initialization",
                Type = initType,
                Kind = GetExpressionKind(varStmt.Initializer),
                Confidence = 0.9f,
                Description = $"Initialized with: {GetExpressionLabel(varStmt.Initializer)}",
                Level = -1,
                SourceScript = script,
                AstNode = varStmt.Initializer,
                Location = GDSourceLocation.FromNode(varStmt.Initializer, script.FullPath)
            };
            RegisterNode(initNode);
            node.Inflows.Add(initNode);
            node.InflowNodeIds.Add(initNode.Id);
        }

        // Check for assignments to this symbol
        var assignments = FindAssignmentsTo(symbol.Name, script);
        foreach (var assignment in assignments.Take(5))
        {
            if (assignment.RightExpression == null)
                continue;

            var assignTypeInfo = semanticModel.TypeSystem.GetType(assignment.RightExpression);
            var assignType = assignTypeInfo.IsVariant ? "Variant" : assignTypeInfo.DisplayName;
            var assignNode = new GDTypeFlowNode
            {
                Id = GenerateNodeId(),
                Label = "Assignment",
                Type = assignType,
                Kind = GDTypeFlowNodeKind.Assignment,
                Confidence = 0.8f,
                Description = $"Assigned: {GetExpressionLabel(assignment.RightExpression)}",
                Level = -1,
                SourceScript = script,
                AstNode = assignment,
                Location = GDSourceLocation.FromNode(assignment, script.FullPath)
            };
            RegisterNode(assignNode);
            node.Inflows.Add(assignNode);
            node.InflowNodeIds.Add(assignNode.Id);
        }
    }

    /// <summary>
    /// Finds assignments to a symbol.
    /// </summary>
    protected static IEnumerable<GDDualOperatorExpression> FindAssignmentsTo(string symbolName, GDScriptFile script)
    {
        if (script?.Class == null || string.IsNullOrEmpty(symbolName))
            yield break;

        foreach (var node in script.Class.AllNodes.OfType<GDDualOperatorExpression>())
        {
            if (node.Operator?.OperatorType != GDDualOperatorType.Assignment)
                continue;

            if (node.LeftExpression is GDIdentifierExpression idExpr &&
                idExpr.Identifier?.Sequence == symbolName)
            {
                yield return node;
            }
        }
    }

    /// <summary>
    /// Gets the kind for an expression.
    /// </summary>
    protected static GDTypeFlowNodeKind GetExpressionKind(GDExpression expression)
    {
        return expression switch
        {
            GDCallExpression => GDTypeFlowNodeKind.MethodCall,
            GDMemberOperatorExpression => GDTypeFlowNodeKind.PropertyAccess,
            GDIndexerExpression => GDTypeFlowNodeKind.IndexerAccess,
            GDIdentifierExpression => GDTypeFlowNodeKind.LocalVariable,
            GDNumberExpression or GDStringExpression or GDBoolExpression => GDTypeFlowNodeKind.Literal,
            GDArrayInitializerExpression or GDDictionaryInitializerExpression => GDTypeFlowNodeKind.Literal,
            _ => GDTypeFlowNodeKind.Unknown
        };
    }

    /// <summary>
    /// Builds inflows for a method (parameters are the inputs, return type is output definition).
    /// </summary>
    protected virtual void BuildMethodInflows(
        GDTypeFlowNode node,
        GDMethodDeclaration method,
        GDScriptFile script,
        GDSemanticModel semanticModel)
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

                // Get parameter type: explicit annotation, or inferred union type from type guards/null checks
                string paramType;
                float confidence;

                if (param.Type != null)
                {
                    // Explicit type annotation - high confidence
                    paramType = param.Type.BuildName();
                    confidence = 1.0f;
                }
                else
                {
                    // No explicit type - try to get union type from type guards and null checks
                    var unionType = semanticModel.TypeSystem.GetUnionType(paramName);
                    if (unionType != null && !unionType.IsEmpty)
                    {
                        paramType = unionType.ToString();
                        confidence = unionType.AllHighConfidence ? 0.8f : 0.5f;
                    }
                    else
                    {
                        var paramTypeInfo = semanticModel.TypeSystem.GetType(param);
                        paramType = paramTypeInfo.IsVariant ? "Variant" : paramTypeInfo.DisplayName;
                        confidence = 0.5f;
                    }
                }

                var paramNode = new GDTypeFlowNode
                {
                    Id = GenerateNodeId(),
                    Label = paramName,
                    Type = paramType,
                    Kind = GDTypeFlowNodeKind.Parameter,
                    Confidence = confidence,
                    Description = $"Parameter: {paramName}",
                    Location = GDSourceLocation.FromNode(param, script.FullPath),
                    SourceScript = script,
                    AstNode = param,
                    Level = -1
                };
                RegisterNode(paramNode);
                node.Inflows.Add(paramNode);
                node.InflowNodeIds.Add(paramNode.Id);
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
                AstNode = method.ReturnType,
                Level = -1
            };
            RegisterNode(returnAnnotationNode);
            node.Inflows.Add(returnAnnotationNode);
            node.InflowNodeIds.Add(returnAnnotationNode.Id);
        }
    }

    /// <summary>
    /// Builds basic outflows (references to this symbol, or return statements for methods).
    /// </summary>
    protected virtual void BuildBasicOutflows(
        GDTypeFlowNode node,
        Semantics.GDSymbolInfo symbol,
        GDScriptFile script,
        GDSemanticModel semanticModel)
    {
        // For methods, add return statements as outflows and call sites
        if (symbol.DeclarationNode is GDMethodDeclaration method)
        {
            BuildMethodOutflows(node, method, symbol, script, semanticModel);
            return;
        }

        // Get references to this symbol
        var references = semanticModel.GetReferencesTo(symbol);
        foreach (var reference in references.Take(20))
        {
            if (reference.ReferenceNode == null)
                continue;

            var usageContext = GetUsageContext(reference);
            var usageKind = GetDetailedUsageKind(reference);
            var (sourceObjectName, sourceType) = GetSourceObjectInfo(reference, symbol.Name, semanticModel);

            // Determine the type based on usage kind
            var usageType = GetUsageResultType(reference, usageKind, node.Type, semanticModel);

            // Build description with line number and context-specific info
            var location = GDSourceLocation.FromNode(reference.ReferenceNode, script.FullPath);
            var description = BuildUsageDescription(usageKind, usageContext, location);

            var usageNode = new GDTypeFlowNode
            {
                Id = GenerateNodeId(),
                Label = GetDetailedUsageLabel(reference, usageContext),
                Type = usageType,
                Kind = usageKind,
                Confidence = CalculateExpressionConfidence(usageType),
                Description = description,
                Level = 1,
                SourceScript = script,
                AstNode = reference.ReferenceNode,
                Location = location,
                SourceObjectName = sourceObjectName,
                SourceType = sourceType
            };
            RegisterNode(usageNode);
            node.Outflows.Add(usageNode);
            node.OutflowNodeIds.Add(usageNode.Id);
        }
    }

    /// <summary>
    /// Gets the result type for a usage based on usage kind.
    /// </summary>
    protected static string GetUsageResultType(GDReference reference, GDTypeFlowNodeKind usageKind, string? defaultType, GDSemanticModel semanticModel)
    {
        // Type checks and null checks always return bool
        if (usageKind == GDTypeFlowNodeKind.TypeCheck || usageKind == GDTypeFlowNodeKind.NullCheck)
            return "bool";

        // Comparisons return bool
        if (usageKind == GDTypeFlowNodeKind.Comparison)
            return "bool";

        // For property access, method call, indexer - infer from the parent expression
        var parent = reference.ReferenceNode?.Parent;
        if (parent != null)
        {
            var parentTypeInfo = semanticModel.TypeSystem.GetType(parent);
            if (!parentTypeInfo.IsVariant)
                return parentTypeInfo.DisplayName;
        }

        // Fall back to the reference's inferred type or default
        var refTypeInfo = semanticModel.TypeSystem.GetType(reference.ReferenceNode);
        return refTypeInfo.IsVariant ? (defaultType ?? "Variant") : refTypeInfo.DisplayName;
    }

    /// <summary>
    /// Gets detailed usage kind by examining parent expressions.
    /// </summary>
    protected static GDTypeFlowNodeKind GetDetailedUsageKind(GDReference reference)
    {
        var node = reference.ReferenceNode;
        var parent = node?.Parent;

        return parent switch
        {
            GDCallExpression call when call.CallerExpression is GDMemberOperatorExpression =>
                GDTypeFlowNodeKind.MethodCall,
            GDCallExpression => GDTypeFlowNodeKind.MethodCall,
            GDMemberOperatorExpression => GDTypeFlowNodeKind.PropertyAccess,
            GDIndexerExpression => GDTypeFlowNodeKind.IndexerAccess,
            GDReturnExpression => GDTypeFlowNodeKind.ReturnValue,
            GDDualOperatorExpression dualOp when IsTypeCheckOperator(dualOp) =>
                GDTypeFlowNodeKind.TypeCheck,
            GDDualOperatorExpression dualOp when IsNullCheckOperator(dualOp) =>
                GDTypeFlowNodeKind.NullCheck,
            GDDualOperatorExpression dualOp when IsComparisonOperator(dualOp) =>
                GDTypeFlowNodeKind.Comparison,
            GDDualOperatorExpression dualOp when IsAssignmentOperator(dualOp) =>
                reference.IsWrite ? GDTypeFlowNodeKind.Assignment : GDTypeFlowNodeKind.Unknown,
            _ => GDTypeFlowNodeKind.Unknown
        };
    }

    /// <summary>
    /// Gets detailed usage label including member name.
    /// </summary>
    protected static string GetDetailedUsageLabel(GDReference reference, string context)
    {
        var node = reference.ReferenceNode;
        var parent = node?.Parent;

        // Get the symbol name being referenced
        var symbolName = (node as GDIdentifierExpression)?.Identifier?.Sequence ?? "?";

        return parent switch
        {
            GDCallExpression call when call.CallerExpression is GDMemberOperatorExpression memberOp =>
                $"{symbolName}.{memberOp.Identifier?.Sequence ?? "?"}()",
            GDMemberOperatorExpression member => $"{symbolName}.{member.Identifier?.Sequence ?? "?"}",
            GDIndexerExpression indexer => GetIndexerLabelWithSymbol(indexer, symbolName),
            GDReturnExpression => "return",
            GDDualOperatorExpression dualOp when IsTypeCheckOperator(dualOp) =>
                $"{symbolName} is {GetTypeCheckTarget(dualOp)}",
            GDDualOperatorExpression dualOp when IsNullCheckOperator(dualOp) =>
                GetNullCheckLabelWithSymbol(dualOp, symbolName),
            _ => context.ToLowerInvariant()
        };
    }

    /// <summary>
    /// Gets source object info (name and type) for a reference.
    /// </summary>
    protected static (string? objectName, string? objectType) GetSourceObjectInfo(
        GDReference reference, string symbolName, GDSemanticModel semanticModel)
    {
        var node = reference.ReferenceNode;

        // If the reference itself is an identifier that's being accessed
        if (node is GDIdentifierExpression idExpr)
        {
            var name = idExpr.Identifier?.Sequence;
            if (name == symbolName)
            {
                var parent = node.Parent;

                // Check if this identifier is being accessed via member operator
                if (parent is GDMemberOperatorExpression memberOp && memberOp.CallerExpression == node)
                {
                    var callerTypeInfo = semanticModel.TypeSystem.GetType(node);
                    return (name, callerTypeInfo.IsVariant ? null : callerTypeInfo.DisplayName);
                }

                // Check if used as caller in a call expression
                if (parent is GDCallExpression call)
                {
                    if (call.CallerExpression is GDMemberOperatorExpression memOp &&
                        memOp.CallerExpression == node)
                    {
                        var callCallerTypeInfo = semanticModel.TypeSystem.GetType(node);
                        return (name, callCallerTypeInfo.IsVariant ? null : callCallerTypeInfo.DisplayName);
                    }
                }

                // Check if used in indexer
                if (parent is GDIndexerExpression indexer && indexer.CallerExpression == node)
                {
                    var indexerTypeInfo = semanticModel.TypeSystem.GetType(node);
                    return (name, indexerTypeInfo.IsVariant ? null : indexerTypeInfo.DisplayName);
                }

                return (name, null);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Gets indexer label like "[0]" or "[key]".
    /// </summary>
    protected static string GetIndexerLabel(GDIndexerExpression indexer)
    {
        var keyExpr = indexer.InnerExpression;
        if (keyExpr == null)
            return "[?]";

        return keyExpr switch
        {
            GDNumberExpression num => $"[{num}]",
            GDStringExpression str => $"[\"{str}\"]",
            GDIdentifierExpression id => $"[{id.Identifier?.Sequence ?? "?"}]",
            _ => "[...]"
        };
    }

    /// <summary>
    /// Gets indexer label with symbol name like "result[\"value\"]".
    /// </summary>
    protected static string GetIndexerLabelWithSymbol(GDIndexerExpression indexer, string symbolName)
    {
        var keyExpr = indexer.InnerExpression;
        if (keyExpr == null)
            return $"{symbolName}[?]";

        return keyExpr switch
        {
            GDNumberExpression num => $"{symbolName}[{num}]",
            GDStringExpression str => $"{symbolName}[\"{str.String?.Sequence ?? ""}\"]",
            GDIdentifierExpression id => $"{symbolName}[{id.Identifier?.Sequence ?? "?"}]",
            _ => $"{symbolName}[...]"
        };
    }

    /// <summary>
    /// Gets the type check target from an 'is' expression.
    /// </summary>
    protected static string GetTypeCheckTarget(GDDualOperatorExpression dualOp)
    {
        return dualOp.RightExpression?.ToString() ?? "?";
    }

    /// <summary>
    /// Gets label for null check.
    /// </summary>
    protected static string GetNullCheckLabel(GDDualOperatorExpression dualOp)
    {
        var opType = dualOp.Operator?.OperatorType;
        return opType == GDDualOperatorType.Equal ? "== null" : "!= null";
    }

    /// <summary>
    /// Gets label for null check with symbol name.
    /// </summary>
    protected static string GetNullCheckLabelWithSymbol(GDDualOperatorExpression dualOp, string symbolName)
    {
        var opType = dualOp.Operator?.OperatorType;
        return opType == GDDualOperatorType.Equal ? $"{symbolName} == null" : $"{symbolName} != null";
    }

    /// <summary>
    /// Checks if operator is a type check (is).
    /// </summary>
    protected static bool IsTypeCheckOperator(GDDualOperatorExpression dualOp)
    {
        return dualOp.Operator?.OperatorType == GDDualOperatorType.Is;
    }

    /// <summary>
    /// Checks if operator is a null check.
    /// </summary>
    protected static bool IsNullCheckOperator(GDDualOperatorExpression dualOp)
    {
        var opType = dualOp.Operator?.OperatorType;
        if (opType != GDDualOperatorType.Equal && opType != GDDualOperatorType.NotEqual)
            return false;

        return IsNullLiteral(dualOp.RightExpression) || IsNullLiteral(dualOp.LeftExpression);
    }

    /// <summary>
    /// Checks if expression is a null literal (identifier "null" or "nil").
    /// </summary>
    protected static bool IsNullLiteral(GDExpression? expr)
    {
        return expr is GDIdentifierExpression idExpr &&
               (idExpr.Identifier?.Sequence == "null" || idExpr.Identifier?.Sequence == "nil");
    }

    /// <summary>
    /// Checks if operator is a comparison.
    /// </summary>
    protected static bool IsComparisonOperator(GDDualOperatorExpression dualOp)
    {
        var opType = dualOp.Operator?.OperatorType;
        return opType switch
        {
            GDDualOperatorType.Equal => true,
            GDDualOperatorType.NotEqual => true,
            GDDualOperatorType.LessThan => true,
            GDDualOperatorType.LessThanOrEqual => true,
            GDDualOperatorType.MoreThan => true,
            GDDualOperatorType.MoreThanOrEqual => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if operator is an assignment.
    /// </summary>
    protected static bool IsAssignmentOperator(GDDualOperatorExpression dualOp)
    {
        var opType = dualOp.Operator?.OperatorType;
        return opType switch
        {
            GDDualOperatorType.Assignment => true,
            GDDualOperatorType.AddAndAssign => true,
            GDDualOperatorType.SubtractAndAssign => true,
            GDDualOperatorType.MultiplyAndAssign => true,
            GDDualOperatorType.DivideAndAssign => true,
            _ => false
        };
    }

    /// <summary>
    /// Builds outflows for a method (return statements inside and call sites).
    /// </summary>
    protected virtual void BuildMethodOutflows(
        GDTypeFlowNode node,
        GDMethodDeclaration method,
        Semantics.GDSymbolInfo symbol,
        GDScriptFile script,
        GDSemanticModel semanticModel)
    {
        // Find return statements inside the method
        if (method.Statements != null)
        {
            var returnStatements = method.AllNodes.OfType<GDReturnExpression>().Take(10);
            foreach (var ret in returnStatements)
            {
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

                var returnNode = new GDTypeFlowNode
                {
                    Id = GenerateNodeId(),
                    Label = ret.Expression != null ? $"return {GetExpressionLabel(ret.Expression)}" : "return",
                    Type = returnedType,
                    Kind = GDTypeFlowNodeKind.ReturnValue,
                    Confidence = 0.9f,
                    Description = "Return statement",
                    Level = 1,
                    SourceScript = script,
                    AstNode = ret,
                    Location = GDSourceLocation.FromNode(ret, script.FullPath)
                };
                RegisterNode(returnNode);
                node.Outflows.Add(returnNode);
                node.OutflowNodeIds.Add(returnNode.Id);
            }
        }

        // Add call sites (references to this method)
        var references = semanticModel.GetReferencesTo(symbol);
        foreach (var reference in references.Take(10))
        {
            if (reference.ReferenceNode == null)
                continue;

            var usageTypeInfo = semanticModel.TypeSystem.GetType(reference.ReferenceNode);
            var usageType = usageTypeInfo.IsVariant ? node.Type : usageTypeInfo.DisplayName;
            var usageNode = new GDTypeFlowNode
            {
                Id = GenerateNodeId(),
                Label = $"{symbol.Name}() call",
                Type = usageType,
                Kind = GDTypeFlowNodeKind.MethodCall,
                Confidence = reference.Confidence == GDReferenceConfidence.Strict ? 0.9f : 0.6f,
                Description = "Call site",
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
    /// Gets a simple label for an expression.
    /// </summary>
    protected static string GetExpressionLabel(GDExpression expression)
    {
        return expression switch
        {
            GDIdentifierExpression id => id.Identifier?.Sequence ?? "?",
            GDCallExpression call => GetCallerName(call) + "()",
            GDMemberOperatorExpression member => member.Identifier?.Sequence ?? "?",
            _ => expression.ToString().Length > 20 ? expression.ToString().Substring(0, 17) + "..." : expression.ToString()
        };
    }

    /// <summary>
    /// Builds a description for a usage node with line number and context-specific info.
    /// </summary>
    protected static string BuildUsageDescription(GDTypeFlowNodeKind usageKind, string context, GDSourceLocation? location)
    {
        var lineInfo = location?.IsValid == true ? $"Line {location.StartLine + 1}: " : "";

        return usageKind switch
        {
            GDTypeFlowNodeKind.TypeCheck => $"{lineInfo}Type guard",
            GDTypeFlowNodeKind.NullCheck => $"{lineInfo}Null check",
            GDTypeFlowNodeKind.Comparison => $"{lineInfo}Comparison",
            GDTypeFlowNodeKind.MethodCall => $"{lineInfo}Method call",
            GDTypeFlowNodeKind.PropertyAccess => $"{lineInfo}Property access",
            GDTypeFlowNodeKind.IndexerAccess => $"{lineInfo}Indexer access",
            GDTypeFlowNodeKind.ReturnValue => $"{lineInfo}Return value",
            GDTypeFlowNodeKind.Assignment => $"{lineInfo}Assignment",
            _ => $"{lineInfo}{context}"
        };
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

        // Check if we already have this node in registry (don't reset graph state)
        var existingNode = FindNodeBySymbol(symbolName, filePath);
        if (existingNode != null)
        {
            // If inflows are not loaded yet, build them
            if (!existingNode.AreInflowsLoaded)
            {
                var symbol = script.SemanticModel.FindSymbol(symbolName);
                if (symbol != null)
                {
                    BuildBasicInflows(existingNode, symbol, script, script.SemanticModel);
                    existingNode.AreInflowsLoaded = true;
                }
            }
            return existingNode.Inflows;
        }

        // Build new graph only if not found in registry
        var node = BuildGraphForSymbol(symbolName, script);
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

        // Check if we already have this node in registry (don't reset graph state)
        var existingNode = FindNodeBySymbol(symbolName, filePath);
        if (existingNode != null)
        {
            // If outflows are not loaded yet, build them
            if (!existingNode.AreOutflowsLoaded)
            {
                var symbol = script.SemanticModel.FindSymbol(symbolName);
                if (symbol != null)
                {
                    BuildBasicOutflows(existingNode, symbol, script, script.SemanticModel);
                    existingNode.AreOutflowsLoaded = true;
                }
            }
            return existingNode.Outflows;
        }

        // Build new graph only if not found in registry
        var node = BuildGraphForSymbol(symbolName, script);
        return node?.Outflows;
    }

    /// <summary>
    /// Finds an existing node in registry by symbol name and file path.
    /// Does not modify graph state.
    /// </summary>
    protected GDTypeFlowNode? FindNodeBySymbol(string symbolName, string filePath)
    {
        foreach (var kvp in _nodeRegistry)
        {
            var node = kvp.Value;
            if (node.Label == symbolName &&
                node.SourceScript?.FullPath != null &&
                node.SourceScript.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }
        }
        return null;
    }

    /// <summary>
    /// Builds graph for a symbol without clearing the entire registry.
    /// Used for lazy loading of connected nodes.
    /// </summary>
    protected virtual GDTypeFlowNode? BuildGraphForSymbol(string symbolName, GDScriptFile script)
    {
        if (script?.SemanticModel == null || string.IsNullOrEmpty(symbolName))
            return null;

        var semanticModel = script.SemanticModel;
        var symbol = semanticModel.FindSymbol(symbolName);
        if (symbol == null)
            return null;

        // Create the node without clearing registry
        var node = CreateNodeFromSymbol(symbol, script, semanticModel);
        if (node == null)
            return null;

        RegisterNode(node);

        // Add union type info
        var unionType = semanticModel.TypeSystem.GetUnionType(symbolName);
        if (unionType?.IsUnion == true)
        {
            node.IsUnionType = true;
            node.UnionTypeInfo = unionType;
            // Update Type to show union types (e.g., "int|String|null")
            node.Type = string.Join("|", unionType.Types.Take(3));
            if (unionType.Types.Count > 3)
                node.Type += "...";
        }

        // Add duck type info (only if not suppressed for known types)
        if (!semanticModel.ShouldSuppressDuckConstraints(symbolName))
        {
            var duckType = semanticModel.TypeSystem.GetDuckType(symbolName);
            if (duckType?.HasRequirements == true)
            {
                node.HasDuckConstraints = true;
                node.DuckTypeInfo = duckType;
            }
        }

        // Build basic inflows
        BuildBasicInflows(node, symbol, script, semanticModel);
        node.AreInflowsLoaded = true;

        // Build basic outflows
        BuildBasicOutflows(node, symbol, script, semanticModel);
        node.AreOutflowsLoaded = true;

        return node;
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

        var astNode = script.SemanticModel.GetNodeAtPosition(line, column);
        if (astNode == null)
            return null;

        var typeInfo = script.SemanticModel.TypeSystem.GetType(astNode);
        return typeInfo.IsVariant ? null : typeInfo.DisplayName;
    }
}
