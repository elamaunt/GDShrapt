namespace GDShrapt.Reader
{
    /// <summary>
    /// Validates GDScript AST using a pipeline of specialized validators.
    /// Runs syntax, scope, type, call, and control flow checks.
    /// </summary>
    public class GDValidator
    {
        /// <summary>
        /// Validates an AST node and returns all diagnostics.
        /// </summary>
        public GDValidationResult Validate(GDNode? node, GDValidationOptions? options = null)
        {
            options = options ?? GDValidationOptions.Default;
            var context = new GDValidationContext(options.RuntimeProvider);

            // Phase 0: Collect all declarations first (enables forward references)
            // This must run before scope/call validation to ensure all symbols are available
            if (options.CheckScope || options.CheckCalls)
            {
                var collector = new GDDeclarationCollector();
                collector.Collect(node, context);
            }

            if (options.CheckSyntax)
            {
                var syntaxValidator = new GDSyntaxValidator(context);
                syntaxValidator.Validate(node);
            }

            if (options.CheckScope)
            {
                var scopeValidator = new GDScopeValidator(context);
                scopeValidator.Validate(node);
            }

            if (options.CheckTypes)
            {
                var typeValidator = new GDTypeValidator(context);
                typeValidator.Validate(node);
            }

            if (options.CheckCalls)
            {
                var callValidator = new GDCallValidator(context, options.CheckResourcePaths);
                callValidator.Validate(node);
            }

            if (options.CheckControlFlow)
            {
                var controlFlowValidator = new GDControlFlowValidator(context);
                controlFlowValidator.Validate(node);
            }

            if (options.CheckIndentation)
            {
                var indentationValidator = new GDIndentationValidator(context);
                indentationValidator.Validate(node);
            }

            // Member access validation using member access analyzer
            if (options.CheckMemberAccess && options.MemberAccessAnalyzer != null)
            {
                var memberAccessValidator = new GDMemberAccessValidator(
                    context,
                    options.MemberAccessAnalyzer,
                    options.MemberAccessSeverity);
                memberAccessValidator.Validate(node);
            }

            if (options.CheckAbstract)
            {
                var abstractValidator = new GDAbstractValidator(context);
                abstractValidator.Validate(node);
            }

            if (options.CheckSignals)
            {
                var signalValidator = new GDSignalValidator(context);
                signalValidator.Validate(node);
            }

            return context.BuildResult();
        }

        /// <summary>
        /// Parses and validates GDScript source code.
        /// </summary>
        public GDValidationResult ValidateCode(string code, GDValidationOptions options = null)
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent(code);
            return Validate(tree, options);
        }

        /// <summary>
        /// Parses and validates a GDScript expression.
        /// </summary>
        public GDValidationResult ValidateExpression(string expression, GDValidationOptions options = null)
        {
            var reader = new GDScriptReader();
            var expr = reader.ParseExpression(expression);
            return Validate(expr, options);
        }

        /// <summary>
        /// Parses and validates a GDScript statement.
        /// </summary>
        public GDValidationResult ValidateStatement(string statement, GDValidationOptions options = null)
        {
            var reader = new GDScriptReader();
            var stmt = reader.ParseStatement(statement);
            return Validate(stmt, options);
        }

        /// <summary>
        /// Quick check if code has no errors.
        /// </summary>
        public bool IsValid(string code, GDValidationOptions options = null)
        {
            return ValidateCode(code, options).IsValid;
        }
    }
}
