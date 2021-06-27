namespace GDShrapt.Reader
{
    public abstract class GDIntendedNode : GDNode, IIntendationReceiver
    {
        internal int Intendation { get; }

        internal GDIntendedNode(int intendation)
        {
            Intendation = intendation;
        }

        internal GDIntendedNode()
        {
        }

        void IIntendationReceiver.HandleReceivedToken(GDIntendation token)
        {
            Form.AddBeforeActiveToken(token);
        }

        public void FormatIntendation()
        {
            

            

            //AllNodes
        }
    }
}
