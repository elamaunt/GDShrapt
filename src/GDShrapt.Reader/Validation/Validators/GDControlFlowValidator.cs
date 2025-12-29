using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Checks break/continue in loops, return/yield/await in functions.
    /// Also detects unreachable code after return/break/continue statements.
    /// Uses depth counters to track nesting.
    /// </summary>
    public class GDControlFlowValidator : GDValidationVisitor
    {
        private int _loopDepth;
        private int _functionDepth;
        private bool _isStaticMethod;

        public GDControlFlowValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            node?.WalkIn(this);
        }

        // Track function nesting
        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            _functionDepth++;
            _isStaticMethod = methodDeclaration.IsStatic;
            CheckUnreachableCode(methodDeclaration.Statements);
        }

        public override void Left(GDMethodDeclaration methodDeclaration)
        {
            _functionDepth--;
            _isStaticMethod = false;
        }

        public override void Visit(GDMethodExpression methodExpression)
        {
            _functionDepth++;
            CheckUnreachableCodeInLambda(methodExpression);
        }

        public override void Left(GDMethodExpression methodExpression) => _functionDepth--;

        // Property getter body - acts like a function
        public override void Visit(GDGetAccessorBodyDeclaration getterBody)
        {
            _functionDepth++;
            CheckUnreachableCode(getterBody.Statements);
        }

        public override void Left(GDGetAccessorBodyDeclaration getterBody) => _functionDepth--;

        // Property setter body - acts like a function
        public override void Visit(GDSetAccessorBodyDeclaration setterBody)
        {
            _functionDepth++;
            CheckUnreachableCode(setterBody.Statements);
        }

        public override void Left(GDSetAccessorBodyDeclaration setterBody) => _functionDepth--;

        // Track loop nesting
        public override void Visit(GDForStatement forStatement)
        {
            _loopDepth++;
            CheckUnreachableCode(forStatement.Statements);
        }

        public override void Left(GDForStatement forStatement) => _loopDepth--;

        public override void Visit(GDWhileStatement whileStatement)
        {
            _loopDepth++;
            CheckUnreachableCode(whileStatement.Statements);
        }

        public override void Left(GDWhileStatement whileStatement) => _loopDepth--;

        // Check if branches
        public override void Visit(GDIfBranch ifBranch)
        {
            CheckUnreachableCode(ifBranch.Statements);
        }

        public override void Visit(GDElseBranch elseBranch)
        {
            CheckUnreachableCode(elseBranch.Statements);
        }

        public override void Visit(GDElifBranch elifBranch)
        {
            CheckUnreachableCode(elifBranch.Statements);
        }

        // Check match cases
        public override void Visit(GDMatchCaseDeclaration matchCase)
        {
            CheckUnreachableCode(matchCase.Statements);
        }

        // Super validation
        public override void Visit(GDCallExpression callExpression)
        {
            // Check for super() call
            if (callExpression.CallerExpression is GDIdentifierExpression identExpr &&
                identExpr.Identifier?.Sequence == "super")
            {
                if (_functionDepth == 0)
                {
                    ReportError(
                        GDDiagnosticCode.SuperOutsideMethod,
                        "'super' can only be called inside a method",
                        callExpression);
                }
                else if (_isStaticMethod)
                {
                    ReportError(
                        GDDiagnosticCode.SuperInStaticMethod,
                        "'super' cannot be called in a static method",
                        callExpression);
                }
            }
        }

        // Also handle member access like super.method()
        public override void Visit(GDMemberOperatorExpression memberExpr)
        {
            if (memberExpr.CallerExpression is GDIdentifierExpression identExpr &&
                identExpr.Identifier?.Sequence == "super")
            {
                if (_functionDepth == 0)
                {
                    ReportError(
                        GDDiagnosticCode.SuperOutsideMethod,
                        "'super' can only be used inside a method",
                        memberExpr);
                }
                else if (_isStaticMethod)
                {
                    ReportError(
                        GDDiagnosticCode.SuperInStaticMethod,
                        "'super' cannot be used in a static method",
                        memberExpr);
                }
            }
        }

        private void CheckUnreachableCode(GDStatementsList statements)
        {
            if (statements == null)
                return;

            var stmtList = statements.ToList();
            for (int i = 0; i < stmtList.Count - 1; i++)
            {
                if (IsTerminatingStatement(stmtList[i]))
                {
                    // All statements after this one are unreachable
                    var nextStmt = stmtList[i + 1];
                    ReportWarning(
                        GDDiagnosticCode.UnreachableCode,
                        "Unreachable code detected after return/break/continue statement",
                        nextStmt);
                    break; // Only report once per block
                }
            }
        }

        private void CheckUnreachableCodeInLambda(GDMethodExpression lambda)
        {
            // Lambda can have either Expression or Statements
            if (lambda.Statements != null)
            {
                CheckUnreachableCode(lambda.Statements);
            }
        }

        private bool IsTerminatingStatement(GDStatement statement)
        {
            if (statement is GDExpressionStatement exprStmt)
            {
                var expr = exprStmt.Expression;
                return expr is GDReturnExpression ||
                       expr is GDBreakExpression ||
                       expr is GDContinueExpression;
            }
            return false;
        }

        public override void Visit(GDBreakExpression breakExpression)
        {
            if (_loopDepth == 0)
            {
                ReportError(
                    GDDiagnosticCode.BreakOutsideLoop,
                    "'break' can only be used inside a loop",
                    breakExpression);
            }
        }

        public override void Visit(GDContinueExpression continueExpression)
        {
            if (_loopDepth == 0)
            {
                ReportError(
                    GDDiagnosticCode.ContinueOutsideLoop,
                    "'continue' can only be used inside a loop",
                    continueExpression);
            }
        }

        public override void Visit(GDReturnExpression returnExpression)
        {
            if (_functionDepth == 0)
            {
                ReportError(
                    GDDiagnosticCode.ReturnOutsideFunction,
                    "'return' can only be used inside a function",
                    returnExpression);
            }
        }

        public override void Visit(GDYieldExpression yieldExpression)
        {
            if (_functionDepth == 0)
            {
                ReportError(
                    GDDiagnosticCode.YieldOutsideFunction,
                    "'yield' can only be used inside a function",
                    yieldExpression);
            }
        }

        public override void Visit(GDAwaitExpression awaitExpression)
        {
            if (_functionDepth == 0)
            {
                ReportError(
                    GDDiagnosticCode.AwaitOutsideFunction,
                    "'await' can only be used inside a function",
                    awaitExpression);
            }
        }
    }
}
