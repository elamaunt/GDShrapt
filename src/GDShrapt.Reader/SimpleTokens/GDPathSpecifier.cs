using System;

namespace GDShrapt.Reader
{
    public class GDPathSpecifier : GDSequenceToken
    {
        public string IdentifierValue { get; set; }
        public GDPathSpecifierType Type { get; set; }

        public override string Sequence
        {
            get
            {
                switch (Type)
                {
                    case GDPathSpecifierType.Current:
                        return ".";
                    case GDPathSpecifierType.Parent:
                        return "..";
                    case GDPathSpecifierType.Identifier:
                        return IdentifierValue;
                    default:
                        throw new NotSupportedException(Type.ToString());
                }
            }
        }

        public override GDSyntaxToken Clone()
        {
            return new GDPathSpecifier() 
            {
                Type = Type,
                IdentifierValue = IdentifierValue
            };
        }
    }
}
