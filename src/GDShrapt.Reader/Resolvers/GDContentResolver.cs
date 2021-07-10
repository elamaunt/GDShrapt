namespace GDShrapt.Reader
{
    internal class GDContentResolver : GDIntendedResolver
    {
        new IIntendedTokenReceiver<GDNode> Owner { get; }
        public GDContentResolver(IIntendedTokenReceiver<GDNode> owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
            Owner = owner;
        }


        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleNewLineAfterIntendation(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleSharpCharAfterIntendation(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}
