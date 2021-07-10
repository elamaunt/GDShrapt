using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDInnerClassDeclaration : GDClassMember,
        ITokenOrSkipReceiver<GDClassKeyword>,
        ITokenOrSkipReceiver<GDIdentifier>,
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
        public GDIdentifier Identifier
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDColon Colon
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDClassMembersList Members
        {
            get => _form.Token3 ?? (_form.Token3 = new GDClassMembersList(Intendation + 1));
            set => _form.Token3 = value;
        }

        enum State
        {
            Class,
            Identifier,
            Colon,
            Members,
            Completed
        }

        readonly GDTokensForm<State, GDClassKeyword, GDIdentifier, GDColon, GDClassMembersList> _form;
        public override GDTokensForm Form => _form;

        internal GDInnerClassDeclaration(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDClassKeyword, GDIdentifier, GDColon, GDClassMembersList>(this);
        }

        public GDInnerClassDeclaration()
        {
            _form = new GDTokensForm<State, GDClassKeyword, GDIdentifier, GDColon, GDClassMembersList>(this);
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
            if (_form.State == State.Class)
            {
                _form.State = State.Identifier;
                ClassKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDClassKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Class)
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDIdentifier>.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.State == State.Identifier)
            { 
                _form.State = State.Colon;
                Identifier = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Members;
                Colon = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Members;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
