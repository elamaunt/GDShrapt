using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for nullability analysis.
/// Provides methods to check if variables can be null at specific locations.
/// </summary>
internal class GDNullabilityService
{
    private readonly IGDRuntimeProvider? _runtimeProvider;

    /// <summary>
    /// Delegate for finding symbol by name.
    /// </summary>
    public delegate GDSymbolInfo? FindSymbolDelegate(string name);

    /// <summary>
    /// Delegate for finding symbols by name.
    /// </summary>
    public delegate IReadOnlyList<GDSymbolInfo> FindSymbolsDelegate(string name);

    /// <summary>
    /// Delegate for getting flow state at location.
    /// </summary>
    public delegate GDFlowState? GetFlowStateDelegate(GDNode atLocation);

    /// <summary>
    /// Delegate for checking if variable is inherited property.
    /// </summary>
    public delegate bool IsInheritedPropertyDelegate(string name);

    private readonly FindSymbolDelegate? _findSymbol;
    private readonly FindSymbolsDelegate? _findSymbols;
    private readonly GetFlowStateDelegate? _getFlowState;
    private readonly IsInheritedPropertyDelegate? _isInheritedProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDNullabilityService"/> class.
    /// </summary>
    public GDNullabilityService(
        IGDRuntimeProvider? runtimeProvider,
        FindSymbolDelegate? findSymbol = null,
        FindSymbolsDelegate? findSymbols = null,
        GetFlowStateDelegate? getFlowState = null,
        IsInheritedPropertyDelegate? isInheritedProperty = null)
    {
        _runtimeProvider = runtimeProvider;
        _findSymbol = findSymbol;
        _findSymbols = findSymbols;
        _getFlowState = getFlowState;
        _isInheritedProperty = isInheritedProperty;
    }

    /// <summary>
    /// Checks if a variable is potentially null at a given location.
    /// </summary>
    public bool IsVariablePotentiallyNull(string variableName, GDNode atLocation)
    {
        if (string.IsNullOrEmpty(variableName) || atLocation == null)
            return true; // Assume potentially null if unknown

        // Check if this is an enum type access (enums are never null)
        if (IsEnumType(variableName))
            return false;

        // Check if this is a class/type name (for static method calls like ClassName.new())
        if (IsClassName(variableName))
            return false;

        // Check if this is a built-in value type (Vector2, Vector3, etc.)
        if (IsBuiltInValueType(variableName))
            return false;

        // Check if inherited property from base class
        if (_isInheritedProperty?.Invoke(variableName) == true)
            return false;

        // Check if this is a signal (signals are never null)
        var symbol = _findSymbol?.Invoke(variableName);
        if (symbol != null)
        {
            if (symbol.Kind == GDSymbolKind.Signal)
                return false;

            if (symbol.Kind == GDSymbolKind.Variable || symbol.Kind == GDSymbolKind.Property)
            {
                if (HasNonNullInitializer(symbol))
                    return false;
            }
        }

        // Use flow analysis
        var state = _getFlowState?.Invoke(atLocation);
        if (state == null)
            return true;

        return state.IsVariablePotentiallyNull(variableName, _runtimeProvider);
    }

    /// <summary>
    /// Checks if a type name represents an enum type.
    /// </summary>
    public bool IsEnumType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        var symbols = _findSymbols?.Invoke(typeName);
        return symbols?.Any(s => s.Kind == GDSymbolKind.Enum) ?? false;
    }

    /// <summary>
    /// Checks if a name represents a class/type name.
    /// </summary>
    public bool IsClassName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var symbols = _findSymbols?.Invoke(name);
        if (symbols?.Any(s => s.Kind == GDSymbolKind.Class) == true)
            return true;

        if (_runtimeProvider?.IsKnownType(name) == true)
            return true;

        if (_runtimeProvider?.GetGlobalClass(name) != null)
            return true;

        return false;
    }

    /// <summary>
    /// Checks if a name represents a built-in value type.
    /// </summary>
    public static bool IsBuiltInValueType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        return typeName is "Vector2" or "Vector2i" or "Vector3" or "Vector3i" or "Vector4" or "Vector4i"
            or "Color" or "Rect2" or "Rect2i" or "Transform2D" or "Transform3D"
            or "Basis" or "Quaternion" or "Plane" or "AABB" or "Projection"
            or "int" or "float" or "bool" or "String" or "StringName" or "RID" or "Callable" or "Signal";
    }

    /// <summary>
    /// Checks if a symbol has a non-null initializer.
    /// </summary>
    public static bool HasNonNullInitializer(GDSymbolInfo symbol)
    {
        if (symbol.DeclarationNode is GDVariableDeclaration varDecl)
        {
            var initializer = varDecl.Initializer;
            if (initializer == null)
                return false;

            if (initializer is GDIdentifierExpression nullIdent && nullIdent.Identifier?.Sequence == "null")
                return false;

            return true;
        }

        return false;
    }
}
