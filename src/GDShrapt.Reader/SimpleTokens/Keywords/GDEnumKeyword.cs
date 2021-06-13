namespace GDShrapt.Reader
{
    public sealed class GDEnumKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "enum";
    }
}
