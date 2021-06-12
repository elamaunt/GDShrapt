namespace GDShrapt.Reader
{
    public abstract class GDIntendedNode : GDNode, IIntendationReceiver
    {
        void IIntendationReceiver.HandleReceivedToken(GDIntendation token)
        {
            Form.AddBeforeActiveToken(token);
        }
    }
}
