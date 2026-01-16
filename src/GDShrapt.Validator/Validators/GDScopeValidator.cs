using GDShrapt.Abstractions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Tracks declarations, detects undefined identifiers and duplicates.
    /// Manages scope stack for classes, methods, loops, etc.
    /// Note: Class-level declarations are collected by GDDeclarationCollector before this runs.
    /// This validator only handles local scopes and identifier validation.
    /// </summary>
    public class GDScopeValidator : GDValidationVisitor
    {
        public GDScopeValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            if (node == null)
                return;

            // Class-level symbols are already collected by GDDeclarationCollector
            // We just need to enter global scope and validate
            EnterScope(GDScopeType.Global, node);
            node.WalkIn(this);
            ExitScope();
        }

        #region Classes

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            // Global scope already has all class-level symbols from GDDeclarationCollector
        }

        public override void Left(GDClassDeclaration classDeclaration)
        {
            // Nothing to do
        }

        // Validate extends clause
        public override void Visit(GDExtendsAttribute extendsAttribute)
        {
            var typeNode = extendsAttribute.Type;
            if (typeNode == null)
                return;

            // Handle string path extends: extends "res://path/script.gd"
            if (typeNode is GDStringTypeNode stringType)
            {
                ValidateExtendsPath(stringType, extendsAttribute);
                return;
            }

            var typeName = typeNode.BuildName();
            if (string.IsNullOrEmpty(typeName))
                return;

            // Check if base type is known (either built-in type, inner class, or from RuntimeProvider)
            if (!Context.RuntimeProvider.IsKnownType(typeName) &&
                !Context.RuntimeProvider.IsBuiltIn(typeName) &&
                LookupSymbol(typeName) == null)
            {
                ReportWarning(
                    GDDiagnosticCode.UnknownBaseType,
                    $"Unknown base type: '{typeName}'",
                    extendsAttribute);
            }
        }

        /// <summary>
        /// Validates an extends clause with a string path (e.g., extends "res://base.gd").
        /// </summary>
        private void ValidateExtendsPath(GDStringTypeNode stringType, GDExtendsAttribute extendsAttribute)
        {
            var path = stringType.Path?.Sequence;
            if (string.IsNullOrEmpty(path))
                return;

            // Check through project runtime provider if available
            if (Context.RuntimeProvider is IGDProjectRuntimeProvider projectProvider)
            {
                // First check if we can get script type info
                var scriptInfo = projectProvider.GetScriptType(path);
                if (scriptInfo != null)
                    return; // Script exists and is known

                // Fallback: check if resource exists
                if (!projectProvider.ResourceExists(path))
                {
                    ReportWarning(
                        GDDiagnosticCode.UnknownBaseType,
                        $"Base script not found: '{path}'",
                        extendsAttribute);
                }
            }
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            // Inner class is already registered by GDDeclarationCollector
            // Don't enter inner class scope - inner classes are separate units
        }

        public override void Left(GDInnerClassDeclaration innerClass)
        {
            // Nothing to do
        }

        #endregion

        #region Methods

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            // Enter method scope and register parameters
            EnterScope(GDScopeType.Method, methodDeclaration);

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
                        TryDeclareSymbol(GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: typeNode));

                        // Validate parameter type annotation
                        ValidateTypeAnnotation(typeNode, param);
                    }
                }
            }

            // Validate return type annotation
            var returnType = methodDeclaration.ReturnType;
            if (returnType != null)
            {
                ValidateTypeAnnotation(returnType, methodDeclaration);
            }
        }

        public override void Left(GDMethodDeclaration methodDeclaration)
        {
            ExitScope();
        }

        #endregion

        #region Variables (class-level are already collected, we validate type annotations)

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            // Class-level variables are already collected by GDDeclarationCollector
            // But we still need to validate type annotations
            var typeNode = variableDeclaration.Type;
            if (typeNode != null)
            {
                ValidateTypeAnnotation(typeNode, variableDeclaration);
            }
        }

        public override void Visit(GDSignalDeclaration signalDeclaration)
        {
            // Already collected by GDDeclarationCollector
        }

        public override void Visit(GDEnumDeclaration enumDeclaration)
        {
            // Already collected by GDDeclarationCollector
        }

        #endregion

        #region Local declarations

        // Local variables (inside methods)
        public override void Visit(GDVariableDeclarationStatement variableDeclaration)
        {
            var varName = variableDeclaration.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var typeNode = variableDeclaration.Type;
                var typeName = typeNode?.BuildName();
                TryDeclareSymbol(GDSymbol.Variable(varName, variableDeclaration, typeName: typeName, typeNode: typeNode));

                // Validate type annotation
                ValidateTypeAnnotation(typeNode, variableDeclaration);
            }
        }

        // For loop - creates scope, declares iterator
        public override void Visit(GDForStatement forStatement)
        {
            EnterScope(GDScopeType.ForLoop, forStatement);

            var iteratorName = forStatement.Variable?.Sequence;
            if (!string.IsNullOrEmpty(iteratorName))
            {
                Context.Declare(GDSymbol.Iterator(iteratorName, forStatement));
            }
        }

        public override void Left(GDForStatement forStatement)
        {
            ExitScope();
        }

        // While loop
        public override void Visit(GDWhileStatement whileStatement)
        {
            EnterScope(GDScopeType.WhileLoop, whileStatement);
        }

        public override void Left(GDWhileStatement whileStatement)
        {
            ExitScope();
        }

        // Conditionals
        public override void Visit(GDIfStatement ifStatement)
        {
            EnterScope(GDScopeType.Conditional, ifStatement);
        }

        public override void Left(GDIfStatement ifStatement)
        {
            ExitScope();
        }

        // Match
        public override void Visit(GDMatchStatement matchStatement)
        {
            EnterScope(GDScopeType.Match, matchStatement);
        }

        public override void Left(GDMatchStatement matchStatement)
        {
            ExitScope();
        }

        // Match case variable binding (var x in pattern)
        public override void Visit(GDMatchCaseVariableExpression matchCaseVariable)
        {
            var varName = matchCaseVariable.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                TryDeclareSymbol(GDSymbol.Variable(varName, matchCaseVariable));
            }
        }

        // Lambdas
        public override void Visit(GDMethodExpression methodExpression)
        {
            EnterScope(GDScopeType.Lambda, methodExpression);

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
                        TryDeclareSymbol(GDSymbol.Parameter(paramName, param, typeName: typeName, typeNode: typeNode));

                        // Validate lambda parameter type annotation
                        ValidateTypeAnnotation(typeNode, param);
                    }
                }
            }

            // Validate lambda return type annotation
            var returnType = methodExpression.ReturnType;
            if (returnType != null)
            {
                ValidateTypeAnnotation(returnType, methodExpression);
            }
        }

        public override void Left(GDMethodExpression methodExpression)
        {
            ExitScope();
        }

        #endregion

        #region Identifier validation

        // Identifier usage - check if declared
        public override void Visit(GDIdentifierExpression identifierExpression)
        {
            var name = identifierExpression.Identifier?.Sequence;
            if (string.IsNullOrEmpty(name))
                return;

            // Skip built-in identifiers
            if (Context.RuntimeProvider.IsBuiltIn(name))
                return;

            // Check if it's a known type (used as value, e.g., Vector2)
            if (Context.RuntimeProvider.IsKnownType(name))
                return;

            // Check scope (includes class-level symbols collected by GDDeclarationCollector)
            var symbol = LookupSymbol(name);
            if (symbol != null)
                return;

            // Check if it's a user-defined function (forward reference support)
            if (Context.IsFunctionDeclared(name))
                return;

            // Check if it's a global class/singleton
            if (Context.RuntimeProvider.GetGlobalClass(name) != null)
                return;

            ReportError(
                GDDiagnosticCode.UndefinedVariable,
                $"Undefined identifier: '{name}'",
                identifierExpression);
        }

        // Check for constant reassignment
        public override void Visit(GDDualOperatorExpression dualOperator)
        {
            var opType = dualOperator.Operator?.OperatorType;
            if (opType == null)
                return;

            // Check all assignment operators
            if (IsAssignmentOperator(opType.Value))
            {
                CheckConstantReassignment(dualOperator.LeftExpression, dualOperator);
            }
        }

        #endregion

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

        private void CheckConstantReassignment(GDExpression leftExpr, GDNode reportOn)
        {
            if (leftExpr is GDIdentifierExpression identExpr)
            {
                var name = identExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(name))
                {
                    var symbol = LookupSymbol(name);
                    if (symbol != null && symbol.IsConst)
                    {
                        ReportError(
                            GDDiagnosticCode.ConstantReassignment,
                            $"Cannot assign to constant '{name}'",
                            reportOn);
                    }
                }
            }
        }

        #region Type Annotation Validation

        // GDScript built-in primitive type names
        private static readonly System.Collections.Generic.HashSet<string> _primitiveTypes = new System.Collections.Generic.HashSet<string>
        {
            "void", "bool", "int", "float", "String",
            "Array", "Dictionary", "Callable", "Signal", "Variant"
        };

        /// <summary>
        /// Validates that a type annotation refers to a known type.
        /// </summary>
        private void ValidateTypeAnnotation(GDTypeNode typeNode, GDNode reportOn)
        {
            if (typeNode == null)
                return;

            var typeName = typeNode.BuildName();
            if (string.IsNullOrEmpty(typeName))
                return;

            // Skip primitive types (always valid)
            if (_primitiveTypes.Contains(typeName))
                return;

            // Skip generic array/dictionary syntax (Array[T], Dictionary[K,V])
            if (typeNode.IsArray || typeNode.IsDictionary)
                return;

            // Check if type is known:
            // 1. Built-in runtime types (Vector2, Node, etc.)
            // 2. Types registered via RuntimeProvider (project types)
            // 3. Inner classes declared in current scope
            if (!Context.RuntimeProvider.IsKnownType(typeName) &&
                !Context.RuntimeProvider.IsBuiltIn(typeName) &&
                LookupSymbol(typeName) == null)
            {
                ReportWarning(
                    GDDiagnosticCode.UnknownType,
                    $"Unknown type: '{typeName}'",
                    reportOn);
            }
        }

        #endregion

    }
}
