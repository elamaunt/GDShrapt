using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Central type resolver that combines multiple type providers for comprehensive type inference.
/// Uses GDShrapt.Reader's type inference engine with custom runtime providers.
/// </summary>
internal class TypeResolver
{
    private readonly GodotTypesProvider _godotTypesProvider;
    private readonly ProjectTypesProvider? _projectTypesProvider;
    private readonly GDSceneTypesProvider? _sceneTypesProvider;
    private readonly CompositeRuntimeProvider _compositeProvider;

    public TypeResolver(
        GodotTypesProvider? godotTypesProvider = null,
        ProjectTypesProvider? projectTypesProvider = null,
        GDSceneTypesProvider? sceneTypesProvider = null)
    {
        _godotTypesProvider = godotTypesProvider ?? new GodotTypesProvider();
        _projectTypesProvider = projectTypesProvider;
        _sceneTypesProvider = sceneTypesProvider;

        _compositeProvider = new CompositeRuntimeProvider(
            _godotTypesProvider,
            _projectTypesProvider,
            _sceneTypesProvider);
    }

    /// <summary>
    /// Gets the runtime provider for use with GDShrapt.Validator.
    /// </summary>
    public IGDRuntimeProvider RuntimeProvider => _compositeProvider;

    /// <summary>
    /// Resolves the type of an expression within a given context.
    /// </summary>
    public TypeResolutionResult ResolveExpressionType(GDExpression expression, GDScriptMap? scriptMap = null)
    {
        if (expression == null)
            return TypeResolutionResult.Unknown();

        try
        {
            // Build scope stack from script context
            var scopeStack = BuildScopeStack(expression, scriptMap);

            // Use the validator's type inference engine
            var engine = new GDTypeInferenceEngine(_compositeProvider, scopeStack);
            var typeName = engine.InferType(expression);
            var typeNode = engine.InferTypeNode(expression);

            if (string.IsNullOrEmpty(typeName))
                return TypeResolutionResult.Unknown();

            return new TypeResolutionResult
            {
                TypeName = typeName,
                TypeNode = typeNode,
                IsResolved = true,
                Source = DetermineTypeSource(typeName)
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"Type resolution failed: {ex.Message}");
            return TypeResolutionResult.Unknown();
        }
    }

    /// <summary>
    /// Resolves the type of an identifier within a given context.
    /// </summary>
    public TypeResolutionResult ResolveIdentifierType(GDIdentifier identifier, GDScriptMap? scriptMap = null)
    {
        if (identifier == null)
            return TypeResolutionResult.Unknown();

        var name = identifier.Sequence;
        if (string.IsNullOrEmpty(name))
            return TypeResolutionResult.Unknown();

        // Check built-ins first
        if (_compositeProvider.IsBuiltIn(name))
        {
            return new TypeResolutionResult
            {
                TypeName = GetBuiltInType(name),
                IsResolved = true,
                Source = TypeSource.BuiltIn
            };
        }

        // Check if it's a known type name
        if (_compositeProvider.IsKnownType(name))
        {
            return new TypeResolutionResult
            {
                TypeName = name,
                IsResolved = true,
                Source = TypeSource.GodotApi
            };
        }

        // Check project types
        if (_projectTypesProvider != null)
        {
            var projectType = _projectTypesProvider.GetTypeInfo(name);
            if (projectType != null)
            {
                return new TypeResolutionResult
                {
                    TypeName = name,
                    IsResolved = true,
                    Source = TypeSource.Project
                };
            }
        }

        // Try to resolve from expression context
        var parent = identifier.Parent;
        if (parent is GDIdentifierExpression identifierExpr)
        {
            return ResolveExpressionType(identifierExpr, scriptMap);
        }

        return TypeResolutionResult.Unknown();
    }

    /// <summary>
    /// Gets member information for a type.
    /// </summary>
    public GDRuntimeMemberInfo GetMember(string typeName, string memberName)
    {
        return _compositeProvider.GetMember(typeName, memberName);
    }

    /// <summary>
    /// Gets type information.
    /// </summary>
    public GDRuntimeTypeInfo GetTypeInfo(string typeName)
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
    public string GetBaseType(string typeName)
    {
        return _compositeProvider.GetBaseType(typeName);
    }

    /// <summary>
    /// Gets the full inheritance chain for a type.
    /// </summary>
    public IReadOnlyList<string> GetInheritanceChain(string typeName)
    {
        var chain = new List<string>();
        var current = typeName;

        while (!string.IsNullOrEmpty(current))
        {
            chain.Add(current);
            current = GetBaseType(current);
        }

        return chain;
    }

    private GDScopeStack BuildScopeStack(GDExpression expression, GDScriptMap? scriptMap)
    {
        var scopeStack = new GDScopeStack();

        // Add global scope
        scopeStack.Push(GDScopeType.Global);

        // Add class scope if we have script context
        if (scriptMap?.Class != null)
        {
            scopeStack.Push(GDScopeType.Class, scriptMap.Class);

            // Add class members to scope
            foreach (var member in scriptMap.Class.Members)
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
        var method = expression.GetParentOfType<GDMethodDeclaration>();
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
        return name switch
        {
            "true" or "false" => "bool",
            "null" => "Variant",
            "PI" or "TAU" or "INF" or "NAN" => "float",
            "self" => "self",
            "super" => "super",
            _ => "Variant"
        };
    }

    private TypeSource DetermineTypeSource(string typeName)
    {
        if (_godotTypesProvider.IsKnownType(typeName))
            return TypeSource.GodotApi;

        if (_projectTypesProvider?.IsKnownType(typeName) == true)
            return TypeSource.Project;

        if (_sceneTypesProvider?.IsKnownType(typeName) == true)
            return TypeSource.Scene;

        return TypeSource.Inferred;
    }
}

/// <summary>
/// Result of type resolution.
/// </summary>
internal class TypeResolutionResult
{
    public string TypeName { get; init; } = "Variant";
    public GDTypeNode? TypeNode { get; init; }
    public bool IsResolved { get; init; }
    public TypeSource Source { get; init; } = TypeSource.Unknown;

    public static TypeResolutionResult Unknown() => new()
    {
        TypeName = "Variant",
        IsResolved = false,
        Source = TypeSource.Unknown
    };
}

/// <summary>
/// Source of type information.
/// </summary>
internal enum TypeSource
{
    Unknown,
    BuiltIn,
    GodotApi,
    Project,
    Scene,
    Inferred
}

/// <summary>
/// Composite runtime provider that combines multiple type sources.
/// </summary>
internal class CompositeRuntimeProvider : IGDRuntimeProvider
{
    private readonly IGDRuntimeProvider[] _providers;

    public CompositeRuntimeProvider(params IGDRuntimeProvider[] providers)
    {
        _providers = providers.Where(p => p != null).ToArray();
    }

    public bool IsKnownType(string typeName)
    {
        foreach (var provider in _providers)
        {
            if (provider.IsKnownType(typeName))
                return true;
        }
        return false;
    }

    public GDRuntimeTypeInfo GetTypeInfo(string typeName)
    {
        foreach (var provider in _providers)
        {
            var info = provider.GetTypeInfo(typeName);
            if (info != null)
                return info;
        }
        return null;
    }

    public GDRuntimeMemberInfo GetMember(string typeName, string memberName)
    {
        foreach (var provider in _providers)
        {
            var member = provider.GetMember(typeName, memberName);
            if (member != null)
                return member;
        }
        return null;
    }

    public string GetBaseType(string typeName)
    {
        foreach (var provider in _providers)
        {
            var baseType = provider.GetBaseType(typeName);
            if (!string.IsNullOrEmpty(baseType))
                return baseType;
        }
        return null;
    }

    public bool IsAssignableTo(string sourceType, string targetType)
    {
        foreach (var provider in _providers)
        {
            if (provider.IsAssignableTo(sourceType, targetType))
                return true;
        }
        return false;
    }

    public GDRuntimeFunctionInfo GetGlobalFunction(string name)
    {
        foreach (var provider in _providers)
        {
            var func = provider.GetGlobalFunction(name);
            if (func != null)
                return func;
        }
        return null;
    }

    public GDRuntimeTypeInfo GetGlobalClass(string className)
    {
        foreach (var provider in _providers)
        {
            var cls = provider.GetGlobalClass(className);
            if (cls != null)
                return cls;
        }
        return null;
    }

    public bool IsBuiltIn(string identifier)
    {
        foreach (var provider in _providers)
        {
            if (provider.IsBuiltIn(identifier))
                return true;
        }
        return false;
    }
}
