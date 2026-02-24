using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Collects references in a GDScript file with full semantic context.
/// Unlike the Validator's GDReferenceCollector, this version:
/// - Uses IGDRuntimeProvider to resolve inherited members
/// - Tracks DeclaringTypeName for inherited member access
/// - Builds a GDSemanticModel for unified querying
/// </summary>
internal class GDSemanticReferenceCollector : GDVisitor
{
    private readonly GDScriptFile _scriptFile;
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly IGDRuntimeTypeInjector? _typeInjector;
    private GDTypeInferenceEngine? _typeEngine;
    private readonly GDTypeNarrowingAnalyzer? _narrowingAnalyzer;

    private GDSemanticModel? _model;
    private GDValidationContext? _validationContext;

    private readonly Stack<GDScopeInfo> _scopeStack = new();
    private GDScopeInfo? _currentScope;

    private readonly Stack<GDScope> _gdScopeStack = new();
    private GDScope? _currentGDScope;

    private readonly Stack<GDTypeNarrowingContext> _narrowingStack = new();
    private GDTypeNarrowingContext? _currentNarrowingContext;

    private bool _inAssignmentLeft;
    private bool _inPropertyWriteCaller;
    private bool _isSimpleAssignment;
    private bool _isCompoundAssignment;
    private readonly HashSet<GDNode> _visitedNodes = new();
    private readonly Dictionary<string, GDSymbolInfo> _inheritedSymbolCache = new();
    private readonly Dictionary<string, (GDAccessorDeclaration? First, GDAccessorDeclaration? Second)> _propertyAccessors = new();

    /// <summary>
    /// Creates a new semantic reference collector.
    /// </summary>
    /// <param name="scriptFile">The script file to analyze.</param>
    /// <param name="runtimeProvider">Runtime provider for type resolution (includes Godot types, project types, etc.).</param>
    /// <param name="typeInjector">Optional type injector for scene-based node type inference.</param>
    public GDSemanticReferenceCollector(
        GDScriptFile scriptFile,
        IGDRuntimeProvider? runtimeProvider = null,
        IGDRuntimeTypeInjector? typeInjector = null)
    {
        _scriptFile = scriptFile ?? throw new ArgumentNullException(nameof(scriptFile));
        _runtimeProvider = runtimeProvider;
        _typeInjector = typeInjector;

        if (runtimeProvider != null)
        {
            _narrowingAnalyzer = new GDTypeNarrowingAnalyzer(runtimeProvider);
        }
    }

    /// <summary>
    /// Builds the semantic model for the script file.
    /// </summary>
    public GDSemanticModel BuildSemanticModel()
    {
        _validationContext = new GDValidationContext(_runtimeProvider);

        if (_scriptFile.Class == null)
        {
            _model = new GDSemanticModel(_scriptFile, _runtimeProvider, _validationContext, null);
            return _model;
        }

        // First pass: collect class-level declarations to populate scopes
        var declarationCollector = new GDDeclarationCollector();
        declarationCollector.Collect(_scriptFile.Class, _validationContext);

        var scopeValidator = new GDScopeValidator(_validationContext);
        scopeValidator.Validate(_scriptFile.Class);

        // Validate() pops all scopes including global — restore them for TypeEngine
        _validationContext.Scopes.ResetToClass();

        if (_runtimeProvider != null)
        {
            if (_typeInjector != null)
            {
                var injectionContext = new GDTypeInjectionContext
                {
                    ScriptPath = _scriptFile.FullPath
                };
                _typeEngine = new GDTypeInferenceEngine(_runtimeProvider, _validationContext.Scopes, _typeInjector, injectionContext);
            }
            else
            {
                _typeEngine = new GDTypeInferenceEngine(_runtimeProvider, _validationContext.Scopes);
            }
        }

        _model = new GDSemanticModel(_scriptFile, _runtimeProvider, _validationContext, _typeEngine);

        // Must connect providers before walking the AST
        if (_typeEngine != null)
        {
            _typeEngine.SetContainerTypeProvider(varName => _model.GetInferredContainerType(varName));
            _typeEngine.SetSymbolLookupFallback((name, contextNode) =>
            {
                var symbolInfo = _model.FindSymbolInScope(name, contextNode);
                return symbolInfo?.Symbol;
            });
        }

        var extendsType = _scriptFile.Class.Extends?.Type;
        if (extendsType != null)
        {
            var extendsName = extendsType.BuildName();
            if (!string.IsNullOrEmpty(extendsName) && !IsBuiltInType(extendsName))
            {
                _model.AddTypeUsage(extendsName, _scriptFile.Class.Extends!, GDTypeUsageKind.Extends);
            }
        }

        CollectDeclarations(_scriptFile.Class, _validationContext);

        _currentScope = new GDScopeInfo(GDScopeType.Global, _scriptFile.Class);
        _scopeStack.Push(_currentScope);

        _currentGDScope = new GDScope(GDScopeType.Global, null, _scriptFile.Class);
        _gdScopeStack.Push(_currentGDScope);

        _currentNarrowingContext = new GDTypeNarrowingContext();

        _scriptFile.Class.WalkIn(this);

        CollectDuckTypes(_scriptFile.Class, _validationContext);
        CollectVariableUsageProfiles(_scriptFile.Class, _validationContext);
        CollectCallSites(_scriptFile.Class);

        var reflectionCollector = new GDReflectionCallSiteCollector(
            _typeEngine, _model, _scriptFile.FullPath ?? "", _scriptFile.TypeName);
        reflectionCollector.Analyze(_scriptFile.Class);

        return _model;
    }

    #region Declaration Collection

    private void CollectDeclarations(GDClassDeclaration classDecl, GDValidationContext context)
    {
        context.EnterScope(GDScopeType.Global, classDecl);
        CollectClassMembers(classDecl, context, _scriptFile.TypeName ?? "Unknown");
        context.ExitScope();
    }

    private void CollectClassMembers(IGDClassDeclaration classDecl, GDValidationContext context, string declaringTypeName)
    {
        foreach (var member in classDecl.Members)
        {
            switch (member)
            {
                case GDVariableDeclaration varDecl:
                    if (varDecl.ConstKeyword != null)
                        RegisterConstant(varDecl, context, declaringTypeName);
                    else if (HasAccessors(varDecl))
                        RegisterProperty(varDecl, context, declaringTypeName);
                    else
                        RegisterClassVariable(varDecl, context, declaringTypeName);
                    break;

                case GDMethodDeclaration methodDecl:
                    RegisterMethod(methodDecl, context, declaringTypeName);
                    break;

                case GDSignalDeclaration signalDecl:
                    RegisterSignal(signalDecl, context, declaringTypeName);
                    break;

                case GDEnumDeclaration enumDecl:
                    RegisterEnum(enumDecl, context, declaringTypeName);
                    break;

                case GDInnerClassDeclaration innerClass:
                    RegisterInnerClass(innerClass, context, declaringTypeName);
                    break;
            }
        }
    }

    /// <summary>
    /// Checks if a variable declaration has get/set accessors (making it a property).
    /// </summary>
    private static bool HasAccessors(GDVariableDeclaration varDecl)
    {
        return varDecl.FirstAccessorDeclarationNode != null || varDecl.SecondAccessorDeclarationNode != null;
    }

    private void RegisterInnerClass(GDInnerClassDeclaration innerClass, GDValidationContext context, string declaringTypeName)
    {
        var className = innerClass.Identifier?.Sequence;
        if (string.IsNullOrEmpty(className))
            return;

        DeclareClassMember(GDSymbol.Class(className, innerClass), context, declaringTypeName);

        context.EnterScope(GDScopeType.Class, innerClass);

        var innerTypeName = $"{declaringTypeName}.{className}";
        CollectClassMembers(innerClass, context, innerTypeName);

        context.ExitScope();
    }

    private void DeclareClassMember(GDSymbol symbol, GDValidationContext context, string declaringTypeName)
    {
        context.Declare(symbol);
        _model!.RegisterSymbol(GDSymbolInfo.ClassMember(symbol, declaringTypeName, _scriptFile));
    }

    private void RegisterClassVariable(GDVariableDeclaration varDecl, GDValidationContext context, string declaringTypeName)
    {
        var name = varDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var typeNode = varDecl.Type;
        var typeName = typeNode?.BuildName();
        var isStatic = varDecl.StaticKeyword != null;
        var symbol = GDSymbol.Variable(name, varDecl, typeName: typeName, typeNode: typeNode, isStatic: isStatic);
        DeclareClassMember(symbol, context, declaringTypeName);
    }

    private void RegisterProperty(GDVariableDeclaration propDecl, GDValidationContext context, string declaringTypeName)
    {
        var name = propDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var typeNode = propDecl.Type;
        var typeName = typeNode?.BuildName();
        var isStatic = propDecl.StaticKeyword != null;
        var symbol = GDSymbol.Property(name, propDecl, typeName: typeName, typeNode: typeNode, isStatic: isStatic);
        DeclareClassMember(symbol, context, declaringTypeName);

        _propertyAccessors[name] = (propDecl.FirstAccessorDeclarationNode, propDecl.SecondAccessorDeclarationNode);
    }

    private void RegisterMethod(GDMethodDeclaration methodDecl, GDValidationContext context, string declaringTypeName)
    {
        var name = methodDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var isStatic = methodDecl.IsStatic;
        var symbol = GDSymbol.Method(name, methodDecl, isStatic: isStatic);
        symbol.ReturnTypeName = methodDecl.ReturnType?.BuildName();
        symbol.Parameters = methodDecl.Parameters?
            .Select((p, i) => new GDParameterSymbolInfo(
                p.Identifier?.Sequence ?? $"param{i}",
                p.Type?.BuildName(),
                p.DefaultValue != null,
                i))
            .ToList();

        DeclareClassMember(symbol, context, declaringTypeName);
    }

    private void RegisterSignal(GDSignalDeclaration signalDecl, GDValidationContext context, string declaringTypeName)
    {
        var name = signalDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        DeclareClassMember(GDSymbol.Signal(name, signalDecl), context, declaringTypeName);
    }

    private void RegisterConstant(GDVariableDeclaration constDecl, GDValidationContext context, string declaringTypeName)
    {
        var name = constDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var typeNode = constDecl.Type;
        var typeName = typeNode?.BuildName();
        DeclareClassMember(GDSymbol.Constant(name, constDecl, typeName: typeName, typeNode: typeNode), context, declaringTypeName);
    }

    private void RegisterEnum(GDEnumDeclaration enumDecl, GDValidationContext context, string declaringTypeName)
    {
        var name = enumDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        DeclareClassMember(GDSymbol.Enum(name, enumDecl), context, declaringTypeName);
    }

    #endregion

    #region Scope Management

    public override void Visit(GDMethodDeclaration methodDeclaration)
    {
        PushScope(GDScopeType.Method, methodDeclaration);
        _validationContext?.Scopes.Push(GDScopeType.Method, methodDeclaration);
        RegisterParameters(methodDeclaration.Parameters, methodDeclaration);
    }

    public override void Left(GDMethodDeclaration methodDeclaration)
    {
        PopScope();
        _validationContext?.Scopes.Pop();
    }

    private void RegisterParameters(IEnumerable<GDParameterDeclaration>? parameters, GDNode declaringScopeNode)
    {
        if (parameters == null)
            return;

        foreach (var param in parameters)
        {
            var paramName = param.Identifier?.Sequence;
            if (string.IsNullOrEmpty(paramName))
                continue;

            var typeNode = param.Type;
            var typeName = typeNode?.BuildName() ?? "Variant";
            var symbol = GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: typeNode);

            _model!.RegisterSymbol(GDSymbolInfo.Local(symbol, _scriptFile, declaringScopeNode: declaringScopeNode));
            _validationContext?.Scopes.TryDeclare(symbol);
        }
    }

    public override void Visit(GDForStatement forStatement)
    {
        PushScope(GDScopeType.ForLoop, forStatement);
        _validationContext?.Scopes.Push(GDScopeType.ForLoop, forStatement);

        var iteratorName = forStatement.Variable?.Sequence;
        if (!string.IsNullOrEmpty(iteratorName))
        {
            string? elementTypeName = null;
            if (forStatement.Collection != null && _typeEngine != null)
            {
                var collectionType = _typeEngine.InferSemanticType(forStatement.Collection)?.DisplayName;
                if (!string.IsNullOrEmpty(collectionType))
                {
                    elementTypeName = GDTypeInferenceUtilities.GetCollectionElementType(collectionType);
                }
            }

            var symbol = GDSymbol.Iterator(iteratorName, forStatement, typeName: elementTypeName);
            var enclosingScope = FindEnclosingScopeNode(forStatement);
            _model!.RegisterSymbol(GDSymbolInfo.Local(symbol, _scriptFile, declaringScopeNode: enclosingScope));
            _validationContext?.Scopes.TryDeclare(symbol);
        }
    }

    public override void Left(GDForStatement forStatement)
    {
        PopScope();
        _validationContext?.Scopes.Pop();
    }

    public override void Visit(GDWhileStatement whileStatement)
    {
        PushScope(GDScopeType.WhileLoop, whileStatement);
        _validationContext?.Scopes.Push(GDScopeType.WhileLoop, whileStatement);
    }

    public override void Left(GDWhileStatement whileStatement)
    {
        PopScope();
        _validationContext?.Scopes.Pop();
    }

    public override void Visit(GDMethodExpression methodExpression)
    {
        PushScope(GDScopeType.Lambda, methodExpression);
        _validationContext?.Scopes.Push(GDScopeType.Lambda, methodExpression);
        RegisterParameters(methodExpression.Parameters, methodExpression);
    }

    public override void Left(GDMethodExpression methodExpression)
    {
        PopScope();
        _validationContext?.Scopes.Pop();
    }

    public override void Visit(GDMatchStatement matchStatement)
    {
        PushScope(GDScopeType.Match, matchStatement);
        _validationContext?.Scopes.Push(GDScopeType.Match, matchStatement);
    }

    public override void Left(GDMatchStatement matchStatement)
    {
        PopScope();
        _validationContext?.Scopes.Pop();
    }

    public override void Visit(GDIfStatement ifStatement)
    {
        PushScope(GDScopeType.Conditional, ifStatement);
        _validationContext?.Scopes.Push(GDScopeType.Conditional, ifStatement);

        _narrowingStack.Push(_currentNarrowingContext ?? new GDTypeNarrowingContext());
    }

    public override void Left(GDIfStatement ifStatement)
    {
        var afterIfNarrowing = ComputePostIfNarrowing(ifStatement);

        if (_narrowingStack.Count > 0)
            _currentNarrowingContext = _narrowingStack.Pop();

        if (afterIfNarrowing != null)
        {
            _currentNarrowingContext = afterIfNarrowing;
            _model!.SetPostIfNarrowing(ifStatement, afterIfNarrowing);
        }

        PopScope();
        _validationContext?.Scopes.Pop();
    }

    private string? ResolveVariableDeclaredType(string name, GDNode contextNode)
    {
        var symbol = _model?.FindSymbolInScope(name, contextNode);
        if (symbol == null)
            return null;

        if (symbol.TypeName != null)
            return symbol.TypeName;

        if (_typeEngine != null && symbol.DeclarationNode != null)
        {
            GDExpression? initializer = null;
            if (symbol.DeclarationNode is GDVariableDeclarationStatement localVar)
                initializer = localVar.Initializer;
            else if (symbol.DeclarationNode is GDVariableDeclaration classVar)
                initializer = classVar.Initializer;

            if (initializer != null)
                return _typeEngine.InferSemanticType(initializer)?.DisplayName;
        }

        return null;
    }

    public override void Visit(GDIfBranch ifBranch)
    {
        if (ifBranch.Condition != null && _narrowingAnalyzer != null)
        {
            _narrowingAnalyzer.SetVariableTypeResolver(name =>
                ResolveVariableDeclaredType(name, ifBranch));
            var ifNarrowing = _narrowingAnalyzer.AnalyzeCondition(ifBranch.Condition, isNegated: false);
            _currentNarrowingContext = ifNarrowing;
            _model!.SetNarrowingContext(ifBranch, ifNarrowing);
        }
    }

    public override void Left(GDIfBranch ifBranch)
    {
        if (_narrowingStack.Count > 0)
            _currentNarrowingContext = _narrowingStack.Peek();
    }

    public override void Visit(GDElifBranch elifBranch)
    {
        if (elifBranch.Condition != null && _narrowingAnalyzer != null)
        {
            _narrowingAnalyzer.SetVariableTypeResolver(name =>
                ResolveVariableDeclaredType(name, elifBranch));
            var elifNarrowing = _narrowingAnalyzer.AnalyzeCondition(elifBranch.Condition, isNegated: false);
            _currentNarrowingContext = elifNarrowing;
            _model!.SetNarrowingContext(elifBranch, elifNarrowing);
        }
    }

    public override void Left(GDElifBranch elifBranch)
    {
        if (_narrowingStack.Count > 0)
            _currentNarrowingContext = _narrowingStack.Peek();
    }

    public override void Visit(GDElseBranch elseBranch)
    {
        if (_narrowingStack.Count > 0)
            _currentNarrowingContext = _narrowingStack.Peek();
        else
            _currentNarrowingContext = new GDTypeNarrowingContext();
    }

    public override void Left(GDElseBranch elseBranch)
    {
    }

    /// <summary>
    /// Computes narrowing for code after an if-statement with early return.
    /// If the if-branch contains unconditional return and no else branch,
    /// the code after will have the inverse condition narrowing.
    /// </summary>
    private GDTypeNarrowingContext? ComputePostIfNarrowing(GDIfStatement ifStatement)
    {
        if (_narrowingAnalyzer == null)
            return null;

        var ifBranch = ifStatement.IfBranch;
        if (ifBranch == null || ifBranch.Condition == null)
            return null;

        // ElseBranch property creates empty branch if null — check ElseKeyword
        if (ifStatement.ElseBranch?.ElseKeyword != null)
            return null;

        if (!BranchHasUnconditionalReturn(ifBranch))
            return null;

        _narrowingAnalyzer.SetVariableTypeResolver(name =>
            ResolveVariableDeclaredType(name, ifBranch));
        return _narrowingAnalyzer.AnalyzeCondition(ifBranch.Condition, isNegated: true);
    }

    /// <summary>
    /// Checks if a branch contains an unconditional return statement.
    /// </summary>
    private static bool BranchHasUnconditionalReturn(GDIfBranch branch)
    {
        var statements = branch.Statements;
        if (statements == null || statements.Count == 0)
            return false;

        var lastStatement = statements.LastOrDefault();
        if (lastStatement is GDExpressionStatement exprStmt)
        {
            return exprStmt.Expression is GDReturnExpression;
        }
        return lastStatement is GDReturnExpression;
    }

    public override void Visit(GDInnerClassDeclaration innerClass)
    {
        PushScope(GDScopeType.Class, innerClass);
        _validationContext?.Scopes.Push(GDScopeType.Class, innerClass);
    }

    public override void Left(GDInnerClassDeclaration innerClass)
    {
        PopScope();
        _validationContext?.Scopes.Pop();
    }

    private void PushScope(GDScopeType type, GDNode node)
    {
        var scope = new GDScopeInfo(type, node, _currentScope);
        _scopeStack.Push(scope);
        _currentScope = scope;

        var gdScope = new GDScope(type, _currentGDScope, node);
        _gdScopeStack.Push(gdScope);
        _currentGDScope = gdScope;
    }

    private void PopScope()
    {
        if (_scopeStack.Count > 0)
        {
            _scopeStack.Pop();
            _currentScope = _scopeStack.Count > 0 ? _scopeStack.Peek() : null;
        }

        if (_gdScopeStack.Count > 0)
        {
            _gdScopeStack.Pop();
            _currentGDScope = _gdScopeStack.Count > 0 ? _gdScopeStack.Peek() : null;
        }
    }

    #endregion

    #region Property Accessor Handling

    public override void Visit(GDGetAccessorBodyDeclaration getterBody)
    {
        PushScope(GDScopeType.Method, getterBody);
        _validationContext?.Scopes.Push(GDScopeType.Method, getterBody);
    }

    public override void Left(GDGetAccessorBodyDeclaration getterBody)
    {
        PopScope();
        _validationContext?.Scopes.Pop();
    }

    public override void Visit(GDSetAccessorBodyDeclaration setterBody)
    {
        PushScope(GDScopeType.Method, setterBody);
        _validationContext?.Scopes.Push(GDScopeType.Method, setterBody);

        var param = setterBody.Parameter;
        if (param != null)
        {
            var paramName = param.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(paramName))
            {
                var propType = GetPropertyTypeFromAccessor(setterBody);
                var typeNode = param.Type;
                var typeName = typeNode?.BuildName() ?? propType;

                var symbol = GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: typeNode);
                _model!.RegisterSymbol(GDSymbolInfo.Local(symbol, _scriptFile, declaringScopeNode: setterBody));
                _validationContext?.Scopes.TryDeclare(symbol);
            }
        }
    }

    public override void Left(GDSetAccessorBodyDeclaration setterBody)
    {
        PopScope();
        _validationContext?.Scopes.Pop();
    }

    /// <summary>
    /// Gets the property type from an accessor by traversing up to the parent GDVariableDeclaration.
    /// </summary>
    private static string? GetPropertyTypeFromAccessor(GDNode accessor)
    {
        var current = accessor.Parent;
        while (current != null)
        {
            if (current is GDVariableDeclaration varDecl)
            {
                return varDecl.Type?.BuildName();
            }
            current = current.Parent;
        }
        return null;
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
            var enclosingScope = FindEnclosingScopeNode(variableDeclaration);
            _model!.RegisterSymbol(GDSymbolInfo.Local(symbol, _scriptFile, declaringScopeNode: enclosingScope));
            _validationContext?.Scopes.TryDeclare(symbol);
        }
    }

    public override void Visit(GDMatchCaseVariableExpression matchCaseVariable)
    {
        var varName = matchCaseVariable.Identifier?.Sequence;
        if (!string.IsNullOrEmpty(varName))
        {
            var symbol = GDSymbol.MatchCaseBinding(varName, matchCaseVariable);
            var enclosingMatchCase = FindEnclosingMatchCase(matchCaseVariable);
            _model!.RegisterSymbol(GDSymbolInfo.Local(symbol, _scriptFile, declaringScopeNode: enclosingMatchCase));
            _validationContext?.Scopes.TryDeclare(symbol);
        }
    }

    private static GDNode? FindEnclosingMatchCase(GDNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is GDMatchCaseDeclaration matchCase)
                return matchCase;
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Finds the enclosing method or lambda node for scope isolation.
    /// </summary>
    private static GDNode? FindEnclosingScopeNode(GDNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is GDMethodDeclaration || current is GDMethodExpression)
                return current as GDNode;
            current = current.Parent;
        }
        return null;
    }

    #endregion

    #region Type Usage Collection

    public override void Visit(GDSingleTypeNode typeNode)
    {
        CollectTypeUsage(typeNode);
    }

    public override void Visit(GDArrayTypeNode typeNode)
    {
        CollectTypeUsage(typeNode);
    }

    public override void Visit(GDDictionaryTypeNode typeNode)
    {
        CollectTypeUsage(typeNode);
    }

    private void CollectTypeUsage(GDTypeNode typeNode)
    {
        var typeName = typeNode.BuildName();
        if (string.IsNullOrEmpty(typeName))
            return;

        if (IsBuiltInType(typeName))
            return;

        var baseType = GetBaseTypeName(typeName);
        if (baseType != typeName && !IsBuiltInType(baseType))
        {
            _model!.AddTypeUsage(baseType, typeNode, GDTypeUsageKind.TypeAnnotation);
        }

        _model!.AddTypeUsage(baseType, typeNode, GDTypeUsageKind.TypeAnnotation);
    }

    public override void Visit(GDDualOperatorExpression dualOperator)
    {
        if (!_visitedNodes.Add(dualOperator))
            return;

        var opType = dualOperator.Operator?.OperatorType;

        if (opType == GDDualOperatorType.Is)
        {
            var rightExpr = dualOperator.RightExpression;
            if (rightExpr != null)
            {
                var typeName = rightExpr.ToString();
                if (!string.IsNullOrEmpty(typeName) && !IsBuiltInType(typeName))
                {
                    _model!.AddTypeUsage(typeName, dualOperator, GDTypeUsageKind.TypeCheck);
                }
            }
        }
        else if (opType != null && IsAssignmentOperator(opType.Value))
        {
            _inAssignmentLeft = true;
            if (opType.Value == GDDualOperatorType.Assignment)
            {
                _isSimpleAssignment = true;
            }
            else
            {
                _isCompoundAssignment = true;
            }
            dualOperator.LeftExpression?.WalkIn(this);
            _inAssignmentLeft = false;
            _isSimpleAssignment = false;
            _isCompoundAssignment = false;

            dualOperator.RightExpression?.WalkIn(this);
            RecordNodeType(dualOperator);
            return;
        }

        RecordNodeType(dualOperator);
    }

    private static bool IsBuiltInType(string typeName) => GDWellKnownTypes.IsBuiltInType(typeName);

    private static string GetBaseTypeName(string typeName)
    {
        return GDGenericTypeHelper.ExtractBaseTypeName(typeName);
    }

    #endregion

    #region Reference Collection

    public override void Visit(GDIdentifierExpression identifierExpression)
    {
        if (!_visitedNodes.Add(identifierExpression))
            return;

        var name = identifierExpression.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        if (_runtimeProvider?.IsBuiltIn(name) == true)
            return;

        var localSymbol = _model!.FindSymbolInScope(name, identifierExpression);
        if (localSymbol != null)
        {
            CreateReference(localSymbol, identifierExpression, GDReferenceConfidence.Strict);
            RecordNodeType(identifierExpression);
            return;
        }

        var inheritedSymbol = ResolveOrCreateInheritedSymbol(name);
        if (inheritedSymbol != null)
        {
            CreateReference(inheritedSymbol, identifierExpression, GDReferenceConfidence.Strict,
                callerTypeName: inheritedSymbol.DeclaringTypeName);
            RecordNodeType(identifierExpression);
            return;
        }

        RecordNodeType(identifierExpression);
    }

    public override void Visit(GDMemberOperatorExpression memberExpression)
    {
        var memberName = memberExpression.Identifier?.Sequence;
        if (string.IsNullOrEmpty(memberName))
        {
            RecordNodeType(memberExpression);
            return;
        }

        var callerExpr = memberExpression.CallerExpression;
        if (callerExpr == null)
        {
            RecordNodeType(memberExpression);
            return;
        }

        var callerType = _typeEngine?.InferSemanticType(callerExpr)?.DisplayName;

        if (!string.IsNullOrEmpty(callerType) && callerType != "Variant" && !callerType.StartsWith("Unknown"))
        {
            var symbolInfo = ResolveMemberOnType(callerType, memberName);
            if (symbolInfo != null)
            {
                CreateReference(symbolInfo, memberExpression, GDReferenceConfidence.Strict, callerType);
            }
            else
            {
                var unresolvedSymbol = GDSymbolInfo.DuckTyped(
                    memberName,
                    GDSymbolKind.Property,
                    callerType,
                    $"Unresolved member on '{callerType}'");
                CreateReference(unresolvedSymbol, memberExpression, GDReferenceConfidence.Potential, callerType);
            }
        }
        else
        {
            var varName = GDFlowNarrowingHelper.GetRootVariableName(callerExpr);
            if (!string.IsNullOrEmpty(varName))
            {
                var narrowedSemanticType = _currentNarrowingContext?.GetConcreteType(varName);
                var narrowedType = narrowedSemanticType?.DisplayName;
                if (!string.IsNullOrEmpty(narrowedType))
                {
                    var symbolInfo = ResolveMemberOnType(narrowedType, memberName);
                    if (symbolInfo != null)
                    {
                        CreateReference(symbolInfo, memberExpression, GDReferenceConfidence.Strict, narrowedType);
                    }
                }
                else
                {
                    var duckSymbol = GDSymbolInfo.DuckTyped(
                        memberName,
                        GDSymbolKind.Property,
                        null,
                        $"Duck-typed access on '{varName}'");

                    CreateReference(duckSymbol, memberExpression, GDReferenceConfidence.Potential,
                        callerTypeName: GDWellKnownTypes.Variant);
                }
            }
        }

        RecordNodeType(memberExpression);

        // In obj.prop = val, the member is the write target but the caller (obj) is a read
        if (_inAssignmentLeft)
        {
            _inAssignmentLeft = false;
            _inPropertyWriteCaller = _isSimpleAssignment;
        }
    }

    public override void Left(GDDualOperatorExpression dualOperator)
    {
        _inAssignmentLeft = false;
        _isSimpleAssignment = false;
        _isCompoundAssignment = false;
        _inPropertyWriteCaller = false;
    }

    public override void Visit(GDCallExpression callExpression)
    {
        var callerExpr = callExpression.CallerExpression;
        if (callerExpr != null)
        {
            if (callerExpr is GDIdentifierExpression idExpr)
            {
                var methodName = idExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(methodName))
                {
                    var methodSymbol = _model!.FindSymbolInScope(methodName, callExpression);
                    if (methodSymbol != null)
                    {
                        CreateReference(methodSymbol, callExpression, GDReferenceConfidence.Strict);
                    }
                    else
                    {
                        var inheritedSymbol = ResolveOrCreateInheritedSymbol(methodName);
                        if (inheritedSymbol != null)
                        {
                            CreateReference(inheritedSymbol, callExpression, GDReferenceConfidence.Strict,
                                callerTypeName: inheritedSymbol.DeclaringTypeName);
                        }
                        else if (_runtimeProvider != null)
                        {
                            var globalInfo = _runtimeProvider.GetMember("@GDScript", methodName);
                            if (globalInfo != null)
                            {
                                var globalSymbol = GDSymbolInfo.BuiltIn(globalInfo, "@GDScript");
                                CreateReference(globalSymbol, callExpression, GDReferenceConfidence.Strict, "@GDScript");
                            }
                        }
                    }

                    switch (methodName)
                    {
                        case "has_method":
                        case "call":
                        case "call_deferred":
                            TrackStringLiteralArg(callExpression, 0, GDSymbolKind.Method, methodName);
                            break;

                        case "has_signal":
                        case "emit_signal":
                        case "connect":
                            TrackStringLiteralArg(callExpression, 0, GDSymbolKind.Signal, methodName);
                            break;

                        case "get":
                        case "set":
                            TrackStringLiteralArg(callExpression, 0, GDSymbolKind.Property, methodName);
                            break;

                        case "Callable":
                            TrackStringLiteralArg(callExpression, 1, GDSymbolKind.Method, "Callable");
                            break;
                    }
                }
            }
            else if (callerExpr is GDMemberOperatorExpression memberOp)
            {
                var methodName = memberOp.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(methodName))
                {
                    var callerType = _typeEngine?.InferSemanticType(memberOp.CallerExpression)?.DisplayName;
                    if (!string.IsNullOrEmpty(callerType) && callerType != "Variant")
                    {
                        var symbolInfo = ResolveMemberOnType(callerType, methodName);
                        if (symbolInfo != null)
                        {
                            CreateReference(symbolInfo, callExpression, GDReferenceConfidence.Strict, callerType);
                        }
                        else
                        {
                            var unresolvedSymbol = GDSymbolInfo.DuckTyped(
                                methodName,
                                GDSymbolKind.Method,
                                callerType,
                                $"Unresolved method on '{callerType}'");
                            CreateReference(unresolvedSymbol, callExpression, GDReferenceConfidence.Potential, callerType);
                        }
                    }

                    switch (methodName)
                    {
                        case "has_method":
                        case "call":
                        case "call_deferred":
                            TrackStringLiteralArg(callExpression, 0, GDSymbolKind.Method, methodName);
                            break;

                        case "has_signal":
                        case "emit_signal":
                        case "connect":
                            TrackStringLiteralArg(callExpression, 0, GDSymbolKind.Signal, methodName);
                            break;

                        case "get":
                        case "set":
                            TrackStringLiteralArg(callExpression, 0, GDSymbolKind.Property, methodName);
                            break;

                        default:
                            if (_runtimeProvider != null && !string.IsNullOrEmpty(callerType))
                            {
                                var memberInfo = _runtimeProvider.GetMember(callerType, methodName);
                                if (memberInfo?.Parameters != null)
                                {
                                    for (int i = 0; i < memberInfo.Parameters.Count; i++)
                                    {
                                        var param = memberInfo.Parameters[i];
                                        if (param.Type == "StringName")
                                        {
                                            var kind = InferSymbolKindFromStringNameParam(param.Name, methodName);
                                            if (kind != GDSymbolKind.Variable) // Variable = Unknown
                                                TrackStringLiteralArg(callExpression, i, kind, methodName);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }

        RecordNodeType(callExpression);
    }

    public override void Visit(GDIndexerExpression indexerExpression)
    {
        RecordNodeType(indexerExpression);

        // In arr[i] = val, the indexer is the write target but the caller (arr) is a read
        if (_inAssignmentLeft)
        {
            _inAssignmentLeft = false;
            _inPropertyWriteCaller = _isSimpleAssignment;
        }
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

    #region Resolution Helpers

    /// <summary>
    /// Resolves an inherited member or returns a cached instance if already resolved.
    /// This ensures we don't create duplicate symbols for the same inherited member.
    /// </summary>
    private GDSymbolInfo? ResolveOrCreateInheritedSymbol(string memberName)
    {
        if (_inheritedSymbolCache.TryGetValue(memberName, out var cached))
            return cached;

        var symbol = ResolveInheritedMember(memberName);
        if (symbol != null)
        {
            _inheritedSymbolCache[memberName] = symbol;
            _model!.RegisterSymbol(symbol);
        }

        return symbol;
    }

    /// <summary>
    /// Resolves an inherited member using the runtime provider.
    /// The provider's GetMember() already walks the inheritance chain internally.
    /// </summary>
    private GDSymbolInfo? ResolveInheritedMember(string memberName)
    {
        var baseTypeName = _scriptFile.Class?.Extends?.Type?.BuildName();
        if (_runtimeProvider == null || string.IsNullOrEmpty(baseTypeName))
            return null;

        var memberInfo = _runtimeProvider.GetMember(baseTypeName, memberName);
        if (memberInfo != null)
        {
            var declaringType = FindDeclaringTypeForMember(baseTypeName, memberName) ?? baseTypeName;
            return GDSymbolInfo.BuiltIn(memberInfo, declaringType);
        }

        return null;
    }

    /// <summary>
    /// Finds which type in the inheritance chain actually declares a member.
    /// </summary>
    private string? FindDeclaringTypeForMember(string typeName, string memberName)
    {
        if (_runtimeProvider == null)
            return null;

        var visited = new HashSet<string>();
        var currentType = typeName;

        while (!string.IsNullOrEmpty(currentType) && visited.Add(currentType))
        {
            var typeInfo = _runtimeProvider.GetTypeInfo(currentType);
            if (typeInfo?.Members != null)
            {
                if (typeInfo.Members.Any(m => m.Name == memberName))
                    return currentType;
            }

            currentType = _runtimeProvider.GetBaseType(currentType);
        }

        return typeName;
    }

    /// <summary>
    /// Resolves a member on a specific type, including inherited members.
    /// </summary>
    private GDSymbolInfo? ResolveMemberOnType(string typeName, string memberName)
    {
        if (_runtimeProvider == null)
            return null;

        var memberInfo = _runtimeProvider.GetMember(typeName, memberName);
        if (memberInfo == null)
            return null;

        var declaringType = FindDeclaringTypeForMember(typeName, memberName) ?? typeName;

        return GDSymbolInfo.BuiltIn(memberInfo, declaringType);
    }

    /// <summary>
    /// Infers the symbol kind from a StringName parameter name and method name.
    /// Returns GDSymbolKind.Variable as a sentinel for "unknown / don't track".
    /// </summary>
    private static GDSymbolKind InferSymbolKindFromStringNameParam(string paramName, string methodName)
    {
        var lower = paramName.ToLowerInvariant();

        if (lower.Contains("method") || lower.Contains("func") || lower == "receiver_func")
            return GDSymbolKind.Method;

        if (lower.Contains("signal"))
            return GDSymbolKind.Signal;

        if (lower.Contains("property"))
            return GDSymbolKind.Property;

        var lowerMethod = methodName.ToLowerInvariant();
        if (lowerMethod.StartsWith("call") || lowerMethod.Contains("method"))
            return GDSymbolKind.Method;

        if (lowerMethod.Contains("signal") || lowerMethod.Contains("emit"))
            return GDSymbolKind.Signal;

        return GDSymbolKind.Variable; // Unknown — don't track
    }

    /// <summary>
    /// Tracks a string literal argument in a reflection-style call (has_method, emit_signal, call, etc.)
    /// as a Potential reference. For concatenated strings, records a warning instead.
    /// </summary>
    private void TrackStringLiteralArg(
        GDCallExpression callExpression,
        int argIndex,
        GDSymbolKind symbolKind,
        string callerMethodName)
    {
        var args = callExpression.Parameters?.ToList();
        if (args == null || args.Count <= argIndex)
            return;

        var argExpr = args[argIndex] as GDExpression;
        if (argExpr == null)
            return;

        var resolver = GDStaticStringExtractor.CreateClassResolver(
            callExpression.RootClassDeclaration);
        var (literalValue, sourceNode) = GDStaticStringExtractor.TryExtractStringWithNode(
            argExpr, resolver);

        if (string.IsNullOrEmpty(literalValue))
            return;

        if (sourceNode != null)
        {
            var reason = $"{callerMethodName}(\"{literalValue}\") string literal";
            var symbol = GDSymbolInfo.DuckTyped(literalValue, symbolKind, null, reason);
            CreateReference(symbol, sourceNode, GDReferenceConfidence.Potential,
                callerTypeName: GDWellKnownTypes.Variant);

            if (_model != null)
            {
                var declaredSymbol = _model.FindSymbol(literalValue);
                if (declaredSymbol != null && declaredSymbol.Kind == symbolKind)
                {
                    CreateReference(declaredSymbol, sourceNode, GDReferenceConfidence.Potential,
                        callerTypeName: null);
                }
            }
        }
        else
        {
            _model?.AddStringReferenceWarning(literalValue, argExpr,
                $"{callerMethodName}() contains concatenated string that evaluates to \"{literalValue}\" — manual update required");
        }
    }

    private void CreateReference(GDSymbolInfo symbol, GDNode node, GDReferenceConfidence confidence, string? callerTypeName = null)
    {
        var reference = new GDReference
        {
            ReferenceNode = node,
            Scope = _currentGDScope,
            IsWrite = _inAssignmentLeft,
            IsRead = !_inAssignmentLeft || _isCompoundAssignment,
            IsPropertyWriteOnCaller = _inPropertyWriteCaller,
            Confidence = confidence,
            ConfidenceReason = BuildConfidenceReason(symbol, confidence),
            CallerTypeName = callerTypeName,
            IdentifierToken = ResolveIdentifierToken(node)
        };

        _inPropertyWriteCaller = false;

        if (_typeEngine != null && node is GDExpression expr && _recordingTypes.Add(expr))
        {
            try
            {
                var typeNode = _typeEngine.InferTypeNode(expr);
                if (typeNode != null)
                {
                    reference.InferredType = GDSemanticType.FromTypeNode(typeNode);
                    reference.InferredTypeNode = typeNode;
                }
            }
            finally
            {
                _recordingTypes.Remove(expr);
            }
        }

        _model!.AddReference(symbol, reference);
        _model.SetNodeSymbol(node, symbol);

        if (!string.IsNullOrEmpty(callerTypeName))
        {
            _model.AddMemberAccess(callerTypeName, symbol.Name, reference);
        }
    }

    private static GDSyntaxToken? ResolveIdentifierToken(GDNode node)
    {
        if (node is GDMemberOperatorExpression memberOp)
            return memberOp.Identifier;

        if (node is GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDMemberOperatorExpression callerMemberOp)
                return callerMemberOp.Identifier;

            if (callExpr.CallerExpression is GDIdentifierExpression callerIdExpr)
                return callerIdExpr.Identifier;
        }

        if (node is GDIdentifierExpression idExpr)
            return idExpr.Identifier;

        // StartColumn points to opening quote; rename service must offset +1
        if (node is GDStringExpression strExpr)
            return strExpr.String;

        if (node is GDStringNameExpression strNameExpr)
            return strNameExpr.String;

        return null;
    }

    private static string BuildConfidenceReason(GDSymbolInfo symbol, GDReferenceConfidence confidence)
    {
        return confidence switch
        {
            GDReferenceConfidence.Strict when symbol.IsInherited =>
                $"Inherited member from {symbol.DeclaringTypeName}",
            GDReferenceConfidence.Strict when symbol.DeclaringTypeName != null =>
                $"Member of {symbol.DeclaringTypeName}",
            GDReferenceConfidence.Strict =>
                "Local symbol",
            GDReferenceConfidence.Potential =>
                symbol.ConfidenceReason ?? "Potential match (duck-typed)",
            _ =>
                "Name match only"
        };
    }

    private readonly HashSet<GDExpression> _recordingTypes = new();

    private void RecordNodeType(GDExpression expression)
    {
        if (_typeEngine == null)
            return;

        if (!_recordingTypes.Add(expression))
            return;

        try
        {
            var typeNode = _typeEngine.InferTypeNode(expression);
            if (typeNode != null)
            {
                _model!.SetNodeType(expression, typeNode.BuildName(), typeNode);
            }
        }
        finally
        {
            _recordingTypes.Remove(expression);
        }
    }

    private static bool IsAssignmentOperator(GDDualOperatorType opType)
    {
        return opType switch
        {
            GDDualOperatorType.Assignment => true,
            GDDualOperatorType.AddAndAssign => true,
            GDDualOperatorType.SubtractAndAssign => true,
            GDDualOperatorType.MultiplyAndAssign => true,
            GDDualOperatorType.DivideAndAssign => true,
            GDDualOperatorType.ModAndAssign => true,
            GDDualOperatorType.BitwiseAndAndAssign => true,
            GDDualOperatorType.BitwiseOrAndAssign => true,
            GDDualOperatorType.PowerAndAssign => true,
            GDDualOperatorType.BitShiftLeftAndAssign => true,
            GDDualOperatorType.BitShiftRightAndAssign => true,
            GDDualOperatorType.XorAndAssign => true,
            _ => false
        };
    }

    #endregion

    #region Duck Type Collection

    private void CollectDuckTypes(GDClassDeclaration classDecl, GDValidationContext context)
    {
        var duckCollector = new GDDuckTypeCollector(context.Scopes, _runtimeProvider);
        duckCollector.Collect(classDecl);

        foreach (var kv in duckCollector.VariableDuckTypes)
        {
            _model!.SetDuckType(kv.Key, kv.Value);
        }
    }

    #endregion

    #region Variable Usage Collection

    private void CollectVariableUsageProfiles(GDClassDeclaration classDecl, GDValidationContext context)
    {
        if (classDecl.Members == null)
            return;

        var classCollector = new GDClassVariableCollector(_typeEngine);
        classCollector.Collect(classDecl);

        foreach (var kv in classCollector.Profiles)
            _model!.SetVariableProfile(kv.Key, kv.Value);

        foreach (var member in classDecl.Members)
        {
            if (member is GDMethodDeclaration method)
            {
                var varCollector = new GDVariableUsageCollector(context.Scopes, _typeEngine);
                varCollector.Collect(method);

                foreach (var kv in varCollector.Profiles)
                    _model!.SetVariableProfile(kv.Key, kv.Value);

                var containerCollector = new GDContainerUsageCollector(context.Scopes, _typeEngine);
                containerCollector.Collect(method);

                foreach (var kv in containerCollector.Profiles)
                    _model!.SetContainerProfile(kv.Key, kv.Value);
            }
        }

        var className = classDecl.ClassName?.Identifier?.Sequence ?? _scriptFile.TypeName ?? "";
        var classContainerCollector = new GDClassContainerUsageCollector(classDecl, _typeEngine);
        classContainerCollector.Collect(classDecl);

        foreach (var kv in classContainerCollector.Profiles)
        {
            _model!.SetClassContainerProfile(className, kv.Key, kv.Value);
        }
    }

    #endregion

    #region Call Site Collection

    /// <summary>
    /// Collects Callable call sites for lambda parameter type inference.
    /// </summary>
    private void CollectCallSites(GDClassDeclaration classDecl)
    {
        if (classDecl == null)
            return;

        var collector = new GDCallableCallSiteCollector(
            _scriptFile,
            expr => _typeEngine?.InferSemanticType(expr));

        collector.Collect(classDecl);

        var registry = _model!.GetOrCreateCallSiteRegistry();
        registry.RegisterCollector(_scriptFile.FullPath ?? "", collector);

        CollectCallableFlow(classDecl, registry);

        if (_typeEngine != null)
        {
            _typeEngine.SetCallSiteRegistry(registry);
            _typeEngine.SetSourceFile(_scriptFile);
        }
    }

    /// <summary>
    /// Collects inter-procedural Callable flow (method profiles and argument bindings).
    /// </summary>
    private void CollectCallableFlow(GDClassDeclaration classDecl, GDCallableCallSiteRegistry registry)
    {
        Func<string, GDMethodDeclaration?> methodResolver = methodName =>
        {
            if (classDecl.Members == null)
                return null;

            foreach (var member in classDecl.Members)
            {
                if (member is GDMethodDeclaration method &&
                    method.Identifier?.Sequence == methodName)
                {
                    return method;
                }
            }
            return null;
        };

        var flowCollector = new GDCallableFlowCollector(
            _scriptFile,
            expr => _typeEngine?.InferSemanticType(expr),
            methodResolver);

        flowCollector.Collect(classDecl);
        registry.RegisterFlowCollector(_scriptFile.FullPath ?? "", flowCollector);
    }

    #endregion
}

/// <summary>
/// Simple scope info for tracking during reference collection.
/// </summary>
internal class GDScopeInfo
{
    public GDScopeType Type { get; }
    public GDNode Node { get; }
    public GDScopeInfo? Parent { get; }

    public GDScopeInfo(GDScopeType type, GDNode node, GDScopeInfo? parent = null)
    {
        Type = type;
        Node = node;
        Parent = parent;
    }
}
