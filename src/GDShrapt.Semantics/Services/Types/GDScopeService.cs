using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for scope-related symbol analysis.
/// Determines declaration scopes, filters references by scope type, and handles local/class member classification.
/// </summary>
public class GDScopeService
{
    /// <summary>
    /// Delegate for getting references to a symbol.
    /// </summary>
    public delegate IReadOnlyList<GDReference> GetReferencesToDelegate(GDSymbolInfo symbol);

    private readonly GetReferencesToDelegate? _getReferencesTo;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDScopeService"/> class.
    /// </summary>
    public GDScopeService(GetReferencesToDelegate? getReferencesTo = null)
    {
        _getReferencesTo = getReferencesTo;
    }

    /// <summary>
    /// Gets the scope type where the symbol was declared.
    /// Inferred from the declaration node type.
    /// </summary>
    public GDScopeType? GetDeclarationScopeType(GDSymbolInfo symbol)
    {
        if (symbol?.DeclarationNode == null)
            return null;

        return symbol.DeclarationNode switch
        {
            // Class-level declarations
            GDMethodDeclaration => GDScopeType.Class,
            GDVariableDeclaration => GDScopeType.Class,
            GDSignalDeclaration => GDScopeType.Class,
            GDEnumDeclaration => GDScopeType.Class,
            GDEnumValueDeclaration => GDScopeType.Class,
            GDInnerClassDeclaration => GDScopeType.Class,

            // Method-level declarations
            GDVariableDeclarationStatement => GDScopeType.Method,
            GDParameterDeclaration => GDScopeType.Method,

            // Loop-level declarations
            GDForStatement => GDScopeType.ForLoop,

            // Match case declarations
            GDMatchCaseVariableExpression => GDScopeType.Match,

            // Lambda declarations (parameters)
            GDMethodExpression => GDScopeType.Lambda,

            _ => null
        };
    }

    /// <summary>
    /// Gets references to a symbol filtered by scope type.
    /// </summary>
    public IEnumerable<GDReference> GetReferencesInScope(GDSymbolInfo symbol, GDScopeType scopeType)
    {
        var refs = _getReferencesTo?.Invoke(symbol) ?? System.Array.Empty<GDReference>();
        return refs.Where(r => r.Scope?.Type == scopeType);
    }

    /// <summary>
    /// Gets references to a symbol filtered by multiple scope types.
    /// </summary>
    public IEnumerable<GDReference> GetReferencesInScopes(GDSymbolInfo symbol, params GDScopeType[] scopeTypes)
    {
        var refs = _getReferencesTo?.Invoke(symbol) ?? System.Array.Empty<GDReference>();

        if (scopeTypes == null || scopeTypes.Length == 0)
            return refs;

        var scopeSet = new HashSet<GDScopeType>(scopeTypes);
        return refs.Where(r => r.Scope != null && scopeSet.Contains(r.Scope.Type));
    }

    /// <summary>
    /// Gets references only within method/lambda scope (local references).
    /// This includes Method, Lambda, ForLoop, WhileLoop, Conditional, Match, and Block scopes.
    /// </summary>
    public IEnumerable<GDReference> GetLocalReferences(GDSymbolInfo symbol)
    {
        var refs = _getReferencesTo?.Invoke(symbol) ?? System.Array.Empty<GDReference>();
        return refs.Where(r => IsLocalScope(r.Scope?.Type));
    }

    /// <summary>
    /// Determines if a symbol is a local variable (declared in method/lambda scope).
    /// Local symbols include: local variables, parameters, for-loop iterators, match case variables.
    /// </summary>
    public bool IsLocalSymbol(GDSymbolInfo symbol)
    {
        if (symbol == null)
            return false;

        // Check by symbol kind first
        switch (symbol.Kind)
        {
            case GDSymbolKind.Parameter:
            case GDSymbolKind.Iterator:
                return true;
            case GDSymbolKind.Method:
            case GDSymbolKind.Signal:
            case GDSymbolKind.Enum:
            case GDSymbolKind.EnumValue:
            case GDSymbolKind.Class:
            case GDSymbolKind.Constant when IsClassLevelDeclaration(symbol.DeclarationNode):
                return false;
        }

        // For variables, check declaration type
        var scopeType = GetDeclarationScopeType(symbol);
        return scopeType != null && IsLocalScope(scopeType.Value);
    }

    /// <summary>
    /// Determines if a symbol is a class member (declared in class scope).
    /// Class members include: methods, signals, class-level variables, constants, enums, inner classes.
    /// </summary>
    public bool IsClassMember(GDSymbolInfo symbol)
    {
        if (symbol == null)
            return false;

        // Check by symbol kind first
        switch (symbol.Kind)
        {
            case GDSymbolKind.Method:
            case GDSymbolKind.Signal:
            case GDSymbolKind.Enum:
            case GDSymbolKind.EnumValue:
            case GDSymbolKind.Class:
                return true;
            case GDSymbolKind.Parameter:
            case GDSymbolKind.Iterator:
                return false;
        }

        // For variables/constants, check declaration type
        var scopeType = GetDeclarationScopeType(symbol);
        return scopeType == GDScopeType.Class || scopeType == GDScopeType.Global;
    }

    /// <summary>
    /// Checks if the scope type is a local (non-class) scope.
    /// </summary>
    public static bool IsLocalScope(GDScopeType? scopeType)
    {
        if (scopeType == null)
            return false;

        return scopeType.Value switch
        {
            GDScopeType.Method => true,
            GDScopeType.Lambda => true,
            GDScopeType.ForLoop => true,
            GDScopeType.WhileLoop => true,
            GDScopeType.Conditional => true,
            GDScopeType.Match => true,
            GDScopeType.Block => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if the declaration is at class level.
    /// </summary>
    public static bool IsClassLevelDeclaration(GDNode? declaration)
    {
        return declaration is GDVariableDeclaration or
            GDMethodDeclaration or
            GDSignalDeclaration or
            GDEnumDeclaration or
            GDInnerClassDeclaration;
    }

    /// <summary>
    /// Gets the enclosing method/lambda scope for a reference.
    /// </summary>
    public GDScope? GetEnclosingMethodScope(GDReference reference)
    {
        var scope = reference?.Scope;
        while (scope != null)
        {
            if (scope.Type == GDScopeType.Method || scope.Type == GDScopeType.Lambda)
                return scope;
            scope = scope.Parent;
        }
        return null;
    }

    /// <summary>
    /// Gets references within the same method/lambda as the symbol's declaration.
    /// For local symbols, this returns references in the declaring method only.
    /// For class members, this returns all references.
    /// </summary>
    public IEnumerable<GDReference> GetReferencesInDeclaringScope(GDSymbolInfo symbol)
    {
        var refs = _getReferencesTo?.Invoke(symbol) ?? System.Array.Empty<GDReference>();

        if (!IsLocalSymbol(symbol))
        {
            // Class members can be referenced from any method
            return refs;
        }

        // For local symbols, find the declaring method node
        var declaringMethodNode = GetDeclaringMethodNode(symbol);
        if (declaringMethodNode == null)
            return refs;

        // Filter references to those in the same method
        return refs.Where(r =>
        {
            var enclosingMethod = GetEnclosingMethodScope(r);
            return enclosingMethod?.Node == declaringMethodNode;
        });
    }

    /// <summary>
    /// Gets the method/lambda node that contains the symbol declaration.
    /// </summary>
    public GDNode? GetDeclaringMethodNode(GDSymbolInfo symbol)
    {
        if (symbol?.DeclarationNode == null)
            return null;

        // Walk up the AST to find the enclosing method
        var node = symbol.DeclarationNode;
        while (node != null)
        {
            if (node is GDMethodDeclaration or GDMethodExpression)
                return node;
            node = node.Parent as GDNode;
        }
        return null;
    }
}
