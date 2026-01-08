namespace GDShrapt.Semantics;

/// <summary>
/// Represents a type declaration in a GDScript project.
/// </summary>
public class GDTypeDeclaration
{
    /// <summary>
    /// Name of the type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Kind of type (Class, InnerClass, Enum).
    /// </summary>
    public GDTypeKind Kind { get; set; }

    /// <summary>
    /// Reference to the script containing this type.
    /// </summary>
    public GDScriptReference? ScriptReference { get; set; }

    /// <summary>
    /// Base type name (from extends clause).
    /// </summary>
    public string? BaseType { get; set; }

    public GDTypeDeclaration(string name)
    {
        Name = name;
    }

    public override string ToString()
    {
        return BaseType != null ? $"{Name} extends {BaseType}" : Name;
    }
}
