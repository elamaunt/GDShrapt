namespace GDShrapt.Reader
{
    internal class GDTypeResolver : GDResolver
    {
        public GDTypeResolver(ITokenReceiver owner)
            : base(owner)
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}
