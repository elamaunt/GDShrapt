using System.Collections.Generic;
using GDShrapt.Abstractions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Shared state for all validators: scope stack, diagnostics collection, and runtime provider.
    /// </summary>
    public class GDValidationContext
    {
        private readonly List<GDDiagnostic> _diagnostics;
        private readonly Dictionary<string, GDFunctionSignature> _userFunctions;

        public GDScopeStack Scopes { get; }
        public IReadOnlyList<GDDiagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// The runtime provider for type information.
        /// </summary>
        public IGDRuntimeProvider RuntimeProvider { get; }

        /// <summary>
        /// User-defined functions collected during declaration collection phase.
        /// </summary>
        public IReadOnlyDictionary<string, GDFunctionSignature> UserFunctions => _userFunctions;

        /// <summary>
        /// The base type of the current class from its extends clause.
        /// For example, "Node2D" for a script with "extends Node2D".
        /// </summary>
        public string CurrentClassBaseType { get; set; }

        public bool IsInLoop => Scopes.IsInLoop;
        public bool IsInFunction => Scopes.IsInFunction;
        public bool IsInClass => Scopes.IsInClass;

        public GDValidationContext() : this(null)
        {
        }

        public GDValidationContext(IGDRuntimeProvider runtimeProvider)
        {
            _diagnostics = new List<GDDiagnostic>();
            _userFunctions = new Dictionary<string, GDFunctionSignature>();
            Scopes = new GDScopeStack();
            RuntimeProvider = runtimeProvider ?? GDDefaultRuntimeProvider.Instance;
        }

        /// <summary>
        /// Registers a user-defined function. Called during declaration collection phase.
        /// </summary>
        public void RegisterFunction(string name, GDFunctionSignature signature)
        {
            if (!string.IsNullOrEmpty(name) && !_userFunctions.ContainsKey(name))
            {
                _userFunctions[name] = signature;
            }
        }

        /// <summary>
        /// Checks if a function is declared (either user-defined or built-in).
        /// </summary>
        public bool IsFunctionDeclared(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // Check user-defined functions
            if (_userFunctions.ContainsKey(name))
                return true;

            // Check global functions from runtime provider
            if (RuntimeProvider.GetGlobalFunction(name) != null)
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a member (method, property, signal) exists in the base class hierarchy.
        /// Uses the pattern from GDMemberAccessValidator.FindMember().
        /// </summary>
        public bool IsBaseClassMember(string memberName)
        {
            if (string.IsNullOrEmpty(memberName) || string.IsNullOrEmpty(CurrentClassBaseType))
                return false;

            var visited = new HashSet<string>();
            var currentType = CurrentClassBaseType;
            while (!string.IsNullOrEmpty(currentType))
            {
                // Prevent infinite loop on cyclic inheritance
                if (!visited.Add(currentType))
                    return false;

                var memberInfo = RuntimeProvider.GetMember(currentType, memberName);
                if (memberInfo != null)
                    return true;

                currentType = RuntimeProvider.GetBaseType(currentType);
            }
            return false;
        }

        /// <summary>
        /// Gets member info from base class hierarchy (returns first found).
        /// </summary>
        public GDRuntimeMemberInfo GetBaseClassMember(string memberName)
        {
            if (string.IsNullOrEmpty(memberName) || string.IsNullOrEmpty(CurrentClassBaseType))
                return null;

            var visited = new HashSet<string>();
            var currentType = CurrentClassBaseType;
            while (!string.IsNullOrEmpty(currentType))
            {
                // Prevent infinite loop on cyclic inheritance
                if (!visited.Add(currentType))
                    return null;

                var memberInfo = RuntimeProvider.GetMember(currentType, memberName);
                if (memberInfo != null)
                    return memberInfo;

                currentType = RuntimeProvider.GetBaseType(currentType);
            }
            return null;
        }

        /// <summary>
        /// Gets the function signature for a user-defined function.
        /// </summary>
        public GDFunctionSignature GetUserFunction(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            _userFunctions.TryGetValue(name, out var signature);
            return signature;
        }

        public void AddError(GDDiagnosticCode code, string message, GDNode node)
        {
            _diagnostics.Add(GDDiagnostic.Error(code, message, node));
        }

        public void AddError(GDDiagnosticCode code, string message, GDSyntaxToken token)
        {
            _diagnostics.Add(GDDiagnostic.Error(code, message, token));
        }

        public void AddWarning(GDDiagnosticCode code, string message, GDNode node)
        {
            _diagnostics.Add(GDDiagnostic.Warning(code, message, node));
        }

        public void AddWarning(GDDiagnosticCode code, string message, GDSyntaxToken token)
        {
            _diagnostics.Add(GDDiagnostic.Warning(code, message, token));
        }

        public void AddHint(GDDiagnosticCode code, string message, GDNode node)
        {
            _diagnostics.Add(GDDiagnostic.Hint(code, message, node));
        }

        public void AddHint(GDDiagnosticCode code, string message, GDSyntaxToken token)
        {
            _diagnostics.Add(GDDiagnostic.Hint(code, message, token));
        }

        public GDScope EnterScope(GDScopeType type, GDNode node = null)
        {
            return Scopes.Push(type, node);
        }

        public GDScope ExitScope()
        {
            return Scopes.Pop();
        }

        /// <summary>
        /// Returns false if symbol already exists in current scope.
        /// </summary>
        public bool TryDeclare(GDSymbol symbol)
        {
            return Scopes.TryDeclare(symbol);
        }

        public void Declare(GDSymbol symbol)
        {
            Scopes.Declare(symbol);
        }

        /// <summary>
        /// Searches all scopes from innermost to outermost.
        /// </summary>
        public GDSymbol Lookup(string name)
        {
            return Scopes.Lookup(name);
        }

        public bool Contains(string name)
        {
            return Scopes.Contains(name);
        }

        public GDValidationResult BuildResult()
        {
            return new GDValidationResult(_diagnostics);
        }
    }
}
