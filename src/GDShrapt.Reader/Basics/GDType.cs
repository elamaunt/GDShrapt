using System;

namespace GDShrapt.Reader
{
    public class GDType : GDIdentifier
    {
        public bool ExtractTypeFromInitializer => Sequence.IsNullOrEmpty();
    }
}