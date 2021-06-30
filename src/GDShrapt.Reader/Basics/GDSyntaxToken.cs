using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Basic syntax node.
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public abstract class GDSyntaxToken : GDReader, ICloneable
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
        /// Creates deep clone of the current token and it's children
        /// </summary>
        /// <returns>New token with all children (if node)</returns>
        public abstract GDSyntaxToken Clone();

        /// <summary>
        /// Starting token's line in the code which is represented by the tree.
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
        /// Ending token's line in the code which is represented by the tree.
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
        /// Starting token's column in the code which is represented by the tree.
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
        /// Ending token's column in the code which is represented by the tree.
        /// </summary>
        public virtual int EndColumn => StartColumn + Length;

        /// <summary>
        /// The length of the code (represented by the token) in characters
        /// </summary>
        public abstract int Length { get; }

        /// <summary>
        /// New line characters in the token
        /// </summary>
        public abstract int NewLinesCount { get; }


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

       /* public bool IsWholeInRange(int startLine, int startColumn, int endLine, int endColumn)
        {
            if (startLine == endLine)
            {
                var column = Column;
                return Line == startLine && column >= startColumn && column + Length <= endColumn;
            }
            else
            {
                if (startLine > endLine)
                    throw new ArgumentOutOfRangeException("startLine must be lower than endLine");

                var line = Line;

                if (line == startLine)
                    return Column >= startColumn;

                if (line == endLine)
                    return Column <= endColumn;

                if (line < startLine || line > endLine)
                    return false;

                return true;
            }
        }*/

        object ICloneable.Clone()
        {
            return Clone();
        }

        [DebuggerHidden]
        internal string DebuggerView => $"{TypeName} '{ToString()}'";
    }
}
