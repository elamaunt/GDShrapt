namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents the ".." rest/spread operator token used in dictionary and array patterns.
    /// </summary>
    public sealed class GDDoubleDot : GDSequenceToken, IGDStructureToken
    {
        public override string Sequence => "..";

        public override GDSyntaxToken Clone()
        {
            return new GDDoubleDot();
        }
    }
}
