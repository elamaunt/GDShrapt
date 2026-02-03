namespace GDShrapt.Reader
{
    public abstract class GDTypeNode : GDNode
    {
        public abstract bool IsArray { get; }
        public abstract bool IsDictionary { get; }
        public abstract string BuildName();

        /// <summary>
        /// Checks if this type is a numeric type (int or float).
        /// </summary>
        public virtual bool IsNumericType() => false;

        /// <summary>
        /// Checks if this type is an integer type.
        /// </summary>
        public virtual bool IsIntType() => false;

        /// <summary>
        /// Checks if this type is a float type.
        /// </summary>
        public virtual bool IsFloatType() => false;

        /// <summary>
        /// Checks if this type is a string type (String or StringName).
        /// </summary>
        public virtual bool IsStringType() => false;

        /// <summary>
        /// Checks if this type is a boolean type.
        /// </summary>
        public virtual bool IsBoolType() => false;

        /// <summary>
        /// Checks if this type is a Vector type (Vector2, Vector2i, Vector3, etc.).
        /// </summary>
        public virtual bool IsVectorType() => false;

        /// <summary>
        /// Checks if this type is a Color type.
        /// </summary>
        public virtual bool IsColorType() => false;

        /// <summary>
        /// Creates a simple type node for the given type name.
        /// </summary>
        public static GDSingleTypeNode CreateSimple(string typeName)
        {
            var node = new GDSingleTypeNode();
            node.Type = new GDType { Sequence = typeName };
            return node;
        }

        /// <summary>
        /// Creates an array type node with an optional element type.
        /// Clones the element type if it's frozen to avoid modifying AST.
        /// </summary>
        public static GDArrayTypeNode CreateArray(GDTypeNode elementType)
        {
            var node = new GDArrayTypeNode();
            node.ArrayKeyword = new GDArrayKeyword();
            if (elementType != null)
            {
                node.SquareOpenBracket = new GDSquareOpenBracket();
                // Clone the element type to avoid modifying frozen AST nodes
                node.InnerType = (GDTypeNode)elementType.Clone();
                node.SquareCloseBracket = new GDSquareCloseBracket();
            }
            return node;
        }
    }
}