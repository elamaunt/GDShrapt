﻿using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDMethodDeclaration : GDIdentifiableClassMember, 
        ITokenOrSkipReceiver<GDStaticKeyword>,
        ITokenOrSkipReceiver<GDFuncKeyword>,
        ITokenOrSkipReceiver<GDIdentifier>,
        ITokenOrSkipReceiver<GDOpenBracket>,
        ITokenOrSkipReceiver<GDParametersList>,
        ITokenOrSkipReceiver<GDCloseBracket>,
        ITokenOrSkipReceiver<GDReturnTypeKeyword>,
        ITokenOrSkipReceiver<GDExpressionsList>,
        ITokenOrSkipReceiver<GDTypeNode>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDStatementsList>,
        ITokenOrSkipReceiver<GDPoint>
    {
        public GDStaticKeyword StaticKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDFuncKeyword FuncKeyword
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public override GDIdentifier Identifier
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public GDOpenBracket OpenBracket
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        public GDParametersList Parameters
        {
            get => _form.Token4 ?? (_form.Token4 = new GDParametersList());
            set => _form.Token4 = value;
        }

        public GDCloseBracket CloseBracket
        {
            get => _form.Token5;
            set => _form.Token5 = value;
        }

        public GDPoint BaseCallPoint
        {
            get => _form.Token6;
            set => _form.Token6 = value;
        }

        public GDOpenBracket BaseCallOpenBracket
        {
            get => _form.Token7;
            set => _form.Token7 = value;
        }

        public GDExpressionsList BaseCallParameters 
        { 
            get => _form.Token8 ?? (_form.Token8 = new GDExpressionsList());
            set => _form.Token8 = value;
        }

        public GDCloseBracket BaseCallCloseBracket
        {
            get => _form.Token9;
            set => _form.Token9 = value;
        }

        public GDReturnTypeKeyword ReturnTypeKeyword
        {
            get => _form.Token10;
            set => _form.Token10 = value;
        }

        public GDTypeNode ReturnType
        {
            get => _form.Token11;
            set => _form.Token11 = value;
        }

        public GDColon Colon
        {
            get => _form.Token12;
            set => _form.Token12 = value;
        }

        public GDExpression Expression
        {
            get => _form.Token13;
            set => _form.Token13 = value;
        }

        public GDStatementsList Statements
        { 
            get => _form.Token14 ?? (_form.Token14 = new GDStatementsList(Intendation + 1));
            set => _form.Token14 = value;
        }

        public override bool IsStatic => StaticKeyword != null;

        public enum State
        {
            Static,
            Func,
            Identifier,
            OpenBracket,
            Parameters,
            CloseBracket,

            BaseCallPoint,
            BaseCallOpenBracket,
            BaseCallParameters,
            BaseCallCloseBracket,

            ReturnTypeKeyword,
            Type,
            Colon,
            Expression,
            Statements,
            Completed,
        }

        readonly GDTokensForm<State, GDStaticKeyword, GDFuncKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket, GDPoint, GDOpenBracket, GDExpressionsList, GDCloseBracket, GDReturnTypeKeyword, GDTypeNode, GDColon, GDExpression, GDStatementsList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDStaticKeyword, GDFuncKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket, GDPoint, GDOpenBracket, GDExpressionsList, GDCloseBracket, GDReturnTypeKeyword, GDTypeNode, GDColon, GDExpression, GDStatementsList> TypedForm => _form;

        internal GDMethodDeclaration(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDStaticKeyword, GDFuncKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket, GDPoint, GDOpenBracket, GDExpressionsList, GDCloseBracket, GDReturnTypeKeyword, GDTypeNode, GDColon, GDExpression, GDStatementsList>(this);
        }

        public GDMethodDeclaration()
        {
            _form = new GDTokensForm<State, GDStaticKeyword, GDFuncKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket, GDPoint, GDOpenBracket, GDExpressionsList, GDCloseBracket, GDReturnTypeKeyword, GDTypeNode, GDColon, GDExpression, GDStatementsList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c) && 
                _form.State != State.Parameters &&
                _form.State != State.BaseCallParameters) 
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
                case State.Static:
                    this.ResolveKeyword<GDStaticKeyword>(c, state);
                    break;
                case State.Func:
                    this.ResolveKeyword<GDFuncKeyword>(c, state);
                    break;
                case State.Identifier:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.OpenBracket:
                    this.ResolveOpenBracket(c, state);
                    break;
                case State.Parameters:
                    _form.State = State.CloseBracket;
                    state.PushAndPass(Parameters, c);
                    break;
                case State.CloseBracket:
                    this.ResolveCloseBracket(c, state);
                    break;

                case State.BaseCallPoint:
                    this.ResolvePoint(c, state);
                    break;
                case State.BaseCallOpenBracket:
                    this.ResolveOpenBracket(c, state);
                    break;
                case State.BaseCallParameters:
                    _form.State = State.BaseCallCloseBracket;
                    state.PushAndPass(BaseCallParameters, c);
                    break;
                case State.BaseCallCloseBracket:
                    this.ResolveCloseBracket(c, state);
                    break;

                case State.ReturnTypeKeyword:
                    this.ResolveKeyword<GDReturnTypeKeyword>(c, state);
                    break;
                case State.Type:
                    this.ResolveType(c, state);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
                    break;

                case State.Expression:
                    this.ResolveExpression(c, state, Intendation);
                    break;
                case State.Statements:
                    this.HandleAsInvalidToken(c, state, x => x.IsSpace() || x.IsNewLine());
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_form.IsOrLowerState(State.Parameters))
            {
                _form.State = State.CloseBracket;
                state.PushAndPassNewLine(Parameters);
                return;
            }

            if (_form.IsOrLowerState(State.BaseCallParameters))
            {
                _form.State = State.BaseCallCloseBracket;
                state.PushAndPassNewLine(BaseCallParameters);
                return;
            }

            if (_form.StateIndex <= (int)State.Statements)
            {
                _form.State = State.Completed;
                state.PushAndPassNewLine(Statements);
                return;
            }

            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDMethodDeclaration();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        public override IEnumerable<GDIdentifier> GetMethodScopeDeclarations(int? beforeLine = null)
        {
            return Parameters.Select(x => x.Identifier).Where(x => x != null);
        }

        void ITokenReceiver<GDStaticKeyword>.HandleReceivedToken(GDStaticKeyword token)
        {
            if (_form.IsOrLowerState(State.Static))
            {
                _form.State = State.Func;
                StaticKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDStaticKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Static))
            {
                _form.State = State.Func;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDFuncKeyword>.HandleReceivedToken(GDFuncKeyword token)
        {
            if (_form.StateIndex <= (int)State.Func)
            {
                _form.State = State.Identifier;
                FuncKeyword = token;
                return;
            }

            throw new GDInvalidStateException(); 
        }

        void ITokenSkipReceiver<GDFuncKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.StateIndex <= (int)State.Func)
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
                _form.State = State.OpenBracket;
                Identifier = token;
                return;
            }

            throw new GDInvalidStateException();
        }
        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.OpenBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedToken(GDOpenBracket token)
        {
            if (_form.IsOrLowerState(State.OpenBracket))
            {
                _form.State = State.Parameters;
                OpenBracket = token;
                return;
            }

            if (_form.IsOrLowerState(State.BaseCallOpenBracket))
            {
                _form.State = State.BaseCallParameters;
                BaseCallOpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.OpenBracket))
            {
                _form.State = State.Parameters;
                return;
            }

            if (_form.IsOrLowerState(State.BaseCallOpenBracket))
            {
                _form.State = State.BaseCallParameters;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedToken(GDCloseBracket token)
        {
            if (_form.IsOrLowerState(State.CloseBracket))
            {
                _form.State = State.BaseCallPoint;
                CloseBracket = token;
                return;
            }

            if (_form.IsOrLowerState(State.BaseCallCloseBracket))
            {
                _form.State = State.ReturnTypeKeyword;
                BaseCallCloseBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.CloseBracket))
            {
                _form.State = State.BaseCallPoint;
                return;
            }

            if (_form.IsOrLowerState(State.BaseCallCloseBracket))
            {
                _form.State = State.ReturnTypeKeyword;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDReturnTypeKeyword>.HandleReceivedToken(GDReturnTypeKeyword token)
        {
            if (_form.IsOrLowerState(State.ReturnTypeKeyword))
            {
                _form.State = State.Type;
                ReturnTypeKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }
        void ITokenReceiver<GDPoint>.HandleReceivedToken(GDPoint token)
        {
            if (_form.IsOrLowerState(State.BaseCallPoint))
            {
                _form.State = State.BaseCallOpenBracket;
                BaseCallPoint = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDPoint>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.BaseCallPoint))
            {
                _form.State = State.ReturnTypeKeyword;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDReturnTypeKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.ReturnTypeKeyword))
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDTypeNode>.HandleReceivedToken(GDTypeNode token)
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.Colon;
                ReturnType = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDTypeNode>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Expression;
                Colon = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Expression;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDParametersList>.HandleReceivedToken(GDParametersList token)
        {
            if (_form.IsOrLowerState(State.Parameters))
            {
                _form.State = State.CloseBracket;
                Parameters = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDParametersList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Parameters))
            {
                _form.State = State.CloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpressionsList>.HandleReceivedToken(GDExpressionsList token)
        {
            if (_form.IsOrLowerState(State.BaseCallParameters))
            {
                _form.State = State.BaseCallCloseBracket;
                BaseCallParameters = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpressionsList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.BaseCallParameters))
            {
                _form.State = State.BaseCallCloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Statements;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Statements;
                Expression = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDStatementsList>.HandleReceivedToken(GDStatementsList token)
        {
            if (_form.IsOrLowerState(State.Statements))
            {
                _form.State = State.Completed;
                Statements = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDStatementsList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Statements))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}