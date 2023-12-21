namespace GDShrapt.Reader
{
    public abstract class GDTypeNode : GDNode
    {
        public abstract GDTypeNode SubType { get; }
        public abstract bool IsArray { get; }
        public abstract string BuildName();
    }
}