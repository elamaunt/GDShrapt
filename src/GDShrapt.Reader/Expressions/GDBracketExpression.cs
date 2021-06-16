namespace GDShrapt.Reader
{
    public sealed class GDBracketExpression : GDExpression, 
        ITokenReceiver<GDOpenBracket>,
        IExpressionsReceiver,
        ITokenReceiver<GDCloseBracket>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Brackets);

        internal GDOpenBracket OpenBracket { get; set; }
        public GDExpression InnerExpression { get; set; }
        internal GDCloseBracket CloseBracket { get; set; }

        enum State
        {
            OpenBracket,
            Expression,
            CloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDOpenBracket, GDExpression, GDCloseBracket> _form = new GDTokensForm<State, GDOpenBracket, GDExpression, GDCloseBracket>();
        internal override GDTokensForm Form => _form;
        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
                case State.OpenBracket:
                    this.ResolveOpenBracket(c, state);
                    break;
                case State.Expression:
                    this.ResolveExpression(c, state);
                    break;
                case State.CloseBracket:
                    this.ResolveCloseBracket(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.OpenBracket:
                    state.PopAndPassNewLine();
                    break;
                case State.Expression:
                case State.CloseBracket:
                    _form.AddBeforeActiveToken(new GDNewLine());
                    break;
                default:
                    state.PopAndPassNewLine();
                    break;
            }
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedToken(GDOpenBracket token)
        {
            throw new System.NotImplementedException();
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedTokenSkip()
        {
            throw new System.NotImplementedException();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            throw new System.NotImplementedException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            throw new System.NotImplementedException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedToken(GDCloseBracket token)
        {
            throw new System.NotImplementedException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedTokenSkip()
        {
            throw new System.NotImplementedException();
        }
    }
}