using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Plugin;

internal class ReferencesInfo
    {
        List<GDIdentifier> _references;
        public GDIdentifier Origin { get; private set; }
        public string ExternalName { get; private set; }
        public ReferencesInfo(GDIdentifier origin)
        {
            Origin = origin;
        }

        public ReferencesInfo(string externalName)
        {
            ExternalName = externalName;
        }

        public ReferencesInfo AddReference(GDIdentifier reference)
        {
            if (_references == null)
                _references = new List<GDIdentifier>();

            _references.Add(reference);
            return this;
        }
}