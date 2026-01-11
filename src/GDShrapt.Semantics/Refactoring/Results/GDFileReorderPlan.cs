using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a planned member reorder for a single file.
/// </summary>
public class GDFileReorderPlan
{
    /// <summary>
    /// File path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// List of members that will be moved.
    /// </summary>
    public IReadOnlyList<GDMemberReorderChange> Changes { get; }

    /// <summary>
    /// The new order of members after reordering.
    /// </summary>
    public IReadOnlyList<GDClassMember>? NewOrder { get; }

    /// <summary>
    /// Original class code before reordering.
    /// </summary>
    public string OriginalCode { get; }

    /// <summary>
    /// Class code after reordering.
    /// </summary>
    public string ReorderedCode { get; }

    /// <summary>
    /// Whether any changes are needed for this file.
    /// </summary>
    public bool HasChanges => Changes != null && Changes.Count > 0;

    /// <summary>
    /// The text edit to apply this reorder.
    /// </summary>
    public GDTextEdit? Edit { get; }

    public GDFileReorderPlan(
        string filePath,
        IReadOnlyList<GDMemberReorderChange> changes,
        IReadOnlyList<GDClassMember>? newOrder,
        string originalCode,
        string reorderedCode,
        GDTextEdit? edit)
    {
        FilePath = filePath;
        Changes = changes ?? new List<GDMemberReorderChange>();
        NewOrder = newOrder;
        OriginalCode = originalCode;
        ReorderedCode = reorderedCode;
        Edit = edit;
    }

    public override string ToString()
    {
        if (!HasChanges)
            return $"{FilePath}: no changes needed";
        return $"{FilePath}: {Changes.Count} member(s) to reorder";
    }
}
