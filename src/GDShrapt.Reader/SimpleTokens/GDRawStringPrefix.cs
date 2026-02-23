namespace GDShrapt.Reader
{
    /// <summary>
    /// The 'r' character prefix for raw string literals: r"..." or r'...'.
    /// Raw strings do not process escape sequences.
    /// </summary>
    public sealed class GDRawStringPrefix : GDSingleCharToken
    {
        public override char Char => 'r';

        public override GDSyntaxToken Clone()
        {
            return new GDRawStringPrefix();
        }
    }
}
