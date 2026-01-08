using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;

namespace GDShrapt.Plugin.Api.Internal;

/// <summary>
/// Implementation of IReferenceFinder that uses GDProjectMap.
/// </summary>
internal class ReferenceFinderImpl : IReferenceFinder
{
    private readonly GDProjectMap _projectMap;

    public ReferenceFinderImpl(GDProjectMap projectMap)
    {
        _projectMap = projectMap;
    }

    public Task<IReadOnlyList<IReferenceInfo>> FindReferencesAsync(
        string symbolName,
        CancellationToken cancellationToken = default)
    {
        var results = new List<IReferenceInfo>();

        foreach (var script in _projectMap.Scripts)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (script.Class == null)
                continue;

            foreach (var token in script.Class.AllTokens)
            {
                if (token is GDIdentifier identifier && identifier.Sequence == symbolName)
                {
                    results.Add(new ReferenceInfoImpl(
                        script.Reference?.FullPath ?? string.Empty,
                        identifier.StartLine,
                        identifier.StartColumn,
                        GetContextLine(script, identifier.StartLine),
                        DetermineReferenceKind(identifier),
                        identifier
                    ));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<IReferenceInfo>>(results);
    }

    public Task<IReadOnlyList<IReferenceInfo>> FindReferencesAtAsync(
        string filePath,
        int line,
        int column,
        CancellationToken cancellationToken = default)
    {
        var script = _projectMap.GetScriptMap(filePath);
        if (script?.Class == null)
            return Task.FromResult<IReadOnlyList<IReferenceInfo>>(new List<IReferenceInfo>());

        // Find the identifier at the given position
        GDIdentifier? targetIdentifier = null;
        foreach (var token in script.Class.AllTokens)
        {
            if (token is GDIdentifier identifier &&
                identifier.StartLine == line &&
                identifier.StartColumn <= column &&
                identifier.EndColumn >= column)
            {
                targetIdentifier = identifier;
                break;
            }
        }

        if (targetIdentifier == null)
            return Task.FromResult<IReadOnlyList<IReferenceInfo>>(new List<IReferenceInfo>());

        return FindReferencesAsync(targetIdentifier.Sequence ?? string.Empty, cancellationToken);
    }

    public int CountReferences(string symbolName)
    {
        int count = 0;

        foreach (var script in _projectMap.Scripts)
        {
            if (script.Class == null)
                continue;

            foreach (var token in script.Class.AllTokens)
            {
                if (token is GDIdentifier identifier && identifier.Sequence == symbolName)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public IReadOnlyDictionary<string, int> GetReferenceCountsForScript(string filePath)
    {
        var counts = new Dictionary<string, int>();
        var script = _projectMap.GetScriptMap(filePath);
        if (script?.Class == null)
            return counts;

        // Get all declarations in this script
        var declarations = new HashSet<string>();
        foreach (var member in script.Class.Members)
        {
            if (member is GDIdentifiableClassMember identifiable && identifiable.Identifier != null)
            {
                declarations.Add(identifiable.Identifier.Sequence ?? string.Empty);
            }
        }

        // Count references for each declaration
        foreach (var declName in declarations)
        {
            counts[declName] = CountReferences(declName);
        }

        return counts;
    }

    private static string GetContextLine(GDScriptMap script, int line)
    {
        try
        {
            if (script.Reference?.FullPath == null)
                return string.Empty;

            var lines = System.IO.File.ReadAllLines(script.Reference.FullPath);
            if (line >= 0 && line < lines.Length)
                return lines[line].Trim();
        }
        catch
        {
            // Ignore file read errors
        }
        return string.Empty;
    }

    private static ReferenceKind DetermineReferenceKind(GDIdentifier identifier)
    {
        var parent = identifier.Parent;

        // Check if it's a declaration
        if (parent is GDMethodDeclaration ||
            parent is GDVariableDeclaration ||
            parent is GDSignalDeclaration ||
            parent is GDParameterDeclaration ||
            parent is GDEnumDeclaration ||
            parent is GDInnerClassDeclaration)
        {
            return ReferenceKind.Declaration;
        }

        // Check if it's being called
        if (parent is GDCallExpression call && call.CallerExpression is GDIdentifierExpression idExpr && idExpr.Identifier == identifier)
        {
            return ReferenceKind.Call;
        }

        // Check if it's being assigned (write)
        if (parent is GDExpressionStatement exprStmt && exprStmt.Expression is GDDualOperatorExpression dualOp)
        {
            if (dualOp.OperatorType == GDDualOperatorType.Assignment ||
                dualOp.OperatorType == GDDualOperatorType.AddAndAssign ||
                dualOp.OperatorType == GDDualOperatorType.SubtractAndAssign ||
                dualOp.OperatorType == GDDualOperatorType.MultiplyAndAssign ||
                dualOp.OperatorType == GDDualOperatorType.DivideAndAssign)
            {
                var left = dualOp.LeftExpression;
                if (left is GDIdentifierExpression leftId && leftId.Identifier == identifier)
                {
                    return ReferenceKind.Write;
                }
            }
        }

        // Default to read
        return ReferenceKind.Read;
    }
}

internal class ReferenceInfoImpl : IReferenceInfo
{
    public ReferenceInfoImpl(string filePath, int line, int column, string contextLine, ReferenceKind kind, GDIdentifier? identifier)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
        ContextLine = contextLine;
        Kind = kind;
        Identifier = identifier;
    }

    public string FilePath { get; }
    public int Line { get; }
    public int Column { get; }
    public string ContextLine { get; }
    public ReferenceKind Kind { get; }
    public GDIdentifier? Identifier { get; }
}
