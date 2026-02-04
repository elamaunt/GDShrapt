using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;

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

            // String type with methods
            AddTypeWithMembers("String", null, true, new[]
            {
                // Case conversion
                GDRuntimeMemberInfo.Method("to_upper", "String", 0, 0),
                GDRuntimeMemberInfo.Method("to_lower", "String", 0, 0),
                GDRuntimeMemberInfo.Method("capitalize", "String", 0, 0),
                GDRuntimeMemberInfo.Method("to_camel_case", "String", 0, 0),
                GDRuntimeMemberInfo.Method("to_pascal_case", "String", 0, 0),
                GDRuntimeMemberInfo.Method("to_snake_case", "String", 0, 0),

                // Trimming
                GDRuntimeMemberInfo.Method("strip_edges", "String", 0, 2),
                GDRuntimeMemberInfo.Method("strip_escapes", "String", 0, 0),
                GDRuntimeMemberInfo.Method("lstrip", "String", 1, 1),
                GDRuntimeMemberInfo.Method("rstrip", "String", 1, 1),
                GDRuntimeMemberInfo.Method("dedent", "String", 0, 0),
                GDRuntimeMemberInfo.Method("indent", "String", 1, 1),

                // Substring operations
                GDRuntimeMemberInfo.Method("substr", "String", 1, 2),
                GDRuntimeMemberInfo.Method("left", "String", 1, 1),
                GDRuntimeMemberInfo.Method("right", "String", 1, 1),
                GDRuntimeMemberInfo.Method("get_slice", "String", 2, 2),
                GDRuntimeMemberInfo.Method("get_slice_count", "int", 1, 1),

                // Search/Find
                GDRuntimeMemberInfo.Method("find", "int", 1, 2),
                GDRuntimeMemberInfo.Method("findn", "int", 1, 2),
                GDRuntimeMemberInfo.Method("rfind", "int", 1, 2),
                GDRuntimeMemberInfo.Method("rfindn", "int", 1, 2),
                GDRuntimeMemberInfo.Method("count", "int", 1, 3),
                GDRuntimeMemberInfo.Method("countn", "int", 1, 3),
                GDRuntimeMemberInfo.Method("contains", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("containsn", "bool", 1, 1),

                // Comparison
                GDRuntimeMemberInfo.Method("begins_with", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("ends_with", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("match", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("matchn", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("is_subsequence_of", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("is_subsequence_ofn", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("similarity", "float", 1, 1),

                // Replace
                GDRuntimeMemberInfo.Method("replace", "String", 2, 2),
                GDRuntimeMemberInfo.Method("replacen", "String", 2, 2),
                GDRuntimeMemberInfo.Method("erase", "String", 2, 2),
                GDRuntimeMemberInfo.Method("insert", "String", 2, 2),

                // Split/Join
                GDRuntimeMemberInfo.Method("split", "PackedStringArray", 1, 3),
                GDRuntimeMemberInfo.Method("rsplit", "PackedStringArray", 1, 3),
                GDRuntimeMemberInfo.Method("split_floats", "PackedFloat64Array", 1, 2),
                GDRuntimeMemberInfo.Method("join", "String", 1, 1),

                // Format
                GDRuntimeMemberInfo.Method("format", "String", 1, 2),
                GDRuntimeMemberInfo.Method("sprintf", "String", 1, 1, true), // varargs in practice
                GDRuntimeMemberInfo.Method("pad_zeros", "String", 1, 1),
                GDRuntimeMemberInfo.Method("pad_decimals", "String", 1, 1),
                GDRuntimeMemberInfo.Method("lpad", "String", 2, 2),
                GDRuntimeMemberInfo.Method("rpad", "String", 2, 2),
                GDRuntimeMemberInfo.Method("repeat", "String", 1, 1),
                GDRuntimeMemberInfo.Method("reverse", "String", 0, 0),

                // Validation
                GDRuntimeMemberInfo.Method("is_empty", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_valid_float", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_valid_hex_number", "bool", 0, 1),
                GDRuntimeMemberInfo.Method("is_valid_html_color", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_valid_identifier", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_valid_int", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_valid_ip_address", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_valid_filename", "bool", 0, 0),

                // Conversion
                GDRuntimeMemberInfo.Method("to_int", "int", 0, 0),
                GDRuntimeMemberInfo.Method("to_float", "float", 0, 0),
                GDRuntimeMemberInfo.Method("hex_to_int", "int", 0, 0),
                GDRuntimeMemberInfo.Method("bin_to_int", "int", 0, 0),
                GDRuntimeMemberInfo.Method("to_ascii_buffer", "PackedByteArray", 0, 0),
                GDRuntimeMemberInfo.Method("to_utf8_buffer", "PackedByteArray", 0, 0),
                GDRuntimeMemberInfo.Method("to_utf16_buffer", "PackedByteArray", 0, 0),
                GDRuntimeMemberInfo.Method("to_utf32_buffer", "PackedByteArray", 0, 0),
                GDRuntimeMemberInfo.Method("to_wchar_buffer", "PackedByteArray", 0, 0),

                // Hashing
                GDRuntimeMemberInfo.Method("hash", "int", 0, 0),
                GDRuntimeMemberInfo.Method("md5_buffer", "PackedByteArray", 0, 0),
                GDRuntimeMemberInfo.Method("md5_text", "String", 0, 0),
                GDRuntimeMemberInfo.Method("sha1_buffer", "PackedByteArray", 0, 0),
                GDRuntimeMemberInfo.Method("sha1_text", "String", 0, 0),
                GDRuntimeMemberInfo.Method("sha256_buffer", "PackedByteArray", 0, 0),
                GDRuntimeMemberInfo.Method("sha256_text", "String", 0, 0),

                // Encoding
                GDRuntimeMemberInfo.Method("c_escape", "String", 0, 0),
                GDRuntimeMemberInfo.Method("c_unescape", "String", 0, 0),
                GDRuntimeMemberInfo.Method("json_escape", "String", 0, 0),
                GDRuntimeMemberInfo.Method("xml_escape", "String", 0, 1),
                GDRuntimeMemberInfo.Method("xml_unescape", "String", 0, 0),
                GDRuntimeMemberInfo.Method("uri_encode", "String", 0, 0),
                GDRuntimeMemberInfo.Method("uri_decode", "String", 0, 0),
                GDRuntimeMemberInfo.Method("validate_node_name", "String", 0, 0),
                GDRuntimeMemberInfo.Method("validate_filename", "String", 0, 0),

                // Path operations
                GDRuntimeMemberInfo.Method("get_base_dir", "String", 0, 0),
                GDRuntimeMemberInfo.Method("get_basename", "String", 0, 0),
                GDRuntimeMemberInfo.Method("get_extension", "String", 0, 0),
                GDRuntimeMemberInfo.Method("get_file", "String", 0, 0),
                GDRuntimeMemberInfo.Method("path_join", "String", 1, 1),
                GDRuntimeMemberInfo.Method("simplify_path", "String", 0, 0),
                GDRuntimeMemberInfo.Method("is_absolute_path", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_relative_path", "bool", 0, 0),

                // Unicode
                GDRuntimeMemberInfo.Method("unicode_at", "int", 1, 1),
                GDRuntimeMemberInfo.Method("length", "int", 0, 0),

                // Properties exposed as methods
                GDRuntimeMemberInfo.Property("length", "int"),
            });

            // Container types
            AddTypeWithMembers("Array", null, true, new[]
            {
                // Size/capacity
                GDRuntimeMemberInfo.Method("size", "int", 0, 0),
                GDRuntimeMemberInfo.Method("is_empty", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_read_only", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("resize", "int", 1, 1),
                GDRuntimeMemberInfo.Method("clear", "void", 0, 0),

                // Access
                GDRuntimeMemberInfo.Method("front", "Variant", 0, 0),
                GDRuntimeMemberInfo.Method("back", "Variant", 0, 0),
                GDRuntimeMemberInfo.Method("pick_random", "Variant", 0, 0),

                // Modification
                GDRuntimeMemberInfo.Method("append", "void", 1, 1),
                GDRuntimeMemberInfo.Method("append_array", "void", 1, 1),
                GDRuntimeMemberInfo.Method("push_back", "void", 1, 1),
                GDRuntimeMemberInfo.Method("push_front", "void", 1, 1),
                GDRuntimeMemberInfo.Method("pop_back", "Variant", 0, 0),
                GDRuntimeMemberInfo.Method("pop_front", "Variant", 0, 0),
                GDRuntimeMemberInfo.Method("pop_at", "Variant", 1, 1),
                GDRuntimeMemberInfo.Method("insert", "int", 2, 2),
                GDRuntimeMemberInfo.Method("remove_at", "void", 1, 1),
                GDRuntimeMemberInfo.Method("fill", "void", 1, 1),
                GDRuntimeMemberInfo.Method("erase", "void", 1, 1),
                GDRuntimeMemberInfo.Method("reverse", "void", 0, 0),
                GDRuntimeMemberInfo.Method("shuffle", "void", 0, 0),

                // Search
                GDRuntimeMemberInfo.Method("find", "int", 1, 2),
                GDRuntimeMemberInfo.Method("rfind", "int", 1, 2),
                GDRuntimeMemberInfo.Method("count", "int", 1, 1),
                GDRuntimeMemberInfo.Method("has", "bool", 1, 1),

                // Comparison
                GDRuntimeMemberInfo.Method("hash", "int", 0, 0),

                // Slicing
                GDRuntimeMemberInfo.Method("slice", "Array", 1, 4),
                GDRuntimeMemberInfo.Method("duplicate", "Array", 0, 1),

                // Sorting
                GDRuntimeMemberInfo.Method("sort", "void", 0, 0),
                GDRuntimeMemberInfo.Method("sort_custom", "void", 1, 1),
                GDRuntimeMemberInfo.Method("bsearch", "int", 1, 2),
                GDRuntimeMemberInfo.Method("bsearch_custom", "int", 2, 3),

                // Functional
                GDRuntimeMemberInfo.Method("map", "Array", 1, 1),
                GDRuntimeMemberInfo.Method("filter", "Array", 1, 1),
                GDRuntimeMemberInfo.Method("reduce", "Variant", 1, 2),
                GDRuntimeMemberInfo.Method("any", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("all", "bool", 1, 1),

                // Min/Max
                GDRuntimeMemberInfo.Method("min", "Variant", 0, 0),
                GDRuntimeMemberInfo.Method("max", "Variant", 0, 0),

                // Assignment
                GDRuntimeMemberInfo.Method("assign", "void", 1, 1),
                GDRuntimeMemberInfo.Method("make_read_only", "void", 0, 0),
            });

            AddTypeWithMembers("Dictionary", null, true, new[]
            {
                // Size
                GDRuntimeMemberInfo.Method("size", "int", 0, 0),
                GDRuntimeMemberInfo.Method("is_empty", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_read_only", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("clear", "void", 0, 0),

                // Access
                GDRuntimeMemberInfo.Method("get", "Variant", 1, 2),
                GDRuntimeMemberInfo.Method("has", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("has_all", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("keys", "Array", 0, 0),
                GDRuntimeMemberInfo.Method("values", "Array", 0, 0),

                // Modification
                GDRuntimeMemberInfo.Method("erase", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("merge", "void", 1, 2),

                // Comparison
                GDRuntimeMemberInfo.Method("hash", "int", 0, 0),
                GDRuntimeMemberInfo.Method("duplicate", "Dictionary", 0, 1),

                // Assignment
                GDRuntimeMemberInfo.Method("make_read_only", "void", 0, 0),
            });

            // Callable type with methods
            AddTypeWithMembers("Callable", null, true, new[]
            {
                // Call methods
                GDRuntimeMemberInfo.Method("call", "Variant", 0, int.MaxValue, true), // varargs
                GDRuntimeMemberInfo.Method("callv", "Variant", 1, 1),
                GDRuntimeMemberInfo.Method("call_deferred", "void", 0, int.MaxValue, true), // varargs

                // Binding
                GDRuntimeMemberInfo.Method("bind", "Callable", 0, int.MaxValue, true), // varargs
                GDRuntimeMemberInfo.Method("bindv", "Callable", 1, 1),
                GDRuntimeMemberInfo.Method("unbind", "Callable", 1, 1),
                GDRuntimeMemberInfo.Method("get_bound_arguments", "Array", 0, 0),
                GDRuntimeMemberInfo.Method("get_bound_arguments_count", "int", 0, 0),

                // Validation
                GDRuntimeMemberInfo.Method("is_valid", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_null", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_standard", "bool", 0, 0),
                GDRuntimeMemberInfo.Method("is_custom", "bool", 0, 0),

                // Information
                GDRuntimeMemberInfo.Method("get_method", "StringName", 0, 0),
                GDRuntimeMemberInfo.Method("get_object", "Object", 0, 0),
                GDRuntimeMemberInfo.Method("get_object_id", "int", 0, 0),
                GDRuntimeMemberInfo.Method("get_argument_count", "int", 0, 0),

                // Hashing
                GDRuntimeMemberInfo.Method("hash", "int", 0, 0),

                // RPC
                GDRuntimeMemberInfo.Method("rpc", "void", 0, int.MaxValue, true), // varargs
                GDRuntimeMemberInfo.Method("rpc_id", "void", 1, int.MaxValue, true), // varargs
            });

            // Signal type with methods
            AddTypeWithMembers("Signal", null, true, new[]
            {
                // Connection
                GDRuntimeMemberInfo.Method("connect", "int", 1, 2),
                GDRuntimeMemberInfo.Method("disconnect", "void", 1, 1),
                GDRuntimeMemberInfo.Method("is_connected", "bool", 1, 1),
                GDRuntimeMemberInfo.Method("get_connections", "Array", 0, 0),

                // Emission
                GDRuntimeMemberInfo.Method("emit", "void", 0, int.MaxValue, true), // varargs

                // Information
                GDRuntimeMemberInfo.Method("get_name", "StringName", 0, 0),
                GDRuntimeMemberInfo.Method("get_object", "Object", 0, 0),
                GDRuntimeMemberInfo.Method("get_object_id", "int", 0, 0),

                // Validation
                GDRuntimeMemberInfo.Method("is_null", "bool", 0, 0),
            });

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

        private void AddTypeWithMembers(string name, string baseType, bool isNative, GDRuntimeMemberInfo[] members)
        {
            var typeInfo = new GDRuntimeTypeInfo(name, baseType, isNative)
            {
                Members = members
            };
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

        public IEnumerable<string> GetAllTypes()
        {
            return _types.Keys;
        }

        public bool IsBuiltinType(string typeName)
        {
            if (typeName == null)
                return false;

            // All types registered in GDDefaultRuntimeProvider are builtin value types
            // (primitives, String, Array, Dictionary, Vector types, etc.)
            return _types.ContainsKey(typeName);
        }

        public IReadOnlyList<string> FindTypesWithMethod(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
                return System.Array.Empty<string>();

            var result = new List<string>();
            foreach (var kvp in _types)
            {
                if (kvp.Value.Members?.Any(m => m.Name == methodName && m.Kind == GDRuntimeMemberKind.Method) == true)
                    result.Add(kvp.Key);
            }
            return result;
        }

        #endregion

        #region Type Traits Implementation

        private static readonly HashSet<string> _numericTypes = new HashSet<string> { "int", "float" };
        private static readonly HashSet<string> _vectorTypes = new HashSet<string>
        {
            "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i"
        };
        private static readonly HashSet<string> _iterableTypes = new HashSet<string>
        {
            "Array", "Dictionary", "String",
            "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
            "PackedFloat32Array", "PackedFloat64Array", "PackedStringArray",
            "PackedVector2Array", "PackedVector3Array", "PackedColorArray"
        };
        private static readonly HashSet<string> _indexableTypes = new HashSet<string>
        {
            "Array", "Dictionary", "String",
            "Vector2", "Vector3", "Vector4", "Vector2i", "Vector3i", "Vector4i",
            "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
            "PackedFloat32Array", "PackedFloat64Array", "PackedStringArray",
            "PackedVector2Array", "PackedVector3Array", "PackedColorArray",
            "Color", "Basis", "Transform2D", "Transform3D", "Projection"
        };
        private static readonly HashSet<string> _packedArrayTypes = new HashSet<string>
        {
            "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
            "PackedFloat32Array", "PackedFloat64Array", "PackedStringArray",
            "PackedVector2Array", "PackedVector3Array", "PackedColorArray"
        };
        private static readonly HashSet<string> _containerTypes = new HashSet<string> { "Array", "Dictionary" };

        private static readonly Dictionary<string, string> _floatVectorVariants = new Dictionary<string, string>
        {
            { "Vector2i", "Vector2" },
            { "Vector3i", "Vector3" },
            { "Vector4i", "Vector4" }
        };

        private static readonly Dictionary<string, string> _packedArrayElementTypes = new Dictionary<string, string>
        {
            { "PackedByteArray", "int" },
            { "PackedInt32Array", "int" },
            { "PackedInt64Array", "int" },
            { "PackedFloat32Array", "float" },
            { "PackedFloat64Array", "float" },
            { "PackedStringArray", "String" },
            { "PackedVector2Array", "Vector2" },
            { "PackedVector3Array", "Vector3" },
            { "PackedColorArray", "Color" }
        };

        public bool IsNumericType(string typeName)
        {
            return !string.IsNullOrEmpty(typeName) && _numericTypes.Contains(typeName);
        }

        public bool IsIterableType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;
            var baseType = ExtractBaseTypeName(typeName);
            return _iterableTypes.Contains(baseType);
        }

        public bool IsIndexableType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;
            var baseType = ExtractBaseTypeName(typeName);
            return _indexableTypes.Contains(baseType);
        }

        public bool IsNullableType(string typeName)
        {
            // Value types are not nullable
            return !IsBuiltinType(typeName);
        }

        public bool IsVectorType(string typeName)
        {
            return !string.IsNullOrEmpty(typeName) && _vectorTypes.Contains(typeName);
        }

        public bool IsContainerType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;
            var baseType = ExtractBaseTypeName(typeName);
            return _containerTypes.Contains(baseType);
        }

        public bool IsPackedArrayType(string typeName)
        {
            return !string.IsNullOrEmpty(typeName) && _packedArrayTypes.Contains(typeName);
        }

        public string GetFloatVectorVariant(string integerVectorType)
        {
            if (string.IsNullOrEmpty(integerVectorType))
                return null;
            _floatVectorVariants.TryGetValue(integerVectorType, out var result);
            return result;
        }

        public string GetPackedArrayElementType(string packedArrayType)
        {
            if (string.IsNullOrEmpty(packedArrayType))
                return null;
            _packedArrayElementTypes.TryGetValue(packedArrayType, out var result);
            return result;
        }

        public string ResolveOperatorResult(string leftType, string operatorName, string rightType)
        {
            // Delegate to GDOperatorTypeResolver for now
            GDDualOperatorType? opType = operatorName switch
            {
                "+" or "Addition" => GDDualOperatorType.Addition,
                "-" or "Subtraction" => GDDualOperatorType.Subtraction,
                "*" or "Multiplication" => GDDualOperatorType.Multiply,
                "/" or "Division" => GDDualOperatorType.Division,
                "%" or "Modulo" => GDDualOperatorType.Mod,
                "**" or "Power" => GDDualOperatorType.Power,
                "&" or "BitwiseAnd" => GDDualOperatorType.BitwiseAnd,
                "|" or "BitwiseOr" => GDDualOperatorType.BitwiseOr,
                "^" or "BitwiseXor" => GDDualOperatorType.Xor,
                "<<" or "ShiftLeft" => GDDualOperatorType.BitShiftLeft,
                ">>" or "ShiftRight" => GDDualOperatorType.BitShiftRight,
                _ => null
            };

            if (opType == null)
                return null;

            return GDOperatorTypeResolver.ResolveOperatorType(opType.Value, leftType, rightType);
        }

        public IReadOnlyList<string> GetTypesWithOperator(string operatorName)
        {
            return operatorName switch
            {
                "+" or "Addition" => new[]
                {
                    "int", "float", "String", "StringName",
                    "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
                    "Color", "Array",
                    "PackedByteArray", "PackedInt32Array", "PackedInt64Array",
                    "PackedFloat32Array", "PackedFloat64Array",
                    "PackedStringArray", "PackedVector2Array", "PackedVector3Array",
                    "PackedColorArray"
                },
                "-" or "Subtraction" => new[]
                {
                    "int", "float",
                    "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
                    "Color"
                },
                "*" or "Multiplication" => new[]
                {
                    "int", "float",
                    "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
                    "Color", "Quaternion", "Basis",
                    "Transform2D", "Transform3D"
                },
                "/" or "Division" => new[]
                {
                    "int", "float",
                    "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
                    "Color"
                },
                "%" or "Modulo" => new[]
                {
                    "int", "float", "String",
                    "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i"
                },
                _ => System.Array.Empty<string>()
            };
        }

        private static string ExtractBaseTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;
            var bracketIndex = typeName.IndexOf('[');
            return bracketIndex > 0 ? typeName.Substring(0, bracketIndex) : typeName;
        }

        #endregion
    }
}
