﻿namespace GDShrapt.Reader
{
    public sealed class GDParameterDeclaration : GDNode,
        IIdentifierReceiver,
        ITokenReceiver<GDColon>,
        ITypeReceiver,
        ITokenReceiver<GDAssign>,
        IExpressionsReceiver
    {
        public GDIdentifier Identifier
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        internal GDColon Colon
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDType Type
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        internal GDAssign Assign
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        public GDExpression DefaultValue
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }

        enum State
        {
            Identifier,
            Colon,
            Type,
            Assign,
            DefaultValue,
            Completed
        }

        readonly GDTokensForm<State, GDIdentifier, GDColon, GDType, GDAssign, GDExpression> _form;
        internal override GDTokensForm Form => _form;
        public GDParameterDeclaration()
        {
            _form = new GDTokensForm<State, GDIdentifier, GDColon, GDType, GDAssign, GDExpression>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveStyleToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Identifier:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
                    break;
                case State.Type:
                    this.ResolveType(c, state);
                    break;
                case State.Assign:
                    this.ResolveAssign(c, state);
                    break;
                case State.DefaultValue:
                    this.ResolveExpression(c, state);
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
            return new GDParameterDeclaration();
        }

        void IIdentifierReceiver.HandleReceivedToken(GDIdentifier token)
        {
            if(_form.State == State.Identifier)
            {
                _form.State = State.Colon;
                Identifier = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IIdentifierReceiver.HandleReceivedIdentifierSkip()
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Type;
                Colon = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Assign;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITypeReceiver.HandleReceivedToken(GDType token)
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Assign;
                Type = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITypeReceiver.HandleReceivedTypeSkip()
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDAssign>.HandleReceivedToken(GDAssign token)
        {
            if (_form.State == State.Assign)
            {
                _form.State = State.DefaultValue;
                Assign = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDAssign>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Assign)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.DefaultValue)
            {
                _form.State = State.Completed;
                DefaultValue = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.DefaultValue)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}