using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Contains information about a type in the runtime environment.
    /// </summary>
    public class GDRuntimeTypeInfo
    {
        /// <summary>
        /// The name of the type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The base type name, or null if no base type.
        /// </summary>
        public string BaseType { get; set; }

        /// <summary>
        /// True if this is a native/built-in type.
        /// </summary>
        public bool IsNative { get; set; }

        /// <summary>
        /// True if this is a singleton/autoload class.
        /// </summary>
        public bool IsSingleton { get; set; }

        /// <summary>
        /// True if this is a reference type (Resource, RefCounted).
        /// </summary>
        public bool IsRefCounted { get; set; }

        /// <summary>
        /// Members of this type (methods, properties, signals, constants).
        /// </summary>
        public IReadOnlyList<GDRuntimeMemberInfo> Members { get; set; }

        /// <summary>
        /// Creates a new type info with the given name.
        /// </summary>
        public GDRuntimeTypeInfo(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Creates a new type info.
        /// </summary>
        public GDRuntimeTypeInfo(string name, string baseType, bool isNative = false)
        {
            Name = name;
            BaseType = baseType;
            IsNative = isNative;
        }

        public override string ToString() => Name;
    }
}
