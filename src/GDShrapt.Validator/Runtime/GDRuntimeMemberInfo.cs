using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Contains information about a member (method, property, signal) of a type.
    /// </summary>
    public class GDRuntimeMemberInfo
    {
        /// <summary>
        /// The member name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The kind of member.
        /// </summary>
        public GDRuntimeMemberKind Kind { get; set; }

        /// <summary>
        /// The type of the member (return type for methods, value type for properties).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// True if this is a static member.
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// For methods: the parameters of this method.
        /// </summary>
        public IReadOnlyList<GDRuntimeParameterInfo> Parameters { get; set; }

        /// <summary>
        /// For methods: the minimum number of required arguments.
        /// </summary>
        public int MinArgs { get; set; }

        /// <summary>
        /// For methods: the maximum number of arguments (-1 for varargs).
        /// </summary>
        public int MaxArgs { get; set; }

        /// <summary>
        /// For methods: true if this method accepts variable arguments.
        /// </summary>
        public bool IsVarArgs { get; set; }

        /// <summary>
        /// Creates a new member info.
        /// </summary>
        public GDRuntimeMemberInfo(string name, GDRuntimeMemberKind kind, string type = null)
        {
            Name = name;
            Kind = kind;
            Type = type;
        }

        /// <summary>
        /// Creates a method member info.
        /// </summary>
        public static GDRuntimeMemberInfo Method(string name, string returnType, int minArgs, int maxArgs, bool isVarArgs = false, bool isStatic = false)
        {
            return new GDRuntimeMemberInfo(name, GDRuntimeMemberKind.Method, returnType)
            {
                MinArgs = minArgs,
                MaxArgs = maxArgs,
                IsVarArgs = isVarArgs,
                IsStatic = isStatic
            };
        }

        /// <summary>
        /// Creates a property member info.
        /// </summary>
        public static GDRuntimeMemberInfo Property(string name, string type, bool isStatic = false)
        {
            return new GDRuntimeMemberInfo(name, GDRuntimeMemberKind.Property, type)
            {
                IsStatic = isStatic
            };
        }

        /// <summary>
        /// Creates a signal member info.
        /// </summary>
        public static GDRuntimeMemberInfo Signal(string name)
        {
            return new GDRuntimeMemberInfo(name, GDRuntimeMemberKind.Signal, "Signal");
        }

        /// <summary>
        /// Creates a constant member info.
        /// </summary>
        public static GDRuntimeMemberInfo Constant(string name, string type)
        {
            return new GDRuntimeMemberInfo(name, GDRuntimeMemberKind.Constant, type);
        }

        public override string ToString() => $"{Kind}: {Name}";
    }
}
