using System;

namespace GDScriptConverter
{
    public class GDType : GDIdentifier
    {
        public bool ExtractTypeFromInitializer => Sequence.IsNullOrEmpty();
    }
}