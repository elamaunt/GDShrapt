using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Default runtime provider with built-in GDScript types and functions.
    /// Does not include Godot-specific classes - use a custom provider for those.
    /// </summary>
    public class GDDefaultRuntimeProvider : IGDRuntimeProvider
    {
        private static GDDefaultRuntimeProvider _instance;

        /// <summary>
        /// Singleton instance of the default provider.
        /// </summary>
        public static GDDefaultRuntimeProvider Instance => _instance ?? (_instance = new GDDefaultRuntimeProvider());

        private readonly Dictionary<string, GDRuntimeTypeInfo> _types;
        private readonly Dictionary<string, GDRuntimeFunctionInfo> _globalFunctions;
        private readonly Dictionary<string, GDRuntimeTypeInfo> _globalClasses;
        private readonly HashSet<string> _builtInIdentifiers;

        public GDDefaultRuntimeProvider()
        {
            _types = new Dictionary<string, GDRuntimeTypeInfo>();
            _globalFunctions = new Dictionary<string, GDRuntimeFunctionInfo>();
            _globalClasses = new Dictionary<string, GDRuntimeTypeInfo>();
            _builtInIdentifiers = new HashSet<string>();

            RegisterBuiltInTypes();
            RegisterBuiltInFunctions();
            RegisterBuiltInConstants();
            RegisterGlobalClasses();
        }

        private void RegisterBuiltInTypes()
        {
            // Primitive types
            AddType("void", null, true);
            AddType("bool", null, true);
            AddType("int", null, true);
            AddType("float", null, true);
            AddType("String", null, true);

            // Container types
            AddType("Array", null, true);
            AddType("Dictionary", null, true);
            AddType("Callable", null, true);
            AddType("Signal", null, true);

            // Vector types
            AddType("Vector2", null, true);
            AddType("Vector2i", null, true);
            AddType("Vector3", null, true);
            AddType("Vector3i", null, true);
            AddType("Vector4", null, true);
            AddType("Vector4i", null, true);

            // Geometry types
            AddType("Rect2", null, true);
            AddType("Rect2i", null, true);
            AddType("AABB", null, true);
            AddType("Plane", null, true);
            AddType("Quaternion", null, true);
            AddType("Basis", null, true);
            AddType("Transform2D", null, true);
            AddType("Transform3D", null, true);
            AddType("Projection", null, true);

            // Other built-in types
            AddType("Color", null, true);
            AddType("NodePath", null, true);
            AddType("RID", null, true);
            AddType("StringName", null, true);

            // Packed arrays
            AddType("PackedByteArray", null, true);
            AddType("PackedInt32Array", null, true);
            AddType("PackedInt64Array", null, true);
            AddType("PackedFloat32Array", null, true);
            AddType("PackedFloat64Array", null, true);
            AddType("PackedStringArray", null, true);
            AddType("PackedVector2Array", null, true);
            AddType("PackedVector3Array", null, true);
            AddType("PackedColorArray", null, true);

            // Base object types
            AddType("Object", null, true);
            AddType("RefCounted", "Object", true);
            AddType("Resource", "RefCounted", true);
            AddType("Node", "Object", true);
            AddType("Node2D", "Node", true);
            AddType("Node3D", "Node", true);
            AddType("Control", "Node", true);
        }

        private void RegisterBuiltInFunctions()
        {
            // Variadic print functions
            AddFunction(GDRuntimeFunctionInfo.VarArgs("print", 0, "void"));
            AddFunction(GDRuntimeFunctionInfo.VarArgs("prints", 0, "void"));
            AddFunction(GDRuntimeFunctionInfo.VarArgs("printt", 0, "void"));
            AddFunction(GDRuntimeFunctionInfo.VarArgs("printraw", 0, "void"));
            AddFunction(GDRuntimeFunctionInfo.VarArgs("print_rich", 0, "void"));
            AddFunction(GDRuntimeFunctionInfo.VarArgs("print_debug", 0, "void"));

            // At least 1 argument
            AddFunction(GDRuntimeFunctionInfo.VarArgs("printerr", 1, "void"));
            AddFunction(GDRuntimeFunctionInfo.VarArgs("push_error", 1, "void"));
            AddFunction(GDRuntimeFunctionInfo.VarArgs("push_warning", 1, "void"));
            AddFunction(GDRuntimeFunctionInfo.VarArgs("str", 1, "String"));
            AddFunction(GDRuntimeFunctionInfo.VarArgs("min", 1));
            AddFunction(GDRuntimeFunctionInfo.VarArgs("max", 1));

            // Exactly 0 arguments
            AddFunction(GDRuntimeFunctionInfo.Exact("randomize", 0, "void"));
            AddFunction(GDRuntimeFunctionInfo.Exact("randi", 0, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("randf", 0, "float"));

            // Exactly 1 argument
            AddFunction(GDRuntimeFunctionInfo.Exact("load", 1, "Resource"));
            AddFunction(GDRuntimeFunctionInfo.Exact("preload", 1, "Resource"));
            AddFunction(GDRuntimeFunctionInfo.Exact("abs", 1));
            AddFunction(GDRuntimeFunctionInfo.Exact("absf", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("absi", 1, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("ceil", 1));
            AddFunction(GDRuntimeFunctionInfo.Exact("ceilf", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("ceili", 1, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("floor", 1));
            AddFunction(GDRuntimeFunctionInfo.Exact("floorf", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("floori", 1, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("round", 1));
            AddFunction(GDRuntimeFunctionInfo.Exact("roundf", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("roundi", 1, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("sign", 1));
            AddFunction(GDRuntimeFunctionInfo.Exact("signf", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("signi", 1, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("sqrt", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("log", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("exp", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("sin", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("cos", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("tan", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("sinh", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("cosh", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("tanh", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("asin", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("acos", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("atan", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("asinh", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("acosh", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("atanh", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("typeof", 1, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("weakref", 1));
            AddFunction(GDRuntimeFunctionInfo.Exact("hash", 1, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("len", 1, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("deg_to_rad", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("rad_to_deg", 1, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("is_nan", 1, "bool"));
            AddFunction(GDRuntimeFunctionInfo.Exact("is_inf", 1, "bool"));
            AddFunction(GDRuntimeFunctionInfo.Exact("is_finite", 1, "bool"));
            AddFunction(GDRuntimeFunctionInfo.Exact("is_zero_approx", 1, "bool"));
            AddFunction(GDRuntimeFunctionInfo.Exact("instance_from_id", 1, "Object"));
            AddFunction(GDRuntimeFunctionInfo.Exact("is_instance_valid", 1, "bool"));
            AddFunction(GDRuntimeFunctionInfo.Exact("is_instance_id_valid", 1, "bool"));
            AddFunction(GDRuntimeFunctionInfo.Exact("type_string", 1, "String"));
            AddFunction(GDRuntimeFunctionInfo.Exact("var_to_str", 1, "String"));
            AddFunction(GDRuntimeFunctionInfo.Exact("str_to_var", 1));
            AddFunction(GDRuntimeFunctionInfo.Exact("get_stack", 0, "Array"));

            // Exactly 2 arguments
            AddFunction(GDRuntimeFunctionInfo.Exact("atan2", 2, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("pow", 2, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("fmod", 2, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("fposmod", 2, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("posmod", 2, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("is_equal_approx", 2, "bool"));
            AddFunction(GDRuntimeFunctionInfo.Exact("snappedf", 2, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("snappedi", 2, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("wrapf", 2, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("wrapi", 2, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("randi_range", 2, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("randf_range", 2, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("seed", 2, "void"));
            AddFunction(GDRuntimeFunctionInfo.Exact("var_to_bytes", 2, "PackedByteArray"));
            AddFunction(GDRuntimeFunctionInfo.Exact("bytes_to_var", 2));

            // Exactly 3 arguments
            AddFunction(GDRuntimeFunctionInfo.Exact("clamp", 3));
            AddFunction(GDRuntimeFunctionInfo.Exact("clampf", 3, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("clampi", 3, "int"));
            AddFunction(GDRuntimeFunctionInfo.Exact("lerp", 3));
            AddFunction(GDRuntimeFunctionInfo.Exact("lerpf", 3, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("lerp_angle", 3, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("inverse_lerp", 3, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("smoothstep", 3, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("move_toward", 3, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("rotate_toward", 3, "float"));

            // Exactly 4 arguments
            AddFunction(GDRuntimeFunctionInfo.Exact("remap", 4, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("bezier_interpolate", 4, "float"));
            AddFunction(GDRuntimeFunctionInfo.Exact("cubic_interpolate", 4, "float"));

            // Special cases
            AddFunction(GDRuntimeFunctionInfo.Range("assert", 1, 2, "void"));
            AddFunction(GDRuntimeFunctionInfo.Range("range", 1, 3, "Array"));
        }

        private void RegisterBuiltInConstants()
        {
            // Constants
            _builtInIdentifiers.Add("null");
            _builtInIdentifiers.Add("true");
            _builtInIdentifiers.Add("false");
            _builtInIdentifiers.Add("self");
            _builtInIdentifiers.Add("super");
            _builtInIdentifiers.Add("PI");
            _builtInIdentifiers.Add("TAU");
            _builtInIdentifiers.Add("INF");
            _builtInIdentifiers.Add("NAN");
        }

        private void RegisterGlobalClasses()
        {
            // Common global singletons (autoloads)
            AddGlobalClass("Input", new GDRuntimeTypeInfo("Input") { IsNative = true, IsSingleton = true });
            AddGlobalClass("Engine", new GDRuntimeTypeInfo("Engine") { IsNative = true, IsSingleton = true });
            AddGlobalClass("OS", new GDRuntimeTypeInfo("OS") { IsNative = true, IsSingleton = true });
            AddGlobalClass("ResourceLoader", new GDRuntimeTypeInfo("ResourceLoader") { IsNative = true, IsSingleton = true });
            AddGlobalClass("Time", new GDRuntimeTypeInfo("Time") { IsNative = true, IsSingleton = true });
        }

        private void AddType(string name, string baseType, bool isNative)
        {
            var typeInfo = new GDRuntimeTypeInfo(name, baseType, isNative);
            _types[name] = typeInfo;
            _builtInIdentifiers.Add(name);
        }

        private void AddFunction(GDRuntimeFunctionInfo funcInfo)
        {
            _globalFunctions[funcInfo.Name] = funcInfo;
            _builtInIdentifiers.Add(funcInfo.Name);
        }

        private void AddGlobalClass(string name, GDRuntimeTypeInfo typeInfo)
        {
            _globalClasses[name] = typeInfo;
            _builtInIdentifiers.Add(name);
        }

        #region IGDRuntimeProvider Implementation

        public bool IsKnownType(string typeName)
        {
            return typeName != null && _types.ContainsKey(typeName);
        }

        public GDRuntimeTypeInfo GetTypeInfo(string typeName)
        {
            if (typeName == null) return null;
            _types.TryGetValue(typeName, out var result);
            return result;
        }

        public GDRuntimeMemberInfo GetMember(string typeName, string memberName)
        {
            if (typeName == null || memberName == null) return null;

            var typeInfo = GetTypeInfo(typeName);
            if (typeInfo?.Members == null) return null;

            return typeInfo.Members.FirstOrDefault(m => m.Name == memberName);
        }

        public string GetBaseType(string typeName)
        {
            return GetTypeInfo(typeName)?.BaseType;
        }

        public bool IsAssignableTo(string sourceType, string targetType)
        {
            if (sourceType == null || targetType == null) return false;
            if (sourceType == targetType) return true;

            // Variant can hold anything
            if (targetType == "Variant") return true;

            // Numeric coercion
            if (targetType == "float" && sourceType == "int") return true;

            // Check inheritance chain
            var current = sourceType;
            while (current != null)
            {
                if (current == targetType) return true;
                current = GetBaseType(current);
            }

            return false;
        }

        public GDRuntimeFunctionInfo GetGlobalFunction(string functionName)
        {
            if (functionName == null) return null;
            _globalFunctions.TryGetValue(functionName, out var result);
            return result;
        }

        public GDRuntimeTypeInfo GetGlobalClass(string className)
        {
            if (className == null) return null;
            _globalClasses.TryGetValue(className, out var result);
            return result;
        }

        public bool IsBuiltIn(string identifier)
        {
            return identifier != null && _builtInIdentifiers.Contains(identifier);
        }

        #endregion
    }
}
