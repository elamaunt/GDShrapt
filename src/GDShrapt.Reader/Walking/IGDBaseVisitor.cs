namespace GDShrapt.Reader
{
    public interface IGDBaseVisitor
    {
        void EnterNode(GDNode node);
        void LeftNode();
    }
}