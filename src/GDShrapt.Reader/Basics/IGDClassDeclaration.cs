using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public interface IGDClassDeclaration : IGDNode
    {
        GDIdentifier Identifier { get; }
        GDClassMembersList Members { get; }
        GDTypeNode BaseType { get; }
        GDNode CreateEmptyInstance();
        IEnumerable<GDVariableDeclaration> Variables { get; }
        IEnumerable<GDMethodDeclaration> Methods { get; }
        IEnumerable<GDEnumDeclaration> Enums { get; }
        IEnumerable<GDInnerClassDeclaration> InnerClasses { get; }
        IEnumerable<GDIdentifiableClassMember> IdentifiableMembers { get; }
        IEnumerable<GDCustomAttribute> CustomAttributes { get; }
    }
}
