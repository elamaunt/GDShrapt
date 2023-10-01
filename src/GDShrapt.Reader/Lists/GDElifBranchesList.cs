namespace GDShrapt.Reader
{
    public sealed class GDElifBranchesList : GDIntendedTokensList<GDElifBranch>
    {
        bool _completed;

        internal GDElifBranchesList(int lineIntendation)
            : base (lineIntendation)
        {
        }

        public GDElifBranchesList()
        {

        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.PushAndPass(new GDElifResolver(this, LineIntendationThreshold), c);
                return;
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDElifResolver(this, LineIntendationThreshold));
                state.PassNewLine();
                return;
            }

            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDElifBranchesList();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }
    }
}
