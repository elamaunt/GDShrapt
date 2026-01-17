using System.Collections.Generic;
using System.Threading;

namespace GDShrapt.Semantics
{
    /// <summary>
    /// Interface for incremental semantic model updates.
    /// Allows updating semantic information when source code changes without full reanalysis.
    /// </summary>
    public interface IGDIncrementalSemanticUpdate
    {
        /// <summary>
        /// Updates the semantic model for a file that has changed.
        /// </summary>
        /// <param name="project">The project containing the file.</param>
        /// <param name="filePath">Path to the changed file.</param>
        /// <param name="oldTree">The previous AST before changes.</param>
        /// <param name="newTree">The new AST after changes.</param>
        /// <param name="changes">The text changes that were applied.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        void UpdateSemanticModel(
            GDScriptProject project,
            string filePath,
            GDClassDeclaration oldTree,
            GDClassDeclaration newTree,
            IReadOnlyList<GDTextChange> changes,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of files that may be affected by changes to the specified file.
        /// </summary>
        /// <param name="project">The project containing the file.</param>
        /// <param name="changedFilePath">Path to the changed file.</param>
        /// <returns>Paths of potentially affected files.</returns>
        IReadOnlyList<string> GetAffectedFiles(
            GDScriptProject project,
            string changedFilePath);

        /// <summary>
        /// Invalidates cached semantic information for a file.
        /// Call this when a file is modified but before incremental update.
        /// </summary>
        /// <param name="project">The project containing the file.</param>
        /// <param name="filePath">Path to the file to invalidate.</param>
        void InvalidateFile(
            GDScriptProject project,
            string filePath);
    }
}
