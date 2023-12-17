using System.Diagnostics;

namespace GDShrapt.Reader
{
    public class GDStringPartsList : GDSeparatedList<GDStringPart, GDMultiLineSplitToken>, 
        ITokenOrSkipReceiver<GDStringPart>,
        ITokenOrSkipReceiver<GDMultiLineSplitToken>
    {
        bool _ended;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_ended)
            {
                state.PopAndPass(c);
                return;
            }

            this.ResolveStringPart(c, state);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _ended = true;
            state.PopAndPassNewLine();
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            base.HandleLeftSlashChar(state);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDStringPartsList();
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        void ITokenReceiver<GDStringPart>.HandleReceivedToken(GDStringPart token)
        {
            ListForm.AddToEnd(token);
        }

        void ITokenSkipReceiver<GDStringPart>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }

        void ITokenReceiver<GDMultiLineSplitToken>.HandleReceivedToken(GDMultiLineSplitToken token)
        {
            ListForm.AddToEnd(token);
        }

        void ITokenSkipReceiver<GDMultiLineSplitToken>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }
    }
}
