namespace GDShrapt.Reader
{
    /// <summary>
    /// The kind of member in a type.
    /// </summary>
    public enum GDRuntimeMemberKind
    {
        /// <summary>
        /// A method or function.
        /// </summary>
        Method,

        /// <summary>
        /// A property (get/set).
        /// </summary>
        Property,

        /// <summary>
        /// A signal declaration.
        /// </summary>
        Signal,

        /// <summary>
        /// A constant value.
        /// </summary>
        Constant,

        /// <summary>
        /// An enum type.
        /// </summary>
        Enum,

        /// <summary>
        /// An enum value.
        /// </summary>
        EnumValue
    }
}
