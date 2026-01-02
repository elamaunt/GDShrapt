namespace GDShrapt.Reader
{
    /// <summary>
    /// Automatically adds type hints to untyped variables using type inference.
    /// This rule requires GDShrapt.Validator for type inference capabilities.
    /// </summary>
    public class GDAutoTypeInferenceFormatRule : GDFormatRule
    {
        public override string RuleId => "GDF007";
        public override string Name => "auto-type-hints";
        public override string Description => "Automatically add inferred type hints to untyped variables";
        public override bool EnabledByDefault => false;

        private GDTypeInferenceEngine _typeInferenceEngine;

        /// <summary>
        /// Sets the type inference engine to use. Must be called before formatting.
        /// If not set, a default engine with GDDefaultRuntimeProvider will be used.
        /// </summary>
        public GDTypeInferenceEngine TypeInferenceEngine
        {
            get => _typeInferenceEngine ?? (_typeInferenceEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance));
            set => _typeInferenceEngine = value;
        }

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            if (!Options.AutoAddTypeHints || !Options.AutoAddTypeHintsToClassVariables)
                return;

            // Skip if already has a type
            if (variableDeclaration.Type != null)
                return;

            // Skip if it's using inferred assignment (:=) - TypeColon exists but Type is null
            if (variableDeclaration.TypeColon != null)
                return;

            // Skip constants - they have inferred types and typically don't need explicit hints
            if (variableDeclaration.ConstKeyword != null)
                return;

            // Try to infer type from the initializer
            var initializer = variableDeclaration.Initializer;
            if (initializer == null)
            {
                // No initializer - use fallback if set
                AddTypeHintToVariable(variableDeclaration, Options.UnknownTypeFallback);
                return;
            }

            var inferredType = InferTypeString(initializer);
            AddTypeHintToVariable(variableDeclaration, inferredType ?? Options.UnknownTypeFallback);
        }

        public override void Visit(GDVariableDeclarationStatement localVariable)
        {
            if (!Options.AutoAddTypeHints || !Options.AutoAddTypeHintsToLocals)
                return;

            // Skip if already has a type
            if (localVariable.Type != null)
                return;

            // Skip if it's using inferred assignment (:=)
            if (localVariable.Colon != null)
                return;

            // Try to infer type from the initializer
            var initializer = localVariable.Initializer;
            if (initializer == null)
            {
                // No initializer - use fallback if set
                AddTypeHintToLocalVariable(localVariable, Options.UnknownTypeFallback);
                return;
            }

            var inferredType = InferTypeString(initializer);
            AddTypeHintToLocalVariable(localVariable, inferredType ?? Options.UnknownTypeFallback);
        }

        public override void Visit(GDParameterDeclaration parameter)
        {
            if (!Options.AutoAddTypeHints || !Options.AutoAddTypeHintsToParameters)
                return;

            // Skip if already has a type
            if (parameter.Type != null)
                return;

            // For parameters, we can only use the fallback since there's no initializer to infer from
            // (default values are not reliable indicators of type)
            var defaultValue = parameter.DefaultValue;
            string inferredType = null;

            if (defaultValue != null)
            {
                inferredType = InferTypeString(defaultValue);
            }

            AddTypeHintToParameter(parameter, inferredType ?? Options.UnknownTypeFallback);
        }

        private string InferTypeString(GDExpression expression)
        {
            return TypeInferenceEngine?.InferType(expression);
        }

        private void AddTypeHintToVariable(GDVariableDeclaration variableDeclaration, string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return;

            if (variableDeclaration.Identifier == null)
                return;

            // Create the type node
            var type = new GDSingleTypeNode { Type = new GDType { Sequence = typeName } };

            // Set the properties directly - the form handles token placement
            variableDeclaration.TypeColon = new GDColon();
            variableDeclaration.Type = type;
        }

        private void AddTypeHintToLocalVariable(GDVariableDeclarationStatement localVariable, string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return;

            if (localVariable.Identifier == null)
                return;

            // Create the type node
            var type = new GDSingleTypeNode { Type = new GDType { Sequence = typeName } };

            // Set the properties directly - the form handles token placement
            localVariable.Colon = new GDColon();
            localVariable.Type = type;
        }

        private void AddTypeHintToParameter(GDParameterDeclaration parameter, string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return;

            if (parameter.Identifier == null)
                return;

            // Create the type node
            var type = new GDSingleTypeNode { Type = new GDType { Sequence = typeName } };

            // Set the properties directly - the form handles token placement
            parameter.Colon = new GDColon();
            parameter.Type = type;
        }
    }
}
