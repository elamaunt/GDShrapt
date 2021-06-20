using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDInnerClassDeclaration : GDClassMember,
        IKeywordReceiver<GDClassKeyword>,
        IIdentifierReceiver,
        ITokenReceiver<GDColon>,
        ITokenReceiver<GDNewLine>
    {
        public IEnumerable<GDVariableDeclaration> Variables => Members.OfType<GDVariableDeclaration>();
        public IEnumerable<GDMethodDeclaration> Methods => Members.OfType<GDMethodDeclaration>();
        public IEnumerable<GDEnumDeclaration> Enums => Members.OfType<GDEnumDeclaration>();
        public IEnumerable<GDInnerClassDeclaration> InnerClasses => Members.OfType<GDInnerClassDeclaration>();

        internal GDClassKeyword ClassKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDIdentifier Identifier
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        internal GDColon Colon
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        internal GDNewLine NewLine
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }
        public GDClassMembersList Members
        {
            get => _form.Token4 ?? (_form.Token4 = new GDClassMembersList(Intendation + 1));
        }

        enum State
        {
            Class,
            Identifier,
            Colon,
            NewLine,
            Members,
            Completed
        }

        readonly GDTokensForm<State, GDClassKeyword, GDIdentifier, GDColon, GDNewLine, GDClassMembersList> _form;
        internal override GDTokensForm Form => _form;

        internal GDInnerClassDeclaration(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDClassKeyword, GDIdentifier, GDColon, GDNewLine, GDClassMembersList>(this);
        }

        public GDInnerClassDeclaration()
        {
            _form = new GDTokensForm<State, GDClassKeyword, GDIdentifier, GDColon, GDNewLine, GDClassMembersList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveStyleToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Class:
                    this.ResolveKeyword(c, state);
                    break;
                case State.Identifier:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
                    break;
                case State.NewLine:
                    this.ResolveInvalidToken(c, state, x => x.IsNewLine());
                    break;
                case State.Members:
                    _form.State = State.Completed;
                    state.PushAndPass(Members, c);
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
                case State.NewLine:
                    _form.State = State.Members;
                    NewLine = new GDNewLine();
                    break;
                default:
                    state.PopAndPassNewLine();
                    break;
            }
        }

        void IKeywordReceiver<GDClassKeyword>.HandleReceivedToken(GDClassKeyword token)
        {
            if (_form.State == State.Class)
            {
                _form.State = State.Identifier;
                ClassKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDClassKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Class)
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IIdentifierReceiver.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.State == State.Identifier)
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
                _form.State = State.NewLine;
                Colon = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.NewLine;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDNewLine>.HandleReceivedToken(GDNewLine token)
        {
            switch (_form.State)
            {
                case State.Class:
                case State.Identifier:
                case State.Colon:
                case State.NewLine:
                    _form.State = State.Members;
                    NewLine = token;
                    break;
                default:
                    throw new GDInvalidReadingStateException();
            }
        }

        void ITokenReceiver<GDNewLine>.HandleReceivedTokenSkip()
        {
            switch (_form.State)
            {
                case State.Class:
                case State.Identifier:
                case State.Colon:
                case State.NewLine:
                    _form.State = State.Members;
                    break;
                default:
                    throw new GDInvalidReadingStateException();
            }
        }
    }
}
