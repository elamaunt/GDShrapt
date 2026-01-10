using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Helper for type inference with confidence tracking.
/// Wraps GDTypeInferenceEngine and adds confidence levels to results.
/// </summary>
public class GDTypeInferenceHelper
{
    private readonly GDScriptAnalyzer? _analyzer;
    private readonly GDTypeResolver? _typeResolver;
    private readonly IGDRuntimeProvider? _runtimeProvider;

    /// <summary>
    /// Creates a type inference helper with an analyzer.
    /// </summary>
    public GDTypeInferenceHelper(GDScriptAnalyzer? analyzer)
    {
        _analyzer = analyzer;
        _typeResolver = null;
        _runtimeProvider = null;
    }

    /// <summary>
    /// Creates a type inference helper with a type resolver.
    /// </summary>
    public GDTypeInferenceHelper(GDTypeResolver? typeResolver)
    {
        _analyzer = null;
        _typeResolver = typeResolver;
        _runtimeProvider = typeResolver?.RuntimeProvider;
    }

    /// <summary>
    /// Creates a type inference helper with both analyzer and type resolver.
    /// </summary>
    public GDTypeInferenceHelper(GDScriptAnalyzer? analyzer, GDTypeResolver? typeResolver)
    {
        _analyzer = analyzer;
        _typeResolver = typeResolver;
        _runtimeProvider = typeResolver?.RuntimeProvider;
    }

    /// <summary>
    /// Infers the type of an expression with confidence level.
    /// </summary>
    public GDInferredType InferExpressionType(GDExpression? expression)
    {
        if (expression == null)
            return GDInferredType.Unknown("Expression is null");

        // 1. Literals have certain types
        if (TryInferLiteralType(expression, out var literalType))
            return GDInferredType.Certain(literalType, "Literal value");

        // 2. Explicitly typed expressions (e.g., from parent variable with type annotation)
        if (TryGetExplicitType(expression, out var explicitType))
            return GDInferredType.Certain(explicitType, "Explicit type annotation");

        // 3. Constructor calls (.new()) have high confidence
        if (expression is GDCallExpression callExpr && IsConstructorCall(callExpr, out var constructedType))
            return GDInferredType.High(constructedType, $"Constructor call: {constructedType}.new()");

        // 4. Try analyzer inference
        var analyzerType = _analyzer?.GetTypeForNode(expression);
        if (!string.IsNullOrEmpty(analyzerType) && analyzerType != "Variant")
        {
            var confidence = DetermineConfidenceFromType(analyzerType);
            return GDInferredType.FromType(analyzerType, confidence, "From type analyzer");
        }

        // 5. Try type resolver
        if (_typeResolver != null)
        {
            var resolution = _typeResolver.ResolveExpressionType(expression);
            if (resolution.IsResolved && !string.IsNullOrEmpty(resolution.TypeName) && resolution.TypeName != "Variant")
            {
                var confidence = DetermineConfidenceFromSource(resolution.Source);
                return GDInferredType.FromType(resolution.TypeName, confidence, $"From {resolution.Source}");
            }
        }

        // 6. Call expressions with known return types
        if (expression is GDCallExpression call)
            return InferCallType(call);

        // 7. Member access
        if (expression is GDMemberOperatorExpression member)
            return InferMemberType(member);

        // 8. Identifier expressions
        if (expression is GDIdentifierExpression identifier)
            return InferIdentifierType(identifier);

        // 9. Node path expressions ($NodePath, %UniqueName)
        if (expression is GDGetNodeExpression or GDGetUniqueNodeExpression)
            return GDInferredType.Low("Node", "Node path expression - actual type depends on scene");

        // 10. Array/Dictionary initializers
        if (expression is GDArrayInitializerExpression)
            return InferArrayInitializerType(expression as GDArrayInitializerExpression);

        if (expression is GDDictionaryInitializerExpression)
            return GDInferredType.Medium("Dictionary", "Dictionary initializer");

        return GDInferredType.Unknown("Cannot determine expression type");
    }

    /// <summary>
    /// Infers type from a variable initializer expression.
    /// </summary>
    public GDInferredType InferInitializerType(GDExpression? initializer)
    {
        return InferExpressionType(initializer);
    }

    /// <summary>
    /// Infers type for a variable declaration.
    /// </summary>
    public GDInferredType InferVariableType(GDVariableDeclaration? varDecl)
    {
        if (varDecl == null)
            return GDInferredType.Unknown("Variable declaration is null");

        // Explicit type annotation
        var typeAnnotation = varDecl.Type?.BuildName();
        if (!string.IsNullOrEmpty(typeAnnotation) && typeAnnotation != "Variant")
            return GDInferredType.Certain(typeAnnotation, "Variable type annotation");

        // Infer from initializer
        if (varDecl.Initializer != null)
            return InferExpressionType(varDecl.Initializer);

        return GDInferredType.Unknown("Variable has no type annotation or initializer");
    }

    /// <summary>
    /// Infers type for a local variable statement.
    /// </summary>
    public GDInferredType InferVariableType(GDVariableDeclarationStatement? varStmt)
    {
        if (varStmt == null)
            return GDInferredType.Unknown("Variable statement is null");

        // Explicit type annotation
        var typeAnnotation = varStmt.Type?.BuildName();
        if (!string.IsNullOrEmpty(typeAnnotation) && typeAnnotation != "Variant")
            return GDInferredType.Certain(typeAnnotation, "Variable type annotation");

        // Infer from initializer
        if (varStmt.Initializer != null)
            return InferExpressionType(varStmt.Initializer);

        return GDInferredType.Unknown("Variable has no type annotation or initializer");
    }

    /// <summary>
    /// Infers type for a parameter declaration.
    /// </summary>
    public GDInferredType InferParameterType(GDParameterDeclaration? paramDecl)
    {
        if (paramDecl == null)
            return GDInferredType.Unknown("Parameter declaration is null");

        // Explicit type annotation
        var typeAnnotation = paramDecl.Type?.BuildName();
        if (!string.IsNullOrEmpty(typeAnnotation) && typeAnnotation != "Variant")
            return GDInferredType.Certain(typeAnnotation, "Parameter type annotation");

        // Infer from default value
        if (paramDecl.DefaultValue != null)
            return InferExpressionType(paramDecl.DefaultValue);

        return GDInferredType.Unknown("Parameter has no type annotation or default value");
    }

    private bool TryInferLiteralType(GDExpression expression, out string type)
    {
        type = expression switch
        {
            GDNumberExpression numExpr => InferNumberType(numExpr),
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            // Note: null is represented as GDIdentifierExpression with "null" identifier
            _ => null
        };

        return type != null;
    }

    private string InferNumberType(GDNumberExpression numExpr)
    {
        var num = numExpr.Number;
        if (num == null)
            return "int";

        var seq = num.Sequence;
        if (seq != null && (seq.Contains('.') || seq.Contains('e') || seq.Contains('E')))
            return "float";

        return "int";
    }

    private bool TryGetExplicitType(GDExpression expression, out string type)
    {
        type = null;

        // Check if this expression is in a variable with type annotation
        var parent = expression.Parent;

        if (parent is GDVariableDeclaration varDecl && varDecl.Initializer == expression)
        {
            type = varDecl.Type?.BuildName();
            return !string.IsNullOrEmpty(type) && type != "Variant";
        }

        if (parent is GDVariableDeclarationStatement varStmt && varStmt.Initializer == expression)
        {
            type = varStmt.Type?.BuildName();
            return !string.IsNullOrEmpty(type) && type != "Variant";
        }

        return false;
    }

    private bool IsConstructorCall(GDCallExpression callExpr, out string constructedType)
    {
        constructedType = null;

        // Check for pattern: ClassName.new()
        if (callExpr.CallerExpression is GDMemberOperatorExpression memberExpr)
        {
            var methodName = memberExpr.Identifier?.Sequence;
            if (methodName == "new")
            {
                // Get the class name from caller
                if (memberExpr.CallerExpression is GDIdentifierExpression identExpr)
                {
                    var className = identExpr.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(className))
                    {
                        // Verify it's a known type
                        if (_runtimeProvider?.IsKnownType(className) == true ||
                            _runtimeProvider?.GetGlobalClass(className) != null)
                        {
                            constructedType = className;
                            return true;
                        }
                        // Even if not known, .new() on an identifier likely constructs that type
                        constructedType = className;
                        return true;
                    }
                }
            }
        }

        // Check for built-in type constructors: Vector2(), Color(), etc.
        if (callExpr.CallerExpression is GDIdentifierExpression typeIdent)
        {
            var typeName = typeIdent.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(typeName) && _runtimeProvider?.IsKnownType(typeName) == true)
            {
                constructedType = typeName;
                return true;
            }
        }

        return false;
    }

    private GDInferredType InferCallType(GDCallExpression callExpr)
    {
        var caller = callExpr.CallerExpression;

        // Direct function call
        if (caller is GDIdentifierExpression identExpr)
        {
            var funcName = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(funcName))
            {
                // Type constructor
                if (_runtimeProvider?.IsKnownType(funcName) == true)
                    return GDInferredType.High(funcName, $"Type constructor: {funcName}()");

                // Global function with known return type
                var funcInfo = _runtimeProvider?.GetGlobalFunction(funcName);
                if (funcInfo != null && !string.IsNullOrEmpty(funcInfo.ReturnType))
                    return GDInferredType.High(funcInfo.ReturnType, $"Return type of {funcName}()");

                // Local method - try to find declaration
                var methodDecl = FindMethodDeclaration(funcName, callExpr);
                if (methodDecl?.ReturnType != null)
                {
                    var returnType = methodDecl.ReturnType.BuildName();
                    if (!string.IsNullOrEmpty(returnType))
                        return GDInferredType.High(returnType, $"Return type of local method {funcName}()");
                }

                return GDInferredType.Medium("Variant", $"Return type unknown for {funcName}()");
            }
        }

        // Method call on object
        if (caller is GDMemberOperatorExpression memberExpr)
        {
            var methodName = memberExpr.Identifier?.Sequence;

            // .new() constructor
            if (methodName == "new")
            {
                var callerType = InferExpressionType(memberExpr.CallerExpression);
                if (!callerType.IsUnknown)
                    return GDInferredType.High(callerType.TypeName, $"Constructor: {callerType.TypeName}.new()");
            }

            var callerTypeResult = InferExpressionType(memberExpr.CallerExpression);

            if (!callerTypeResult.IsUnknown && !string.IsNullOrEmpty(methodName))
            {
                var memberInfo = _runtimeProvider?.GetMember(callerTypeResult.TypeName, methodName);
                if (memberInfo != null && memberInfo.Kind == GDRuntimeMemberKind.Method)
                {
                    if (!string.IsNullOrEmpty(memberInfo.Type))
                        return GDInferredType.High(memberInfo.Type, $"Return type of {callerTypeResult.TypeName}.{methodName}()");
                }

                return GDInferredType.Medium("Variant", $"Return type unknown for {callerTypeResult.TypeName}.{methodName}()");
            }

            return GDInferredType.Unknown($"Caller type unknown for method {methodName}()");
        }

        return GDInferredType.Unknown("Cannot determine call return type");
    }

    private GDInferredType InferMemberType(GDMemberOperatorExpression memberExpr)
    {
        var memberName = memberExpr.Identifier?.Sequence;
        var callerTypeResult = InferExpressionType(memberExpr.CallerExpression);

        if (!callerTypeResult.IsUnknown && !string.IsNullOrEmpty(memberName))
        {
            var memberInfo = _runtimeProvider?.GetMember(callerTypeResult.TypeName, memberName);
            if (memberInfo != null && !string.IsNullOrEmpty(memberInfo.Type))
            {
                return GDInferredType.High(memberInfo.Type, $"Property type: {callerTypeResult.TypeName}.{memberName}");
            }

            // Member exists on type but type unknown
            return GDInferredType.Medium("Variant", $"Property type unknown for {callerTypeResult.TypeName}.{memberName}");
        }

        return GDInferredType.Unknown($"Cannot determine type for member {memberName}");
    }

    private GDInferredType InferIdentifierType(GDIdentifierExpression identExpr)
    {
        var name = identExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return GDInferredType.Unknown("Identifier is empty");

        // Built-in constants
        switch (name)
        {
            case "true":
            case "false":
                return GDInferredType.Certain("bool", "Boolean literal");
            case "null":
                return GDInferredType.Certain("null", "Null literal");
            case "PI":
            case "TAU":
            case "INF":
            case "NAN":
                return GDInferredType.Certain("float", "Math constant");
            case "self":
                return GDInferredType.High("self", "Self reference");
        }

        // Known type name (used as value)
        if (_runtimeProvider?.IsKnownType(name) == true)
            return GDInferredType.High(name, "Type used as value");

        // Global class
        var globalClass = _runtimeProvider?.GetGlobalClass(name);
        if (globalClass != null)
            return GDInferredType.High(name, "Global class");

        return GDInferredType.Unknown($"Cannot determine type for identifier '{name}'");
    }

    private GDInferredType InferArrayInitializerType(GDArrayInitializerExpression? arrayExpr)
    {
        if (arrayExpr?.Values == null || arrayExpr.Values.Count == 0)
            return GDInferredType.Medium("Array", "Empty array initializer");

        string? commonType = null;
        var allSameType = true;

        foreach (var entry in arrayExpr.Values)
        {
            var entryType = InferExpressionType(entry);
            if (entryType.IsUnknown)
            {
                allSameType = false;
                break;
            }

            if (commonType == null)
            {
                commonType = entryType.TypeName;
            }
            else if (commonType != entryType.TypeName)
            {
                allSameType = false;
                break;
            }
        }

        if (allSameType && !string.IsNullOrEmpty(commonType))
            return GDInferredType.Medium($"Array[{commonType}]", $"Array with homogeneous {commonType} elements");

        return GDInferredType.Medium("Array", "Array with mixed or unknown element types");
    }

    private GDMethodDeclaration? FindMethodDeclaration(string methodName, GDNode context)
    {
        var classDecl = context.RootClassDeclaration;
        if (classDecl?.Members == null)
            return null;

        foreach (var member in classDecl.Members)
        {
            if (member is GDMethodDeclaration methodDecl &&
                methodDecl.Identifier?.Sequence == methodName)
            {
                return methodDecl;
            }
        }

        return null;
    }

    private GDTypeConfidence DetermineConfidenceFromType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName) || typeName == "Variant")
            return GDTypeConfidence.Unknown;

        // Primitive types have high confidence
        if (typeName is "int" or "float" or "bool" or "String" or "void")
            return GDTypeConfidence.High;

        // Godot types have high confidence
        if (_runtimeProvider?.IsKnownType(typeName) == true)
            return GDTypeConfidence.High;

        return GDTypeConfidence.Medium;
    }

    private GDTypeConfidence DetermineConfidenceFromSource(GDTypeSource source)
    {
        return source switch
        {
            GDTypeSource.GodotApi => GDTypeConfidence.High,
            GDTypeSource.Project => GDTypeConfidence.High,
            GDTypeSource.Scene => GDTypeConfidence.High,
            GDTypeSource.BuiltIn => GDTypeConfidence.Certain,
            GDTypeSource.Inferred => GDTypeConfidence.Medium,
            _ => GDTypeConfidence.Medium
        };
    }
}
