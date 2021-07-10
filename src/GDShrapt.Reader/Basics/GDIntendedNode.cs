namespace GDShrapt.Reader
{
    public abstract class GDIntendedNode : GDNode, ITokenOrSkipReceiver<GDIntendation>
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

        void ITokenSkipReceiver<GDIntendation>.HandleReceivedTokenSkip()
        {
            // Ignore?
        }
    }
}
