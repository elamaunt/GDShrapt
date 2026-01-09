using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents a reference from one location to a symbol.
    /// </summary>
    public class GDReference
    {
        /// <summary>
        /// The symbol being referenced.
        /// </summary>
        public GDSymbol Symbol { get; set; }

        /// <summary>
        /// The node where the reference occurs.
        /// </summary>
        public GDNode ReferenceNode { get; set; }

        /// <summary>
        /// The scope in which the reference occurs.
        /// </summary>
        public GDScope Scope { get; set; }

        /// <summary>
        /// The inferred type at this reference point (for reverse type inference).
        /// </summary>
        public string InferredType { get; set; }

        /// <summary>
        /// The type node at this reference point (for generic types).
        /// </summary>
        public GDTypeNode InferredTypeNode { get; set; }

        /// <summary>
        /// True if this is a write (assignment) to the symbol.
        /// </summary>
        public bool IsWrite { get; set; }

        /// <summary>
        /// True if this is a read of the symbol.
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// The confidence level of this reference.
        /// </summary>
        public GDReferenceConfidence Confidence { get; set; } = GDReferenceConfidence.NameMatch;

        /// <summary>
        /// The resolved type of the caller expression (for member access/calls).
        /// Null if type could not be resolved.
        /// </summary>
        public string CallerResolvedType { get; set; }

        /// <summary>
        /// The expected declaring type of the symbol being referenced.
        /// Used for cross-file type matching.
        /// </summary>
        public string ExpectedDeclaringType { get; set; }

        /// <summary>
        /// Duck type information if the caller is untyped.
        /// </summary>
        public GDDuckType CallerDuckType { get; set; }

        /// <summary>
        /// Reason for the confidence determination (for debugging/UI).
        /// </summary>
        public string ConfidenceReason { get; set; }
    }

    /// <summary>
    /// Indicates the confidence level of a reference being a true reference to the target symbol.
    /// </summary>
    public enum GDReferenceConfidence
    {
        /// <summary>
        /// The reference is confirmed via resolved type information.
        /// The caller's type is known and matches the symbol's declaring type.
        /// </summary>
        Strict,

        /// <summary>
        /// The reference may be correct but type cannot be fully resolved.
        /// Examples: Variant-typed variables, duck-typed access, untyped parameters.
        /// </summary>
        Potential,

        /// <summary>
        /// The reference is based on name matching only.
        /// The caller's type is incompatible or cannot be determined.
        /// </summary>
        NameMatch
    }

    /// <summary>
    /// Result of reference collection containing all references and type information.
    /// </summary>
    public class GDReferenceResult
    {
        private readonly Dictionary<GDSymbol, List<GDReference>> _forwardReferences;
        private readonly Dictionary<GDNode, GDSymbol> _nodeToSymbol;
        private readonly Dictionary<GDNode, string> _nodeTypes;
        private readonly Dictionary<GDNode, GDTypeNode> _nodeTypeNodes;
        private readonly Dictionary<string, GDDuckType> _variableDuckTypes;
        private readonly Dictionary<GDNode, GDTypeNarrowingContext> _narrowingContexts;

        public GDReferenceResult()
        {
            _forwardReferences = new Dictionary<GDSymbol, List<GDReference>>();
            _nodeToSymbol = new Dictionary<GDNode, GDSymbol>();
            _nodeTypes = new Dictionary<GDNode, string>();
            _nodeTypeNodes = new Dictionary<GDNode, GDTypeNode>();
            _variableDuckTypes = new Dictionary<string, GDDuckType>();
            _narrowingContexts = new Dictionary<GDNode, GDTypeNarrowingContext>();
        }

        /// <summary>
        /// All symbols in the collected scope.
        /// </summary>
        public IEnumerable<GDSymbol> Symbols => _forwardReferences.Keys;

        /// <summary>
        /// Gets all references to a symbol (forward referencing: who uses this symbol).
        /// </summary>
        public IReadOnlyList<GDReference> GetReferencesTo(GDSymbol symbol)
        {
            return _forwardReferences.TryGetValue(symbol, out var refs)
                ? refs
                : (IReadOnlyList<GDReference>)System.Array.Empty<GDReference>();
        }

        /// <summary>
        /// Gets the symbol that a node references (back referencing: what does this node reference).
        /// </summary>
        public GDSymbol GetSymbolForNode(GDNode node)
        {
            return _nodeToSymbol.TryGetValue(node, out var symbol) ? symbol : null;
        }

        /// <summary>
        /// Gets the inferred type for a node.
        /// </summary>
        public string GetTypeForNode(GDNode node)
        {
            return _nodeTypes.TryGetValue(node, out var type) ? type : null;
        }

        /// <summary>
        /// Gets the full type node (with generics) for a node.
        /// </summary>
        public GDTypeNode GetTypeNodeForNode(GDNode node)
        {
            return _nodeTypeNodes.TryGetValue(node, out var typeNode) ? typeNode : null;
        }

        /// <summary>
        /// Finds all symbols of a specific kind.
        /// </summary>
        public IEnumerable<GDSymbol> GetSymbolsOfKind(GDSymbolKind kind)
        {
            return _forwardReferences.Keys.Where(s => s.Kind == kind);
        }

        /// <summary>
        /// Finds a symbol by name.
        /// </summary>
        public GDSymbol FindSymbol(string name)
        {
            return _forwardReferences.Keys.FirstOrDefault(s => s.Name == name);
        }

        internal void AddReference(GDSymbol symbol, GDReference reference)
        {
            if (!_forwardReferences.TryGetValue(symbol, out var refs))
            {
                refs = new List<GDReference>();
                _forwardReferences[symbol] = refs;
            }
            refs.Add(reference);
        }

        internal void SetNodeSymbol(GDNode node, GDSymbol symbol)
        {
            _nodeToSymbol[node] = symbol;
        }

        internal void SetNodeType(GDNode node, string type, GDTypeNode typeNode = null)
        {
            if (!string.IsNullOrEmpty(type))
                _nodeTypes[node] = type;
            if (typeNode != null)
                _nodeTypeNodes[node] = typeNode;
        }

        internal void RegisterSymbol(GDSymbol symbol)
        {
            if (!_forwardReferences.ContainsKey(symbol))
                _forwardReferences[symbol] = new List<GDReference>();
        }

        /// <summary>
        /// Gets duck type information for a variable (what methods/properties it must have).
        /// </summary>
        public GDDuckType GetDuckType(string variableName)
        {
            return _variableDuckTypes.TryGetValue(variableName, out var duckType) ? duckType : null;
        }

        /// <summary>
        /// Gets all collected duck types.
        /// </summary>
        public IReadOnlyDictionary<string, GDDuckType> DuckTypes => _variableDuckTypes;

        /// <summary>
        /// Gets the type narrowing context at a specific node (for flow-sensitive analysis).
        /// </summary>
        public GDTypeNarrowingContext GetNarrowingContext(GDNode node)
        {
            return _narrowingContexts.TryGetValue(node, out var context) ? context : null;
        }

        /// <summary>
        /// Gets the narrowed type for a variable at a specific location in the code.
        /// </summary>
        /// <param name="variableName">The variable name</param>
        /// <param name="atNode">The location to check narrowing</param>
        /// <returns>The narrowed type or null if no narrowing applies</returns>
        public string GetNarrowedType(string variableName, GDNode atNode)
        {
            var context = GetNarrowingContext(atNode);
            return context?.GetConcreteType(variableName);
        }

        /// <summary>
        /// Gets the effective type for a variable, considering both declared type and duck typing.
        /// </summary>
        public string GetEffectiveType(string variableName, GDNode atNode = null)
        {
            // First check for narrowed type at location
            if (atNode != null)
            {
                var narrowed = GetNarrowedType(variableName, atNode);
                if (narrowed != null)
                    return narrowed;
            }

            // Check for symbol with declared type
            var symbol = FindSymbol(variableName);
            if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName))
                return symbol.TypeName;

            // Return duck type info as a string representation
            var duckType = GetDuckType(variableName);
            return duckType?.ToString();
        }

        internal void SetDuckType(string variableName, GDDuckType duckType)
        {
            _variableDuckTypes[variableName] = duckType;
        }

        internal void MergeDuckType(string variableName, GDDuckType duckType)
        {
            if (!_variableDuckTypes.TryGetValue(variableName, out var existing))
            {
                _variableDuckTypes[variableName] = duckType;
            }
            else
            {
                existing.MergeWith(duckType);
            }
        }

        internal void SetNarrowingContext(GDNode node, GDTypeNarrowingContext context)
        {
            _narrowingContexts[node] = context;
        }
    }

    /// <summary>
    /// Collects references between symbols in a GDScript AST.
    /// Builds forward references (symbol → who uses it) and back references (node → what symbol).
    /// Also tracks inferred types for each node for reverse type inference.
    /// Supports duck typing analysis and type narrowing from control flow.
    /// </summary>
    public class GDReferenceCollector : GDVisitor
    {
        private GDValidationContext _context;
        private GDReferenceResult _result;
        private GDTypeInferenceEngine _typeEngine;
        private GDTypeNarrowingAnalyzer _narrowingAnalyzer;
        private GDScope _currentScope;
        private GDTypeNarrowingContext _currentNarrowingContext;
        private bool _inAssignmentLeft;
        private readonly Stack<GDTypeNarrowingContext> _narrowingStack = new Stack<GDTypeNarrowingContext>();

        /// <summary>
        /// Collects all references from the AST.
        /// </summary>
        /// <param name="node">The root AST node to analyze.</param>
        /// <param name="context">Optional validation context with pre-collected declarations.</param>
        /// <param name="runtimeProvider">Optional runtime provider for type inference.</param>
        /// <returns>Reference collection result with forward/back references and types.</returns>
        public GDReferenceResult Collect(GDNode node, GDValidationContext context = null, IGDRuntimeProvider runtimeProvider = null)
        {
            if (node == null)
                return new GDReferenceResult();

            _result = new GDReferenceResult();

            // Use provided context or create new one
            _context = context ?? new GDValidationContext(runtimeProvider);

            // If no context provided, we need to collect declarations first
            if (context == null)
            {
                var collector = new GDDeclarationCollector();
                collector.Collect(node, _context);
            }

            // Create type inference engine
            _typeEngine = new GDTypeInferenceEngine(
                _context.RuntimeProvider,
                _context.Scopes);

            // Create type narrowing analyzer
            _narrowingAnalyzer = new GDTypeNarrowingAnalyzer(_context.RuntimeProvider);
            _currentNarrowingContext = new GDTypeNarrowingContext();

            // Register all symbols from declarations
            foreach (var symbol in _context.Scopes.Global?.Symbols ?? Enumerable.Empty<GDSymbol>())
            {
                _result.RegisterSymbol(symbol);
            }

            // Enter global scope and walk the tree
            _currentScope = _context.EnterScope(GDScopeType.Global, node);
            node.WalkIn(this);
            _context.ExitScope();

            // Collect duck types from member accesses
            var duckCollector = new GDDuckTypeCollector(_context.Scopes);
            duckCollector.Collect(node);
            foreach (var kv in duckCollector.VariableDuckTypes)
            {
                _result.SetDuckType(kv.Key, kv.Value);
            }

            return _result;
        }

        #region Scope Management

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            _currentScope = _context.EnterScope(GDScopeType.Method, methodDeclaration);

            // Register parameters
            var parameters = methodDeclaration.Parameters;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var paramName = param.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        var typeNode = param.Type;
                        var typeName = typeNode?.BuildName();
                        var symbol = GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: typeNode);
                        _context.Declare(symbol);
                        _result.RegisterSymbol(symbol);
                    }
                }
            }
        }

        public override void Left(GDMethodDeclaration methodDeclaration)
        {
            _currentScope = _context.ExitScope();
        }

        public override void Visit(GDForStatement forStatement)
        {
            _currentScope = _context.EnterScope(GDScopeType.ForLoop, forStatement);

            var iteratorName = forStatement.Variable?.Sequence;
            if (!string.IsNullOrEmpty(iteratorName))
            {
                var symbol = GDSymbol.Iterator(iteratorName, forStatement);
                _context.Declare(symbol);
                _result.RegisterSymbol(symbol);
            }
        }

        public override void Left(GDForStatement forStatement)
        {
            _currentScope = _context.ExitScope();
        }

        public override void Visit(GDWhileStatement whileStatement)
        {
            _currentScope = _context.EnterScope(GDScopeType.WhileLoop, whileStatement);
        }

        public override void Left(GDWhileStatement whileStatement)
        {
            _currentScope = _context.ExitScope();
        }

        public override void Visit(GDMethodExpression methodExpression)
        {
            _currentScope = _context.EnterScope(GDScopeType.Lambda, methodExpression);

            var parameters = methodExpression.Parameters;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var paramName = param.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        var typeNode = param.Type;
                        var typeName = typeNode?.BuildName();
                        var symbol = GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: typeNode);
                        _context.Declare(symbol);
                        _result.RegisterSymbol(symbol);
                    }
                }
            }
        }

        public override void Left(GDMethodExpression methodExpression)
        {
            _currentScope = _context.ExitScope();
        }

        public override void Visit(GDMatchStatement matchStatement)
        {
            _currentScope = _context.EnterScope(GDScopeType.Match, matchStatement);
        }

        public override void Left(GDMatchStatement matchStatement)
        {
            _currentScope = _context.ExitScope();
        }

        public override void Visit(GDIfStatement ifStatement)
        {
            _currentScope = _context.EnterScope(GDScopeType.Conditional, ifStatement);

            // Analyze condition for type narrowing (condition is in IfBranch)
            var condition = ifStatement.IfBranch?.Condition;
            if (condition != null)
            {
                var narrowingContext = _narrowingAnalyzer.AnalyzeCondition(condition, isNegated: false);
                _narrowingStack.Push(_currentNarrowingContext);
                _currentNarrowingContext = narrowingContext;
                _result.SetNarrowingContext(ifStatement, narrowingContext);
            }
        }

        public override void Left(GDIfStatement ifStatement)
        {
            // Restore previous narrowing context
            if (_narrowingStack.Count > 0)
                _currentNarrowingContext = _narrowingStack.Pop();

            _currentScope = _context.ExitScope();
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            // Don't recurse into inner classes - they need separate analysis
        }

        public override void Left(GDInnerClassDeclaration innerClass)
        {
            // Nothing to do
        }

        #endregion

        #region Local Declarations

        public override void Visit(GDVariableDeclarationStatement variableDeclaration)
        {
            var varName = variableDeclaration.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var typeNode = variableDeclaration.Type;
                var typeName = typeNode?.BuildName();
                var symbol = GDSymbol.Variable(varName, variableDeclaration, typeName: typeName, typeNode: typeNode);
                _context.Declare(symbol);
                _result.RegisterSymbol(symbol);
            }
        }

        public override void Visit(GDMatchCaseVariableExpression matchCaseVariable)
        {
            var varName = matchCaseVariable.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var symbol = GDSymbol.Variable(varName, matchCaseVariable);
                _context.Declare(symbol);
                _result.RegisterSymbol(symbol);
            }
        }

        #endregion

        #region Reference Collection

        public override void Visit(GDIdentifierExpression identifierExpression)
        {
            var name = identifierExpression.Identifier?.Sequence;
            if (string.IsNullOrEmpty(name))
                return;

            // Skip built-in identifiers
            if (_context.RuntimeProvider.IsBuiltIn(name))
                return;

            // Try to resolve the symbol
            var symbol = _context.Lookup(name);
            if (symbol != null)
            {
                // Create reference
                var reference = new GDReference
                {
                    Symbol = symbol,
                    ReferenceNode = identifierExpression,
                    Scope = _currentScope,
                    IsWrite = _inAssignmentLeft,
                    IsRead = !_inAssignmentLeft
                };

                // Add type information
                var typeNode = _typeEngine.InferTypeNode(identifierExpression);
                if (typeNode != null)
                {
                    reference.InferredType = typeNode.BuildName();
                    reference.InferredTypeNode = typeNode;
                }

                _result.AddReference(symbol, reference);
                _result.SetNodeSymbol(identifierExpression, symbol);
            }

            // Record type for the node regardless of symbol resolution
            RecordNodeType(identifierExpression);
        }

        public override void Visit(GDDualOperatorExpression dualOperator)
        {
            var opType = dualOperator.Operator?.OperatorType;
            if (opType != null && IsAssignmentOperator(opType.Value))
            {
                // Mark that we're in the left side of an assignment
                _inAssignmentLeft = true;
            }

            // Record type for the operator expression
            RecordNodeType(dualOperator);
        }

        public override void Left(GDDualOperatorExpression dualOperator)
        {
            _inAssignmentLeft = false;
        }

        public override void Visit(GDCallExpression callExpression)
        {
            RecordNodeType(callExpression);
        }

        public override void Visit(GDMemberOperatorExpression memberExpression)
        {
            RecordNodeType(memberExpression);
        }

        public override void Visit(GDIndexerExpression indexerExpression)
        {
            RecordNodeType(indexerExpression);
        }

        public override void Visit(GDNumberExpression numberExpression)
        {
            RecordNodeType(numberExpression);
        }

        public override void Visit(GDStringExpression stringExpression)
        {
            RecordNodeType(stringExpression);
        }

        public override void Visit(GDBoolExpression boolExpression)
        {
            RecordNodeType(boolExpression);
        }

        public override void Visit(GDArrayInitializerExpression arrayExpression)
        {
            RecordNodeType(arrayExpression);
        }

        public override void Visit(GDDictionaryInitializerExpression dictionaryExpression)
        {
            RecordNodeType(dictionaryExpression);
        }

        #endregion

        #region Helpers

        private void RecordNodeType(GDExpression expression)
        {
            var typeNode = _typeEngine.InferTypeNode(expression);
            if (typeNode != null)
            {
                _result.SetNodeType(expression, typeNode.BuildName(), typeNode);
            }
        }

        private bool IsAssignmentOperator(GDDualOperatorType opType)
        {
            switch (opType)
            {
                case GDDualOperatorType.Assignment:
                case GDDualOperatorType.AddAndAssign:
                case GDDualOperatorType.SubtractAndAssign:
                case GDDualOperatorType.MultiplyAndAssign:
                case GDDualOperatorType.DivideAndAssign:
                case GDDualOperatorType.ModAndAssign:
                case GDDualOperatorType.BitwiseAndAndAssign:
                case GDDualOperatorType.BitwiseOrAndAssign:
                case GDDualOperatorType.PowerAndAssign:
                case GDDualOperatorType.BitShiftLeftAndAssign:
                case GDDualOperatorType.BitShiftRightAndAssign:
                case GDDualOperatorType.XorAndAssign:
                    return true;
                default:
                    return false;
            }
        }

        #endregion
    }
}
