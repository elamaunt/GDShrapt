using System;

namespace GDShrapt.Reader
{
    internal sealed class GDStartTrimmingResolver : GDResolver
    {
        private readonly Func<GDResolver> _factory;

        public GDStartTrimmingResolver(IStyleTokensReceiver owner, Func<GDResolver> factory)
            : base(owner)
        { 
            _factory = factory;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (Owner.ResolveStyleToken(c, state))
                return;

            state.Pop();
            state.PushAndPass(_factory(), c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            Owner.HandleReceivedToken(new GDNewLine());
        }
    }
}
