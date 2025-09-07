﻿namespace GDShrapt.Reader
{
    public sealed class GDVariableDeclaration : GDIdentifiableClassMember,
        ITokenOrSkipReceiver<GDConstKeyword>,
        ITokenOrSkipReceiver<GDStaticKeyword>,
        ITokenOrSkipReceiver<GDIdentifier>,
        ITokenOrSkipReceiver<GDVarKeyword>,
        ITokenOrSkipReceiver<GDTypeNode>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDAssign>,
        IIntendedTokenOrSkipReceiver<GDAccessorDeclaration>,
        ITokenOrSkipReceiver<GDComma>
    {
        public GDConstKeyword ConstKeyword 
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDStaticKeyword StaticKeyword
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDVarKeyword VarKeyword
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public override GDIdentifier Identifier
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        public GDColon TypeColon
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }

        public GDTypeNode Type
        {
            get => _form.Token5;
            set => _form.Token5 = value;
        }

        public GDAssign Assign
        {
            get => _form.Token6;
            set => _form.Token6 = value;
        }

        public GDExpression Initializer
        {
            get => _form.Token7;
            set => _form.Token7 = value;
        }

        public GDColon Colon
        {
            get => _form.Token8;
            set => _form.Token8 = value;
        }

        public GDAccessorDeclaration FirstAccessorDeclarationNode
        {
            get => _form.Token9;
            set => _form.Token9 = value;
        }

        public GDComma Comma
        {
            get => _form.Token10;
            set => _form.Token10 = value;
        }

        public GDAccessorDeclaration SecondAccessorDeclarationNode
        {
            get => _form.Token11;
            set => _form.Token11 = value;
        }

        public bool IsConstant => ConstKeyword != null;
        public override bool IsStatic => StaticKeyword != null;

        public enum State
        { 
            Const,
            Static,
            Var,
            Identifier,
            TypeColon,
            Type,
            Assign,
            Initializer,
            Colon,
            FirstAccessorDeclarationNode,
            Comma,
            SecondAccessorDeclarationNode,
            Completed
        }

        readonly GDTokensForm<State, GDConstKeyword, GDStaticKeyword, GDVarKeyword, GDIdentifier, GDColon, GDTypeNode, GDAssign, GDExpression, GDColon, GDAccessorDeclaration, GDComma, GDAccessorDeclaration> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDConstKeyword, GDStaticKeyword, GDVarKeyword, GDIdentifier, GDColon, GDTypeNode, GDAssign, GDExpression, GDColon, GDAccessorDeclaration, GDComma, GDAccessorDeclaration> TypedForm => _form;

        internal GDVariableDeclaration(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDConstKeyword, GDStaticKeyword, GDVarKeyword, GDIdentifier, GDColon, GDTypeNode, GDAssign, GDExpression, GDColon, GDAccessorDeclaration, GDComma, GDAccessorDeclaration>(this);
        }

        public GDVariableDeclaration()
        {
            _form = new GDTokensForm<State, GDConstKeyword, GDStaticKeyword, GDVarKeyword, GDIdentifier, GDColon, GDTypeNode, GDAssign, GDExpression, GDColon, GDAccessorDeclaration, GDComma, GDAccessorDeclaration>(this);
        }

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
                case State.Const:
                    state.PushAndPass(new GDKeywordResolver<GDConstKeyword>(this), c);
                    break;
                case State.Static:
                    state.PushAndPass(new GDKeywordResolver<GDStaticKeyword>(this), c);
                    break;
                case State.Var:
                    state.PushAndPass(new GDKeywordResolver<GDVarKeyword>(this), c);
                    break;
                case State.Identifier:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.TypeColon:
                case State.Colon:
                    state.PushAndPass(new GDSingleCharTokenResolver<GDColon>(this), c);
                    break;
                case State.Type:
                    this.ResolveType(c, state);
                    break;
                case State.Assign:
                    state.PushAndPass(new GDSingleCharTokenResolver<GDAssign>(this), c);
                    break;
                case State.Initializer:
                    state.PushAndPass(new GDExpressionResolver(this, Intendation), c);
                    break;
                case State.FirstAccessorDeclarationNode:
                    state.PushAndPass(new GDSetGetAccessorsResolver<GDVariableDeclaration>(this, this, true, false, Intendation + 1), c);
                    break;
                case State.Comma:
                    state.PushAndPass(new GDSetGetAccessorsResolver<GDVariableDeclaration>(this, this, true, true, Intendation + 1), c);
                    return;
                case State.SecondAccessorDeclarationNode:
                    state.PushAndPass(new GDSetGetAccessorsResolver<GDVariableDeclaration>(this, this, Comma != null, false, Intendation + 1), c);
                    break;
                default:
                    this.HandleAsInvalidToken(c, state, x => x.IsNewLine());
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_form.IsOrLowerState(State.FirstAccessorDeclarationNode))
            {
                state.PushAndPassNewLine(new GDSetGetAccessorsResolver<GDVariableDeclaration>(this, this, false, false, Intendation + 1));
                return;
            }

            if (_form.State == State.Comma)
            {
                state.PushAndPassNewLine(new GDSetGetAccessorsResolver<GDVariableDeclaration>(this, this, false, true, Intendation + 1));
                return;
            }

            if (_form.State == State.SecondAccessorDeclarationNode)
            {
                state.PushAndPassNewLine(new GDSetGetAccessorsResolver<GDVariableDeclaration>(this, this, Comma != null, false, Intendation + 1));
                return;
            }

            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDVariableDeclaration();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDConstKeyword>.HandleReceivedToken(GDConstKeyword token)
        {
            if (_form.IsOrLowerState(State.Const))
            {
                _form.State = State.Var;
                ConstKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDConstKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Const))
            {
                _form.State = State.Static;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDStaticKeyword>.HandleReceivedToken(GDStaticKeyword token)
        {
            if (_form.IsOrLowerState(State.Static))
            {
                _form.State = State.Var;
                StaticKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDStaticKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Static))
            {
                _form.State = State.Var;
                return;
            }

            throw new GDInvalidStateException();
        }
        
        void ITokenReceiver<GDVarKeyword>.HandleReceivedToken(GDVarKeyword token)
        {
            if (_form.StateIndex <= (int)State.Var)
            {
                _form.State = State.Identifier;
                VarKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDVarKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.StateIndex <= (int)State.Var)
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.IsOrLowerState(State.TypeColon))
            {
                _form.State = State.Type;
                TypeColon = token;
                return;
            }

            if (_form.IsOrLowerState(State.Colon))
            {
                Colon = token;
                _form.State = State.FirstAccessorDeclarationNode;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.TypeColon))
            {
                _form.State = State.Assign;
                return;
            }

            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.FirstAccessorDeclarationNode;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDAssign>.HandleReceivedToken(GDAssign token)
        {
            if (_form.IsOrLowerState(State.Assign))
            {
                _form.State = State.Initializer;
                Assign = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDAssign>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Assign))
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.Initializer))
            {
                _form.State = State.Colon;
                Initializer = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Initializer))
            {
                _form.State = State.Colon;

                return;
            }

            throw new GDInvalidStateException();
        }
       
        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            if (_form.IsOrLowerState(State.Comma))
            {
                _form.State = State.SecondAccessorDeclarationNode;
                Comma = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDComma>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Comma))
            {
                _form.State = State.SecondAccessorDeclarationNode;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDIdentifier>.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.TypeColon;
                Identifier = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.TypeColon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDTypeNode>.HandleReceivedToken(GDTypeNode token)
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.Assign;
                Type = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDTypeNode>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.Assign;
                return;
            }

            throw new GDInvalidStateException();
        }

        void IIntendedTokenReceiver.HandleReceivedToken(GDIntendation token)
        {
            _form.AddBeforeActiveToken(token);
        }

        void INewLineReceiver.HandleReceivedToken(GDNewLine token)
        {
            _form.AddBeforeActiveToken(token);
        }

        void ITokenReceiver<GDAccessorDeclaration>.HandleReceivedToken(GDAccessorDeclaration token)
        {
            if (_form.IsOrLowerState(State.FirstAccessorDeclarationNode))
            {
                FirstAccessorDeclarationNode = token;
                _form.State = State.Comma;
                return;
            }

            if (_form.State == State.SecondAccessorDeclarationNode)
            {
                SecondAccessorDeclarationNode = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDAccessorDeclaration>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.FirstAccessorDeclarationNode))
            {
                _form.State = State.Completed;
                return;
            }

            if (_form.IsOrLowerState(State.SecondAccessorDeclarationNode))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}