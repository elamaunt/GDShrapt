namespace GDShrapt.Reader
{
    internal class GDContentResolver : GDIntendedResolver
    {
        public GDContentResolver(IIntendationReceiver owner, int lineIntendation) 
            : base(owner, lineIntendation)
        {
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
