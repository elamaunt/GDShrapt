﻿namespace GDShrapt.Reader
{
    public sealed class GDAt : GDSingleCharToken
    {
        public override char Char => '@';

        public override GDSyntaxToken Clone()
        {
            return new GDAt();
        }
    }
}
