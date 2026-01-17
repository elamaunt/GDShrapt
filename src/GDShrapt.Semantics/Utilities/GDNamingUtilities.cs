using System;
using System.Collections.Generic;
using System.Text;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Shared utilities for GDScript identifier naming, validation, and conversion.
/// Consolidates common naming logic used across refactoring services.
/// </summary>
public static class GDNamingUtilities
{
    /// <summary>
    /// Reserved GDScript keywords that cannot be used as identifiers.
    /// </summary>
    public static IReadOnlySet<string> ReservedKeywords { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        // Control flow
        "if", "elif", "else", "for", "while", "match", "break", "continue", "pass", "return", "await",
        // Declarations
        "class", "class_name", "extends", "func", "signal", "var", "const", "enum", "static",
        // Operators and values
        "and", "or", "not", "in", "is", "as", "true", "false", "null", "self", "super",
        // Other
        "preload", "assert", "breakpoint", "tool", "export", "onready", "setget", "master", "puppet",
        "remote", "sync", "remotesync", "mastersync", "puppetsync", "void"
    };

    /// <summary>
    /// Built-in type names that should not be used as variable identifiers.
    /// </summary>
    public static IReadOnlySet<string> BuiltInTypes { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "Array", "Dictionary", "String", "Vector2", "Vector3", "Vector4",
        "Color", "Rect2", "Transform2D", "Transform3D", "Basis", "Quaternion",
        "AABB", "Plane", "NodePath", "RID", "Object", "Callable", "Signal",
        "StringName", "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
        "PackedFloat32Array", "PackedFloat64Array", "PackedStringArray",
        "PackedVector2Array", "PackedVector3Array", "PackedColorArray",
        "int", "float", "bool", "Variant", "void",
        // Node types
        "Node", "Node2D", "Node3D", "Control", "Sprite2D", "Sprite3D",
        "Camera2D", "Camera3D", "AudioStreamPlayer", "Area2D", "Area3D",
        "CharacterBody2D", "CharacterBody3D", "RigidBody2D", "RigidBody3D",
        "StaticBody2D", "StaticBody3D", "CollisionShape2D", "CollisionShape3D",
        "AnimationPlayer", "Timer", "Label", "Button", "TextureRect"
    };

    /// <summary>
    /// Validates that a name is a valid GDScript identifier.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <param name="errorMessage">Error message if invalid, null if valid.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool ValidateIdentifier(string name, out string? errorMessage)
    {
        if (string.IsNullOrEmpty(name))
        {
            errorMessage = "Identifier cannot be empty";
            return false;
        }

        // Must start with letter or underscore
        if (!char.IsLetter(name[0]) && name[0] != '_')
        {
            errorMessage = "Identifier must start with a letter or underscore";
            return false;
        }

        // Rest must be letters, digits, or underscores
        for (int i = 1; i < name.Length; i++)
        {
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
            {
                errorMessage = $"Invalid character '{name[i]}' in identifier";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Checks if a name is a reserved GDScript keyword.
    /// </summary>
    public static bool IsReservedKeyword(string name)
    {
        return ReservedKeywords.Contains(name);
    }

    /// <summary>
    /// Checks if a name is a built-in type name.
    /// </summary>
    public static bool IsBuiltInType(string name)
    {
        return BuiltInTypes.Contains(name);
    }

    /// <summary>
    /// Converts a PascalCase or camelCase name to snake_case.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The snake_case version of the name.</returns>
    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "value";

        var result = new StringBuilder();
        bool previousWasUpper = false;
        bool previousWasUnderscore = true; // Treat start as after underscore

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                // Add underscore before uppercase if:
                // - Not at start
                // - Previous was not uppercase (to handle acronyms like HTTPClient -> http_client)
                // - Previous was not underscore
                if (i > 0 && !previousWasUnderscore && !previousWasUpper)
                {
                    result.Append('_');
                }
                // Handle end of acronym: XMLParser -> xml_parser
                else if (i > 0 && previousWasUpper && i + 1 < name.Length && char.IsLower(name[i + 1]))
                {
                    result.Append('_');
                }

                result.Append(char.ToLowerInvariant(c));
                previousWasUpper = true;
                previousWasUnderscore = false;
            }
            else if (c == '_')
            {
                result.Append(c);
                previousWasUpper = false;
                previousWasUnderscore = true;
            }
            else if (char.IsLetterOrDigit(c))
            {
                result.Append(char.ToLowerInvariant(c));
                previousWasUpper = false;
                previousWasUnderscore = false;
            }
            // Skip other characters
        }

        var normalized = result.ToString().TrimStart('_');
        if (string.IsNullOrEmpty(normalized) || char.IsDigit(normalized[0]))
            return "value";

        return normalized;
    }

    /// <summary>
    /// Converts a snake_case name to PascalCase.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The PascalCase version of the name.</returns>
    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Value";

        var result = new StringBuilder();
        bool capitalizeNext = true;

        foreach (var c in name)
        {
            if (c == '_')
            {
                capitalizeNext = true;
            }
            else if (char.IsLetterOrDigit(c))
            {
                result.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
                capitalizeNext = false;
            }
        }

        var normalized = result.ToString();
        if (string.IsNullOrEmpty(normalized) || char.IsDigit(normalized[0]))
            return "Value";

        return normalized;
    }

    /// <summary>
    /// Converts a name to SCREAMING_SNAKE_CASE (for constants).
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The SCREAMING_SNAKE_CASE version of the name.</returns>
    public static string ToScreamingSnakeCase(string name)
    {
        var snakeCase = ToSnakeCase(name);
        return snakeCase.ToUpperInvariant();
    }

    /// <summary>
    /// Normalizes a variable name to valid snake_case format.
    /// </summary>
    /// <param name="name">The name to normalize.</param>
    /// <returns>A valid snake_case variable name.</returns>
    public static string NormalizeVariableName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "new_variable";

        // Convert to snake_case
        var result = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                result.Append(char.ToLowerInvariant(c));
            }
            else if (char.IsWhiteSpace(c))
            {
                result.Append('_');
            }
        }

        var normalized = result.ToString();
        if (string.IsNullOrEmpty(normalized) || char.IsDigit(normalized[0]))
            return "new_variable";

        return normalized;
    }

    /// <summary>
    /// Normalizes a constant name to valid SCREAMING_SNAKE_CASE format.
    /// </summary>
    /// <param name="name">The name to normalize.</param>
    /// <returns>A valid constant name.</returns>
    public static string NormalizeConstantName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "NEW_CONSTANT";

        return ToScreamingSnakeCase(name);
    }

    /// <summary>
    /// Suggests a variable name based on an expression.
    /// </summary>
    /// <param name="expression">The expression to derive a name from.</param>
    /// <returns>A suggested variable name.</returns>
    public static string SuggestVariableName(GDExpression? expression)
    {
        if (expression == null)
            return "value";

        switch (expression)
        {
            case GDCallExpression call:
                // get_player() -> player, load_scene() -> scene
                var methodName = GetMethodName(call);
                if (!string.IsNullOrEmpty(methodName))
                {
                    // Remove common prefixes
                    if (methodName.StartsWith("get_", StringComparison.OrdinalIgnoreCase))
                        return methodName.Substring(4);
                    if (methodName.StartsWith("load_", StringComparison.OrdinalIgnoreCase))
                        return methodName.Substring(5);
                    if (methodName.StartsWith("create_", StringComparison.OrdinalIgnoreCase))
                        return methodName.Substring(7);
                    if (methodName.StartsWith("find_", StringComparison.OrdinalIgnoreCase))
                        return methodName.Substring(5);
                    return methodName;
                }
                break;

            case GDMemberOperatorExpression memberOp:
                // obj.property -> property
                var memberName = memberOp.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(memberName))
                    return ToSnakeCase(memberName);
                break;

            case GDIdentifierExpression ident:
                // Existing identifier
                var identName = ident.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(identName))
                    return identName;
                break;

            case GDNumberExpression:
                return "number";

            case GDStringExpression:
                return "text";

            case GDBoolExpression:
                return "flag";

            case GDArrayInitializerExpression:
                return "items";

            case GDDictionaryInitializerExpression:
                return "data";
        }

        return "value";
    }

    /// <summary>
    /// Suggests a constant name based on an expression.
    /// </summary>
    /// <param name="expression">The expression to derive a name from.</param>
    /// <returns>A suggested constant name in SCREAMING_SNAKE_CASE.</returns>
    public static string SuggestConstantName(GDExpression? expression)
    {
        var baseName = SuggestVariableName(expression);
        return ToScreamingSnakeCase(baseName);
    }

    /// <summary>
    /// Suggests a variable name from a node path (for @onready declarations).
    /// </summary>
    /// <param name="nodePath">The node path string (e.g., "Player/Camera2D").</param>
    /// <returns>A suggested variable name.</returns>
    public static string SuggestVariableFromNodePath(string nodePath)
    {
        if (string.IsNullOrEmpty(nodePath))
            return "node";

        // Get the last part of the path
        var lastSlash = nodePath.LastIndexOf('/');
        var nodeName = lastSlash >= 0 ? nodePath.Substring(lastSlash + 1) : nodePath;

        // Remove @ prefix if present
        if (nodeName.StartsWith("@"))
            nodeName = nodeName.Substring(1);

        // Convert to snake_case
        return ToSnakeCase(nodeName);
    }

    /// <summary>
    /// Generates a unique name by appending a number suffix.
    /// </summary>
    /// <param name="baseName">The base name to make unique.</param>
    /// <param name="existingNames">Set of names that already exist.</param>
    /// <returns>A unique name.</returns>
    public static string GenerateUniqueName(string baseName, ISet<string> existingNames)
    {
        if (!existingNames.Contains(baseName))
            return baseName;

        int suffix = 1;
        string candidate;
        do
        {
            candidate = $"{baseName}_{suffix}";
            suffix++;
        }
        while (existingNames.Contains(candidate));

        return candidate;
    }

    #region Private Helpers

    private static string? GetMethodName(GDCallExpression call)
    {
        if (call.CallerExpression is GDIdentifierExpression ident)
        {
            return ident.Identifier?.Sequence;
        }
        if (call.CallerExpression is GDMemberOperatorExpression member)
        {
            return member.Identifier?.Sequence;
        }
        return null;
    }

    #endregion
}
