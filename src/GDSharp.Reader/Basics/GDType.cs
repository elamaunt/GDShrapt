using System;

namespace GDSharp.Reader
{
    public class GDType : GDIdentifier
    {
        public bool ExtractTypeFromInitializer => Sequence.IsNullOrEmpty();
    }
}