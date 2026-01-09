using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Validates @abstract annotations on classes and methods.
    /// Checks:
    /// - Abstract methods should not have implementation body
    /// - Classes with abstract methods must be marked @abstract
    /// - Cannot call super() in abstract methods
    /// - Cannot instantiate abstract classes with ClassName.new()
    /// </summary>
    public class GDAbstractValidator : GDValidationVisitor
    {
        private bool _isInAbstractMethod;
        private GDMethodDeclaration _currentMethod;

        public GDAbstractValidator(GDValidationContext context) : base(context)
        {
        }

        public void Validate(GDNode node)
        {
            node?.WalkIn(this);
        }

        public override void Visit(GDClassDeclaration classDecl)
        {
            ValidateAbstractClass(classDecl);
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            ValidateAbstractInnerClass(innerClass);
        }

        public override void Visit(GDMethodDeclaration method)
        {
            _currentMethod = method;
            _isInAbstractMethod = IsMethodAbstract(method);

            if (_isInAbstractMethod)
            {
                ValidateAbstractMethod(method);
            }
        }

        public override void Left(GDMethodDeclaration method)
        {
            _currentMethod = null;
            _isInAbstractMethod = false;
        }

        public override void Visit(GDCallExpression callExpression)
        {
            // Check for super() call in abstract method
            if (_isInAbstractMethod && IsSuperCall(callExpression))
            {
                ReportError(
                    GDDiagnosticCode.SuperInAbstractMethod,
                    "'super' cannot be called in an abstract method",
                    callExpression);
            }

            // Check for abstract class instantiation: AbstractClass.new()
            ValidateInstantiation(callExpression);
        }

        public override void Visit(GDMemberOperatorExpression memberExpr)
        {
            // Check for super.method() in abstract method
            if (_isInAbstractMethod && IsSuperAccess(memberExpr))
            {
                ReportError(
                    GDDiagnosticCode.SuperInAbstractMethod,
                    "'super' cannot be used in an abstract method",
                    memberExpr);
            }
        }

        private void ValidateAbstractClass(GDClassDeclaration classDecl)
        {
            bool isClassAbstract = classDecl.CustomAttributes
                .Any(attr => attr.Attribute?.IsAbstract() == true);

            var abstractMethods = classDecl.Methods
                .Where(m => IsMethodAbstract(m))
                .ToList();

            if (abstractMethods.Any() && !isClassAbstract)
            {
                var firstAbstractMethod = abstractMethods.First();
                ReportError(
                    GDDiagnosticCode.ClassNotAbstract,
                    "Class contains abstract methods but is not marked @abstract",
                    firstAbstractMethod);
            }
        }

        private void ValidateAbstractInnerClass(GDInnerClassDeclaration innerClass)
        {
            bool isClassAbstract = innerClass.AttributesDeclaredBefore
                .Any(attr => attr.Attribute?.IsAbstract() == true);

            var abstractMethods = innerClass.Methods
                .Where(m => IsMethodAbstract(m))
                .ToList();

            if (abstractMethods.Any() && !isClassAbstract)
            {
                var firstAbstractMethod = abstractMethods.First();
                ReportError(
                    GDDiagnosticCode.ClassNotAbstract,
                    "Inner class contains abstract methods but is not marked @abstract",
                    firstAbstractMethod);
            }
        }

        private void ValidateAbstractMethod(GDMethodDeclaration method)
        {
            // Abstract methods should not have a body (no colon, no statements, no expression)
            bool hasColon = method.Colon != null;
            bool hasStatements = method.Statements?.Any() == true;
            bool hasExpression = method.Expression != null;

            if (hasColon || hasStatements || hasExpression)
            {
                ReportError(
                    GDDiagnosticCode.AbstractMethodHasBody,
                    "Abstract method should not have an implementation body",
                    method);
            }
        }

        private bool IsMethodAbstract(GDMethodDeclaration method)
        {
            return method.AttributesDeclaredBefore
                .Any(attr => attr.Attribute?.IsAbstract() == true);
        }

        private bool IsSuperCall(GDCallExpression callExpr)
        {
            return callExpr.CallerExpression is GDIdentifierExpression identExpr &&
                   identExpr.Identifier?.Sequence == "super";
        }

        private bool IsSuperAccess(GDMemberOperatorExpression memberExpr)
        {
            return memberExpr.CallerExpression is GDIdentifierExpression identExpr &&
                   identExpr.Identifier?.Sequence == "super";
        }

        private void ValidateInstantiation(GDCallExpression callExpr)
        {
            // Check for ClassName.new() pattern
            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp &&
                memberOp.Identifier?.Sequence == "new" &&
                memberOp.CallerExpression is GDIdentifierExpression classIdentExpr)
            {
                var className = classIdentExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(className))
                {
                    // Look up the class to see if it's abstract
                    var symbol = LookupSymbol(className);
                    if (symbol?.Kind == GDSymbolKind.Class)
                    {
                        // Check if the class has @abstract annotation
                        if (symbol.Declaration is GDInnerClassDeclaration innerClass)
                        {
                            if (innerClass.AttributesDeclaredBefore.Any(attr => attr.Attribute?.IsAbstract() == true))
                            {
                                ReportError(
                                    GDDiagnosticCode.AbstractClassInstantiation,
                                    $"Cannot instantiate abstract class '{className}'",
                                    callExpr);
                            }
                        }
                    }
                }
            }
        }
    }
}
