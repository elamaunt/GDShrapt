namespace GDShrapt.Reader
{
    public abstract class GDIntendedNode : GDNode, ITokenReceiver<GDIntendation>
    {
        internal int Intendation { get; }

        internal GDIntendedNode(int intendation)
        {
            Intendation = intendation;
        }

        internal GDIntendedNode()
        {
        }

        void ITokenReceiver<GDIntendation>.HandleReceivedToken(GDIntendation token)
        {
            Form.AddBeforeActiveToken(token);
        }
    }
}
