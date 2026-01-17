namespace GDShrapt.Reader
{
    /// <summary>
    /// Information about a user-defined function.
    /// </summary>
    public class GDFunctionSignature
    {
        public string Name { get; set; }
        public int MinParameters { get; set; }
        public int MaxParameters { get; set; }
        public bool HasVarArgs { get; set; }
        public GDMethodDeclaration Declaration { get; set; }
        public bool IsStatic { get; set; }
    }
}
