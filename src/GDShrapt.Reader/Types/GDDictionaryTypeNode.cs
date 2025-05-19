using System;
using System.Collections.Generic;
using System.Text;

namespace GDShrapt.Reader
{
    public class GDDictionaryTypeNode : GDTypeNode,
        ITokenOrSkipReceiver<GDDictionaryKeyword>,
        ITokenOrSkipReceiver<GDSquareOpenBracket>,
        ITokenOrSkipReceiver<GDComma>,
        ITokenOrSkipReceiver<GDSquareCloseBracket>,
        ITokenOrSkipReceiver<GDTypeNode>
    {
        public override bool IsArray => false;

        public override bool IsDictionary => true;

        public GDDictionaryKeyword DictionaryKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDSquareOpenBracket SquareOpenBracket
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDTypeNode KeyType
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public GDComma Comma
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        public GDTypeNode ValueType
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }

        public GDSquareCloseBracket SquareCloseBracket
        {
            get => _form.Token5;
            set => _form.Token5 = value;
        }

        public enum State
        {
            Dictionary,
            SquareOpenBracket,
            KeyType,
            Comma,
            ValueType,
            SquareCloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDDictionaryKeyword, GDSquareOpenBracket, GDTypeNode, GDComma, GDTypeNode, GDSquareCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDDictionaryKeyword, GDSquareOpenBracket, GDTypeNode, GDComma, GDTypeNode, GDSquareCloseBracket> TypedForm => _form;

        public GDDictionaryTypeNode()
        {
            _form = new GDTokensForm<State, GDDictionaryKeyword, GDSquareOpenBracket, GDTypeNode, GDComma, GDTypeNode, GDSquareCloseBracket>(this);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDDictionaryTypeNode();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Dictionary:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDDictionaryKeyword>(c, state);
                    break;
                case State.SquareOpenBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveSquareOpenBracket(c, state);
                    break;
                case State.KeyType:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveType(c, state);
                    break;
                case State.Comma:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveComma(c, state);
                    break;
                case State.ValueType:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveType(c, state);
                    break;
                case State.SquareCloseBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveSquareCloseBracket(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _form.State = State.Completed;
            state.PopAndPassNewLine();
        }

        void ITokenReceiver<GDDictionaryKeyword>.HandleReceivedToken(GDDictionaryKeyword token)
        {
            if (_form.State == State.Dictionary)
            {
                _form.State = State.SquareOpenBracket;
                DictionaryKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDDictionaryKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Dictionary)
            {
                _form.State = State.SquareOpenBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDSquareOpenBracket>.HandleReceivedToken(GDSquareOpenBracket token)
        {
            if (_form.IsOrLowerState(State.SquareOpenBracket))
            {
                _form.State = State.KeyType;
                SquareOpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSquareOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.SquareOpenBracket))
            {
                _form.State = State.KeyType;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDTypeNode>.HandleReceivedToken(GDTypeNode token)
        {
            if (_form.State == State.KeyType)
            {
                _form.State = State.Comma;
                KeyType = token;
                return;
            }
            else if (_form.State == State.ValueType)
            {
                _form.State = State.SquareCloseBracket;
                ValueType = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDTypeNode>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.KeyType)
            {
                _form.State = State.Comma;
                return;
            }
            else if (_form.State == State.ValueType)
            {
                _form.State = State.SquareCloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            if (_form.IsOrLowerState(State.SquareCloseBracket))
            {
                _form.State = State.ValueType;
                Comma = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDComma>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.SquareCloseBracket))
            {
                _form.State = State.ValueType;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDSquareCloseBracket>.HandleReceivedToken(GDSquareCloseBracket token)
        {
            if (_form.IsOrLowerState(State.SquareCloseBracket))
            {
                _form.State = State.Completed;
                SquareCloseBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSquareCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.SquareCloseBracket))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        public override string BuildName()
        {
            return $"Dictionary[{KeyType?.BuildName()},{ValueType?.BuildName()}]";
        }
    }
}
