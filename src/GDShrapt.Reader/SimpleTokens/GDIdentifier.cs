using System;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDIdentifier : GDNameToken, IEquatable<GDIdentifier>
    {
        public bool IsPi => string.Equals(Sequence, "PI", StringComparison.Ordinal);
        public bool IsTau => string.Equals(Sequence, "TAU", StringComparison.Ordinal);
        public bool IsInfinity => string.Equals(Sequence, "INF", StringComparison.Ordinal);
        public bool IsNaN => string.Equals(Sequence, "NAN", StringComparison.Ordinal);
        public bool IsTrue => string.Equals(Sequence, "true", StringComparison.Ordinal);
        public bool IsFalse => string.Equals(Sequence, "false", StringComparison.Ordinal);
        public bool IsSelf => string.Equals(Sequence, "self", StringComparison.Ordinal);

        string _sequence;
        public override string Sequence
        {
            get => _sequence;
            set
            {
                CheckIdentifierValue(value);
                _sequence = value;
            }
        }

        private void CheckIdentifierValue(string value)
        {
            if (value.IsNullOrWhiteSpace() || value.IsNullOrEmpty())
                throw new ArgumentException("Invalid identifier format");

            if (char.IsNumber(value[0]))
                throw new ArgumentException("Invalid identifier format");

            if (value.Any(x => !char.IsLetter(x) && !char.IsDigit(x) && x != '_'))
                throw new ArgumentException("Invalid identifier format");

        }

        StringBuilder _builder = new StringBuilder();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_builder.Length == 0)
            {
                if (IsIdentifierStartChar(c))
                {
                    _builder.Append(c);
                }
                else
                {
                    state.PopAndPass(c);
                }
            }
            else
            {
                if (c == '_' || char.IsLetterOrDigit(c))
                {
                    _builder.Append(c);
                }
                else
                {
                    Sequence = _builder.ToString();
                    state.PopAndPass(c);
                }
            }

        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_builder.Length > 0)
                Sequence = _builder.ToString();

            state.PopAndPassNewLine();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            if (_builder.Length > 0)
                Sequence = _builder.ToString();
            base.HandleSharpChar(state);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            if (_builder.Length > 0)
                Sequence = _builder.ToString();
            base.ForceComplete(state);
        }

        /// <summary>
        /// Trying to find a local scope variable or a parameter declaration which has the same name.
        /// </summary>
        /// <returns>True if found. Otherwise false</returns>
        public bool TryExtractLocalScopeVisibleDeclarationFromParents(out GDIdentifier declaration)
        {
            declaration = null;

            var startLine = StartLine;

            GDNode node = Parent;

            bool? isStaticContext = null;

            while (true)
            {
                if (node == null)
                    break;

                if (node is GDVariableDeclarationStatement varDeclaration)
                {
                    if (varDeclaration.Identifier == this)
                    {
                        declaration = varDeclaration.Identifier;
                        break;
                    }

                    node = node.Parent;
                    continue;
                }

                if (node is GDInnerClassDeclaration innerClass)
                {
                    if (isStaticContext.HasValue)
                    {
                        if (isStaticContext.Value)
                        {
                            foreach (var member in innerClass.IdentifiableMembers)
                            {
                                if (member.IsStatic && member.Identifier == this)
                                {
                                    declaration = member.Identifier;
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            foreach (var member in innerClass.IdentifiableMembers)
                            {
                                if (member.Identifier == this)
                                {
                                    declaration = member.Identifier;
                                    return true;
                                }
                            }
                        }
                    }

                    break;
                }

                if (node is GDClassDeclaration @class)
                {
                    if (isStaticContext.HasValue && isStaticContext.Value)
                    {
                        foreach (var member in @class.IdentifiableMembers)
                        {
                            if (member.IsStatic && member.Identifier == this)
                            {
                                declaration = member.Identifier;
                                return true;
                            }
                        }
                    }
                    else
                    {
                        foreach (var member in @class.IdentifiableMembers)
                        {
                            if (member.Identifier == this)
                            {
                                declaration = member.Identifier;
                                return true;
                            }
                        }

                    }

                    break;
                }

                if (node is GDMethodDeclaration method)
                {
                    foreach (var item in node.GetMethodScopeDeclarations(startLine))
                    {
                        if (item == this)
                        {
                            declaration = item;
                            return true;
                        }
                    }

                    if (method.Identifier == this)
                    {
                        declaration = method.Identifier;
                        return true;
                    }

                    isStaticContext = method.IsStatic;

                    node = node.Parent;
                    continue;
                }

                if (node is GDMethodExpression methodExpr)
                {
                    foreach (var item in node.GetMethodScopeDeclarations(startLine))
                    {
                        if (item == this)
                        {
                            declaration = item;
                            return true;
                        }
                    }

                    if (methodExpr.Identifier == this)
                    {
                        declaration = methodExpr.Identifier;
                        return true;
                    }

                    node = node.Parent;
                    continue;
                }

                foreach (var item in node.GetMethodScopeDeclarations(startLine))
                {
                    if (item == this)
                    {
                        declaration = item;
                        return true;
                    }
                }

                node = node.Parent;
            }

            return false;
        }

        public override GDSyntaxToken Clone()
        {
            return new GDIdentifier()
            {
                Sequence = Sequence
            };
        }

        public static bool operator ==(GDIdentifier one, GDIdentifier two)
        {
            if (ReferenceEquals(one, null))
                return ReferenceEquals(two, null);

            if (ReferenceEquals(two, null))
                return false;

            return string.Equals(one.Sequence, two.Sequence, StringComparison.Ordinal);
        }

        public static bool operator !=(GDIdentifier one, GDIdentifier two)
        {
            if (ReferenceEquals(one, null))
                return !ReferenceEquals(two, null);

            if (ReferenceEquals(two, null))
                return true;

            return !string.Equals(one.Sequence, two.Sequence, StringComparison.Ordinal);
        }

        public static implicit operator GDIdentifier(string id)
        {
            return new GDIdentifier()
            {
                Sequence = id
            };
        }

        public override int GetHashCode()
        {
            return Sequence?.GetHashCode() ?? base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is GDIdentifier identifier)
                return ReferenceEquals(Sequence, identifier.Sequence) || string.Equals(Sequence, identifier.Sequence, StringComparison.Ordinal);
            return base.Equals(obj);
        }

        public bool Equals(GDIdentifier other)
        {
            return string.Equals(Sequence, other.Sequence, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}