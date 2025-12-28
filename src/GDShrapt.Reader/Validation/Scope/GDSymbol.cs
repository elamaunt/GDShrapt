namespace GDShrapt.Reader
{
    /// <summary>
    /// A declared identifier: variable, constant, method, signal, class, enum, etc.
    /// </summary>
    public class GDSymbol
    {
        public string Name { get; }
        public GDSymbolKind Kind { get; }
        public GDType Type { get; }
        public string TypeName { get; }
        public GDNode Declaration { get; }
        public bool IsStatic { get; }
        public bool IsConst => Kind == GDSymbolKind.Constant;

        public GDSymbol(string name, GDSymbolKind kind, GDNode declaration, GDType type = null, string typeName = null, bool isStatic = false)
        {
            Name = name;
            Kind = kind;
            Declaration = declaration;
            Type = type;
            TypeName = typeName;
            IsStatic = isStatic;
        }

        // Factory methods for common symbol types
        public static GDSymbol Variable(string name, GDNode declaration, GDType type = null, string typeName = null, bool isStatic = false)
            => new GDSymbol(name, GDSymbolKind.Variable, declaration, type, typeName, isStatic);

        public static GDSymbol Constant(string name, GDNode declaration, GDType type = null, string typeName = null)
            => new GDSymbol(name, GDSymbolKind.Constant, declaration, type, typeName);

        public static GDSymbol Parameter(string name, GDNode declaration, GDType type = null, string typeName = null)
            => new GDSymbol(name, GDSymbolKind.Parameter, declaration, type, typeName);

        public static GDSymbol Method(string name, GDNode declaration, bool isStatic = false)
            => new GDSymbol(name, GDSymbolKind.Method, declaration, isStatic: isStatic);

        public static GDSymbol Signal(string name, GDNode declaration)
            => new GDSymbol(name, GDSymbolKind.Signal, declaration);

        public static GDSymbol Class(string name, GDNode declaration)
            => new GDSymbol(name, GDSymbolKind.Class, declaration);

        public static GDSymbol Enum(string name, GDNode declaration)
            => new GDSymbol(name, GDSymbolKind.Enum, declaration);

        public static GDSymbol EnumValue(string name, GDNode declaration)
            => new GDSymbol(name, GDSymbolKind.EnumValue, declaration);

        /// <summary>
        /// For-loop iterator variable.
        /// </summary>
        public static GDSymbol Iterator(string name, GDNode declaration, GDType type = null, string typeName = null)
            => new GDSymbol(name, GDSymbolKind.Iterator, declaration, type, typeName);

        public override string ToString() => $"{Kind}: {Name}";
    }
}
