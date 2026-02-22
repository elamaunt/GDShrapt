using GDShrapt.Reader;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Collects flow-sensitive type narrowing entries.
/// Finds identifier references where GetNarrowedType() returns a narrowed type.
/// </summary>
public class FlowNarrowingCollector
{
    public record NarrowingEntry(
        string MethodName,
        int Line,
        int Column,
        string VariableName,
        string NarrowedType,
        string BaseType);

    public List<NarrowingEntry> CollectEntries(GDScriptFile file)
    {
        var result = new List<NarrowingEntry>();

        var model = file.SemanticModel;
        if (model == null || file.Class == null)
            return result;

        // Walk all methods
        foreach (var method in file.Class.AllNodes.OfType<GDMethodDeclaration>())
        {
            var methodName = method.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                continue;

            // Walk all identifier expressions inside the method
            foreach (var identifier in method.AllNodes.OfType<GDIdentifierExpression>())
            {
                var varName = identifier.Identifier?.Sequence;
                if (string.IsNullOrEmpty(varName))
                    continue;

                int line = GetNodeLine(identifier);
                int column = GetNodeColumn(identifier);
                if (line == 0 && column == 0)
                    continue;

                // Check if there's a narrowed type at this location
                var narrowedType = model.TypeSystem.GetNarrowedType(varName, identifier);
                if (narrowedType == null)
                    continue;

                // Get base type for comparison
                var baseType = model.TypeSystem.GetType(identifier).DisplayName;

                // Only include if narrowing actually changed the type
                result.Add(new NarrowingEntry(
                    methodName,
                    line, column,
                    varName,
                    narrowedType,
                    baseType));
            }
        }

        return result.OrderBy(e => e.Line).ThenBy(e => e.Column).ToList();
    }

    private int GetNodeLine(GDNode? node)
    {
        if (node == null) return 0;
        var token = node.AllTokens.FirstOrDefault();
        return token != null ? token.StartLine + 1 : 0;
    }

    private int GetNodeColumn(GDNode? node)
    {
        if (node == null) return 0;
        var token = node.AllTokens.FirstOrDefault();
        return token?.StartColumn ?? 0;
    }
}
