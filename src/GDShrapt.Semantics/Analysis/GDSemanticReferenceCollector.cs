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

    // The semantic model being built
    private GDSemanticModel? _model;

    // Validation context for TypeEngine scope management
    private GDValidationContext? _validationContext;

    // Scope tracking (for internal use during collection)
    private readonly Stack<GDScopeInfo> _scopeStack = new();
    private GDScopeInfo? _currentScope;

    // GDScope tracking (for Reference.Scope - uses Abstractions type)
    private readonly Stack<GDScope> _gdScopeStack = new();
    private GDScope? _currentGDScope;

    // Type narrowing
    private readonly Stack<GDTypeNarrowingContext> _narrowingStack = new();
    private GDTypeNarrowingContext? _currentNarrowingContext;

    // Assignment tracking
    private bool _inAssignmentLeft;

    // Track nodes we've already visited to prevent duplicate processing
    private readonly HashSet<GDNode> _visitedNodes = new();

    // Cache for inherited symbols (to avoid creating duplicates)
    private readonly Dictionary<string, GDSymbolInfo> _inheritedSymbolCache = new();

    // Property accessors cache (property name -> (first accessor, second accessor))
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
        // Use validator's declaration collector to properly set up scopes
        var declarationCollector = new GDDeclarationCollector();
        declarationCollector.Collect(_scriptFile.Class, _validationContext);

        // Second pass: collect local declarations (local variables, parameters, iterators)
        // This is needed so GDTypeInferenceEngine can resolve local variable types
        var scopeValidator = new GDScopeValidator(_validationContext);
        scopeValidator.Validate(_scriptFile.Class);

        // Reset scope stack to class scope after validation
        // GDScopeValidator.Validate() pops all scopes including global, which breaks
        // GDTypeInferenceEngine.Lookup() since it needs access to class-level declarations
        // (methods, signals, class variables). ResetToClass() restores both Global and Class scopes.
        _validationContext.Scopes.ResetToClass();

        // Create type engine with proper scopes (after all declarations are collected)
        // Store in field so it can be used during reference collection
        // If type injector is provided, use it for scene-based node type inference
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

        // Connect container type provider to type engine for usage-based inference
        // This must be done before walking the AST so that indexer type inference works
        if (_typeEngine != null)
        {
            _typeEngine.SetContainerTypeProvider(varName => _model.GetInferredContainerType(varName));

            // Connect symbol lookup fallback for when scope-based lookup fails
            // This is needed during validation when method scopes have been popped
            // but SemanticModel still has all symbols registered
            _typeEngine.SetSymbolLookupFallback((name, contextNode) =>
            {
                var symbolInfo = _model.FindSymbolInScope(name, contextNode);
                return symbolInfo?.Symbol;
            });
        }

        // Collect extends type usage
        var extendsType = _scriptFile.Class.Extends?.Type;
        if (extendsType != null)
        {
            var extendsName = extendsType.BuildName();
            if (!string.IsNullOrEmpty(extendsName) && !IsBuiltInType(extendsName))
            {
                _model.AddTypeUsage(extendsName, _scriptFile.Class.Extends!, GDTypeUsageKind.Extends);
            }
        }

        // Register declarations in semantic model
        CollectDeclarations(_scriptFile.Class, _validationContext);

        // Initialize scopes for reference collection (both internal GDScopeInfo and GDScope for references)
        _currentScope = new GDScopeInfo(GDScopeType.Global, _scriptFile.Class);
        _scopeStack.Push(_currentScope);

        _currentGDScope = new GDScope(GDScopeType.Global, null, _scriptFile.Class);
        _gdScopeStack.Push(_currentGDScope);

        _currentNarrowingContext = new GDTypeNarrowingContext();

        // Second pass: collect references
        _scriptFile.Class.WalkIn(this);

        // Collect duck types (pass scopes to filter out typed variables)
        CollectDuckTypes(_scriptFile.Class, _validationContext);

        // Collect variable usage profiles for Union type inference
        CollectVariableUsageProfiles(_scriptFile.Class, _validationContext);

        // Collect Callable call sites for lambda parameter inference
        CollectCallSites(_scriptFile.Class);

        return _model;
    }

    #region Declaration Collection

    private void CollectDeclarations(GDClassDeclaration classDecl, GDValidationContext context)
    {
        // Enter global scope
        context.EnterScope(GDScopeType.Global, classDecl);

        // Collect class members
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
                    // Constants are GDVariableDeclaration with ConstKeyword
                    if (varDecl.ConstKeyword != null)
                        RegisterConstant(varDecl, context, declaringTypeName);
                    // Properties are GDVariableDeclaration with get/set accessors
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

        // Register the inner class itself as a symbol
        var symbol = GDSymbol.Class(className, innerClass);
        context.Declare(symbol);

        var symbolInfo = GDSymbolInfo.ClassMember(
            symbol,
            declaringTypeName: declaringTypeName,
            declaringScript: _scriptFile);
        _model!.RegisterSymbol(symbolInfo);

        // Enter inner class scope and collect its members
        context.EnterScope(GDScopeType.Class, innerClass);

        var innerTypeName = $"{declaringTypeName}.{className}";
        CollectClassMembers(innerClass, context, innerTypeName);

        context.ExitScope();
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

        context.Declare(symbol);

        var symbolInfo = GDSymbolInfo.ClassMember(
            symbol,
            declaringTypeName: declaringTypeName,
            declaringScript: _scriptFile);

        _model!.RegisterSymbol(symbolInfo);
    }

    /// <summary>
    /// Registers a property (a variable with get/set accessors).
    /// </summary>
    private void RegisterProperty(GDVariableDeclaration propDecl, GDValidationContext context, string declaringTypeName)
    {
        var name = propDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var typeNode = propDecl.Type;
        var typeName = typeNode?.BuildName();
        var isStatic = propDecl.StaticKeyword != null;

        // Create property symbol
        var symbol = GDSymbol.Property(name, propDecl, typeName: typeName, typeNode: typeNode, isStatic: isStatic);

        context.Declare(symbol);

        var symbolInfo = GDSymbolInfo.ClassMember(
            symbol,
            declaringTypeName: declaringTypeName,
            declaringScript: _scriptFile);

        _model!.RegisterSymbol(symbolInfo);

        // Store property info for accessor resolution
        _propertyAccessors[name] = (propDecl.FirstAccessorDeclarationNode, propDecl.SecondAccessorDeclarationNode);
    }

    private void RegisterMethod(GDMethodDeclaration methodDecl, GDValidationContext context, string declaringTypeName)
    {
        var name = methodDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var isStatic = methodDecl.IsStatic;
        var symbol = GDSymbol.Method(name, methodDecl, isStatic: isStatic);

        context.Declare(symbol);

        var symbolInfo = GDSymbolInfo.ClassMember(
            symbol,
            declaringTypeName: declaringTypeName,
            declaringScript: _scriptFile);

        _model!.RegisterSymbol(symbolInfo);
    }

    private void RegisterSignal(GDSignalDeclaration signalDecl, GDValidationContext context, string declaringTypeName)
    {
        var name = signalDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var symbol = GDSymbol.Signal(name, signalDecl);
        context.Declare(symbol);

        var symbolInfo = GDSymbolInfo.ClassMember(
            symbol,
            declaringTypeName: declaringTypeName,
            declaringScript: _scriptFile);

        _model!.RegisterSymbol(symbolInfo);
    }

    private void RegisterConstant(GDVariableDeclaration constDecl, GDValidationContext context, string declaringTypeName)
    {
        var name = constDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var typeNode = constDecl.Type;
        var typeName = typeNode?.BuildName();
        var symbol = GDSymbol.Constant(name, constDecl, typeName: typeName, typeNode: typeNode);

        context.Declare(symbol);

        var symbolInfo = GDSymbolInfo.ClassMember(
            symbol,
            declaringTypeName: declaringTypeName,
            declaringScript: _scriptFile);

        _model!.RegisterSymbol(symbolInfo);
    }

    private void RegisterEnum(GDEnumDeclaration enumDecl, GDValidationContext context, string declaringTypeName)
    {
        var name = enumDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var symbol = GDSymbol.Enum(name, enumDecl);
        context.Declare(symbol);

        var symbolInfo = GDSymbolInfo.ClassMember(
            symbol,
            declaringTypeName: declaringTypeName,
            declaringScript: _scriptFile);

        _model!.RegisterSymbol(symbolInfo);
    }

    #endregion

    #region Scope Management

    public override void Visit(GDMethodDeclaration methodDeclaration)
    {
        PushScope(GDScopeType.Method, methodDeclaration);

        // Also push scope in validation context for TypeEngine to access local symbols
        _validationContext?.Scopes.Push(GDScopeType.Method, methodDeclaration);

        // Register parameters with the method as the declaring scope
        var parameters = methodDeclaration.Parameters;
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                var paramName = param.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(paramName))
                {
                    var typeNode = param.Type;
                    // Use explicit type if present, otherwise Variant (for duck typing)
                    var typeName = typeNode?.BuildName() ?? "Variant";
                    var symbol = GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: typeNode);

                    // Pass the method as the declaring scope for parameter isolation
                    var symbolInfo = GDSymbolInfo.Local(symbol, _scriptFile, declaringScopeNode: methodDeclaration);
                    _model!.RegisterSymbol(symbolInfo);

                    // Also add to validation context scopes for TypeEngine
                    _validationContext?.Scopes.TryDeclare(symbol);
                }
            }
        }
    }

    public override void Left(GDMethodDeclaration methodDeclaration)
    {
        PopScope();

        // Also pop from validation context
        _validationContext?.Scopes.Pop();
    }

    public override void Visit(GDForStatement forStatement)
    {
        PushScope(GDScopeType.ForLoop, forStatement);
        _validationContext?.Scopes.Push(GDScopeType.ForLoop, forStatement);

        var iteratorName = forStatement.Variable?.Sequence;
        if (!string.IsNullOrEmpty(iteratorName))
        {
            // Infer element type from collection for proper iterator typing
            // This prevents false positives like GD3013 when iterating Array of Dictionary
            string? elementTypeName = null;
            if (forStatement.Collection != null && _typeEngine != null)
            {
                var collectionType = _typeEngine.InferType(forStatement.Collection);
                if (!string.IsNullOrEmpty(collectionType))
                {
                    elementTypeName = GDTypeInferenceUtilities.GetCollectionElementType(collectionType);
                }
            }

            var symbol = GDSymbol.Iterator(iteratorName, forStatement, typeName: elementTypeName);
            // Pass the enclosing method/lambda for scope isolation
            var enclosingScope = FindEnclosingScopeNode(forStatement);
            var symbolInfo = GDSymbolInfo.Local(symbol, _scriptFile, declaringScopeNode: enclosingScope);
            _model!.RegisterSymbol(symbolInfo);

            // Also add to validation context scopes for TypeEngine
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

        // Register lambda parameters with the lambda as the declaring scope
        var parameters = methodExpression.Parameters;
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                var paramName = param.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(paramName))
                {
                    var typeNode = param.Type;
                    // Use explicit type if present, otherwise Variant (for duck typing)
                    var typeName = typeNode?.BuildName() ?? "Variant";
                    var symbol = GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: typeNode);

                    // Pass the lambda as the declaring scope for parameter isolation
                    var symbolInfo = GDSymbolInfo.Local(symbol, _scriptFile, declaringScopeNode: methodExpression);
                    _model!.RegisterSymbol(symbolInfo);

                    // Also add to validation context scopes for TypeEngine
                    _validationContext?.Scopes.TryDeclare(symbol);
                }
            }
        }
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

        // Store parent narrowing context
        _narrowingStack.Push(_currentNarrowingContext ?? new GDTypeNarrowingContext());
    }

    public override void Left(GDIfStatement ifStatement)
    {
        // Check for early return pattern: if the if-branch contains an unconditional return,
        // then the code after the if-statement should have the inverse narrowing
        var afterIfNarrowing = ComputePostIfNarrowing(ifStatement);

        if (_narrowingStack.Count > 0)
            _currentNarrowingContext = _narrowingStack.Pop();

        // Apply early return narrowing to code after this if-statement
        if (afterIfNarrowing != null)
        {
            _currentNarrowingContext = afterIfNarrowing;
            // Register for the containing method's statements that follow
            _model!.SetPostIfNarrowing(ifStatement, afterIfNarrowing);
        }

        PopScope();
        _validationContext?.Scopes.Pop();
    }

    public override void Visit(GDIfBranch ifBranch)
    {
        // Analyze if-branch condition
        if (ifBranch.Condition != null && _narrowingAnalyzer != null)
        {
            var ifNarrowing = _narrowingAnalyzer.AnalyzeCondition(ifBranch.Condition, isNegated: false);
            _currentNarrowingContext = ifNarrowing;
            _model!.SetNarrowingContext(ifBranch, ifNarrowing);
        }
    }

    public override void Left(GDIfBranch ifBranch)
    {
        // Restore parent context after leaving if branch
        // This ensures elif/else branches don't inherit if-branch narrowing
        if (_narrowingStack.Count > 0)
        {
            _currentNarrowingContext = _narrowingStack.Peek();
        }
    }

    public override void Visit(GDElifBranch elifBranch)
    {
        // Analyze elif branch condition
        if (elifBranch.Condition != null && _narrowingAnalyzer != null)
        {
            var elifNarrowing = _narrowingAnalyzer.AnalyzeCondition(elifBranch.Condition, isNegated: false);
            _currentNarrowingContext = elifNarrowing;
            _model!.SetNarrowingContext(elifBranch, elifNarrowing);
        }
    }

    public override void Left(GDElifBranch elifBranch)
    {
        // Restore parent context after leaving elif branch
        // This ensures subsequent elif/else branches don't inherit this branch's narrowing
        if (_narrowingStack.Count > 0)
        {
            _currentNarrowingContext = _narrowingStack.Peek();
        }
    }

    public override void Visit(GDElseBranch elseBranch)
    {
        // In else branch, all previous conditions were false
        // Reset narrowing context to parent (no narrowing from if/elif branches applies here)
        // The _narrowingStack contains the parent context pushed in Visit(GDIfStatement)
        if (_narrowingStack.Count > 0)
        {
            // Peek the parent context without popping (will be popped in Left(GDIfStatement))
            _currentNarrowingContext = _narrowingStack.Peek();
        }
        else
        {
            // No parent context, use empty
            _currentNarrowingContext = new GDTypeNarrowingContext();
        }
    }

    public override void Left(GDElseBranch elseBranch)
    {
        // Nothing special to do
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

        // Only apply if there's no actual else branch (early return pattern)
        // Check for ElseKeyword since ElseBranch property creates empty branch if null
        if (ifStatement.ElseBranch?.ElseKeyword != null)
            return null;

        // Check if if-branch has unconditional return
        if (!BranchHasUnconditionalReturn(ifBranch))
            return null;

        // The code after the if has the inverse of the condition
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

        // Check if the last statement is a return
        var lastStatement = statements.LastOrDefault();
        if (lastStatement is GDExpressionStatement exprStmt)
        {
            return exprStmt.Expression is GDReturnExpression;
        }
        return lastStatement is GDReturnExpression;
    }

    public override void Visit(GDInnerClassDeclaration innerClass)
    {
        // Inner class symbol is already registered in CollectClassMembers/RegisterInnerClass
        // Here we just push scope for reference tracking during the visitor walk
        PushScope(GDScopeType.Class, innerClass);
        _validationContext?.Scopes.Push(GDScopeType.Class, innerClass);
    }

    public override void Left(GDInnerClassDeclaration innerClass)
    {
        // Exit inner class scope
        PopScope();
        _validationContext?.Scopes.Pop();
    }

    private void PushScope(GDScopeType type, GDNode node)
    {
        // Push internal scope
        var scope = new GDScopeInfo(type, node, _currentScope);
        _scopeStack.Push(scope);
        _currentScope = scope;

        // Push GDScope for reference tracking
        var gdScope = new GDScope(type, _currentGDScope, node);
        _gdScopeStack.Push(gdScope);
        _currentGDScope = gdScope;
    }

    private void PopScope()
    {
        // Pop internal scope
        if (_scopeStack.Count > 0)
        {
            _scopeStack.Pop();
            _currentScope = _scopeStack.Count > 0 ? _scopeStack.Peek() : null;
        }

        // Pop GDScope
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
        // Getter body acts like a method scope
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
        // Setter body acts like a method scope
        PushScope(GDScopeType.Method, setterBody);
        _validationContext?.Scopes.Push(GDScopeType.Method, setterBody);

        // Register the setter parameter
        var param = setterBody.Parameter;
        if (param != null)
        {
            var paramName = param.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(paramName))
            {
                // Try to infer type from the property declaration
                var propType = GetPropertyTypeFromAccessor(setterBody);
                var typeNode = param.Type;
                var typeName = typeNode?.BuildName() ?? propType;

                var symbol = GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: typeNode);
                var symbolInfo = GDSymbolInfo.Local(symbol, _scriptFile, declaringScopeNode: setterBody);
                _model!.RegisterSymbol(symbolInfo);

                // Also add to validation context scopes for TypeEngine
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

            // Pass the enclosing method/lambda for scope isolation
            var enclosingScope = FindEnclosingScopeNode(variableDeclaration);
            var symbolInfo = GDSymbolInfo.Local(symbol, _scriptFile, declaringScopeNode: enclosingScope);
            _model!.RegisterSymbol(symbolInfo);

            // Also add to validation context scopes for TypeEngine to access local variable types
            _validationContext?.Scopes.TryDeclare(symbol);
        }
    }

    public override void Visit(GDMatchCaseVariableExpression matchCaseVariable)
    {
        var varName = matchCaseVariable.Identifier?.Sequence;
        if (!string.IsNullOrEmpty(varName))
        {
            var symbol = GDSymbol.Variable(varName, matchCaseVariable);
            // Pass the enclosing method/lambda for scope isolation
            var enclosingScope = FindEnclosingScopeNode(matchCaseVariable);
            var symbolInfo = GDSymbolInfo.Local(symbol, _scriptFile, declaringScopeNode: enclosingScope);
            _model!.RegisterSymbol(symbolInfo);

            // Also add to validation context scopes for TypeEngine
            _validationContext?.Scopes.TryDeclare(symbol);
        }
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
        // Collect type annotations (var x: ClassName, func f(x: ClassName))
        var typeName = typeNode.BuildName();
        if (string.IsNullOrEmpty(typeName))
            return;

        // Skip built-in types
        if (IsBuiltInType(typeName))
            return;

        // For generic types like Array[Player], also track the inner type
        var baseType = GetBaseTypeName(typeName);
        if (baseType != typeName && !IsBuiltInType(baseType))
        {
            _model!.AddTypeUsage(baseType, typeNode, GDTypeUsageKind.TypeAnnotation);
        }

        _model!.AddTypeUsage(baseType, typeNode, GDTypeUsageKind.TypeAnnotation);
    }

    public override void Visit(GDDualOperatorExpression dualOperator)
    {
        // Skip if already visited (prevents duplicate processing when manually walking)
        if (!_visitedNodes.Add(dualOperator))
            return;

        var opType = dualOperator.Operator?.OperatorType;

        // Track 'is' type checks
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
        // Track assignment operators - need to handle left/right separately
        else if (opType != null && IsAssignmentOperator(opType.Value))
        {
            // Visit left side as write target
            _inAssignmentLeft = true;
            dualOperator.LeftExpression?.WalkIn(this);
            _inAssignmentLeft = false;

            // Visit right side as read (not a write target)
            dualOperator.RightExpression?.WalkIn(this);

            // Record the overall expression type
            RecordNodeType(dualOperator);

            // Note: WalkIn will still walk children after Visit returns,
            // but they will be skipped due to _visitedNodes check
            return;
        }

        RecordNodeType(dualOperator);
    }

    private static bool IsBuiltInType(string typeName)
    {
        return typeName switch
        {
            "int" or "float" or "bool" or "String" or "void" or "Variant" => true,
            "Array" or "Dictionary" or "Callable" or "Signal" or "NodePath" or "StringName" => true,
            "Vector2" or "Vector2i" or "Vector3" or "Vector3i" or "Vector4" or "Vector4i" => true,
            "Rect2" or "Rect2i" or "AABB" or "Transform2D" or "Transform3D" => true,
            "Basis" or "Projection" or "Quaternion" or "Plane" => true,
            "Color" or "RID" or "Object" or "Nil" => true,
            "PackedByteArray" or "PackedInt32Array" or "PackedInt64Array" => true,
            "PackedFloat32Array" or "PackedFloat64Array" => true,
            "PackedStringArray" or "PackedVector2Array" or "PackedVector3Array" => true,
            "PackedColorArray" or "PackedVector4Array" => true,
            _ => false
        };
    }

    private static string GetBaseTypeName(string typeName)
    {
        // Extract base type from generics: Array[Player] -> Player
        var bracketIndex = typeName.IndexOf('[');
        if (bracketIndex > 0)
        {
            var inner = typeName.Substring(bracketIndex + 1, typeName.Length - bracketIndex - 2);
            // Handle nested: Dictionary[String,Player] -> just return first non-builtin
            var parts = inner.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!IsBuiltInType(trimmed))
                    return trimmed;
            }
            return typeName.Substring(0, bracketIndex);
        }
        return typeName;
    }

    #endregion

    #region Reference Collection

    public override void Visit(GDIdentifierExpression identifierExpression)
    {
        // Skip if already visited (prevents duplicate processing when manually walking)
        if (!_visitedNodes.Add(identifierExpression))
            return;

        var name = identifierExpression.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        // Skip built-in identifiers
        if (_runtimeProvider?.IsBuiltIn(name) == true)
            return;

        // Try to resolve locally first using scope-aware lookup
        var localSymbol = _model!.FindSymbolInScope(name, identifierExpression);
        if (localSymbol != null)
        {
            CreateReference(localSymbol, identifierExpression, GDReferenceConfidence.Strict);
            RecordNodeType(identifierExpression);
            return;
        }

        // Try to resolve as inherited member
        var inheritedSymbol = ResolveOrCreateInheritedSymbol(name);
        if (inheritedSymbol != null)
        {
            CreateReference(inheritedSymbol, identifierExpression, GDReferenceConfidence.Strict);
            RecordNodeType(identifierExpression);
            return;
        }

        // Record type anyway
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

        // Infer caller type
        var callerType = _typeEngine?.InferType(callerExpr);

        if (!string.IsNullOrEmpty(callerType) && callerType != "Variant" && !callerType.StartsWith("Unknown"))
        {
            // Type is known - resolve member with declaring type info
            var symbolInfo = ResolveMemberOnType(callerType, memberName);
            if (symbolInfo != null)
            {
                CreateReference(symbolInfo, memberExpression, GDReferenceConfidence.Strict, callerType);
            }
        }
        else
        {
            // Type unknown - check for type narrowing
            var varName = GDFlowNarrowingHelpers.GetRootVariableName(callerExpr);
            if (!string.IsNullOrEmpty(varName))
            {
                var narrowedType = _currentNarrowingContext?.GetConcreteType(varName);
                if (!string.IsNullOrEmpty(narrowedType))
                {
                    // Type was narrowed via 'is' check
                    var symbolInfo = ResolveMemberOnType(narrowedType, memberName);
                    if (symbolInfo != null)
                    {
                        CreateReference(symbolInfo, memberExpression, GDReferenceConfidence.Strict, narrowedType);
                    }
                }
                else
                {
                    // Duck typed access - no callerTypeName for duck-typed
                    var duckSymbol = GDSymbolInfo.DuckTyped(
                        memberName,
                        GDSymbolKind.Property,
                        null,
                        $"Duck-typed access on '{varName}'");

                    CreateReference(duckSymbol, memberExpression, GDReferenceConfidence.Potential);
                }
            }
        }

        RecordNodeType(memberExpression);
    }

    public override void Left(GDDualOperatorExpression dualOperator)
    {
        _inAssignmentLeft = false;
    }

    public override void Visit(GDCallExpression callExpression)
    {
        // Create reference for the called method
        var callerExpr = callExpression.CallerExpression;
        if (callerExpr != null)
        {
            // Direct function call: func_name()
            if (callerExpr is GDIdentifierExpression idExpr)
            {
                var methodName = idExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(methodName))
                {
                    // Try to resolve locally first (local method)
                    var methodSymbol = _model!.FindSymbolInScope(methodName, callExpression);
                    if (methodSymbol != null)
                    {
                        CreateReference(methodSymbol, callExpression, GDReferenceConfidence.Strict);
                    }
                    else
                    {
                        // Try inherited method
                        var inheritedSymbol = ResolveOrCreateInheritedSymbol(methodName);
                        if (inheritedSymbol != null)
                        {
                            CreateReference(inheritedSymbol, callExpression, GDReferenceConfidence.Strict);
                        }
                        else if (_runtimeProvider != null)
                        {
                            // Check if it's a global function (@GDScript built-in like str2var, load, etc.)
                            var globalInfo = _runtimeProvider.GetMember("@GDScript", methodName);
                            if (globalInfo != null)
                            {
                                var globalSymbol = GDSymbolInfo.BuiltIn(globalInfo, "@GDScript");
                                CreateReference(globalSymbol, callExpression, GDReferenceConfidence.Strict, "@GDScript");
                            }
                        }
                    }
                }
            }
            // Member method call: obj.method()
            else if (callerExpr is GDMemberOperatorExpression memberOp)
            {
                var methodName = memberOp.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(methodName))
                {
                    var callerType = _typeEngine?.InferType(memberOp.CallerExpression);
                    if (!string.IsNullOrEmpty(callerType) && callerType != "Variant")
                    {
                        var symbolInfo = ResolveMemberOnType(callerType, methodName);
                        if (symbolInfo != null)
                        {
                            CreateReference(symbolInfo, callExpression, GDReferenceConfidence.Strict, callerType);
                        }
                    }
                }
            }
        }

        RecordNodeType(callExpression);
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

    #region Resolution Helpers

    /// <summary>
    /// Resolves an inherited member or returns a cached instance if already resolved.
    /// This ensures we don't create duplicate symbols for the same inherited member.
    /// </summary>
    private GDSymbolInfo? ResolveOrCreateInheritedSymbol(string memberName)
    {
        // Check cache first
        if (_inheritedSymbolCache.TryGetValue(memberName, out var cached))
            return cached;

        // Resolve the inherited member
        var symbol = ResolveInheritedMember(memberName);
        if (symbol != null)
        {
            // Cache it and register with the model
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

        // GetMember already handles inheritance chain walking internally
        var memberInfo = _runtimeProvider.GetMember(baseTypeName, memberName);
        if (memberInfo != null)
        {
            // Find the actual declaring type for proper attribution
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

        // Find the declaring type (may differ from typeName if inherited)
        var declaringType = FindDeclaringTypeForMember(typeName, memberName) ?? typeName;

        return GDSymbolInfo.BuiltIn(memberInfo, declaringType);
    }

    private void CreateReference(GDSymbolInfo symbol, GDNode node, GDReferenceConfidence confidence, string? callerTypeName = null)
    {
        var reference = new GDReference
        {
            ReferenceNode = node,
            Scope = _currentGDScope,
            IsWrite = _inAssignmentLeft,
            IsRead = !_inAssignmentLeft,
            Confidence = confidence,
            ConfidenceReason = BuildConfidenceReason(symbol, confidence),
            CallerTypeName = callerTypeName
        };

        // Add type information if available (with recursion guard)
        if (_typeEngine != null && node is GDExpression expr && _recordingTypes.Add(expr))
        {
            try
            {
                var typeNode = _typeEngine.InferTypeNode(expr);
                if (typeNode != null)
                {
                    reference.InferredType = typeNode.BuildName();
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

        // Index by caller type for member access queries (built-in methods, global functions, etc.)
        if (!string.IsNullOrEmpty(callerTypeName))
        {
            _model.AddMemberAccess(callerTypeName, symbol.Name, reference);
        }
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

    // Guard against recursive calls during type inference
    private readonly HashSet<GDExpression> _recordingTypes = new();

    private void RecordNodeType(GDExpression expression)
    {
        if (_typeEngine == null)
            return;

        // Prevent infinite recursion
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
        // Pass scopes so that GDDuckTypeCollector can filter out typed variables.
        // Without scopes, ALL variables would be considered untyped and duck types
        // would be incorrectly collected for member access on typed variables.
        // Pass runtimeProvider to filter out universal Object members (has_method, get_class, etc.)
        // that exist on all objects and don't contribute to duck-type specificity.
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

        // Collect class-level Variant variables first
        var classCollector = new GDClassVariableCollector(_typeEngine);
        classCollector.Collect(classDecl);

        foreach (var kv in classCollector.Profiles)
        {
            _model!.SetVariableProfile(kv.Key, kv.Value);
        }

        // Collect local variables per method
        foreach (var member in classDecl.Members)
        {
            if (member is GDMethodDeclaration method)
            {
                // Collect variable usage profiles
                var varCollector = new GDVariableUsageCollector(context.Scopes, _typeEngine);
                varCollector.Collect(method);

                foreach (var kv in varCollector.Profiles)
                {
                    _model!.SetVariableProfile(kv.Key, kv.Value);
                }

                // Collect container usage profiles (local containers)
                var containerCollector = new GDContainerUsageCollector(context.Scopes, _typeEngine);
                containerCollector.Collect(method);

                foreach (var kv in containerCollector.Profiles)
                {
                    _model!.SetContainerProfile(kv.Key, kv.Value);
                }
            }
        }

        // Collect class-level container usage profiles
        // This tracks untyped class-level Array/Dictionary variables and infers element types from usage
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

        // Create collector with type inference callback
        var collector = new GDCallableCallSiteCollector(
            _scriptFile,
            expr => _typeEngine?.InferType(expr));

        collector.Collect(classDecl);

        // Register in semantic model
        var registry = _model!.GetOrCreateCallSiteRegistry();
        registry.RegisterCollector(_scriptFile.FullPath ?? "", collector);

        CollectCallableFlow(classDecl, registry);

        // Also connect registry to type engine for lambda parameter inference
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
        // Method resolver for looking up method declarations
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
            expr => _typeEngine?.InferType(expr),
            methodResolver);

        flowCollector.Collect(classDecl);

        // Register in registry
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
