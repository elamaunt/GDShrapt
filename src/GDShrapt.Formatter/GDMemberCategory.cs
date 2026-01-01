namespace GDShrapt.Reader
{
    /// <summary>
    /// Categories for class member ordering according to GDScript style guide.
    /// </summary>
    public enum GDMemberCategory
    {
        /// <summary>
        /// Class-level attributes: class_name, extends, @tool
        /// </summary>
        ClassAttribute,

        /// <summary>
        /// Signal declarations
        /// </summary>
        Signal,

        /// <summary>
        /// Enum declarations
        /// </summary>
        Enum,

        /// <summary>
        /// Constant declarations (const)
        /// </summary>
        Constant,

        /// <summary>
        /// Variables with @export attribute
        /// </summary>
        ExportVariable,

        /// <summary>
        /// Public variables (without _ prefix)
        /// </summary>
        PublicVariable,

        /// <summary>
        /// Private variables (with _ prefix)
        /// </summary>
        PrivateVariable,

        /// <summary>
        /// Variables with @onready attribute
        /// </summary>
        OnreadyVariable,

        /// <summary>
        /// Built-in Godot methods: _init, _ready, _process, _physics_process,
        /// _enter_tree, _exit_tree, _input, _unhandled_input, etc.
        /// </summary>
        BuiltinMethod,

        /// <summary>
        /// Public methods (without _ prefix)
        /// </summary>
        PublicMethod,

        /// <summary>
        /// Private methods (with _ prefix, excluding built-in methods)
        /// </summary>
        PrivateMethod,

        /// <summary>
        /// Inner class declarations
        /// </summary>
        InnerClass
    }
}
