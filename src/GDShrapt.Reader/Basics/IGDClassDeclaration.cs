using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public interface IGDClassDeclaration : IGDSyntaxToken
    {
        GDIdentifier Identifier { get; }
        GDClassMembersList Members { get; }
        GDTypeNode BaseType { get; }
        GDNode CreateEmptyInstance();
        IEnumerable<GDVariableDeclaration> Variables { get; }
        IEnumerable<GDMethodDeclaration> Methods { get; }
        IEnumerable<GDEnumDeclaration> Enums { get; }
        IEnumerable<GDInnerClassDeclaration> InnerClasses { get; }
    }
}
