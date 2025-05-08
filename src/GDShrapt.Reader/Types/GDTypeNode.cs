namespace GDShrapt.Reader
{
    public abstract class GDTypeNode : GDNode
    {
        public abstract bool IsArray { get; }
        public abstract bool IsDictionary { get; }
        public abstract string BuildName();
    }
}