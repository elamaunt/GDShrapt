using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDInnerClassDeclaration : GDClassMember, IGDClassDeclaration,
        ITokenOrSkipReceiver<GDClassKeyword>,
        ITokenOrSkipReceiver<GDIdentifier>,
        ITokenOrSkipReceiver<GDExtendsKeyword>,
        ITokenOrSkipReceiver<GDString>,
        ITokenOrSkipReceiver<GDType>,
        ITokenOrSkipReceiver<GDClassMembersList>,
        ITokenOrSkipReceiver<GDColon>
    {
        public IEnumerable<GDVariableDeclaration> Variables => Members.OfType<GDVariableDeclaration>();
        public IEnumerable<GDMethodDeclaration> Methods => Members.OfType<GDMethodDeclaration>();
        public IEnumerable<GDEnumDeclaration> Enums => Members.OfType<GDEnumDeclaration>();
        public IEnumerable<GDInnerClassDeclaration> InnerClasses => Members.OfType<GDInnerClassDeclaration>();

        public GDClassKeyword ClassKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public override GDIdentifier Identifier
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDExtendsKeyword Extends
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDType BaseType
        {
            get => (GDType)_form.Token3;
            set => _form.Token3 = value;
        }
        public GDString BaseTypePath
        {
            get => (GDString)_form.Token3;
            set => _form.Token3 = value;
        }
        public GDColon Colon
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }
        public GDClassMembersList Members
        {
            get => _form.Token5 ?? (_form.Token5 = new GDClassMembersList(Intendation + 1));
            set => _form.Token5 = value;
        }

        public enum State
        {
            Class,
            Identifier,
            Extends,
            BaseTypePath,
            BaseType,
            Colon,
            Members,
            Completed
        }

        readonly GDTokensForm<State, GDClassKeyword, GDIdentifier, GDExtendsKeyword, GDDataToken, GDColon, GDClassMembersList> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDClassKeyword, GDIdentifier, GDExtendsKeyword, GDDataToken, GDColon, GDClassMembersList> TypedForm => _form;

        internal GDInnerClassDeclaration(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDClassKeyword, GDIdentifier, GDExtendsKeyword, GDDataToken, GDColon, GDClassMembersList>(this);
        }

        public GDInnerClassDeclaration()
        {
            _form = new GDTokensForm<State, GDClassKeyword, GDIdentifier, GDExtendsKeyword, GDDataToken, GDColon, GDClassMembersList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveSpaceToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Class:
                    this.ResolveKeyword<GDClassKeyword>(c, state);
                    break;
                case State.Identifier:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.Extends:
                    this.ResolveKeyword<GDExtendsKeyword>(c, state);
                    break;
                case State.BaseTypePath:
                    this.ResolveString(c, state);
                    break;
                case State.BaseType:
                    this.ResolveType(c, state);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
                    break;
                case State.Members:
                    this.ResolveInvalidToken(c, state, x => x.IsNewLine());
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
                case State.Class:
                case State.Identifier:
                case State.Extends:
                case State.BaseType:
                case State.BaseTypePath:
                case State.Colon:
                case State.Members:
                    _form.State = State.Completed;
                    state.PushAndPassNewLine(Members);
                    break;
                default:
                    state.PopAndPassNewLine();
                    break;
            }
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDInnerClassDeclaration();
        }

        void ITokenReceiver<GDClassKeyword>.HandleReceivedToken(GDClassKeyword token)
        {
            if (_form.IsOrLowerState(State.Class))
            {
                _form.State = State.Identifier;
                ClassKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDClassKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Class))
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
                _form.State = State.Extends;
                Identifier = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.Extends;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExtendsKeyword>.HandleReceivedToken(GDExtendsKeyword token)
        {
            if (_form.IsOrLowerState(State.Extends))
            {
                _form.State = State.BaseTypePath;
                Extends = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExtendsKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Extends))
            {
                _form.State = State.BaseTypePath;
                return;
            }

            throw new GDInvalidStateException();
        }
        void ITokenReceiver<GDString>.HandleReceivedToken(GDString token)
        {
            if (_form.IsOrLowerState(State.BaseTypePath))
            {
                _form.State = State.Colon;
                BaseTypePath = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDString>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.BaseTypePath))
            {
                _form.State = State.BaseType;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDType>.HandleReceivedToken(GDType token)
        {
            if (_form.IsOrLowerState(State.BaseType))
            {
                _form.State = State.Colon;
                BaseType = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDType>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.BaseType))
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
                _form.State = State.Members;
                Colon = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Members;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDClassMembersList>.HandleReceivedToken(GDClassMembersList token)
        {
            if (_form.IsOrLowerState(State.Members))
            {
                Members = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDClassMembersList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Members))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
