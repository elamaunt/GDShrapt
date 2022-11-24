namespace GDShrapt.Reader
{
    public abstract class GDClassMember : GDIntendedNode
    {
        public abstract GDIdentifier Identifier { get; set; }
        public abstract bool IsStatic { get; }

        internal GDClassMember(int intendation) 
            : base(intendation)
        {
        }

        internal GDClassMember()
            : base()
        {
        }
    }
}
