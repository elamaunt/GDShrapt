using System.Collections.Generic;
using System.Threading;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Interface for incremental GDScript parsing.
    /// Allows efficient re-parsing of code after edits by reusing unchanged portions
    /// of the AST when possible.
    /// </summary>
    public interface IGDIncrementalParser
    {
        /// <summary>
        /// Parses incrementally, reusing parts of old tree where possible.
        /// </summary>
        /// <param name="oldTree">The previous parse tree (can be null for first parse).</param>
        /// <param name="newText">The new complete source text after edits.</param>
        /// <param name="changes">The text changes that were applied to transform old text to new text.</param>
        /// <param name="cancellationToken">Token to cancel parsing.</param>
        /// <returns>A new parse tree representing the parsed code. May share structure with oldTree for unchanged portions.</returns>
        GDClassDeclaration ParseIncremental(
            GDClassDeclaration oldTree,
            string newText,
            IReadOnlyList<GDTextChange> changes,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the changed ranges between two parse trees.
        /// Useful for determining which regions need to be re-analyzed semantically.
        /// </summary>
        /// <param name="oldTree">The previous parse tree.</param>
        /// <param name="newTree">The new parse tree.</param>
        /// <returns>List of text spans in the new tree that are different from the old tree.</returns>
        IReadOnlyList<GDTextSpan> GetChangedRanges(
            GDClassDeclaration oldTree,
            GDClassDeclaration newTree);
    }
}
