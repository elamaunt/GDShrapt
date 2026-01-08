using GDShrapt.Reader;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Api.Internal;

/// <summary>
/// Implementation of ICodeModifier that wraps existing command functionality.
/// </summary>
internal class CodeModifierImpl : ICodeModifier
{
    private readonly GDProjectMap _projectMap;

    public CodeModifierImpl(GDProjectMap projectMap)
    {
        _projectMap = projectMap;
    }

    public async Task<IRenameResult> RenameAsync(
        string filePath,
        int line,
        int column,
        string newName,
        RenameOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Get the preview first - properly await instead of blocking with .Result
        var changes = await PreviewRenameAsync(filePath, line, column, newName, options, cancellationToken);

        if (changes.Count == 0)
        {
            return new RenameResultImpl(
                false,
                "No symbol found at the specified location",
                0, 0, new List<ITextChange>()
            );
        }

        // Apply changes in reverse order (to preserve line numbers)
        var sortedChanges = new List<ITextChange>(changes);
        sortedChanges.Sort((a, b) =>
        {
            var fileCompare = string.Compare(b.FilePath, a.FilePath);
            if (fileCompare != 0) return fileCompare;
            var lineCompare = b.StartLine.CompareTo(a.StartLine);
            if (lineCompare != 0) return lineCompare;
            return b.StartColumn.CompareTo(a.StartColumn);
        });

        var filesModified = new HashSet<string>();
        var errors = new List<string>();

        foreach (var change in sortedChanges)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var lines = await System.IO.File.ReadAllLinesAsync(change.FilePath, cancellationToken);
                if (change.StartLine < lines.Length)
                {
                    var lineText = lines[change.StartLine];

                    // Validate bounds before substring operations
                    if (change.StartColumn >= 0 && change.StartColumn <= lineText.Length &&
                        change.EndColumn >= change.StartColumn && change.EndColumn <= lineText.Length)
                    {
                        var before = lineText.Substring(0, change.StartColumn);
                        var after = lineText.Substring(change.EndColumn);
                        lines[change.StartLine] = before + change.NewText + after;
                        await System.IO.File.WriteAllLinesAsync(change.FilePath, lines, cancellationToken);
                        filesModified.Add(change.FilePath);
                    }
                    else
                    {
                        errors.Add($"Invalid column range in {change.FilePath}:{change.StartLine}");
                    }
                }
                else
                {
                    errors.Add($"Line {change.StartLine} out of range in {change.FilePath}");
                }
            }
            catch (System.Exception ex)
            {
                errors.Add($"Error modifying {change.FilePath}: {ex.Message}");
                Logger.Error($"CodeModifier: Error applying change to {change.FilePath}: {ex.Message}");
            }
        }

        var success = errors.Count == 0;
        var errorMessage = errors.Count > 0 ? string.Join("; ", errors) : null;

        return new RenameResultImpl(
            success,
            errorMessage,
            filesModified.Count,
            changes.Count - errors.Count,
            changes
        );
    }

    public Task<IExtractMethodResult> ExtractMethodAsync(
        string filePath,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        string methodName,
        CancellationToken cancellationToken = default)
    {
        // This is a simplified implementation
        // Full implementation would use GDShrapt.Builder to generate proper method code
        try
        {
            var lines = System.IO.File.ReadAllLines(filePath);
            if (startLine >= lines.Length || endLine >= lines.Length)
            {
                return Task.FromResult<IExtractMethodResult>(new ExtractMethodResultImpl(
                    false,
                    "Invalid line range",
                    null, 0, new List<ITextChange>()
                ));
            }

            // Extract the selected code
            var extractedLines = new List<string>();
            for (int i = startLine; i <= endLine; i++)
            {
                var line = lines[i];
                if (i == startLine && i == endLine)
                {
                    extractedLines.Add(line.Substring(startColumn, endColumn - startColumn));
                }
                else if (i == startLine)
                {
                    extractedLines.Add(line.Substring(startColumn));
                }
                else if (i == endLine)
                {
                    extractedLines.Add(line.Substring(0, endColumn));
                }
                else
                {
                    extractedLines.Add(line);
                }
            }

            var extractedCode = string.Join("\n\t", extractedLines);
            var generatedMethod = $"func {methodName}():\n\t{extractedCode}";

            // Format the generated method using GDFormatter
            try
            {
                var formatter = new GDFormatter(GDFormatterOptions.Default);
                generatedMethod = formatter.FormatCode(generatedMethod);
            }
            catch
            {
                // Use unformatted code if formatting fails
            }

            return Task.FromResult<IExtractMethodResult>(new ExtractMethodResultImpl(
                true,
                null,
                generatedMethod,
                endLine + 2,
                new List<ITextChange>()
            ));
        }
        catch (System.Exception ex)
        {
            return Task.FromResult<IExtractMethodResult>(new ExtractMethodResultImpl(
                false,
                ex.Message,
                null, 0, new List<ITextChange>()
            ));
        }
    }

    public Task<IReadOnlyList<ITextChange>> PreviewRenameAsync(
        string filePath,
        int line,
        int column,
        string newName,
        RenameOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var script = _projectMap.GetScriptMap(filePath);
        if (script?.Class == null)
            return Task.FromResult<IReadOnlyList<ITextChange>>(new List<ITextChange>());

        // Find the identifier at the given position
        GDShrapt.Reader.GDIdentifier? targetIdentifier = null;
        foreach (var token in script.Class.AllTokens)
        {
            if (token is GDShrapt.Reader.GDIdentifier identifier &&
                identifier.StartLine == line &&
                identifier.StartColumn <= column &&
                identifier.EndColumn >= column)
            {
                targetIdentifier = identifier;
                break;
            }
        }

        if (targetIdentifier == null)
            return Task.FromResult<IReadOnlyList<ITextChange>>(new List<ITextChange>());

        var symbolName = targetIdentifier.Sequence ?? string.Empty;
        var changes = new List<ITextChange>();

        // Find all references across all scripts
        foreach (var scriptMap in _projectMap.Scripts)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (scriptMap.Class == null)
                continue;

            foreach (var token in scriptMap.Class.AllTokens)
            {
                if (token is GDShrapt.Reader.GDIdentifier identifier && identifier.Sequence == symbolName)
                {
                    // Check strong typing option
                    if (options?.RenameOnlyStrongTyped == true)
                    {
                        // Skip if not strongly typed (simplified check)
                        var parent = identifier.Parent;
                        if (!(parent is GDShrapt.Reader.GDVariableDeclaration varDecl && varDecl.Type != null) &&
                            !(parent is GDShrapt.Reader.GDParameterDeclaration paramDecl && paramDecl.Type != null))
                        {
                            // Not strongly typed, but check if it's a declaration
                            if (!(parent is GDShrapt.Reader.GDMethodDeclaration) &&
                                !(parent is GDShrapt.Reader.GDSignalDeclaration))
                            {
                                continue;
                            }
                        }
                    }

                    changes.Add(new TextChangeImpl(
                        scriptMap.Reference?.FullPath ?? string.Empty,
                        identifier.StartLine,
                        identifier.StartColumn,
                        identifier.EndLine,
                        identifier.EndColumn,
                        symbolName,
                        newName
                    ));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<ITextChange>>(changes);
    }
}

internal class RenameResultImpl : IRenameResult
{
    public RenameResultImpl(bool success, string? errorMessage, int filesModified, int referencesRenamed, IReadOnlyList<ITextChange> changes)
    {
        Success = success;
        ErrorMessage = errorMessage;
        FilesModified = filesModified;
        ReferencesRenamed = referencesRenamed;
        Changes = changes;
    }

    public bool Success { get; }
    public string? ErrorMessage { get; }
    public int FilesModified { get; }
    public int ReferencesRenamed { get; }
    public IReadOnlyList<ITextChange> Changes { get; }
}

internal class ExtractMethodResultImpl : IExtractMethodResult
{
    public ExtractMethodResultImpl(bool success, string? errorMessage, string? generatedMethod, int methodInsertedAtLine, IReadOnlyList<ITextChange> changes)
    {
        Success = success;
        ErrorMessage = errorMessage;
        GeneratedMethod = generatedMethod;
        MethodInsertedAtLine = methodInsertedAtLine;
        Changes = changes;
    }

    public bool Success { get; }
    public string? ErrorMessage { get; }
    public string? GeneratedMethod { get; }
    public int MethodInsertedAtLine { get; }
    public IReadOnlyList<ITextChange> Changes { get; }
}

internal class TextChangeImpl : ITextChange
{
    public TextChangeImpl(string filePath, int startLine, int startColumn, int endLine, int endColumn, string oldText, string newText)
    {
        FilePath = filePath;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
        OldText = oldText;
        NewText = newText;
    }

    public string FilePath { get; }
    public int StartLine { get; }
    public int StartColumn { get; }
    public int EndLine { get; }
    public int EndColumn { get; }
    public string OldText { get; }
    public string NewText { get; }
}
