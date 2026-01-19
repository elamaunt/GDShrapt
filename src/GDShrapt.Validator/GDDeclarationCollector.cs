using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Collects all class-level declarations before validation.
    /// This enables forward references for methods and other class members.
    /// </summary>
    public class GDDeclarationCollector : GDVisitor
    {
        private GDValidationContext _context;

        /// <summary>
        /// Collects all declarations from the AST into the validation context.
        /// </summary>
        public void Collect(GDNode node, GDValidationContext context)
        {
            if (node == null || context == null)
                return;

            _context = context;

            // Enter global scope for class-level declarations
            _context.EnterScope(GDScopeType.Global, node);

            // Walk the tree to collect declarations
            node.WalkIn(this);

            // Reset scope stack but keep collected symbols
            _context.Scopes.ResetToGlobal();
        }

        #region Extends Clause

        public override void Visit(GDExtendsAttribute extendsAttribute)
        {
            var typeNode = extendsAttribute.Type;
            if (typeNode == null)
                return;

            // Handle string path extends (e.g., extends "res://path.gd")
            if (typeNode is GDStringTypeNode stringTypeNode)
            {
                var scriptPath = stringTypeNode.Path?.Sequence;
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    // Try to resolve the script path via project provider
                    if (_context.RuntimeProvider is IGDProjectRuntimeProvider projectProvider)
                    {
                        var scriptInfo = projectProvider.GetScriptType(scriptPath);
                        if (scriptInfo != null)
                        {
                            // Use class_name if available, otherwise use the script path itself
                            var baseType = scriptInfo.ClassName ?? scriptPath;
                            _context.CurrentClassBaseType = baseType;
                            return;
                        }
                    }

                    // Fallback: use the script path directly as the base type
                    // RuntimeProvider.GetMember() and GetBaseType() can resolve paths
                    // via _pathToTypeName mapping in GDProjectTypesProvider
                    _context.CurrentClassBaseType = scriptPath;
                }
                return;
            }

            var typeName = typeNode.BuildName();
            if (!string.IsNullOrEmpty(typeName))
            {
                _context.CurrentClassBaseType = typeName;
            }
        }

        #endregion

        #region Methods

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            var methodName = methodDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                return;

            var parameters = methodDeclaration.Parameters?.ToList() ?? new List<GDParameterDeclaration>();

            // Count required and optional parameters
            int minParams = 0;
            int maxParams = parameters.Count;

            foreach (var param in parameters)
            {
                if (param.DefaultValue == null)
                    minParams++;
            }

            var signature = new GDFunctionSignature
            {
                Name = methodName,
                MinParameters = minParams,
                MaxParameters = maxParams,
                HasVarArgs = false,
                Declaration = methodDeclaration,
                IsStatic = methodDeclaration.IsStatic
            };

            _context.RegisterFunction(methodName, signature);

            // Register as a symbol for scope lookup, report duplicate if already exists
            if (!_context.TryDeclare(GDSymbol.Method(methodName, methodDeclaration, methodDeclaration.IsStatic)))
            {
                _context.AddError(
                    GDDiagnosticCode.DuplicateDeclaration,
                    $"Method '{methodName}' is already declared",
                    methodDeclaration);
            }
        }

        #endregion

        #region Variables

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            var varName = variableDeclaration.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                return;

            var typeNode = variableDeclaration.Type;
            var typeName = typeNode?.BuildName();

            bool declared;
            if (variableDeclaration.ConstKeyword != null)
            {
                declared = _context.TryDeclare(GDSymbol.Constant(varName, variableDeclaration, typeName: typeName, typeNode: typeNode));
            }
            else
            {
                declared = _context.TryDeclare(GDSymbol.Variable(varName, variableDeclaration, typeName: typeName, typeNode: typeNode, isStatic: variableDeclaration.IsStatic));
            }

            if (!declared)
            {
                _context.AddError(
                    GDDiagnosticCode.DuplicateDeclaration,
                    $"Variable '{varName}' is already declared",
                    variableDeclaration);
            }
        }

        #endregion

        #region Signals

        public override void Visit(GDSignalDeclaration signalDeclaration)
        {
            var signalName = signalDeclaration.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(signalName))
            {
                if (!_context.TryDeclare(GDSymbol.Signal(signalName, signalDeclaration)))
                {
                    _context.AddError(
                        GDDiagnosticCode.DuplicateDeclaration,
                        $"Signal '{signalName}' is already declared",
                        signalDeclaration);
                }
            }
        }

        #endregion

        #region Enums

        public override void Visit(GDEnumDeclaration enumDeclaration)
        {
            var enumName = enumDeclaration.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(enumName))
            {
                if (!_context.TryDeclare(GDSymbol.Enum(enumName, enumDeclaration)))
                {
                    _context.AddError(
                        GDDiagnosticCode.DuplicateDeclaration,
                        $"Enum '{enumName}' is already declared",
                        enumDeclaration);
                }
            }

            var values = enumDeclaration.Values;
            if (values != null)
            {
                foreach (var value in values)
                {
                    var valueName = value.Identifier?.Sequence;
                    if (!string.IsNullOrEmpty(valueName))
                    {
                        if (!_context.TryDeclare(GDSymbol.EnumValue(valueName, value)))
                        {
                            _context.AddError(
                                GDDiagnosticCode.DuplicateDeclaration,
                                $"Enum value '{valueName}' is already declared",
                                value);
                        }
                    }
                }
            }
        }

        #endregion

        #region Inner Classes

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            var className = innerClass.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(className))
            {
                // Register inner class symbol in parent scope
                if (!_context.TryDeclare(GDSymbol.Class(className, innerClass)))
                {
                    _context.AddError(
                        GDDiagnosticCode.DuplicateDeclaration,
                        $"Inner class '{className}' is already declared",
                        innerClass);
                }
            }

            // Enter inner class scope - members will be declared in this isolated scope
            _context.EnterScope(GDScopeType.Class, innerClass);
        }

        public override void Left(GDInnerClassDeclaration innerClass)
        {
            // Exit inner class scope
            _context.ExitScope();
        }

        #endregion

        #region Skip non-class-level nodes

        // Don't collect local variables - those are handled by ScopeValidator
        public override void Visit(GDVariableDeclarationStatement variableDeclaration)
        {
            // Skip - local variables are not class-level
        }

        // Don't enter methods during collection
        public override void Left(GDMethodDeclaration methodDeclaration)
        {
            // Nothing to do
        }

        #endregion
    }
}
