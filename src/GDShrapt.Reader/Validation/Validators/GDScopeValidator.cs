namespace GDShrapt.Reader
{
    /// <summary>
    /// Tracks declarations, detects undefined identifiers and duplicates.
    /// Manages scope stack for classes, methods, loops, etc.
    /// </summary>
    public class GDScopeValidator : GDValidationVisitor
    {
        public GDScopeValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            EnterScope(GDScopeType.Global, node);
            node?.WalkIn(this);
            ExitScope();
        }

        // Classes
        public override void Visit(GDClassDeclaration classDeclaration)
        {
            EnterScope(GDScopeType.Class, classDeclaration);
        }

        public override void Left(GDClassDeclaration classDeclaration)
        {
            ExitScope();
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            var className = innerClass.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(className))
            {
                TryDeclareSymbol(GDSymbol.Class(className, innerClass));
            }

            EnterScope(GDScopeType.Class, innerClass);
        }

        public override void Left(GDInnerClassDeclaration innerClass)
        {
            ExitScope();
        }

        // Methods
        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            var methodName = methodDeclaration.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(methodName))
            {
                TryDeclareSymbol(GDSymbol.Method(methodName, methodDeclaration, methodDeclaration.IsStatic));
            }

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

        public override void Left(GDMethodDeclaration methodDeclaration)
        {
            ExitScope();
        }

        // Variables (class-level)
        public override void Visit(GDVariableDeclaration variableDeclaration)
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

        // Local variables (inside methods)
        public override void Visit(GDVariableDeclarationStatement variableDeclaration)
        {
            var varName = variableDeclaration.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                TryDeclareSymbol(GDSymbol.Variable(varName, variableDeclaration));
            }
        }

        // Signals
        public override void Visit(GDSignalDeclaration signalDeclaration)
        {
            var signalName = signalDeclaration.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(signalName))
            {
                TryDeclareSymbol(GDSymbol.Signal(signalName, signalDeclaration));
            }
        }

        // Enums (also registers enum values as symbols)
        public override void Visit(GDEnumDeclaration enumDeclaration)
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
                        TryDeclareSymbol(GDSymbol.Parameter(paramName, param));
                    }
                }
            }
        }

        public override void Left(GDMethodExpression methodExpression)
        {
            ExitScope();
        }

        // Identifier usage - check if declared
        public override void Visit(GDIdentifierExpression identifierExpression)
        {
            var name = identifierExpression.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name) && !IsBuiltIn(name))
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
            var opType = dualOperator.Operator?.OperatorType;
            if (opType == null)
                return;

            // Check all assignment operators
            if (IsAssignmentOperator(opType.Value))
            {
                CheckConstantReassignment(dualOperator.LeftExpression, dualOperator);
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

        /// <summary>
        /// GDScript built-in constants, types, functions, and globals.
        /// </summary>
        private bool IsBuiltIn(string name)
        {
            switch (name)
            {
                // Constants
                case "null":
                case "true":
                case "false":
                case "self":
                case "super":
                case "PI":
                case "TAU":
                case "INF":
                case "NAN":
                // Types
                case "int":
                case "float":
                case "bool":
                case "String":
                case "Vector2":
                case "Vector2i":
                case "Vector3":
                case "Vector3i":
                case "Vector4":
                case "Vector4i":
                case "Rect2":
                case "Rect2i":
                case "Transform2D":
                case "Transform3D":
                case "Plane":
                case "Quaternion":
                case "AABB":
                case "Basis":
                case "Projection":
                case "Color":
                case "NodePath":
                case "RID":
                case "Object":
                case "Callable":
                case "Signal":
                case "Dictionary":
                case "Array":
                case "PackedByteArray":
                case "PackedInt32Array":
                case "PackedInt64Array":
                case "PackedFloat32Array":
                case "PackedFloat64Array":
                case "PackedStringArray":
                case "PackedVector2Array":
                case "PackedVector3Array":
                case "PackedColorArray":
                case "StringName":
                case "Node":
                case "Node2D":
                case "Node3D":
                case "Control":
                case "Resource":
                // Functions
                case "print":
                case "prints":
                case "printt":
                case "printraw":
                case "printerr":
                case "push_error":
                case "push_warning":
                case "str":
                case "range":
                case "load":
                case "preload":
                case "assert":
                case "abs":
                case "ceil":
                case "floor":
                case "round":
                case "sign":
                case "sqrt":
                case "pow":
                case "log":
                case "exp":
                case "sin":
                case "cos":
                case "tan":
                case "asin":
                case "acos":
                case "atan":
                case "atan2":
                case "min":
                case "max":
                case "clamp":
                case "lerp":
                case "typeof":
                case "weakref":
                case "randomize":
                case "randi":
                case "randf":
                case "hash":
                case "get_tree":
                case "get_node":
                case "queue_free":
                case "is_instance_valid":
                // Globals
                case "Input":
                case "Engine":
                case "OS":
                case "ResourceLoader":
                case "Time":
                    return true;
            }

            return false;
        }
    }
}
