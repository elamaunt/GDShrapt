namespace GDShrapt.Semantics;

/// <summary>
/// Service for "Find Implementations" — locates subclass overrides of methods/classes.
/// </summary>
public class GDImplementationService : GDRefactoringServiceBase
{
    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel? _projectModel;

    public GDImplementationService(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project;
        _projectModel = projectModel;
    }

    /// <summary>
    /// Find all implementations (overrides) of the symbol at cursor.
    /// For a class: finds all scripts that extend it.
    /// For a method: finds all overrides in subclasses.
    /// </summary>
    public IReadOnlyList<GDImplementationLocation> FindImplementations(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return [];

        var semanticModel = context.Script?.SemanticModel;
        if (semanticModel == null)
            return [];

        var symbol = semanticModel.GetSymbolAtPosition(context.Cursor.Line, context.Cursor.Column);

        // If no symbol found, check if cursor is on a class_name declaration
        if (symbol == null)
        {
            var classNameId = TryGetClassNameAtPosition(context);
            if (classNameId != null)
                return FindSubclasses(classNameId);

            return [];
        }

        var className = context.Script?.TypeName ?? semanticModel.BaseTypeName;
        if (string.IsNullOrEmpty(className))
            return [];

        switch (symbol.Kind)
        {
            case GDSymbolKind.Class:
                return FindSubclasses(symbol.Name);

            case GDSymbolKind.Method:
            case GDSymbolKind.Signal:
            case GDSymbolKind.Variable:
            case GDSymbolKind.Constant:
                return FindMemberOverrides(className, symbol.Name, symbol.Kind);

            default:
                return [];
        }
    }

    private string? TryGetClassNameAtPosition(GDRefactoringContext context)
    {
        var classDecl = context.Script?.Class;
        if (classDecl == null)
            return null;

        var classNameAttr = classDecl.ClassName;
        var identifier = classNameAttr?.Identifier;
        if (identifier == null || string.IsNullOrEmpty(identifier.Sequence))
            return null;

        // Check if cursor is on the class_name identifier
        if (context.Cursor.Line == identifier.StartLine &&
            context.Cursor.Column >= identifier.StartColumn &&
            context.Cursor.Column <= identifier.StartColumn + identifier.Sequence.Length)
        {
            return identifier.Sequence;
        }

        return null;
    }

    private IReadOnlyList<GDImplementationLocation> FindSubclasses(string className)
    {
        var results = new List<GDImplementationLocation>();

        foreach (var script in _project.ScriptFiles)
        {
            if (script?.Class == null || script.FullPath == null)
                continue;

            var baseType = script.SemanticModel?.BaseTypeName;
            if (baseType == className || IsSubclassOf(script, className))
            {
                var classNameToken = script.Class.ClassName?.Identifier;
                var line = classNameToken?.StartLine ?? 0;
                var column = classNameToken?.StartColumn ?? 0;

                results.Add(new GDImplementationLocation
                {
                    FilePath = script.FullPath,
                    Line = line,
                    Column = column,
                    SymbolName = script.TypeName ?? System.IO.Path.GetFileNameWithoutExtension(script.FullPath),
                    Kind = GDSymbolKind.Class
                });
            }
        }

        return results;
    }

    private IReadOnlyList<GDImplementationLocation> FindMemberOverrides(
        string declaringClass, string memberName, GDSymbolKind memberKind)
    {
        var results = new List<GDImplementationLocation>();

        foreach (var script in _project.ScriptFiles)
        {
            if (script?.Class == null || script.FullPath == null)
                continue;

            // Check if this script is a subclass of the declaring class
            if (!IsSubclassOf(script, declaringClass))
                continue;

            // Check if it redeclares the member
            var symbol = script.SemanticModel?.FindSymbol(memberName);
            if (symbol == null || symbol.DeclarationNode == null)
                continue;

            // Ensure the member is declared in THIS file (not inherited)
            var posToken = symbol.PositionToken;
            var line = posToken?.StartLine ?? symbol.DeclarationNode.StartLine;
            var column = posToken?.StartColumn ?? symbol.DeclarationNode.StartColumn;

            results.Add(new GDImplementationLocation
            {
                FilePath = script.FullPath,
                Line = line,
                Column = column,
                SymbolName = memberName,
                Kind = memberKind
            });
        }

        return results;
    }

    private bool IsSubclassOf(GDScriptFile script, string baseClassName)
    {
        var visited = new HashSet<string>();
        var current = script.SemanticModel?.BaseTypeName;

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            if (current == baseClassName)
                return true;

            // Walk up the inheritance chain via project scripts
            var parentScript = _project.GetScriptByTypeName(current);
            current = parentScript?.SemanticModel?.BaseTypeName;
        }

        return false;
    }
}

/// <summary>
/// An implementation location (override/subclass).
/// </summary>
public class GDImplementationLocation
{
    /// <summary>Full file path.</summary>
    public required string FilePath { get; init; }

    /// <summary>Line (0-based).</summary>
    public int Line { get; init; }

    /// <summary>Column (0-based).</summary>
    public int Column { get; init; }

    /// <summary>Symbol name.</summary>
    public required string SymbolName { get; init; }

    /// <summary>Symbol kind.</summary>
    public GDSymbolKind? Kind { get; init; }
}
