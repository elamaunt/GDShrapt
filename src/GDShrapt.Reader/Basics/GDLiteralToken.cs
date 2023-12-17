namespace GDShrapt.Reader
{
    public abstract class GDLiteralToken : GDSimpleSyntaxToken
    {
        /// <summary>
        /// Throws ArgumentException if set invalid format value
        /// </summary>
        public abstract string Sequence { get; set; }

        public override int GetHashCode()
        {
            return Sequence?.GetHashCode() ?? base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is GDLiteralToken other)
                return string.Equals(Sequence, other.Sequence);
            return base.Equals(obj);
        }
    }
}
