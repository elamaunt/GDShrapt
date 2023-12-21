using System.Collections.Generic;
using System.Text;

namespace GDShrapt.Reader
{
    public interface IGDSyntaxToken
    {
        string TypeName { get; }
        GDNode Parent { get; }

        /// <summary>
        /// Nearest class member
        /// </summary>
        GDClassMember ClassMember { get; }

        /// <summary>
        /// Main class if exists
        /// </summary>
        GDClassDeclaration RootClassDeclaration { get; }

        /// <summary>
        /// Nearest inner class if exists
        /// </summary>
        GDInnerClassDeclaration InnerClassDeclaration { get; }

        /// <summary>
        /// Nearest owning class if exists
        /// </summary>
        IGDClassDeclaration ClassDeclaration { get; }

        /// <summary>
        /// All parent nodes enumeration
        /// </summary>
        IEnumerable<GDNode> Parents { get; }

        bool RemoveFromParent();
        void AppendTo(StringBuilder builder);
        GDSyntaxToken Clone();

        /// <summary>
        /// Starting token's line in the code which is represented by the tree. Calculating property.
        /// </summary>
        int StartLine { get; }

        /// <summary>
        /// Ending token's line in the code which is represented by the tree. Calculating property.
        /// </summary>
        int EndLine { get; }

        /// <summary>
        /// Starting token's column in the code which is represented by the tree. Calculating property.
        /// </summary>
        int StartColumn { get; }

        /// <summary>
        /// Ending token's column in the code which is represented by the tree. Calculating property.
        /// </summary>
        int EndColumn { get; }

        /// <summary>
        /// The length of the code (represented by the token and its children) in characters. Calculating property.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// New line characters in the token. Checks children. Calculating property.
        /// </summary>
        int NewLinesCount { get; }

        /// <summary>
        /// Returns the next node in parent or null
        /// </summary>
        GDNode NextNode { get; }

        /// <summary>
        /// Returns the next token in parent or null
        /// </summary>
        GDSyntaxToken NextToken { get; }

        /// <summary>
        /// Returns the previous node in parent or null
        /// </summary>
        GDNode PreviousNode { get; }

        /// <summary>
        /// Returns the previous token in parent or null
        /// </summary>
        GDSyntaxToken PreviousToken { get; }


        /// <summary>
        /// Checks whether the token start in range
        /// </summary>
        /// <returns>True if start in range</returns>
        bool IsStartInRange(int startLine, int startColumn, int endLine, int endColumn);

        /// <summary>
        /// Checks wrether the entire token lies in range.
        /// </summary>
        /// <returns>True if lies</returns>
        bool IsWholeInRange(int startLine, int startColumn, int endLine, int endColumn);

        /// <summary>
        /// Checks wrether the entire token contains the position.
        /// </summary>
        /// <returns>True if position at the end or at the start of the token. Also true if the position in the midst of the token. Otherwise false</returns>
        bool ContainsPosition(int line, int column);

        /// <summary>
        /// Returns enumeration of visible Identifiers of variables defined before the token.
        /// </summary>
        /// <param name="owningMember">The Class member which contains the token</param>
        /// <returns>Enumeration</returns>
        List<GDIdentifier> ExtractAllMethodScopeVisibleDeclarationsFromParents(out GDIdentifiableClassMember owningMember);

        /// <summary>
        /// Returns enumeration of visible Identifiers of variables defined before the token.
        /// </summary>
        /// <param name="owningMember">The Class member which contains the token</param>
        /// <returns>Enumeration</returns>
        List<GDIdentifier> ExtractAllMethodScopeVisibleDeclarationsFromParents(int beforeLine, out GDIdentifiableClassMember owningMember);
    }
}