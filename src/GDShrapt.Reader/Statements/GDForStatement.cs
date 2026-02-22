using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public sealed class GDForStatement : GDStatement,
        ITokenOrSkipReceiver<GDForKeyword>,
        ITokenOrSkipReceiver<GDInKeyword>,
        ITokenOrSkipReceiver<GDIdentifier>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDTypeNode>,
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
        public GDColon TypeColon
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDTypeNode VariableType
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }
        public GDInKeyword InKeyword
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }
        public GDExpression Collection
        {
            get => _form.Token5;
            set => _form.Token5 = value;
        }
        public GDColon Colon
        {
            get => _form.Token6;
            set => _form.Token6 = value;
        }
        public GDExpression Expression
        {
            get => _form.Token7;
            set => _form.Token7 = value;
        }

        public GDStatementsList Statements
        {
            get => _form.GetOrInit(8, new GDStatementsList(LineIntendation + 1));
            set => _form.Token8 = value;
        }

        public enum State
        {
            For,
            Variable,
            TypeAnnotation,
            Type,
            In,
            Collection,
            Colon,
            Expression,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDForKeyword, GDIdentifier, GDColon, GDTypeNode, GDInKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> _form;
        GDType _pendingTypeName;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDForKeyword, GDIdentifier, GDColon, GDTypeNode, GDInKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> TypedForm => _form;

        internal GDForStatement(int lineIntendation)
            : base(lineIntendation)
        {
            _form = new GDTokensForm<State, GDForKeyword, GDIdentifier, GDColon, GDTypeNode, GDInKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        public GDForStatement()
        {
            _form = new GDTokensForm<State, GDForKeyword, GDIdentifier, GDColon, GDTypeNode, GDInKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            // In State.Type with a pending type name, don't consume spaces at the form level
            // because the space belongs AFTER the type node, not before it
            if (_form.State != State.Type || _pendingTypeName == null)
            {
                if (this.ResolveSpaceToken(c, state))
                    return;
            }

            switch (_form.State)
            {
                case State.For:
                    state.Push(new GDKeywordResolver<GDForKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Variable:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.TypeAnnotation:
                    if (c == ':')
                    {
                        state.RepassPendingChars();
                        state.CompleteActiveCharSequence();
                        this.ResolveColon(c, state);
                    }
                    else
                    {
                        // Not a colon — skip type annotation, go to In
                        _form.State = State.In;
                        state.RepassPendingChars();
                        state.Push(new GDKeywordResolver<GDInKeyword>(this));
                        state.PassChar(c);
                    }
                    break;
                case State.Type:
                    if (_pendingTypeName == null)
                    {
                        if (c.IsIdentifierStartChar())
                        {
                            _pendingTypeName = new GDType();
                            state.PushAndPass(_pendingTypeName, c);
                        }
                        else
                        {
                            // No type found, skip
                            ((ITokenSkipReceiver<GDTypeNode>)this).HandleReceivedTokenSkip();
                            state.PassChar(c);
                        }
                    }
                    else
                    {
                        if (c == '[' && (_pendingTypeName.IsArray || _pendingTypeName.IsDictionary))
                        {
                            // Generic type: Array[int] or Dictionary[String, int]
                            if (_pendingTypeName.IsArray)
                            {
                                var arrayTypeNode = new GDArrayTypeNode();
                                VariableType = arrayTypeNode;
                                _form.State = State.In;
                                state.Push(arrayTypeNode);
                                var seq = _pendingTypeName.Sequence;
                                for (int i = 0; i < seq.Length; i++)
                                    state.PassChar(seq[i]);
                                state.PassChar(c);
                            }
                            else
                            {
                                var dictTypeNode = new GDDictionaryTypeNode();
                                VariableType = dictTypeNode;
                                _form.State = State.In;
                                state.Push(dictTypeNode);
                                var seq = _pendingTypeName.Sequence;
                                for (int i = 0; i < seq.Length; i++)
                                    state.PassChar(seq[i]);
                                state.PassChar(c);
                            }
                            _pendingTypeName = null;
                        }
                        else
                        {
                            // Simple type (int, String, Node, etc.)
                            VariableType = new GDSingleTypeNode() { Type = _pendingTypeName };
                            _pendingTypeName = null;
                            _form.State = State.In;
                            // Pass the char (space before 'in') to be handled in In state
                            if (c.IsSpace())
                            {
                                this.ResolveSpaceToken(c, state);
                            }
                            else
                            {
                                state.Push(new GDKeywordResolver<GDInKeyword>(this));
                                state.PassChar(c);
                            }
                        }
                    }
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
                case State.TypeAnnotation:
                case State.Type:
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

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.TypeAnnotation:
                case State.Type:
                case State.In:
                case State.Variable:
                case State.Colon:
                case State.Collection:
                case State.Expression:
                case State.Statements:
                    _form.State = State.Completed;
                    state.Push(Statements);
                    state.PassCarriageReturnChar();
                    break;
                default:
                    state.Pop();
                    state.PassCarriageReturnChar();
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
            if (_form.IsOrLowerState(State.TypeAnnotation))
            {
                TypeColon = token;
                _form.State = State.Type;
                return;
            }

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
            if (_form.IsOrLowerState(State.TypeAnnotation))
            {
                _form.State = State.In;
                return;
            }

            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Expression;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDTypeNode>.HandleReceivedToken(GDTypeNode token)
        {
            if (_form.IsOrLowerState(State.Type))
            {
                VariableType = token;
                _form.State = State.In;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDTypeNode>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.In;
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
                _form.State = State.TypeAnnotation;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Variable))
            {
                _form.State = State.TypeAnnotation;
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
