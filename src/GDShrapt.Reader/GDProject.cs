using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public class GDProject
    {
        public List<GDClassDeclaration> Classes { get; } = new List<GDClassDeclaration>();
    }
}