using System;

namespace GDShrapt.Reader
{
    public abstract class GDDataToken : GDSimpleSyntaxToken
    {
        public abstract string StringDataRepresentation { get; }

        public override int GetHashCode()
        {
            return StringDataRepresentation?.GetHashCode() ?? base.GetHashCode();
        }

        public abstract GDDataToken CloneWith(string stringValue);

        public override bool Equals(object obj)
        {
            if (obj is GDDataToken other)
                return string.Equals(StringDataRepresentation, other.StringDataRepresentation);
            return base.Equals(obj);
        }
    }
}
