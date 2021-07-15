using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Basic GDScript node, may contains multiple tokens
    /// </summary>
    public abstract class GDNode : GDSyntaxToken,
        ITokenReceiver<GDComment>,
        ITokenReceiver<GDSpace>
    {
        public abstract GDTokensForm Form { get; }
        
        public IEnumerable<GDSyntaxToken> Tokens => Form.Direct();
        public IEnumerable<GDSyntaxToken> TokensReversed => Form.Reversed();
        public IEnumerable<GDNode> Nodes => Tokens.OfType<GDNode>();
        public IEnumerable<GDNode> NodesReversed => TokensReversed.OfType<GDNode>();

        public IEnumerable<GDSyntaxToken> AllTokens
        {
            get
            {
                foreach (var token in Form)
                {
                    if (token is GDNode node)
                    {
                        foreach (var nodeToken in node.AllTokens)
                            yield return nodeToken;
                    }
                    else
                        yield return token;
                }
            }
        }

        public IEnumerable<GDSyntaxToken> AllTokensReversed
        {
            get
            {
                foreach (var token in Form.Reversed())
                {
                    if (token is GDNode node)
                    {
                        foreach (var nodeToken in node.AllTokensReversed)
                            yield return nodeToken;
                    }
                    else
                        yield return token;
                }
            }
        }

        public IEnumerable<GDNode> AllNodes
        {
            get
            {
                foreach (var token in Form)
                {
                    if (token is GDNode node)
                    {
                        yield return node;

                        foreach (var nodeToken in node.AllNodes)
                            yield return nodeToken;
                    }
                }
            }
        }

        public IEnumerable<GDNode> AllNodesReversed
        {
            get
            {
                foreach (var token in Form.Reversed())
                {
                    if (token is GDNode node)
                    {
                        foreach (var nodeToken in node.AllNodesReversed)
                            yield return nodeToken;

                        yield return node;
                    }
                }
            }
        }

        /// <summary>
        /// Removes child node or does nothing if node is already removed.
        /// </summary>
        /// <param name="token">Child token</param>
        public bool RemoveChild(GDSyntaxToken token)
        {
            if (!ReferenceEquals(token.Parent, this))
                throw new InvalidOperationException("The specified node has a different parent.");

            return Form.Remove(token);
        }

        /// <summary>
        /// Checks the last newLine in the node. If no new lines in the node returns the length of the token. 
        /// Otherwise returns character count before the last new line.
        /// </summary>
        /// <param name="tokensCount">Count of the character before the last 'new line'</param>
        /// <returns>Is 'new line' character exist in the node</returns>
        public bool CheckNewLine(out int tokensCount)
        {
            tokensCount = 0;

            foreach (var token in AllTokensReversed)
            {
                if (token.NewLinesCount > 0)
                    return true;
                tokensCount += token.Length;
            }

            return false;
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            Form.AddBeforeActiveToken(state.Push(new GDComment()));
            state.PassSharpChar();
        }

        public override void AppendTo(StringBuilder builder)
        {
            foreach (var token in Form)
                token.AppendTo(builder);
        }

        public sealed override string ToString()
        {
            var builder = new StringBuilder();
            
            foreach (var token in Form)
                token.AppendTo(builder);

            return builder.ToString();
        }

        /// <summary>
        /// Creates empty instance of this node type
        /// </summary>
        /// <returns>New empty instance</returns>
        public abstract GDNode CreateEmptyInstance();

        public override GDSyntaxToken Clone()
        {
            var node = CreateEmptyInstance();
            node.Form.CloneFrom(Form);
            return node;
        }

        /// <summary>
        /// Indexer to get 'Add' syntax working in tokens building
        /// </summary>
        public GDSyntaxToken this[int index]
        {
            set => Form.AddBeforeToken(value, index);
        }

        public override int Length => Tokens.Sum(x => x.Length);

        public override int NewLinesCount => Tokens.Sum(x => x.NewLinesCount);

        public override int EndColumn
        {
            get
            {
                int column = 0;

                foreach (var item in TokensReversed)
                {
                    if (item is GDNode node)
                    {
                        int tokensCount;
                        if (node.CheckNewLine(out tokensCount))
                        {
                            return tokensCount + column;
                        }
                        else
                        {
                            column += tokensCount;
                        }
                    }
                    else
                    {
                        if (item.NewLinesCount == 0)
                            column += item.Length;
                        else
                            return column;
                    }
                }

                var parent = Parent;

                if (parent == null)
                    return column;

                bool found = false;

                foreach (var item in parent.TokensReversed)
                {
                    if (item == this)
                    {
                        found = true;
                        continue;
                    }

                    if (!found)
                        continue;

                    if (item is GDNode node)
                    {
                        int tokensCount;
                        if (node.CheckNewLine(out tokensCount))
                        {
                            return column + tokensCount;
                        }
                        else
                        {
                            column += tokensCount;
                        }
                    }
                    else
                    {
                        if (item.NewLinesCount == 0)
                            column += item.Length;
                        else
                            return column;
                    }
                }

                return parent.StartColumn + column;
            }
        }

        public int TokensCount => Form.TokensCount;
        public bool HasTokens => Form.HasTokens;

        /// <summary>
        /// Returns variable identifiers that are visible before line and defined by this node and its children
        /// Actual only for method scope.
        /// </summary>
        /// <param name="beforeLine">Excluded line. Actual for <see cref="GDStatementsList"/></param>
        /// <returns>Enumeration</returns>
        public virtual IEnumerable<GDIdentifier> GetMethodScopeDeclarations(int? beforeLine = null)
        {
            return Enumerable.Empty<GDIdentifier>();
        }

        /// <summary>
        /// Searches all <see cref="GDIntendation"/> tokens and calls <see cref="GDIntendation.Update"/> for every one
        /// </summary>
        public void UpdateIntendation()
        {
            foreach (var intendation in AllTokens.OfType<GDIntendation>())
                intendation.Update();
        }

        public virtual IEnumerable<GDIdentifier> GetDependencies()
        {
            return Nodes.SelectMany(x => x.GetDependencies());
        }

        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
            Form.AddBeforeActiveToken(token);
        }

        void ITokenReceiver<GDComment>.HandleReceivedToken(GDComment token)
        {
            Form.AddBeforeActiveToken(token);
        }

        void ITokenReceiver<GDSpace>.HandleReceivedToken(GDSpace token)
        {
            Form.AddBeforeActiveToken(token);
        }

        void ITokenReceiver.HandleReceivedToken(GDSpace token)
        {
            Form.AddBeforeActiveToken(token);
        }

        void ITokenReceiver.HandleReceivedToken(GDComment token)
        {
            Form.AddBeforeActiveToken(token);
        }
    }
}