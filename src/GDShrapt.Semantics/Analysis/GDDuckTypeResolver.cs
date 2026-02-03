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
        if (duckType.ExcludedTypes.Contains(typeName))
            return false;

        // Check if type is in possible types (if any defined)
        if (duckType.PossibleTypes.Count > 0)
        {
            var matchesPossible = duckType.PossibleTypes.Any(pt =>
                pt == typeName || _runtimeProvider.IsAssignableTo(typeName, pt));
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
    private bool TypeSupportsOperator(string typeName, GDDualOperatorType op, IReadOnlyList<string> operandTypes)
    {
        // Get the set of types that support this operator
        var supportingTypes = GetTypesForOperator(op);
        if (supportingTypes == null || supportingTypes.Count == 0)
            return true; // Unknown operator, assume compatible

        // Check if the type (or a base type) supports this operator
        return supportingTypes.Contains(typeName) ||
               supportingTypes.Any(st => _runtimeProvider.IsAssignableTo(typeName, st));
    }

    /// <summary>
    /// Gets the set of types that support a specific operator.
    /// </summary>
    private static HashSet<string>? GetTypesForOperator(GDDualOperatorType op)
    {
        switch (op)
        {
            case GDDualOperatorType.Addition:
                // Types that support +: int, float, String, StringName, Vector*, Color, Array, PackedArray types
                return new HashSet<string>
                {
                    "int", "float", "String", "StringName",
                    "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
                    "Color", "Array",
                    "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
                    "PackedFloat32Array", "PackedFloat64Array",
                    "PackedStringArray", "PackedVector2Array", "PackedVector3Array",
                    "PackedColorArray"
                };

            case GDDualOperatorType.Subtraction:
                // Types that support -: int, float, Vector*, Color
                return new HashSet<string>
                {
                    "int", "float",
                    "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
                    "Color"
                };

            case GDDualOperatorType.Multiply:
                // Types that support *: int, float, Vector*, Color, Quaternion, Basis, Transform*
                return new HashSet<string>
                {
                    "int", "float",
                    "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
                    "Color", "Quaternion", "Basis",
                    "Transform2D", "Transform3D"
                };

            case GDDualOperatorType.Division:
                // Types that support /: int, float, Vector*, Color
                return new HashSet<string>
                {
                    "int", "float",
                    "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
                    "Color"
                };

            case GDDualOperatorType.Mod:
                // Types that support %: int, float, Vector*
                return new HashSet<string>
                {
                    "int", "float",
                    "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i"
                };

            default:
                // Unknown operator - assume any type can work
                return null;
        }
    }
}
