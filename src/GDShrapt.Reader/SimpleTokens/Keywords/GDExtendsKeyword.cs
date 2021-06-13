namespace GDShrapt.Reader
{
    public sealed class GDExtendsKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "extends";
    }
}
