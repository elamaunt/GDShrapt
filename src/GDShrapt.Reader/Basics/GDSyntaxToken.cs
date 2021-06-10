using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Basic syntax node.
    /// </summary>
    public abstract class GDSyntaxToken : GDReader
    {
       //readonly WeakReference<GDNode> _parentWeakRef = new WeakReference<GDNode>(null);
       // WeakReference<LinkedListNode<GDSyntaxToken>> _parentListNodeWeakRef = new WeakReference<LinkedListNode<GDSyntaxToken>>(null);

        /// <summary>
        /// Name of the node Type class.
        /// </summary>
        public string NodeName => GetType().Name;

        /* /// <summary>
         /// Parent node in a lexical tree
         /// </summary>
         public GDNode Parent
         {
             get
             {
                 _parentWeakRef.TryGetTarget(out GDNode parent);
                 return parent;
             }

             internal set
             {
                 _parentWeakRef.SetTarget(value);
             }
         }

         /// <summary>
         /// LinkedListNode reference, if the node has a parent. Used by <see cref="RemoveChild"/> method.
         /// </summary>
         internal LinkedListNode<GDSyntaxToken> ParentLinkedListNode
         {
             get
             {
                 _parentListNodeWeakRef.TryGetTarget(out LinkedListNode<GDSyntaxToken> linkedListNode);
                 return linkedListNode;
             }
         }

         /// <summary>
         /// Removes this node parent or do nothing if <see cref="Parent"/> <see langword="null"/>
         /// </summary>
         public void RemoveFromParent()
         {
             Parent?.RemoveChild(this);
         }*/

        /// <summary>
        /// Adds token string representation to <see cref="StringBuilder"/> instance.
        /// </summary>
        /// <param name="builder"></param>
        public virtual void AppendTo(StringBuilder builder)
        {
            builder.Append(ToString());
        }
    }
}
