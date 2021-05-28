using System.Collections.Generic;

namespace GDSharp.Reader
{
    public class GDProject
    {
        public List<GDTypeDeclaration> Types { get; } = new List<GDTypeDeclaration>();
    }
}