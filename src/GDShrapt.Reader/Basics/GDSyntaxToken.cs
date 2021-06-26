using System;
using System.Diagnostics;
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
        public string NodeName => GetType().Name;

        /// <summary>
        /// Parent node in a lexical tree
        /// </summary>
        public GDNode Parent
        {
            get => _parent;

            internal set
            {
                if (_parent != null && _parent != value)
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

        object ICloneable.Clone()
        {
            return Clone();
        }

        [DebuggerHidden]
        internal string DebuggerView => $"{NodeName} '{ToString()}'";
    }
}
