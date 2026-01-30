using System.Text;

namespace GDShrapt.Reader
{
    internal class GDPathSpecifierResolver : GDResolver
    {
        private GDPathSpecifierType? _type;
        private StringBuilder _identifierBuilder;
        private readonly bool _allowNonStringIdentifiers;

        public new ITokenOrSkipReceiver<GDPathSpecifier> Owner { get; }
        public GDPathSpecifierResolver(ITokenOrSkipReceiver<GDPathSpecifier> owner, bool allowNonStringIdentifiers)
            : base(owner)
        {
            Owner = owner;
            _allowNonStringIdentifiers = allowNonStringIdentifiers;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            // Stop on whitespace for unquoted paths - operator keywords like 'as' follow
            // But allow whitespace inside quoted paths ($"My Sprite")
            if (!_allowNonStringIdentifiers && (c == ' ' || c == '\t'))
            {
                Complete(c, state);
                return;
            }

            if (c == '/' || c == ':' || c == '"' || c == '\'')
            {
                Complete(c, state);
                return;
            }

            if (_type.HasValue)
            {
                if (c == '.')
                {
                    if (!_allowNonStringIdentifiers)
                    {
                        Complete(c, state);
                        return;
                    }

                    switch (_type.Value)
                    {
                        case GDPathSpecifierType.Current:
                            _type = GDPathSpecifierType.Parent;
                            break;
                        case GDPathSpecifierType.Parent:
                            _type = GDPathSpecifierType.Identifier;
                            _identifierBuilder = new StringBuilder();
                            _identifierBuilder.Append('.');
                            _identifierBuilder.Append('.');
                            _identifierBuilder.Append(c);
                            return;
                        case GDPathSpecifierType.Identifier:
                            _identifierBuilder.Append(c);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (_type.Value)
                    {
                        case GDPathSpecifierType.Current:
                            _type = GDPathSpecifierType.Identifier;
                            _identifierBuilder = new StringBuilder();
                            _identifierBuilder.Append('.');
                            _identifierBuilder.Append(c);
                            break;
                        case GDPathSpecifierType.Parent:
                            _type = GDPathSpecifierType.Identifier;
                            _identifierBuilder = new StringBuilder();
                            _identifierBuilder.Append('.');
                            _identifierBuilder.Append('.');
                            _identifierBuilder.Append(c);
                            return;
                        case GDPathSpecifierType.Identifier:
                            _identifierBuilder.Append(c);
                            break;
                        default:
                            break;
                    }
                }

            } 
            else
            {
                if (c == '.')
                {
                    _type = GDPathSpecifierType.Current;
                }
                else 
                {
                    _type = GDPathSpecifierType.Identifier;
                    _identifierBuilder = new StringBuilder();
                    _identifierBuilder.Append(c);
                }
            }
        }

        private void Complete(char c, GDReadingState state)
        {
            if (_type.HasValue)
            {
                Owner.HandleReceivedToken(new GDPathSpecifier() 
                { 
                    IdentifierValue = _identifierBuilder?.ToString(),
                    Type = _type.Value
                });
            }
            else
            {
                Owner.HandleReceivedTokenSkip();
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            Complete('\n', state);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            if (_type.HasValue)
            {
                Owner.HandleReceivedToken(new GDPathSpecifier()
                {
                    IdentifierValue = _identifierBuilder?.ToString(),
                    Type = _type.Value
                });
            }
            else
            {
                Owner.HandleReceivedTokenSkip();
            }

            base.ForceComplete(state);
        }
    }
}
