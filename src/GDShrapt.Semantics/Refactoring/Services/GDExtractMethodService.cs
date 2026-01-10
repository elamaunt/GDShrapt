using System.Collections.Generic;
using System.Linq;
using System.Text;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for extracting selected statements into a new method.
/// Analyzes dependencies to determine required parameters.
/// </summary>
public class GDExtractMethodService
{
    /// <summary>
    /// Checks if the extract method refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (context?.ClassDeclaration == null)
            return false;

        // Must have statements selected
        return context.HasStatementsSelected;
    }

    /// <summary>
    /// Plans the extract method refactoring without applying changes.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="methodName">Name for the new method</param>
    /// <returns>Plan result with preview information</returns>
    public GDExtractMethodResult Plan(GDRefactoringContext context, string methodName)
    {
        if (!CanExecute(context))
            return GDExtractMethodResult.Failed("Cannot extract method at this position");

        var extractionInfo = AnalyzeExtraction(context);
        if (extractionInfo == null)
            return GDExtractMethodResult.Failed("Could not analyze selection for extraction");

        var normalizedName = NormalizeMethodName(methodName);

        // Build preview
        var generatedMethod = BuildMethodDeclaration(
            normalizedName,
            extractionInfo.DetectedParameters,
            extractionInfo.IsStatic,
            extractionInfo.TokensToExtract);

        var generatedCall = BuildMethodCall(normalizedName, extractionInfo.DetectedParameters);

        return GDExtractMethodResult.Planned(
            normalizedName,
            extractionInfo.DetectedParameters.Select(p => p.Sequence).ToList(),
            extractionInfo.IsStatic,
            generatedMethod,
            generatedCall);
    }

    /// <summary>
    /// Executes the extract method refactoring.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="methodName">Name for the new method</param>
    /// <returns>Result with text edits to apply</returns>
    public GDRefactoringResult Execute(GDRefactoringContext context, string methodName)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot extract method at this position");

        var extractionInfo = AnalyzeExtraction(context);
        if (extractionInfo == null)
            return GDRefactoringResult.Failed("Could not analyze selection for extraction");

        var normalizedName = NormalizeMethodName(methodName);
        var filePath = context.Script.Reference.FullPath;

        // Build the new method
        var generatedMethod = BuildMethodDeclaration(
            normalizedName,
            extractionInfo.DetectedParameters,
            extractionInfo.IsStatic,
            extractionInfo.TokensToExtract);

        // Build the method call
        var generatedCall = BuildMethodCall(normalizedName, extractionInfo.DetectedParameters);

        // Get original code range to replace
        var selection = context.Selection;
        var originalText = GetSelectedStatementsText(extractionInfo.TokensToExtract);

        // Create edits:
        // 1. Replace selected statements with method call
        // 2. Add new method after the containing method
        var edits = new List<GDTextEdit>();

        // Edit 1: Replace selection with method call (using proper indentation)
        var callIndent = GetIndentation(extractionInfo.StatementsListUnderRefactoring);
        var callEdit = new GDTextEdit(
            filePath,
            selection.StartLine,
            selection.StartColumn,
            originalText,
            callIndent + generatedCall);
        edits.Add(callEdit);

        // Edit 2: Add new method after the owning member
        if (extractionInfo.OwningMember != null)
        {
            var insertLine = extractionInfo.OwningMember.EndLine + 1;
            var methodText = "\n\n" + generatedMethod;
            var methodEdit = new GDTextEdit(
                filePath,
                insertLine,
                0,
                "",
                methodText);
            edits.Add(methodEdit);
        }

        return GDRefactoringResult.Succeeded(edits);
    }

    #region Extraction Analysis

    private ExtractionInfo AnalyzeExtraction(GDRefactoringContext context)
    {
        var selection = context.Selection;
        var classDecl = context.ClassDeclaration;

        // Find the statements list containing the selection
        GDStatementsList statementsListUnderRefactoring = null;
        List<GDSyntaxToken> tokensToExtract = null;

        foreach (var statementsList in classDecl.AllNodesReversed.OfType<GDStatementsList>())
        {
            var tokensToExtractFromStatementsList = new List<GDSyntaxToken>();

            foreach (var token in statementsList.Tokens
                .SkipWhile(x => x is GDIntendation)
                .SkipWhile(x => x is GDSpace)
                .SkipWhile(x => x is GDNewLine))
            {
                var line = token.StartLine;

                if (line > selection.EndLine || (line == selection.EndLine && token is GDNewLine))
                    break;

                if (selection.StartLine <= line)
                    tokensToExtractFromStatementsList.Add(token);
            }

            if (statementsListUnderRefactoring == null)
            {
                if (tokensToExtractFromStatementsList.Count > 0)
                {
                    tokensToExtract = tokensToExtractFromStatementsList;
                    statementsListUnderRefactoring = statementsList;
                }
            }
            else
            {
                if (tokensToExtractFromStatementsList.Count > 0 && statementsListUnderRefactoring.StartLine >= selection.StartLine)
                {
                    tokensToExtract = tokensToExtractFromStatementsList;
                    statementsListUnderRefactoring = statementsList;
                }
            }
        }

        if (statementsListUnderRefactoring == null || tokensToExtract == null || tokensToExtract.Count == 0)
            return null;

        // Get available identifiers and owning member
        var availableIdentifiers = statementsListUnderRefactoring.ExtractAllMethodScopeVisibleDeclarationsFromParents(
            selection.StartLine, out GDIdentifiableClassMember owningMember);

        if (owningMember == null)
            return null;

        // Find used identifiers from available scope
        var usedIdentifiers = new HashSet<GDIdentifier>();

        if (availableIdentifiers.Count > 0)
        {
            foreach (var token in tokensToExtract)
            {
                if (token is GDNode node)
                {
                    foreach (var dependency in node.GetDependencies())
                    {
                        if (availableIdentifiers.Contains(dependency))
                        {
                            usedIdentifiers.Add(dependency);
                        }
                    }
                }
            }
        }

        // Check if the enclosing method is static
        bool isStatic = statementsListUnderRefactoring.ClassMember is GDMethodDeclaration method && method.IsStatic;

        return new ExtractionInfo
        {
            StatementsListUnderRefactoring = statementsListUnderRefactoring,
            TokensToExtract = tokensToExtract,
            DetectedParameters = usedIdentifiers.ToList(),
            OwningMember = owningMember,
            IsStatic = isStatic
        };
    }

    private class ExtractionInfo
    {
        public GDStatementsList StatementsListUnderRefactoring { get; set; }
        public List<GDSyntaxToken> TokensToExtract { get; set; }
        public List<GDIdentifier> DetectedParameters { get; set; }
        public GDIdentifiableClassMember OwningMember { get; set; }
        public bool IsStatic { get; set; }
    }

    #endregion

    #region Code Generation

    private string BuildMethodDeclaration(
        string methodName,
        List<GDIdentifier> parameters,
        bool isStatic,
        List<GDSyntaxToken> bodyTokens)
    {
        var sb = new StringBuilder();

        // Static keyword if needed
        if (isStatic)
            sb.Append("static ");

        // Function signature
        sb.Append("func ");
        sb.Append(methodName);
        sb.Append("(");

        // Parameters
        for (int i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(parameters[i].Sequence);
        }

        sb.AppendLine("):");

        // Body - extract statements with proper indentation
        var bodyText = GetExtractedBodyText(bodyTokens);
        sb.Append(bodyText);

        return sb.ToString();
    }

    private string BuildMethodCall(string methodName, List<GDIdentifier> parameters)
    {
        var sb = new StringBuilder();
        sb.Append(methodName);
        sb.Append("(");

        for (int i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(parameters[i].Sequence);
        }

        sb.Append(")");
        return sb.ToString();
    }

    private string GetExtractedBodyText(List<GDSyntaxToken> tokens)
    {
        if (tokens == null || tokens.Count == 0)
            return "\tpass\n";

        var sb = new StringBuilder();

        foreach (var token in tokens)
        {
            var tokenText = token.ToString();
            // Ensure proper indentation for method body
            if (token is GDStatement)
            {
                var lines = tokenText.Split('\n');
                foreach (var line in lines)
                {
                    var trimmed = line.TrimStart('\t', ' ');
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        sb.Append('\t');
                        sb.AppendLine(trimmed);
                    }
                }
            }
            else if (!(token is GDNewLine && sb.Length == 0))
            {
                sb.Append(tokenText);
            }
        }

        if (sb.Length == 0)
            return "\tpass\n";

        return sb.ToString();
    }

    private string GetSelectedStatementsText(List<GDSyntaxToken> tokens)
    {
        if (tokens == null || tokens.Count == 0)
            return "";

        var sb = new StringBuilder();
        foreach (var token in tokens)
        {
            sb.Append(token.ToString());
        }
        return sb.ToString();
    }

    private string GetIndentation(GDStatementsList statementsList)
    {
        // Get indentation from the statements list context
        if (statementsList?.Parent is GDNode parent)
        {
            var indent = parent.Tokens.OfType<GDIntendation>().FirstOrDefault();
            if (indent != null)
            {
                return new string('\t', indent.LineIntendationThreshold + 1);
            }
        }
        return "\t";
    }

    private string NormalizeMethodName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "_new_method";

        // Convert to snake_case if needed
        var result = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                result.Append(char.ToLowerInvariant(c));
            }
            else if (char.IsWhiteSpace(c))
            {
                result.Append('_');
            }
        }

        var normalized = result.ToString();
        if (string.IsNullOrEmpty(normalized) || char.IsDigit(normalized[0]))
            return "_new_method";

        return normalized;
    }

    #endregion
}

/// <summary>
/// Result of extract method planning operation.
/// </summary>
public class GDExtractMethodResult : GDRefactoringResult
{
    /// <summary>
    /// The normalized method name.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Parameters detected from outer scope variables used in selection.
    /// </summary>
    public IReadOnlyList<string> DetectedParameters { get; }

    /// <summary>
    /// Whether the new method should be static.
    /// </summary>
    public bool IsStatic { get; }

    /// <summary>
    /// Preview of the generated method code.
    /// </summary>
    public string GeneratedMethodCode { get; }

    /// <summary>
    /// Preview of the generated method call.
    /// </summary>
    public string GeneratedCallCode { get; }

    private GDExtractMethodResult(
        bool success,
        string errorMessage,
        IReadOnlyList<GDTextEdit> edits,
        string methodName,
        IReadOnlyList<string> detectedParameters,
        bool isStatic,
        string generatedMethodCode,
        string generatedCallCode)
        : base(success, errorMessage, edits)
    {
        MethodName = methodName;
        DetectedParameters = detectedParameters ?? System.Array.Empty<string>();
        IsStatic = isStatic;
        GeneratedMethodCode = generatedMethodCode;
        GeneratedCallCode = generatedCallCode;
    }

    /// <summary>
    /// Creates a planned result with preview information.
    /// </summary>
    public static GDExtractMethodResult Planned(
        string methodName,
        IReadOnlyList<string> detectedParameters,
        bool isStatic,
        string generatedMethodCode,
        string generatedCallCode)
    {
        return new GDExtractMethodResult(
            true, null, null,
            methodName, detectedParameters, isStatic,
            generatedMethodCode, generatedCallCode);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDExtractMethodResult Failed(string errorMessage)
    {
        return new GDExtractMethodResult(
            false, errorMessage, null,
            null, null, false, null, null);
    }
}
