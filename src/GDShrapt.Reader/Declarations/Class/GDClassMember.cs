namespace GDShrapt.Reader
{
    public abstract class GDClassMember : GDIntendedNode
    {
        public abstract GDIdentifier Identifier { get; set; }

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
