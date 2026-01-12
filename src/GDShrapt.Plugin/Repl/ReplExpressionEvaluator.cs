using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using GDShrapt.Reader;

namespace GDShrapt.Plugin;

/// <summary>
/// Evaluates parsed GDScript expressions by converting them to Godot API calls.
/// </summary>
internal class ReplExpressionEvaluator
{
    /// <summary>
    /// Evaluates an expression in the context of a target GodotObject.
    /// </summary>
    public ReplResult Evaluate(GDExpression expression, GodotObject target)
    {
        if (expression == null)
            return ReplResult.Error("Expression is null");

        if (target == null)
            return ReplResult.Error("Target node is null");

        try
        {
            var result = EvaluateExpression(expression, target);
            return ReplResult.WithValue(result);
        }
        catch (Exception ex)
        {
            return ReplResult.Error(ex.Message);
        }
    }

    private Variant EvaluateExpression(GDExpression expression, GodotObject context)
    {
        return expression switch
        {
            // Literals
            GDNumberExpression numExpr => EvaluateNumber(numExpr),
            GDStringExpression strExpr => EvaluateString(strExpr),
            GDBoolExpression boolExpr => EvaluateBool(boolExpr),

            // Identifiers (property access on context)
            GDIdentifierExpression idExpr => EvaluateIdentifier(idExpr, context),

            // Member access (object.property)
            GDMemberOperatorExpression memberExpr => EvaluateMemberAccess(memberExpr, context),

            // Method calls
            GDCallExpression callExpr => EvaluateCall(callExpr, context),

            // Indexer (array[index])
            GDIndexerExpression indexerExpr => EvaluateIndexer(indexerExpr, context),

            // Binary operators
            GDDualOperatorExpression dualExpr => EvaluateDualOperator(dualExpr, context),

            // Unary operators
            GDSingleOperatorExpression singleExpr => EvaluateSingleOperator(singleExpr, context),

            // Brackets (parentheses)
            GDBracketExpression bracketExpr => EvaluateBracket(bracketExpr, context),

            // Array initializer
            GDArrayInitializerExpression arrayExpr => EvaluateArrayInitializer(arrayExpr, context),

            // Dictionary initializer
            GDDictionaryInitializerExpression dictExpr => EvaluateDictionaryInitializer(dictExpr, context),

            // If expression (ternary)
            GDIfExpression ifExpr => EvaluateIfExpression(ifExpr, context),

            _ => throw new NotSupportedException($"Expression type '{expression.GetType().Name}' is not supported in REPL")
        };
    }

    private Variant EvaluateNumber(GDNumberExpression numExpr)
    {
        var sequence = numExpr.Number?.Sequence;
        if (string.IsNullOrEmpty(sequence))
            return 0;

        // Handle hex, binary, and float formats
        if (sequence.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.ToInt64(sequence.Substring(2), 16);
        }
        if (sequence.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.ToInt64(sequence.Substring(2), 2);
        }

        // Remove underscores (GDScript allows them as separators)
        sequence = sequence.Replace("_", "");

        if (sequence.Contains('.') || sequence.Contains('e') || sequence.Contains('E'))
        {
            if (double.TryParse(sequence, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;
        }

        if (long.TryParse(sequence, out var l))
            return l;

        if (double.TryParse(sequence, NumberStyles.Float, CultureInfo.InvariantCulture, out var fallback))
            return fallback;

        return 0;
    }

    private Variant EvaluateString(GDStringExpression strExpr)
    {
        // GDStringNode.EscapedSequence contains the string with escape sequences resolved
        return strExpr.String?.EscapedSequence ?? "";
    }

    private Variant EvaluateBool(GDBoolExpression boolExpr)
    {
        return boolExpr.Value ?? false;
    }

    private Variant EvaluateIdentifier(GDIdentifierExpression idExpr, GodotObject context)
    {
        var name = idExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            throw new Exception("Empty identifier");

        // Check for built-in constants
        switch (name)
        {
            case "null":
                return default;
            case "true":
                return true;
            case "false":
                return false;
            case "PI":
                return Math.PI;
            case "TAU":
                return Math.PI * 2;
            case "INF":
                return double.PositiveInfinity;
            case "NAN":
                return double.NaN;
        }

        // Try to get property from context
        return context.Get(name);
    }

    private Variant EvaluateMemberAccess(GDMemberOperatorExpression memberExpr, GodotObject context)
    {
        var memberName = memberExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(memberName))
            throw new Exception("Empty member name");

        // Evaluate the caller expression
        var callerResult = EvaluateExpression(memberExpr.CallerExpression, context);

        // Handle struct types (Vector2, Vector3, Color, etc.)
        return GetMember(callerResult, memberName);
    }

    private Variant GetMember(Variant obj, string memberName)
    {
        // Handle Godot structs
        switch (obj.VariantType)
        {
            case Variant.Type.Vector2:
                var v2 = obj.AsVector2();
                return memberName switch
                {
                    "x" => v2.X,
                    "y" => v2.Y,
                    _ => throw new Exception($"Vector2 has no member '{memberName}'")
                };

            case Variant.Type.Vector3:
                var v3 = obj.AsVector3();
                return memberName switch
                {
                    "x" => v3.X,
                    "y" => v3.Y,
                    "z" => v3.Z,
                    _ => throw new Exception($"Vector3 has no member '{memberName}'")
                };

            case Variant.Type.Vector4:
                var v4 = obj.AsVector4();
                return memberName switch
                {
                    "x" => v4.X,
                    "y" => v4.Y,
                    "z" => v4.Z,
                    "w" => v4.W,
                    _ => throw new Exception($"Vector4 has no member '{memberName}'")
                };

            case Variant.Type.Color:
                var color = obj.AsColor();
                return memberName switch
                {
                    "r" => color.R,
                    "g" => color.G,
                    "b" => color.B,
                    "a" => color.A,
                    "r8" => (int)(color.R * 255),
                    "g8" => (int)(color.G * 255),
                    "b8" => (int)(color.B * 255),
                    "a8" => (int)(color.A * 255),
                    "h" => color.H,
                    "s" => color.S,
                    "v" => color.V,
                    _ => throw new Exception($"Color has no member '{memberName}'")
                };

            case Variant.Type.Rect2:
                var rect = obj.AsRect2();
                return memberName switch
                {
                    "position" => rect.Position,
                    "size" => rect.Size,
                    "end" => rect.End,
                    _ => throw new Exception($"Rect2 has no member '{memberName}'")
                };

            case Variant.Type.Transform2D:
                var t2d = obj.AsTransform2D();
                return memberName switch
                {
                    "origin" => t2d.Origin,
                    "x" => t2d.X,
                    "y" => t2d.Y,
                    _ => throw new Exception($"Transform2D has no member '{memberName}'")
                };

            case Variant.Type.Transform3D:
                var t3d = obj.AsTransform3D();
                return memberName switch
                {
                    "origin" => t3d.Origin,
                    "basis" => t3d.Basis,
                    _ => throw new Exception($"Transform3D has no member '{memberName}'")
                };

            case Variant.Type.Object:
                var godotObj = obj.AsGodotObject();
                if (godotObj != null)
                    return godotObj.Get(memberName);
                throw new Exception("Object is null");

            case Variant.Type.Dictionary:
                var dict = obj.AsGodotDictionary();
                if (dict.ContainsKey(memberName))
                    return dict[memberName];
                throw new Exception($"Dictionary has no key '{memberName}'");

            default:
                throw new Exception($"Cannot access member '{memberName}' on type {obj.VariantType}");
        }
    }

    private Variant EvaluateCall(GDCallExpression callExpr, GodotObject context)
    {
        // Evaluate arguments
        var args = new List<Variant>();
        if (callExpr.Parameters != null)
        {
            foreach (var param in callExpr.Parameters)
            {
                if (param is GDExpression paramExpr)
                    args.Add(EvaluateExpression(paramExpr, context));
            }
        }

        // Get the method name and caller
        var caller = callExpr.CallerExpression;

        // Simple function call: func()
        if (caller is GDIdentifierExpression idExpr)
        {
            var methodName = idExpr.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                throw new Exception("Empty method name");

            // Built-in global functions
            var builtinResult = TryEvaluateBuiltinFunction(methodName, args);
            if (builtinResult.HasValue)
                return builtinResult.Value;

            // Call on context
            return CallMethod(context, methodName, args);
        }

        // Member method call: obj.method()
        if (caller is GDMemberOperatorExpression memberExpr)
        {
            var methodName = memberExpr.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                throw new Exception("Empty method name");

            var callerResult = EvaluateExpression(memberExpr.CallerExpression, context);

            // Call method on the result
            return CallMethodOnVariant(callerResult, methodName, args);
        }

        throw new Exception($"Cannot call expression of type '{caller?.GetType().Name}'");
    }

    private Variant? TryEvaluateBuiltinFunction(string name, List<Variant> args)
    {
        // Common GDScript built-in functions
        return name switch
        {
            "abs" when args.Count == 1 => Math.Abs(args[0].AsDouble()),
            "sign" when args.Count == 1 => Math.Sign(args[0].AsDouble()),
            "floor" when args.Count == 1 => Math.Floor(args[0].AsDouble()),
            "ceil" when args.Count == 1 => Math.Ceiling(args[0].AsDouble()),
            "round" when args.Count == 1 => Math.Round(args[0].AsDouble()),
            "sqrt" when args.Count == 1 => Math.Sqrt(args[0].AsDouble()),
            "pow" when args.Count == 2 => Math.Pow(args[0].AsDouble(), args[1].AsDouble()),
            "sin" when args.Count == 1 => Math.Sin(args[0].AsDouble()),
            "cos" when args.Count == 1 => Math.Cos(args[0].AsDouble()),
            "tan" when args.Count == 1 => Math.Tan(args[0].AsDouble()),
            "asin" when args.Count == 1 => Math.Asin(args[0].AsDouble()),
            "acos" when args.Count == 1 => Math.Acos(args[0].AsDouble()),
            "atan" when args.Count == 1 => Math.Atan(args[0].AsDouble()),
            "atan2" when args.Count == 2 => Math.Atan2(args[0].AsDouble(), args[1].AsDouble()),
            "min" when args.Count == 2 => Math.Min(args[0].AsDouble(), args[1].AsDouble()),
            "max" when args.Count == 2 => Math.Max(args[0].AsDouble(), args[1].AsDouble()),
            "clamp" when args.Count == 3 => Math.Clamp(args[0].AsDouble(), args[1].AsDouble(), args[2].AsDouble()),
            "lerp" when args.Count == 3 => args[0].AsDouble() + (args[1].AsDouble() - args[0].AsDouble()) * args[2].AsDouble(),
            "str" when args.Count >= 1 => string.Join("", args.ConvertAll(a => GD.VarToStr(a))),
            "len" when args.Count == 1 => GetLength(args[0]),
            "typeof" when args.Count == 1 => (int)args[0].VariantType,
            "type_string" when args.Count == 1 => args[0].VariantType.ToString(),
            "Vector2" when args.Count == 2 => new Vector2((float)args[0].AsDouble(), (float)args[1].AsDouble()),
            "Vector2" when args.Count == 0 => Vector2.Zero,
            "Vector3" when args.Count == 3 => new Vector3((float)args[0].AsDouble(), (float)args[1].AsDouble(), (float)args[2].AsDouble()),
            "Vector3" when args.Count == 0 => Vector3.Zero,
            "Color" when args.Count == 4 => new Color((float)args[0].AsDouble(), (float)args[1].AsDouble(), (float)args[2].AsDouble(), (float)args[3].AsDouble()),
            "Color" when args.Count == 3 => new Color((float)args[0].AsDouble(), (float)args[1].AsDouble(), (float)args[2].AsDouble()),
            "Color" when args.Count == 1 && args[0].VariantType == Variant.Type.String => new Color(args[0].AsString()),
            _ => null
        };
    }

    private long GetLength(Variant v)
    {
        return v.VariantType switch
        {
            Variant.Type.String => v.AsString().Length,
            Variant.Type.Array => v.AsGodotArray().Count,
            Variant.Type.Dictionary => v.AsGodotDictionary().Count,
            Variant.Type.PackedByteArray => v.AsByteArray().Length,
            Variant.Type.PackedInt32Array => v.AsInt32Array().Length,
            Variant.Type.PackedInt64Array => v.AsInt64Array().Length,
            Variant.Type.PackedFloat32Array => v.AsFloat32Array().Length,
            Variant.Type.PackedFloat64Array => v.AsFloat64Array().Length,
            Variant.Type.PackedStringArray => v.AsStringArray().Length,
            Variant.Type.PackedVector2Array => v.AsVector2Array().Length,
            Variant.Type.PackedVector3Array => v.AsVector3Array().Length,
            _ => throw new Exception($"Cannot get length of {v.VariantType}")
        };
    }

    private static Godot.Collections.Array CreateArrayFromStrings(string[] strings)
    {
        var array = new Godot.Collections.Array();
        foreach (var s in strings)
            array.Add(s);
        return array;
    }

    private Variant CallMethod(GodotObject obj, string methodName, List<Variant> args)
    {
        return obj.Call(methodName, args.ToArray());
    }

    private Variant CallMethodOnVariant(Variant obj, string methodName, List<Variant> args)
    {
        if (obj.VariantType == Variant.Type.Object)
        {
            var godotObj = obj.AsGodotObject();
            if (godotObj != null)
                return CallMethod(godotObj, methodName, args);
            throw new Exception("Object is null");
        }

        // Handle struct methods
        return CallStructMethod(obj, methodName, args);
    }

    private Variant CallStructMethod(Variant obj, string methodName, List<Variant> args)
    {
        switch (obj.VariantType)
        {
            case Variant.Type.Vector2:
                var v2 = obj.AsVector2();
                return methodName switch
                {
                    "length" => v2.Length(),
                    "length_squared" => v2.LengthSquared(),
                    "normalized" => v2.Normalized(),
                    "distance_to" when args.Count == 1 => v2.DistanceTo(args[0].AsVector2()),
                    "dot" when args.Count == 1 => v2.Dot(args[0].AsVector2()),
                    "angle" => v2.Angle(),
                    "angle_to" when args.Count == 1 => v2.AngleTo(args[0].AsVector2()),
                    "rotated" when args.Count == 1 => v2.Rotated((float)args[0].AsDouble()),
                    "lerp" when args.Count == 2 => v2.Lerp(args[0].AsVector2(), (float)args[1].AsDouble()),
                    "abs" => v2.Abs(),
                    "floor" => v2.Floor(),
                    "ceil" => v2.Ceil(),
                    "round" => v2.Round(),
                    _ => throw new Exception($"Vector2 has no method '{methodName}'")
                };

            case Variant.Type.Vector3:
                var v3 = obj.AsVector3();
                return methodName switch
                {
                    "length" => v3.Length(),
                    "length_squared" => v3.LengthSquared(),
                    "normalized" => v3.Normalized(),
                    "distance_to" when args.Count == 1 => v3.DistanceTo(args[0].AsVector3()),
                    "dot" when args.Count == 1 => v3.Dot(args[0].AsVector3()),
                    "cross" when args.Count == 1 => v3.Cross(args[0].AsVector3()),
                    "lerp" when args.Count == 2 => v3.Lerp(args[0].AsVector3(), (float)args[1].AsDouble()),
                    "abs" => v3.Abs(),
                    "floor" => v3.Floor(),
                    "ceil" => v3.Ceil(),
                    "round" => v3.Round(),
                    _ => throw new Exception($"Vector3 has no method '{methodName}'")
                };

            case Variant.Type.String:
                var str = obj.AsString();
                return methodName switch
                {
                    "length" => str.Length,
                    "to_upper" => str.ToUpper(),
                    "to_lower" => str.ToLower(),
                    "strip_edges" => str.Trim(),
                    "begins_with" when args.Count == 1 => str.StartsWith(args[0].AsString()),
                    "ends_with" when args.Count == 1 => str.EndsWith(args[0].AsString()),
                    "contains" when args.Count == 1 => str.Contains(args[0].AsString()),
                    "find" when args.Count == 1 => str.IndexOf(args[0].AsString()),
                    "replace" when args.Count == 2 => str.Replace(args[0].AsString(), args[1].AsString()),
                    "split" when args.Count == 1 => CreateArrayFromStrings(str.Split(args[0].AsString())),
                    "substr" when args.Count == 2 => str.Substring((int)args[0].AsInt64(), (int)args[1].AsInt64()),
                    "substr" when args.Count == 1 => str.Substring((int)args[0].AsInt64()),
                    _ => throw new Exception($"String has no method '{methodName}'")
                };

            case Variant.Type.Color:
                var color = obj.AsColor();
                return methodName switch
                {
                    "to_html" => color.ToHtml(),
                    "inverted" => color.Inverted(),
                    "lightened" when args.Count == 1 => color.Lightened((float)args[0].AsDouble()),
                    "darkened" when args.Count == 1 => color.Darkened((float)args[0].AsDouble()),
                    "lerp" when args.Count == 2 => color.Lerp(args[0].AsColor(), (float)args[1].AsDouble()),
                    _ => throw new Exception($"Color has no method '{methodName}'")
                };

            case Variant.Type.Array:
                var arr = obj.AsGodotArray();
                return methodName switch
                {
                    "size" => arr.Count,
                    "is_empty" => arr.Count == 0,
                    "front" => arr.Count > 0 ? arr[0] : default,
                    "back" => arr.Count > 0 ? arr[arr.Count - 1] : default,
                    "has" when args.Count == 1 => arr.Contains(args[0]),
                    "find" when args.Count == 1 => arr.IndexOf(args[0]),
                    _ => throw new Exception($"Array has no method '{methodName}'")
                };

            case Variant.Type.Dictionary:
                var dict = obj.AsGodotDictionary();
                return methodName switch
                {
                    "size" => dict.Count,
                    "is_empty" => dict.Count == 0,
                    "has" when args.Count == 1 => dict.ContainsKey(args[0]),
                    "keys" => new Godot.Collections.Array(dict.Keys),
                    "values" => new Godot.Collections.Array(dict.Values),
                    "get" when args.Count == 1 => dict.ContainsKey(args[0]) ? dict[args[0]] : default,
                    "get" when args.Count == 2 => dict.ContainsKey(args[0]) ? dict[args[0]] : args[1],
                    _ => throw new Exception($"Dictionary has no method '{methodName}'")
                };

            default:
                throw new Exception($"Cannot call method '{methodName}' on type {obj.VariantType}");
        }
    }

    private Variant EvaluateIndexer(GDIndexerExpression indexerExpr, GodotObject context)
    {
        var callerResult = EvaluateExpression(indexerExpr.CallerExpression, context);
        var indexResult = EvaluateExpression(indexerExpr.InnerExpression, context);

        return callerResult.VariantType switch
        {
            Variant.Type.Array => callerResult.AsGodotArray()[(int)indexResult.AsInt64()],
            Variant.Type.Dictionary => callerResult.AsGodotDictionary()[indexResult],
            Variant.Type.String => callerResult.AsString()[(int)indexResult.AsInt64()].ToString(),
            Variant.Type.PackedByteArray => callerResult.AsByteArray()[(int)indexResult.AsInt64()],
            Variant.Type.PackedInt32Array => callerResult.AsInt32Array()[(int)indexResult.AsInt64()],
            Variant.Type.PackedInt64Array => callerResult.AsInt64Array()[(int)indexResult.AsInt64()],
            Variant.Type.PackedFloat32Array => callerResult.AsFloat32Array()[(int)indexResult.AsInt64()],
            Variant.Type.PackedFloat64Array => callerResult.AsFloat64Array()[(int)indexResult.AsInt64()],
            Variant.Type.PackedStringArray => callerResult.AsStringArray()[(int)indexResult.AsInt64()],
            Variant.Type.PackedVector2Array => callerResult.AsVector2Array()[(int)indexResult.AsInt64()],
            Variant.Type.PackedVector3Array => callerResult.AsVector3Array()[(int)indexResult.AsInt64()],
            _ => throw new Exception($"Cannot index type {callerResult.VariantType}")
        };
    }

    private Variant EvaluateDualOperator(GDDualOperatorExpression dualExpr, GodotObject context)
    {
        var left = EvaluateExpression(dualExpr.LeftExpression, context);
        var right = EvaluateExpression(dualExpr.RightExpression, context);

        return dualExpr.OperatorType switch
        {
            // Arithmetic
            GDDualOperatorType.Addition => Add(left, right),
            GDDualOperatorType.Subtraction => Subtract(left, right),
            GDDualOperatorType.Multiply => Multiply(left, right),
            GDDualOperatorType.Division => Divide(left, right),
            GDDualOperatorType.Mod => left.AsDouble() % right.AsDouble(),
            GDDualOperatorType.Power => Math.Pow(left.AsDouble(), right.AsDouble()),

            // Comparison
            GDDualOperatorType.Equal => AreEqual(left, right),
            GDDualOperatorType.NotEqual => !AreEqual(left, right),
            GDDualOperatorType.LessThan => left.AsDouble() < right.AsDouble(),
            GDDualOperatorType.MoreThan => left.AsDouble() > right.AsDouble(),
            GDDualOperatorType.LessThanOrEqual => left.AsDouble() <= right.AsDouble(),
            GDDualOperatorType.MoreThanOrEqual => left.AsDouble() >= right.AsDouble(),

            // Logical
            GDDualOperatorType.And or GDDualOperatorType.And2 => left.AsBool() && right.AsBool(),
            GDDualOperatorType.Or or GDDualOperatorType.Or2 => left.AsBool() || right.AsBool(),

            // Bitwise
            GDDualOperatorType.BitwiseAnd => left.AsInt64() & right.AsInt64(),
            GDDualOperatorType.BitwiseOr => left.AsInt64() | right.AsInt64(),
            GDDualOperatorType.Xor => left.AsInt64() ^ right.AsInt64(),
            GDDualOperatorType.BitShiftLeft => left.AsInt64() << (int)right.AsInt64(),
            GDDualOperatorType.BitShiftRight => left.AsInt64() >> (int)right.AsInt64(),

            // Type checking
            GDDualOperatorType.Is => EvaluateIs(left, right),
            GDDualOperatorType.In => EvaluateIn(left, right),

            _ => throw new NotSupportedException($"Operator '{dualExpr.OperatorType}' is not supported")
        };
    }

    private Variant Add(Variant left, Variant right)
    {
        if (left.VariantType == Variant.Type.String || right.VariantType == Variant.Type.String)
            return GD.VarToStr(left) + GD.VarToStr(right);

        if (left.VariantType == Variant.Type.Vector2 && right.VariantType == Variant.Type.Vector2)
            return left.AsVector2() + right.AsVector2();

        if (left.VariantType == Variant.Type.Vector3 && right.VariantType == Variant.Type.Vector3)
            return left.AsVector3() + right.AsVector3();

        return left.AsDouble() + right.AsDouble();
    }

    private Variant Subtract(Variant left, Variant right)
    {
        if (left.VariantType == Variant.Type.Vector2 && right.VariantType == Variant.Type.Vector2)
            return left.AsVector2() - right.AsVector2();

        if (left.VariantType == Variant.Type.Vector3 && right.VariantType == Variant.Type.Vector3)
            return left.AsVector3() - right.AsVector3();

        return left.AsDouble() - right.AsDouble();
    }

    private Variant Multiply(Variant left, Variant right)
    {
        if (left.VariantType == Variant.Type.Vector2)
        {
            if (right.VariantType == Variant.Type.Vector2)
                return left.AsVector2() * right.AsVector2();
            return left.AsVector2() * (float)right.AsDouble();
        }

        if (left.VariantType == Variant.Type.Vector3)
        {
            if (right.VariantType == Variant.Type.Vector3)
                return left.AsVector3() * right.AsVector3();
            return left.AsVector3() * (float)right.AsDouble();
        }

        if (right.VariantType == Variant.Type.Vector2)
            return (float)left.AsDouble() * right.AsVector2();

        if (right.VariantType == Variant.Type.Vector3)
            return (float)left.AsDouble() * right.AsVector3();

        return left.AsDouble() * right.AsDouble();
    }

    private Variant Divide(Variant left, Variant right)
    {
        if (left.VariantType == Variant.Type.Vector2)
        {
            if (right.VariantType == Variant.Type.Vector2)
                return left.AsVector2() / right.AsVector2();
            return left.AsVector2() / (float)right.AsDouble();
        }

        if (left.VariantType == Variant.Type.Vector3)
        {
            if (right.VariantType == Variant.Type.Vector3)
                return left.AsVector3() / right.AsVector3();
            return left.AsVector3() / (float)right.AsDouble();
        }

        return left.AsDouble() / right.AsDouble();
    }

    private bool AreEqual(Variant left, Variant right)
    {
        if (left.VariantType != right.VariantType)
            return false;

        return left.Equals(right);
    }

    private bool EvaluateIs(Variant left, Variant right)
    {
        // Simple type name comparison
        if (right.VariantType == Variant.Type.String)
        {
            var typeName = right.AsString();
            if (left.VariantType == Variant.Type.Object)
            {
                var obj = left.AsGodotObject();
                return obj?.IsClass(typeName) ?? false;
            }
        }
        return false;
    }

    private bool EvaluateIn(Variant left, Variant right)
    {
        return right.VariantType switch
        {
            Variant.Type.Array => right.AsGodotArray().Contains(left),
            Variant.Type.Dictionary => right.AsGodotDictionary().ContainsKey(left),
            Variant.Type.String => right.AsString().Contains(GD.VarToStr(left)),
            _ => false
        };
    }

    private Variant EvaluateSingleOperator(GDSingleOperatorExpression singleExpr, GodotObject context)
    {
        var target = EvaluateExpression(singleExpr.TargetExpression, context);

        return singleExpr.OperatorType switch
        {
            GDSingleOperatorType.Negate => Negate(target),
            GDSingleOperatorType.Not or GDSingleOperatorType.Not2 => !target.AsBool(),
            GDSingleOperatorType.BitwiseNegate => ~target.AsInt64(),
            _ => throw new NotSupportedException($"Unary operator '{singleExpr.OperatorType}' is not supported")
        };
    }

    private Variant Negate(Variant value)
    {
        return value.VariantType switch
        {
            Variant.Type.Int => -value.AsInt64(),
            Variant.Type.Float => -value.AsDouble(),
            Variant.Type.Vector2 => -value.AsVector2(),
            Variant.Type.Vector3 => -value.AsVector3(),
            _ => -value.AsDouble()
        };
    }

    private Variant EvaluateBracket(GDBracketExpression bracketExpr, GodotObject context)
    {
        return EvaluateExpression(bracketExpr.InnerExpression, context);
    }

    private Variant EvaluateArrayInitializer(GDArrayInitializerExpression arrayExpr, GodotObject context)
    {
        var result = new Godot.Collections.Array();
        if (arrayExpr.Values != null)
        {
            foreach (var item in arrayExpr.Values)
            {
                if (item is GDExpression expr)
                    result.Add(EvaluateExpression(expr, context));
            }
        }
        return result;
    }

    private Variant EvaluateDictionaryInitializer(GDDictionaryInitializerExpression dictExpr, GodotObject context)
    {
        var result = new Godot.Collections.Dictionary();
        if (dictExpr.KeyValues != null)
        {
            foreach (var kv in dictExpr.KeyValues)
            {
                if (kv.Key != null && kv.Value != null)
                {
                    var key = EvaluateExpression(kv.Key, context);
                    var value = EvaluateExpression(kv.Value, context);
                    result[key] = value;
                }
            }
        }
        return result;
    }

    private Variant EvaluateIfExpression(GDIfExpression ifExpr, GodotObject context)
    {
        var condition = EvaluateExpression(ifExpr.Condition, context);
        if (condition.AsBool())
            return EvaluateExpression(ifExpr.TrueExpression, context);
        else
            return EvaluateExpression(ifExpr.FalseExpression, context);
    }
}
