namespace GDShrapt.Reader
{
    public interface IVisitor
    {
        void EnterNode(GDNode node);
        void LeftNode();
    }
}