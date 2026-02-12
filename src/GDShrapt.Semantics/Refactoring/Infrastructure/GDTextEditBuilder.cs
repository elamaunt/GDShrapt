using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Fluent builder for creating GDTextEdit objects.
/// </summary>
internal class GDTextEditBuilder
{
    private readonly List<GDTextEdit> _edits = new();
    private string? _filePath;

    /// <summary>
    /// Creates a new builder for the specified file.
    /// </summary>
    public static GDTextEditBuilder ForFile(string? filePath) => new GDTextEditBuilder().SetFile(filePath);

    /// <summary>
    /// Sets the file path for subsequent edits.
    /// </summary>
    public GDTextEditBuilder SetFile(string? filePath)
    {
        _filePath = filePath;
        return this;
    }

    /// <summary>
    /// Adds an insertion at the specified position.
    /// </summary>
    public GDTextEditBuilder Insert(int line, int column, string text)
    {
        if (_filePath == null) return this;
        _edits.Add(new GDTextEdit(_filePath, line, column, "", text));
        return this;
    }

    /// <summary>
    /// Adds an insertion at the start of line.
    /// </summary>
    public GDTextEditBuilder InsertLine(int line, string text)
        => Insert(line, 0, text + "\n");

    /// <summary>
    /// Adds a replacement at the specified node position.
    /// </summary>
    public GDTextEditBuilder Replace(GDNode node, string newText)
    {
        if (_filePath == null || node == null) return this;
        _edits.Add(new GDTextEdit(
            _filePath,
            node.StartLine + 1,
            node.StartColumn + 1,
            node.ToString(),
            newText));
        return this;
    }

    /// <summary>
    /// Adds a replacement at the specified position.
    /// </summary>
    public GDTextEditBuilder Replace(int line, int column, string oldText, string newText)
    {
        if (_filePath == null) return this;
        _edits.Add(new GDTextEdit(_filePath, line, column, oldText, newText));
        return this;
    }

    /// <summary>
    /// Adds a replacement with confidence level.
    /// </summary>
    public GDTextEditBuilder Replace(
        int line, int column,
        string oldText, string newText,
        GDReferenceConfidence confidence,
        string? reason = null)
    {
        if (_filePath == null) return this;
        _edits.Add(new GDTextEdit(_filePath, line, column, oldText, newText, confidence, reason));
        return this;
    }

    /// <summary>
    /// Adds a deletion at the specified node.
    /// </summary>
    public GDTextEditBuilder Delete(GDNode node)
        => Replace(node, "");

    /// <summary>
    /// Adds a deletion at the specified position.
    /// </summary>
    public GDTextEditBuilder Delete(int line, int column, string oldText)
        => Replace(line, column, oldText, "");

    /// <summary>
    /// Adds all edits from another builder.
    /// </summary>
    public GDTextEditBuilder AddAll(GDTextEditBuilder other)
    {
        _edits.AddRange(other._edits);
        return this;
    }

    /// <summary>
    /// Adds all edits from a list.
    /// </summary>
    public GDTextEditBuilder AddAll(IEnumerable<GDTextEdit> edits)
    {
        _edits.AddRange(edits);
        return this;
    }

    /// <summary>
    /// Gets the number of edits in the builder.
    /// </summary>
    public int Count => _edits.Count;

    /// <summary>
    /// Builds the list of edits.
    /// </summary>
    public IReadOnlyList<GDTextEdit> Build() => _edits;

    /// <summary>
    /// Creates a successful result with the built edits.
    /// </summary>
    public GDRefactoringResult ToResult() => GDRefactoringResult.Succeeded(_edits);
}
