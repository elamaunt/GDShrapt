using System;

namespace GDShrapt.Reader
{
    internal class GDMatchCaseResolver : GDIntendedResolver
    {
        public GDMatchCaseResolver(ITokensContainer owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            var declaration = new GDMatchCaseDeclaration(LineIntendationThreshold);
            Append(declaration);
            state.Push(declaration);
            state.PassChar(c);
        }
    }
}
