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
        public GDValidationResult Validate(GDNode node, GDValidationOptions options = null)
        {
            options = options ?? GDValidationOptions.Default;
            var context = new GDValidationContext();

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
                var callValidator = new GDCallValidator(context);
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
