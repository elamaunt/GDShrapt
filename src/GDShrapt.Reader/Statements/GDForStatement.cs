﻿using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public sealed class GDForStatement : GDStatement,
        ITokenOrSkipReceiver<GDForKeyword>,
        ITokenOrSkipReceiver<GDInKeyword>,
        ITokenOrSkipReceiver<GDIdentifier>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDStatementsList>
    {
        public GDForKeyword ForKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDIdentifier Variable
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDInKeyword InKeyword
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDExpression Collection
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }
        public GDColon Colon
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }
        public GDExpression Expression
        {
            get => _form.Token5;
            set => _form.Token5 = value;
        }

        public GDStatementsList Statements 
        {
            get => _form.Token6 ?? (_form.Token6 = new GDStatementsList(LineIntendation + 1));
            set => _form.Token6 = value;
        }

        public enum State
        {
            For,
            Variable,
            In,
            Collection,
            Colon,
            Expression,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDForKeyword, GDIdentifier, GDInKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDForKeyword, GDIdentifier, GDInKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> TypedForm => _form;

        internal GDForStatement(int lineIntendation)
            : base(lineIntendation)
        {
            _form = new GDTokensForm<State, GDForKeyword, GDIdentifier, GDInKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        public GDForStatement()
        {
            _form = new GDTokensForm<State, GDForKeyword, GDIdentifier, GDInKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveSpaceToken(c, state))
                return;

            switch (_form.State)
            {
                case State.For:
                    state.Push(new GDKeywordResolver<GDForKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Variable:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.In:
                    state.Push(new GDKeywordResolver<GDInKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Collection:
                    state.Push(new GDExpressionResolver(this, Intendation));
                    state.PassChar(c);
                    break;
                case State.Colon:
                    state.Push(new GDSingleCharTokenResolver<GDColon>(this));
                    state.PassChar(c);
                    break;
                case State.Expression:
                    this.ResolveExpression(c, state, Intendation);
                    break;
                case State.Statements:
                    this.HandleAsInvalidToken(c, state, x => x.IsSpace() || x.IsNewLine());
                    break;
                default:
                    state.Pop();
                    state.PassChar(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.In:
                case State.Variable:
                case State.Colon:
                case State.Collection:
                case State.Expression:
                case State.Statements:
                    _form.State = State.Completed;
                    state.Push(Statements);
                    state.PassNewLine();
                    break;
                default:
                    state.Pop();
                    state.PassNewLine();
                    break;
            }
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDForStatement();
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
            var v = Variable;
            if (v != null)
                yield return v;
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.Collection))
            {
                _form.State = State.Colon;
                Collection = token;
                return;
            }

            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Completed;
                Expression = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Collection))
            {
                _form.State = State.Colon;
                return;
            }

            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Statements;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                Colon = token;
                _form.State = State.Expression;
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

        void ITokenReceiver<GDInKeyword>.HandleReceivedToken(GDInKeyword token)
        {
            if (_form.IsOrLowerState(State.In))
            {
                InKeyword = token;
                _form.State = State.Collection;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDInKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.In))
            {
                _form.State = State.Collection;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDForKeyword>.HandleReceivedToken(GDForKeyword token)
        {
            if (_form.IsOrLowerState(State.For))
            {
                ForKeyword = token;
                _form.State = State.Variable;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDForKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.For))
            {
                _form.State = State.Variable;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDIdentifier>.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.IsOrLowerState(State.Variable))
            {
                Variable = token;
                _form.State = State.In;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Variable))
            {
                _form.State = State.In;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDStatementsList>.HandleReceivedToken(GDStatementsList token)
        {
            if (_form.IsOrLowerState(State.Statements))
            {
                Statements = token;
                _form.State = State.Completed;
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
