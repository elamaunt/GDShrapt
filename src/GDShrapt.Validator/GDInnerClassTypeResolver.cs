using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Resolves inner class types, including:
    /// - Simple names (BaseData)
    /// - Qualified names (Level1.Level2.Level3)
    /// - Inheritance chains (DerivedData → BaseData)
    /// </summary>
    public class GDInnerClassTypeResolver
    {
        private readonly GDValidationContext _context;
        private readonly GDClassDeclaration? _rootClass;

        public GDInnerClassTypeResolver(GDValidationContext context, GDClassDeclaration? rootClass)
        {
            _context = context;
            _rootClass = rootClass;
        }

        /// <summary>
        /// Resolves a type name to an inner class declaration.
        /// Handles both simple names and qualified names.
        /// </summary>
        public GDInnerClassDeclaration? ResolveInnerClass(string typeName)
        {
            if (string.IsNullOrEmpty(typeName) || _rootClass == null)
                return null;

            // Check for qualified name (e.g., "Level1.Level2.Level3")
            if (typeName.Contains('.'))
                return ResolveQualifiedType(typeName);

            // Simple name - search in root class's inner classes
            return FindInnerClassByName(_rootClass, typeName);
        }

        /// <summary>
        /// Resolves qualified type name by navigating through nested inner classes.
        /// </summary>
        private GDInnerClassDeclaration? ResolveQualifiedType(string qualifiedName)
        {
            var parts = qualifiedName.Split('.');
            if (parts.Length == 0 || _rootClass == null)
                return null;

            // Start from root class
            IGDClassDeclaration? currentContainer = _rootClass;
            GDInnerClassDeclaration? result = null;

            foreach (var part in parts)
            {
                var innerClass = FindInnerClassByName(currentContainer, part);
                if (innerClass == null)
                    return null;

                result = innerClass;
                currentContainer = innerClass;
            }

            return result;
        }

        /// <summary>
        /// Finds an inner class by name within a class declaration.
        /// </summary>
        private GDInnerClassDeclaration? FindInnerClassByName(
            IGDClassDeclaration? container, string name)
        {
            if (container == null)
                return null;

            return container.InnerClasses
                .FirstOrDefault(ic => ic.Identifier?.Sequence == name);
        }

        /// <summary>
        /// Gets the inheritance chain for an inner class.
        /// Returns classes from most derived to base.
        /// </summary>
        public List<GDInnerClassDeclaration> GetInheritanceChain(
            GDInnerClassDeclaration innerClass)
        {
            var chain = new List<GDInnerClassDeclaration> { innerClass };
            var visited = new HashSet<string>();
            var current = innerClass;

            while (current != null)
            {
                var baseName = current.BaseType?.BuildName();
                if (string.IsNullOrEmpty(baseName) || !visited.Add(baseName))
                    break;

                var baseClass = ResolveInnerClass(baseName);
                if (baseClass == null)
                    break;

                chain.Add(baseClass);
                current = baseClass;
            }

            return chain;
        }

        /// <summary>
        /// Checks if an inner class has a specific member (variable or method).
        /// Searches through inheritance chain.
        /// </summary>
        public bool HasMember(GDInnerClassDeclaration innerClass, string memberName)
        {
            var chain = GetInheritanceChain(innerClass);

            foreach (var cls in chain)
            {
                // Check variables
                var hasVar = cls.Members
                    .OfType<GDVariableDeclaration>()
                    .Any(v => v.Identifier?.Sequence == memberName);
                if (hasVar) return true;

                // Check methods
                var hasMethod = cls.Members
                    .OfType<GDMethodDeclaration>()
                    .Any(m => m.Identifier?.Sequence == memberName);
                if (hasMethod) return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a member from an inner class, searching through inheritance chain.
        /// </summary>
        public GDNode? GetMember(GDInnerClassDeclaration innerClass, string memberName)
        {
            var chain = GetInheritanceChain(innerClass);

            foreach (var cls in chain)
            {
                // Check variables
                var varDecl = cls.Members
                    .OfType<GDVariableDeclaration>()
                    .FirstOrDefault(v => v.Identifier?.Sequence == memberName);
                if (varDecl != null) return varDecl;

                // Check methods
                var methodDecl = cls.Members
                    .OfType<GDMethodDeclaration>()
                    .FirstOrDefault(m => m.Identifier?.Sequence == memberName);
                if (methodDecl != null) return methodDecl;
            }

            return null;
        }

        /// <summary>
        /// Tries to infer the type from an expression (e.g., ClassName.new()).
        /// </summary>
        public string? InferTypeFromExpression(GDExpression? expr)
        {
            if (expr == null)
                return null;

            // Handle: ClassName.new() or Outer.Inner.new()
            if (expr is GDCallExpression call &&
                call.CallerExpression is GDMemberOperatorExpression memberOp &&
                memberOp.Identifier?.Sequence == "new")
            {
                return BuildQualifiedTypeName(memberOp.CallerExpression);
            }

            return null;
        }

        /// <summary>
        /// Builds a qualified type name from an expression (e.g., Level1.Level2.Level3).
        /// </summary>
        public string? BuildQualifiedTypeName(GDExpression? expr)
        {
            if (expr is GDIdentifierExpression id)
                return id.Identifier?.Sequence;

            if (expr is GDMemberOperatorExpression member)
            {
                var baseName = BuildQualifiedTypeName(member.CallerExpression);
                var memberName = member.Identifier?.Sequence;
                if (baseName != null && memberName != null)
                    return $"{baseName}.{memberName}";
            }

            return null;
        }
    }
}
