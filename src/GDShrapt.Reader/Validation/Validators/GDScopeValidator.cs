namespace GDShrapt.Reader
{
    /// <summary>
    /// Tracks declarations, detects undefined identifiers and duplicates.
    /// Manages scope stack for classes, methods, loops, etc.
    /// Uses two-pass approach: first collects class-level declarations,
    /// then validates identifier usage (supports forward references).
    /// </summary>
    public class GDScopeValidator : GDValidationVisitor
    {
        private bool _isCollectionPass;

        public GDScopeValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            if (node == null)
                return;

            // Pass 1: Collect all class-level declarations (methods, variables, signals, enums)
            // This enables forward references within a class
            _isCollectionPass = true;
            EnterScope(GDScopeType.Global, node);
            node.WalkIn(this);
            // Don't exit Global scope - we need symbols for pass 2

            // Reset to just Global scope (keeping all collected class-level symbols)
            Context.Scopes.ResetToGlobal();

            // Pass 2: Validate identifier usage
            _isCollectionPass = false;
            node.WalkIn(this);
            ExitScope();
        }

        #region Classes

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            // In first pass, we stay in Global scope to collect all class-level symbols
            // In second pass, Global scope already has all symbols collected
        }

        public override void Left(GDClassDeclaration classDeclaration)
        {
            // Nothing to do - symbols stay in Global scope
        }

        // Validate extends clause
        public override void Visit(GDExtendsAttribute extendsAttribute)
        {
            if (_isCollectionPass)
                return;

            var typeNode = extendsAttribute.Type;
            if (typeNode == null)
                return;

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

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            if (_isCollectionPass)
            {
                // Register inner class name in current scope
                var className = innerClass.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(className))
                {
                    TryDeclareSymbol(GDSymbol.Class(className, innerClass));
                }
            }
            // Don't enter inner class scope - inner classes are separate units
            // that would need their own validation pass
        }

        public override void Left(GDInnerClassDeclaration innerClass)
        {
            // Nothing to do
        }

        #endregion

        #region Class-level declarations (collected in first pass)

        // Methods - declared at class level, supports forward references
        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            if (_isCollectionPass)
            {
                // First pass: register method declaration
                var methodName = methodDeclaration.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(methodName))
                {
                    TryDeclareSymbol(GDSymbol.Method(methodName, methodDeclaration, methodDeclaration.IsStatic));
                }
            }
            else
            {
                // Second pass: enter method scope and register parameters
                EnterScope(GDScopeType.Method, methodDeclaration);

                var parameters = methodDeclaration.Parameters;
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        var paramName = param.Identifier?.Sequence;
                        if (!string.IsNullOrEmpty(paramName))
                        {
                            TryDeclareSymbol(GDSymbol.Parameter(paramName, param));
                        }
                    }
                }
            }
        }

        public override void Left(GDMethodDeclaration methodDeclaration)
        {
            if (!_isCollectionPass)
            {
                ExitScope();
            }
        }

        // Variables (class-level) - declared at class level, supports forward references
        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            if (_isCollectionPass)
            {
                var varName = variableDeclaration.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(varName))
                {
                    if (variableDeclaration.ConstKeyword != null)
                    {
                        TryDeclareSymbol(GDSymbol.Constant(varName, variableDeclaration));
                    }
                    else
                    {
                        TryDeclareSymbol(GDSymbol.Variable(varName, variableDeclaration, isStatic: variableDeclaration.IsStatic));
                    }
                }
            }
        }

        // Signals - declared at class level
        public override void Visit(GDSignalDeclaration signalDeclaration)
        {
            if (_isCollectionPass)
            {
                var signalName = signalDeclaration.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(signalName))
                {
                    TryDeclareSymbol(GDSymbol.Signal(signalName, signalDeclaration));
                }
            }
        }

        // Enums - declared at class level
        public override void Visit(GDEnumDeclaration enumDeclaration)
        {
            if (_isCollectionPass)
            {
                var enumName = enumDeclaration.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(enumName))
                {
                    TryDeclareSymbol(GDSymbol.Enum(enumName, enumDeclaration));
                }

                var values = enumDeclaration.Values;
                if (values != null)
                {
                    foreach (var value in values)
                    {
                        var valueName = value.Identifier?.Sequence;
                        if (!string.IsNullOrEmpty(valueName))
                        {
                            TryDeclareSymbol(GDSymbol.EnumValue(valueName, value));
                        }
                    }
                }
            }
        }

        #endregion

        #region Local declarations (processed in second pass only)

        // Local variables (inside methods)
        public override void Visit(GDVariableDeclarationStatement variableDeclaration)
        {
            if (!_isCollectionPass)
            {
                var varName = variableDeclaration.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(varName))
                {
                    TryDeclareSymbol(GDSymbol.Variable(varName, variableDeclaration));
                }
            }
        }

        // For loop - creates scope, declares iterator
        public override void Visit(GDForStatement forStatement)
        {
            if (!_isCollectionPass)
            {
                EnterScope(GDScopeType.ForLoop, forStatement);

                var iteratorName = forStatement.Variable?.Sequence;
                if (!string.IsNullOrEmpty(iteratorName))
                {
                    Context.Declare(GDSymbol.Iterator(iteratorName, forStatement));
                }
            }
        }

        public override void Left(GDForStatement forStatement)
        {
            if (!_isCollectionPass)
            {
                ExitScope();
            }
        }

        // While loop
        public override void Visit(GDWhileStatement whileStatement)
        {
            if (!_isCollectionPass)
            {
                EnterScope(GDScopeType.WhileLoop, whileStatement);
            }
        }

        public override void Left(GDWhileStatement whileStatement)
        {
            if (!_isCollectionPass)
            {
                ExitScope();
            }
        }

        // Conditionals
        public override void Visit(GDIfStatement ifStatement)
        {
            if (!_isCollectionPass)
            {
                EnterScope(GDScopeType.Conditional, ifStatement);
            }
        }

        public override void Left(GDIfStatement ifStatement)
        {
            if (!_isCollectionPass)
            {
                ExitScope();
            }
        }

        // Match
        public override void Visit(GDMatchStatement matchStatement)
        {
            if (!_isCollectionPass)
            {
                EnterScope(GDScopeType.Match, matchStatement);
            }
        }

        public override void Left(GDMatchStatement matchStatement)
        {
            if (!_isCollectionPass)
            {
                ExitScope();
            }
        }

        // Match case variable binding (var x in pattern)
        public override void Visit(GDMatchCaseVariableExpression matchCaseVariable)
        {
            if (!_isCollectionPass)
            {
                var varName = matchCaseVariable.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(varName))
                {
                    TryDeclareSymbol(GDSymbol.Variable(varName, matchCaseVariable));
                }
            }
        }

        // Lambdas
        public override void Visit(GDMethodExpression methodExpression)
        {
            if (!_isCollectionPass)
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
                            TryDeclareSymbol(GDSymbol.Parameter(paramName, param));
                        }
                    }
                }
            }
        }

        public override void Left(GDMethodExpression methodExpression)
        {
            if (!_isCollectionPass)
            {
                ExitScope();
            }
        }

        #endregion

        #region Identifier validation (second pass only)

        // Identifier usage - check if declared
        public override void Visit(GDIdentifierExpression identifierExpression)
        {
            if (_isCollectionPass)
                return;

            var name = identifierExpression.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name) && !Context.RuntimeProvider.IsBuiltIn(name))
            {
                var symbol = LookupSymbol(name);
                if (symbol == null)
                {
                    ReportError(
                        GDDiagnosticCode.UndefinedVariable,
                        $"Undefined identifier: '{name}'",
                        identifierExpression);
                }
            }
        }

        // Check for constant reassignment
        public override void Visit(GDDualOperatorExpression dualOperator)
        {
            if (_isCollectionPass)
                return;

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

    }
}
