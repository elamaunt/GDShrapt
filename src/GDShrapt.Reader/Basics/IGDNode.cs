using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public interface IGDNode : IGDSyntaxToken
    {
        IEnumerable<GDNode> Nodes { get; }
        IEnumerable<GDNode> NodesReversed { get; }
        IEnumerable<GDNode> AllNodes { get; }
        IEnumerable<GDNode> AllNodesReversed { get; }
        IEnumerable<GDSyntaxToken> AllTokens { get; }
        IEnumerable<GDSyntaxToken> AllTokensReversed { get; }

        int TokensCount { get; }
        bool HasTokens { get; }
        GDTokensForm Form { get; }
        GDSyntaxToken[] FormTokensSetter { set; }

        void UpdateIntendation();
    }
}
