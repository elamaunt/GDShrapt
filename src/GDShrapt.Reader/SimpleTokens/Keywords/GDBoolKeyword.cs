namespace GDShrapt.Reader
{
    public abstract class GDBoolKeyword : GDSequenceToken, IGDKeywordToken
    {
        public abstract bool Value { get; }
    }
}
