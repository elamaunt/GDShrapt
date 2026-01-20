namespace GDShrapt.Reader
{
    /// <summary>
    /// The &amp; character token, used as prefix for StringName literals.
    /// </summary>
    public sealed class GDAmpersand : GDSingleCharToken
    {
        public override char Char => '&';

        public override GDSyntaxToken Clone()
        {
            return new GDAmpersand();
        }
    }
}
