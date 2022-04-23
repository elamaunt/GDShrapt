using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Basic syntax node.
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public abstract class GDSyntaxToken : GDReader, ICloneable, IGDSyntaxToken
    {
        GDNode _parent;

        /// <summary>
        /// Name of the node Type class.
        /// </summary>
        public string TypeName => GetType().Name;

        /// <summary>
        /// Parent node in a lexical tree
        /// </summary>
        public GDNode Parent
        {
            get => _parent;

            internal set
            {
                if (_parent != null && value != null && value != _parent)
                    RemoveFromParent();

                _parent = value;
            }
        }

        /// <summary>
        /// Nearest class member
        /// </summary>
        public GDClassMember ClassMember => Parents.OfType<GDClassMember>().FirstOrDefault();

        /// <summary>
        /// Main class if exists
        /// </summary>
        public GDClassDeclaration RootClassDeclaration => Parents.OfType<GDClassDeclaration>().FirstOrDefault();

        /// <summary>
        /// Nearest inner class if exists
        /// </summary>
        public GDInnerClassDeclaration InnerClassDeclaration => Parents.OfType<GDInnerClassDeclaration>().FirstOrDefault();

        /// <summary>
        /// Nearest owning class if exists
        /// </summary>
        public IGDClassDeclaration ClassDeclaration => Parents.OfType<IGDClassDeclaration>().FirstOrDefault();

        /// <summary>
        /// All parent nodes enumeration
        /// </summary>
        public IEnumerable<GDNode> Parents
        {
            get
            {
                var p = Parent;

                while (p != null)
                {
                    yield return p;
                    p = p.Parent;
                }
            }
        }

        /// <summary>
        /// Removes this node from parent or do nothing if <see cref="Parent"/> <see langword="null"/>
        /// </summary>
        public bool RemoveFromParent()
        {
            return Parent?.RemoveChild(this) ?? false;
        }

        /// <summary>
        /// Adds token string representation to <see cref="StringBuilder"/> instance.
        /// </summary>
        /// <param name="builder"></param>
        public virtual void AppendTo(StringBuilder builder)
        {
            builder.Append(ToString());
        }

        /// <summary>
        /// Creates deep clone of the current token and it's children.
        /// </summary>
        /// <returns>New token with all children (if node)</returns>
        public abstract GDSyntaxToken Clone();

        /// <summary>
        /// Starting token's line in the code which is represented by the tree. Calculating property.
        /// </summary>
        public int StartLine
        {
            get
            {
                var parent = _parent;

                if (parent == null)
                    return 0;

                var tokensBefore = parent.Form.GetTokensBefore(this);

                return parent.StartLine + tokensBefore.Sum(x => x.NewLinesCount);
            }
        }

        /// <summary>
        /// Ending token's line in the code which is represented by the tree. Calculating property.
        /// </summary>
        public int EndLine
        {
            get
            {
                var parent = _parent;

                if (parent == null)
                    return 0;

                var tokensBefore = parent.Form.GetTokensBefore(this);

                return parent.StartLine + tokensBefore.Sum(x => x.NewLinesCount) + NewLinesCount;
            }
        }

        /// <summary>
        /// Starting token's column in the code which is represented by the tree. Calculating property.
        /// </summary>
        public int StartColumn
        {
            get
            {
                var parent = _parent;

                if (parent == null)
                    return 0;

                int start = 0;
                bool found = false;

                foreach (var item in parent.TokensReversed)
                {
                    if (item == this)
                    {
                        found = true;
                        continue;
                    }

                    if (!found)
                        continue;

                    if (item is GDNode node)
                    {
                        int tokensCount;
                        if (node.CheckNewLine(out tokensCount))
                        {
                            return start + tokensCount;
                        }
                        else
                        {
                            start += tokensCount;
                        }
                    }
                    else
                    {
                        if (item.NewLinesCount == 0)
                            start += item.Length;
                        else
                            return start;
                    }
                }

                return parent.StartColumn + start;
            }
        }

        /// <summary>
        /// Ending token's column in the code which is represented by the tree. Calculating property.
        /// </summary>
        public virtual int EndColumn => StartColumn + Length;

        /// <summary>
        /// The length of the code (represented by the token and its children) in characters. Calculating property.
        /// </summary>
        public abstract int Length { get; }

        /// <summary>
        /// New line characters in the token. Checks children. Calculating property.
        /// </summary>
        public abstract int NewLinesCount { get; }

        /// <summary>
        /// Returns the next node in parent or null
        /// </summary>
        public GDNode NextNode => Parent?.Form.NextAfter<GDNode>(this);

        /// <summary>
        /// Returns the next token in parent or null
        /// </summary>
        public GDSyntaxToken NextToken => Parent?.Form.NextTokenAfter(this);

        /// <summary>
        /// Returns the previous node in parent or null
        /// </summary>
        public GDNode PreviousNode => Parent?.Form.PreviousBefore<GDNode>(this);

        /// <summary>
        /// Returns the previous token in parent or null
        /// </summary>
        public GDSyntaxToken PreviousToken => Parent?.Form.PreviousTokenBefore(this);


        /// <summary>
        /// Checks whether the token start in range
        /// </summary>
        /// <returns>True if start in range</returns>
        public bool IsStartInRange(int startLine, int startColumn, int endLine, int endColumn)
        {
            if (startLine == endLine)
            {
                if (startColumn > endColumn)
                    throw new ArgumentOutOfRangeException("startColumn must be lower or equals endColumn");

                var column = StartColumn;
                return StartLine == startLine && column >= startColumn && column <= endColumn;
            }
            else
            {
                if (startLine > endLine)
                    throw new ArgumentOutOfRangeException("startLine must be lower than endLine");
                
                var line = StartLine;

                if (line == startLine)
                    return StartColumn >= startColumn;

                if (line == endLine)
                    return StartColumn <= endColumn;

                if (line < startLine || line > endLine)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Checks wrether the entire token lies in range.
        /// </summary>
        /// <returns>True if lies</returns>
        public bool IsWholeInRange(int startLine, int startColumn, int endLine, int endColumn)
        {
            if (startLine > endLine)
                throw new ArgumentOutOfRangeException("StartLine must be equals or lower than endLine");

            if (startLine == endLine && startColumn > endColumn)
                throw new ArgumentOutOfRangeException("StartColumn must be equals or lower than endColumn");

            var tokenStartLine = StartLine;

            if (startLine > tokenStartLine)
                return false;

            if (startLine == tokenStartLine && StartColumn < startColumn)
                return false;

            var tokenEndLine = EndLine;

            if (endLine < tokenEndLine)
                return false;

            if (endLine == tokenEndLine && EndColumn > endColumn)
                return false;

            return true;
        }

        /// <summary>
        /// Checks wrether the entire token contains the position.
        /// </summary>
        /// <returns>True if position at the end or at the start of the token. Also true if the position in the midst of the token. Otherwise false</returns>
        public bool ContainsPosition(int line, int column)
        {
            var startLine = StartLine;
            var endLine = EndLine;

            if (endLine == line)
            {
                if (startLine == line)
                {
                    return column >= StartColumn && column <= EndColumn;
                }
                else
                {
                    return column <= EndColumn;
                }
            }
            else
            {
                if (startLine == line)
                {
                    return column >= StartColumn;
                }
                else
                {
                    if (line >= startLine && line < endLine)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns enumeration of visible Identifiers of variables defined before the token.
        /// </summary>
        /// <param name="owningMember">The Class member which contains the token</param>
        /// <returns>Enumeration</returns>
        public List<GDIdentifier> ExtractAllMethodScopeVisibleDeclarationsFromParents(out GDClassMember owningMember)
        {
            owningMember = null;

            var startLine = StartLine;

            GDNode node = (this is GDNode n) ? n : Parent;

            var results = new List<GDIdentifier>();

            while (true)
            {
                node = node.Parent;

                if (node == null || node is GDClassDeclaration || node is GDInnerClassDeclaration)
                    break;

                if (node is GDMethodDeclaration method)
                    owningMember = method;

                foreach (var item in node.GetMethodScopeDeclarations(startLine))
                    results.Add(item);
            }

            return results;
        }

        /// <summary>
        /// Returns enumeration of visible Identifiers of variables defined before the token.
        /// </summary>
        /// <param name="owningMember">The Class member which contains the token</param>
        /// <returns>Enumeration</returns>
        public List<GDIdentifier> ExtractAllMethodScopeVisibleDeclarationsFromParents(int beforeLine, out GDClassMember owningMember)
        {
            owningMember = null;

            GDNode node = (this is GDNode n) ? n : Parent;

            var results = new List<GDIdentifier>();

            while (true)
            {
                node = node.Parent;

                if (node == null || node is GDClassDeclaration || node is GDInnerClassDeclaration)
                    break;

                if (node is GDMethodDeclaration method)
                    owningMember = method;

                foreach (var item in node.GetMethodScopeDeclarations(beforeLine))
                    results.Add(item);
            }

            return results;
        }


        object ICloneable.Clone()
        {
            return Clone();
        }

        [DebuggerHidden]
        internal string DebuggerView => $"{TypeName} '{ToString()}'";
    }
}
