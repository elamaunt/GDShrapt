namespace GDShrapt.Reader
{
    public abstract class GDIdentifiableClassMember : GDClassMember
    {
        public abstract GDIdentifier Identifier { get; set; }
        public abstract bool IsStatic { get; }

        internal GDIdentifiableClassMember(int intendation) 
            : base(intendation)
        {
        }

        internal GDIdentifiableClassMember()
            : base()
        {
        }
    }
}
