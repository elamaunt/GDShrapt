using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDInnerClassDeclaration : GDClassMember, IClassMembersReceiver
    {
        readonly int _lineIntendation;
        bool _membersChecked;

        public List<GDClassMember> Members { get; } = new List<GDClassMember>();

        public GDType ExtendsClass => Members.OfType<GDExtendsAtribute>().FirstOrDefault()?.Type;
        public GDIdentifier Name => Members.OfType<GDClassNameAtribute>().FirstOrDefault()?.Identifier;
        public bool IsTool => Members.OfType<GDToolAtribute>().Any();

        public IEnumerable<GDVariableDeclaration> Variables => Members.OfType<GDVariableDeclaration>();
        public IEnumerable<GDMethodDeclaration> Methods => Members.OfType<GDMethodDeclaration>();
        public IEnumerable<GDEnumDeclaration> Enums => Members.OfType<GDEnumDeclaration>();
        public IEnumerable<GDInnerClassDeclaration> InnerClasses => Members.OfType<GDInnerClassDeclaration>();

        internal GDInnerClassDeclaration(int lineIntendation)
        {
            _lineIntendation = lineIntendation;
        }

        public GDInnerClassDeclaration()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_membersChecked)
            {
                _membersChecked = true;
                state.Push(new GDClassMemberResolver(this, _lineIntendation + 1));
                state.PassChar(c);
                return;
            }

            // Complete reading
            state.Pop();
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (!_membersChecked)
            {
                _membersChecked = true;
                state.Push(new GDClassMemberResolver(this, _lineIntendation + 1));
                state.PassLineFinish();
                return;
            }

            // Complete reading
            state.Pop();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            for (int i = 0; i < Members.Count; i++)
                builder.AppendLine(Members[i].ToString());

            return builder.ToString();
        }

        void IClassMembersReceiver.HandleReceivedToken(GDClassMember token)
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDComment token)
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDSpace token)
        {
            throw new System.NotImplementedException();
        }

        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}
