﻿namespace GDShrapt.Reader
{
    public sealed class GDMatchCaseVariableExpression : GDExpression,
        ITokenOrSkipReceiver<GDVarKeyword>,
        ITokenOrSkipReceiver<GDIdentifier>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.MatchCaseVariable);

        public GDVarKeyword VarKeyword 
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDIdentifier Identifier
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public enum State
        {
            Var,
            Identifier,
            Completed
        }

        readonly GDTokensForm<State, GDVarKeyword, GDIdentifier> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDVarKeyword, GDIdentifier> TypedForm => _form;
        public GDMatchCaseVariableExpression()
        {
            _form = new GDTokensForm<State, GDVarKeyword, GDIdentifier>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Var:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDVarKeyword>(c, state);
                    break;
                case State.Identifier:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveIdentifier(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDMatchCaseVariableExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDVarKeyword>.HandleReceivedToken(GDVarKeyword token)
        { 
            if (_form.IsOrLowerState(State.Var))
            {
                _form.State = State.Identifier;
                VarKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDVarKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Var))
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDIdentifier>.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.Completed;
                Identifier = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
