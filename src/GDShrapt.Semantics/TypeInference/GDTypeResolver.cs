using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Central type resolver that combines multiple type providers for comprehensive type inference.
/// Uses GDShrapt.Reader's type inference engine with custom runtime providers.
/// </summary>
public class GDTypeResolver
{
    private readonly GDGodotTypesProvider _godotTypesProvider;
    private readonly GDProjectTypesProvider? _projectTypesProvider;
    private readonly GDAutoloadsProvider? _autoloadsProvider;
    private readonly GDSceneTypesProvider? _sceneTypesProvider;
    private readonly IGDScriptProvider? _scriptProvider;
    private readonly GDCompositeRuntimeProvider _compositeProvider;
    private readonly GDNodeTypeInjector? _nodeTypeInjector;
    private readonly IGDLogger _logger;

    public GDTypeResolver(
        GDGodotTypesProvider? godotTypesProvider = null,
        GDProjectTypesProvider? projectTypesProvider = null,
        GDAutoloadsProvider? autoloadsProvider = null,
        GDSceneTypesProvider? sceneTypesProvider = null,
        IGDScriptProvider? scriptProvider = null,
        IGDLogger? logger = null)
    {
        _godotTypesProvider = godotTypesProvider ?? new GDGodotTypesProvider();
        _projectTypesProvider = projectTypesProvider;
        _autoloadsProvider = autoloadsProvider;
        _sceneTypesProvider = sceneTypesProvider;
        _scriptProvider = scriptProvider;
        _logger = logger ?? GDNullLogger.Instance;

        _compositeProvider = new GDCompositeRuntimeProvider(
            _godotTypesProvider,
            _projectTypesProvider,
            _autoloadsProvider,
            _sceneTypesProvider);

        // Create node type injector for $NodePath, get_node(), preload(), and signal type inference
        if (_sceneTypesProvider != null || _scriptProvider != null || _godotTypesProvider != null)
        {
            _nodeTypeInjector = new GDNodeTypeInjector(
                _sceneTypesProvider,
                _scriptProvider,
                _godotTypesProvider,
                _logger);
        }
    }

    /// <summary>
    /// Gets the runtime provider for use with GDShrapt.Validator.
    /// </summary>
    public IGDRuntimeProvider RuntimeProvider => _compositeProvider;

    /// <summary>
    /// Gets the Godot types provider. Internal - external code should use RuntimeProvider.
    /// </summary>
    internal GDGodotTypesProvider GodotTypesProvider => _godotTypesProvider;

    /// <summary>
    /// Resolves the type of an expression within a given context.
    /// </summary>
    public GDTypeResolutionResult ResolveExpressionType(GDExpression expression, IGDScriptInfo? scriptInfo = null)
    {
        if (expression == null)
            return GDTypeResolutionResult.Unknown();

        try
        {
            // Build scope stack from script context
            var scopeStack = BuildScopeStack(expression, scriptInfo);

            // Create injection context with script path for scene resolution
            var injectionContext = new GDTypeInjectionContext
            {
                ScriptPath = scriptInfo?.FullPath
            };

            // Use the validator's type inference engine with node type injector
            var engine = _nodeTypeInjector != null
                ? new GDTypeInferenceEngine(_compositeProvider, scopeStack, _nodeTypeInjector, injectionContext)
                : new GDTypeInferenceEngine(_compositeProvider, scopeStack);

            var semanticType = engine.InferSemanticType(expression);
            var typeNode = engine.InferTypeNode(expression);

            if (semanticType == null || semanticType.IsVariant)
                return GDTypeResolutionResult.Unknown();

            return new GDTypeResolutionResult
            {
                TypeName = semanticType,
                TypeNode = typeNode,
                IsResolved = true,
                Source = DetermineTypeSource(semanticType.DisplayName)
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Type resolution failed: {ex.Message}");
            return GDTypeResolutionResult.Unknown();
        }
    }

    /// <summary>
    /// Resolves the type of an identifier within a given context.
    /// </summary>
    public GDTypeResolutionResult ResolveIdentifierType(GDIdentifier identifier, IGDScriptInfo? scriptInfo = null)
    {
        if (identifier == null)
            return GDTypeResolutionResult.Unknown();

        var name = identifier.Sequence;
        if (string.IsNullOrEmpty(name))
            return GDTypeResolutionResult.Unknown();

        // Check built-ins first
        if (_compositeProvider.IsBuiltIn(name))
        {
            return new GDTypeResolutionResult
            {
                TypeName = GDSemanticType.FromRuntimeTypeName(GetBuiltInType(name)),
                IsResolved = true,
                Source = GDTypeSource.BuiltIn
            };
        }

        // Check if it's a known type name
        if (_compositeProvider.IsKnownType(name))
        {
            return new GDTypeResolutionResult
            {
                TypeName = GDSemanticType.FromRuntimeTypeName(name),
                IsResolved = true,
                Source = GDTypeSource.GodotApi
            };
        }

        // Check project types
        if (_projectTypesProvider != null)
        {
            var projectType = _projectTypesProvider.GetTypeInfo(name);
            if (projectType != null)
            {
                return new GDTypeResolutionResult
                {
                    TypeName = GDSemanticType.FromRuntimeTypeName(name),
                    IsResolved = true,
                    Source = GDTypeSource.Project
                };
            }
        }

        // Try to resolve from expression context
        var parent = identifier.Parent;
        if (parent is GDIdentifierExpression identifierExpr)
        {
            return ResolveExpressionType(identifierExpr, scriptInfo);
        }

        return GDTypeResolutionResult.Unknown();
    }

    /// <summary>
    /// Gets member information for a type.
    /// </summary>
    public GDRuntimeMemberInfo? GetMember(string typeName, string memberName)
    {
        return _compositeProvider.GetMember(typeName, memberName);
    }

    /// <summary>
    /// Gets type information.
    /// </summary>
    public GDRuntimeTypeInfo? GetTypeInfo(string typeName)
    {
        return _compositeProvider.GetTypeInfo(typeName);
    }

    /// <summary>
    /// Checks if two types are compatible for assignment.
    /// </summary>
    public bool AreTypesCompatible(string sourceType, string targetType)
    {
        return _compositeProvider.IsAssignableTo(sourceType, targetType);
    }

    /// <summary>
    /// Gets the base type of a type.
    /// </summary>
    public string? GetBaseType(string typeName)
    {
        return _compositeProvider.GetBaseType(typeName);
    }

    /// <summary>
    /// Gets the full inheritance chain for a type.
    /// </summary>
    public IReadOnlyList<string> GetInheritanceChain(string typeName)
    {
        var chain = new List<string>();
        var visited = new HashSet<string>();
        var current = typeName;

        while (!string.IsNullOrEmpty(current))
        {
            // Prevent infinite loop on cyclic inheritance
            if (!visited.Add(current))
                break;

            chain.Add(current);
            current = GetBaseType(current);
        }

        return chain;
    }

    private GDScopeStack BuildScopeStack(GDExpression expression, IGDScriptInfo? scriptInfo)
    {
        var scopeStack = new GDScopeStack();

        // Add global scope
        scopeStack.Push(GDScopeType.Global);

        // Add class scope if we have script context
        if (scriptInfo?.Class != null)
        {
            scopeStack.Push(GDScopeType.Class, scriptInfo.Class);

            // Add class members to scope
            foreach (var member in scriptInfo.Class.Members)
            {
                if (member is GDIdentifiableClassMember identifiable && identifiable.Identifier != null)
                {
                    var symbol = CreateSymbolFromMember(member);
                    if (symbol != null)
                    {
                        scopeStack.TryDeclare(symbol);
                    }
                }
            }
        }

        // Add method scope if expression is inside a method
        var method = FindParentOfType<GDMethodDeclaration>(expression);
        if (method != null)
        {
            scopeStack.Push(GDScopeType.Method, method);

            // Add parameters
            if (method.Parameters != null)
            {
                foreach (var param in method.Parameters)
                {
                    if (param.Identifier != null)
                    {
                        var typeName = param.Type?.BuildName() ?? "Variant";
                        var symbol = GDSymbol.Parameter(param.Identifier.Sequence, param, typeName: typeName);
                        scopeStack.TryDeclare(symbol);
                    }
                }
            }

            // Add local variables declared before this expression
            AddLocalVariablesToScope(expression, method.Statements, scopeStack);
        }

        return scopeStack;
    }

    private void AddLocalVariablesToScope(GDExpression expression, GDStatementsList? statements, GDScopeStack scopeStack)
    {
        if (statements == null)
            return;

        var exprLine = expression.StartLine;

        foreach (var statement in statements)
        {
            // Only include variables declared before the expression
            if (statement.StartLine >= exprLine)
                break;

            if (statement is GDVariableDeclarationStatement varDecl && varDecl.Identifier != null)
            {
                var typeName = varDecl.Type?.BuildName() ?? InferInitializerType(varDecl.Initializer);
                var symbol = GDSymbol.Variable(varDecl.Identifier.Sequence, varDecl, typeName: typeName);
                scopeStack.TryDeclare(symbol);
            }
        }
    }

    private string InferInitializerType(GDExpression? initializer)
    {
        if (initializer == null)
            return "Variant";

        // Quick type inference for common initializer patterns
        return initializer switch
        {
            GDNumberExpression num => num.Number?.ResolveNumberType() switch
            {
                GDNumberType.LongDecimal or GDNumberType.LongBinary or GDNumberType.LongHexadecimal => "int",
                GDNumberType.Double => "float",
                _ => "int"
            },
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            GDArrayInitializerExpression => "Array",
            GDDictionaryInitializerExpression => "Dictionary",
            _ => "Variant"
        };
    }

    private GDSymbol? CreateSymbolFromMember(GDClassMember member)
    {
        return member switch
        {
            GDMethodDeclaration method when method.Identifier != null =>
                GDSymbol.Method(method.Identifier.Sequence, method, method.IsStatic),

            GDVariableDeclaration variable when variable.Identifier != null =>
                variable.ConstKeyword != null
                    ? GDSymbol.Constant(variable.Identifier.Sequence, variable, typeName: variable.Type?.BuildName() ?? InferInitializerType(variable.Initializer))
                    : GDSymbol.Variable(variable.Identifier.Sequence, variable, typeName: variable.Type?.BuildName() ?? InferInitializerType(variable.Initializer), isStatic: variable.StaticKeyword != null),

            GDSignalDeclaration signal when signal.Identifier != null =>
                GDSymbol.Signal(signal.Identifier.Sequence, signal),

            GDEnumDeclaration enumDecl when enumDecl.Identifier != null =>
                GDSymbol.Enum(enumDecl.Identifier.Sequence, enumDecl),

            _ => null
        };
    }

    private string GetBuiltInType(string name)
    {
        if (GDWellKnownTypes.BuiltinIdentifierTypes.TryGetValue(name, out var type))
            return type == GDWellKnownTypes.Null ? GDWellKnownTypes.Variant : type;

        return name switch
        {
            GDWellKnownTypes.Self => GDWellKnownTypes.Self,
            "super" => "super",
            _ => GDWellKnownTypes.Variant
        };
    }

    private GDTypeSource DetermineTypeSource(string typeName)
    {
        if (_godotTypesProvider.IsKnownType(typeName))
            return GDTypeSource.GodotApi;

        if (_projectTypesProvider?.IsKnownType(typeName) == true)
            return GDTypeSource.Project;

        if (_sceneTypesProvider?.IsKnownType(typeName) == true)
            return GDTypeSource.Scene;

        return GDTypeSource.Inferred;
    }

    /// <summary>
    /// Finds a parent node of the specified type in the AST hierarchy.
    /// </summary>
    private static T? FindParentOfType<T>(GDNode node) where T : GDNode
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is T result)
                return result;
            current = current.Parent;
        }
        return null;
    }
}
