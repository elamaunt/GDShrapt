using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Resolves duck types to possible concrete types using runtime provider.
/// Moved from Abstractions to keep logic in Semantics layer.
/// </summary>
internal class GDDuckTypeResolver
{
    private readonly IGDRuntimeProvider _runtimeProvider;

    public GDDuckTypeResolver(IGDRuntimeProvider runtimeProvider)
    {
        _runtimeProvider = runtimeProvider;
    }

    /// <summary>
    /// Finds all known types that satisfy the duck type requirements.
    /// </summary>
    public IEnumerable<string> FindCompatibleTypes(GDDuckType duckType)
    {
        foreach (var typeName in _runtimeProvider.GetAllTypes())
        {
            if (IsCompatibleWith(duckType, typeName))
                yield return typeName;
        }
    }

    /// <summary>
    /// Checks if a concrete type satisfies duck type requirements.
    /// </summary>
    public bool IsCompatibleWith(GDDuckType duckType, string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return true; // Unknown type is potentially compatible

        if (duckType == null)
            return true;

        // Check excluded types
        if (duckType.ExcludedTypes.Any(t => t.DisplayName == typeName))
            return false;

        // Check if type is in possible types (if any defined)
        if (duckType.PossibleTypes.Count > 0)
        {
            var matchesPossible = duckType.PossibleTypes.Any(pt =>
                pt.DisplayName == typeName || _runtimeProvider.IsAssignableTo(typeName, pt.DisplayName));
            if (!matchesPossible)
                return false;
        }

        // Check required methods
        foreach (var method in duckType.RequiredMethods.Keys)
        {
            var member = _runtimeProvider.GetMember(typeName, method);
            if (member == null || member.Kind != GDRuntimeMemberKind.Method)
                return false;
        }

        // Check required properties
        foreach (var prop in duckType.RequiredProperties.Keys)
        {
            var member = _runtimeProvider.GetMember(typeName, prop);
            if (member == null || member.Kind != GDRuntimeMemberKind.Property)
                return false;
        }

        // Check required signals
        foreach (var signal in duckType.RequiredSignals)
        {
            var member = _runtimeProvider.GetMember(typeName, signal);
            if (member == null || member.Kind != GDRuntimeMemberKind.Signal)
                return false;
        }

        // Check required operators
        foreach (var kv in duckType.RequiredOperators)
        {
            if (!TypeSupportsOperator(typeName, kv.Key, kv.Value))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a type supports a specific operator with given operand types.
    /// </summary>
    private bool TypeSupportsOperator(string typeName, GDDualOperatorType op, IReadOnlyList<GDSemanticType> operandTypes)
    {
        // Convert operator to string name for runtime provider
        var operatorName = ConvertOperatorToString(op);
        if (operatorName == null)
            return true; // Unknown operator, assume compatible

        // Get types that support this operator from runtime provider
        var supportingTypes = _runtimeProvider.GetTypesWithOperator(operatorName);
        if (supportingTypes == null || supportingTypes.Count == 0)
            return true; // No info about operator, assume compatible

        // Check if the type (or a base type) supports this operator
        return supportingTypes.Contains(typeName) ||
               supportingTypes.Any(st => _runtimeProvider.IsAssignableTo(typeName, st));
    }

    /// <summary>
    /// Converts GDDualOperatorType to string operator name for runtime provider.
    /// </summary>
    private static string? ConvertOperatorToString(GDDualOperatorType op)
    {
        return op switch
        {
            GDDualOperatorType.Addition => "+",
            GDDualOperatorType.Subtraction => "-",
            GDDualOperatorType.Multiply => "*",
            GDDualOperatorType.Division => "/",
            GDDualOperatorType.Mod => "%",
            GDDualOperatorType.Power => "**",
            GDDualOperatorType.BitwiseAnd => "&",
            GDDualOperatorType.BitwiseOr => "|",
            GDDualOperatorType.Xor => "^",
            GDDualOperatorType.BitShiftLeft => "<<",
            GDDualOperatorType.BitShiftRight => ">>",
            _ => null
        };
    }
}
