using System.Text.RegularExpressions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Resolves the cursor context for code completion — determines WHERE the cursor is
/// (class level, method body, type annotation, extends clause, etc.).
/// </summary>
public class GDCompletionContextResolver
{
    private static readonly Regex ExtendsPattern = new(@"^\s*extends(\s+\S*)?$", RegexOptions.Compiled);
    private static readonly Regex TypeAnnotationVarPattern = new(@"(?:var|const)\s+\w+\s*:\s*\S*$", RegexOptions.Compiled);
    private static readonly Regex TypeAnnotationParamPattern = new(@"\w+\s*:\s*\S*$", RegexOptions.Compiled);
    private static readonly Regex ReturnTypePattern = new(@"->\s*\S*$", RegexOptions.Compiled);
    private static readonly Regex AnnotationPattern = new(@"^\s*@\w*$", RegexOptions.Compiled);

    /// <summary>
    /// Resolves cursor context from AST position with text fallback.
    /// </summary>
    /// <param name="semanticModel">Semantic model for the file.</param>
    /// <param name="line0">0-based line number.</param>
    /// <param name="column0">0-based column number.</param>
    /// <param name="textBeforeCursor">Text before cursor on the current line (for fallback heuristics).</param>
    public static GDCursorContext Resolve(GDSemanticModel? semanticModel, int line0, int column0, string? textBeforeCursor)
    {
        if (semanticModel == null)
            return ResolveFromText(textBeforeCursor);

        // 1. Try token-based detection first
        var token = semanticModel.GetTokenAtPosition(line0, column0);

        if (token != null)
        {
            var tokenContext = ResolveFromToken(token);
            if (tokenContext != GDCursorContext.Unknown)
                return tokenContext;
        }

        // 2. Try node-based detection via parent chain
        var node = semanticModel.GetNodeAtPosition(line0, column0);

        if (node != null)
        {
            var nodeContext = ResolveFromNodeParentChain(node, line0, column0);
            if (nodeContext != GDCursorContext.Unknown)
                return nodeContext;
        }

        // 3. Fallback: text-based heuristics
        var textContext = ResolveFromText(textBeforeCursor);
        if (textContext != GDCursorContext.Unknown)
            return textContext;

        // 4. Final fallback: check if inside method or at class level
        return ResolveClassOrMethod(semanticModel, node, line0, column0);
    }

    private static GDCursorContext ResolveFromToken(GDSyntaxToken token)
    {
        // String literal
        if (token is GDStringPart)
            return GDCursorContext.StringLiteral;

        // Check if token is inside a string node
        var parent = token.Parent;
        while (parent != null)
        {
            if (parent is GDStringExpression || parent is GDStringNameExpression)
                return GDCursorContext.StringLiteral;
            if (parent is GDStringNode)
                return GDCursorContext.StringLiteral;
            parent = parent.Parent;
        }

        // Comment
        if (token is GDComment)
            return GDCursorContext.Comment;

        return GDCursorContext.Unknown;
    }

    private static GDCursorContext ResolveFromNodeParentChain(GDNode node, int line0, int column0)
    {
        var current = node;
        while (current != null)
        {
            // Extends clause: extends <TypeName>
            if (current is GDExtendsAttribute)
                return GDCursorContext.ExtendsClause;

            // Type annotation: var x: <Type>, param: <Type>, -> <Type>
            // But if inside extends clause, it's ExtendsClause not TypeAnnotation
            if (current is GDTypeNode)
            {
                var typeParent = current.Parent as GDNode;
                if (typeParent is GDExtendsAttribute)
                    return GDCursorContext.ExtendsClause;
                return GDCursorContext.TypeAnnotation;
            }

            // Inside call expression arguments
            if (current is GDCallExpression call)
            {
                if (IsPositionInCallArguments(call, line0, column0))
                    return GDCursorContext.FuncCallArgs;
            }

            // Match case pattern (not the body)
            if (current is GDMatchCaseDeclaration matchCase)
            {
                // If cursor is in the pattern area (before the colon/body), it's a match pattern
                if (IsInMatchPattern(matchCase, line0, column0))
                    return GDCursorContext.MatchPattern;
            }

            // Enum body
            if (current is GDEnumDeclaration)
                return GDCursorContext.EnumBody;

            // Custom attribute (@export, @onready, etc.)
            if (current is GDAttribute)
                return GDCursorContext.Annotation;

            current = current.Parent as GDNode;
        }

        return GDCursorContext.Unknown;
    }

    private static GDCursorContext ResolveFromText(string? textBeforeCursor)
    {
        if (string.IsNullOrEmpty(textBeforeCursor))
            return GDCursorContext.Unknown;

        var trimmed = textBeforeCursor.TrimEnd();

        // extends ClassName
        if (ExtendsPattern.IsMatch(trimmed))
            return GDCursorContext.ExtendsClause;

        // var x: Type or const x: Type
        if (TypeAnnotationVarPattern.IsMatch(trimmed))
            return GDCursorContext.TypeAnnotation;

        // -> ReturnType
        if (ReturnTypePattern.IsMatch(trimmed))
            return GDCursorContext.TypeAnnotation;

        // @annotation
        if (AnnotationPattern.IsMatch(trimmed))
            return GDCursorContext.Annotation;

        // Check for parameter type annotation inside func signature: func foo(param: Type
        if (trimmed.Contains("(") && TypeAnnotationParamPattern.IsMatch(trimmed))
        {
            // Make sure it's inside a function signature, not a call
            var lastParen = trimmed.LastIndexOf('(');
            var funcMatch = trimmed.LastIndexOf("func ", StringComparison.Ordinal);
            if (funcMatch >= 0 && funcMatch < lastParen)
                return GDCursorContext.TypeAnnotation;
        }

        return GDCursorContext.Unknown;
    }

    private static GDCursorContext ResolveClassOrMethod(GDSemanticModel semanticModel, GDNode? node, int line0, int column0)
    {
        // Walk parent chain to see if we're inside a method
        var current = node;
        while (current != null)
        {
            if (current is GDMethodDeclaration)
                return GDCursorContext.MethodBody;
            if (current is GDClassDeclaration)
                return GDCursorContext.ClassLevel;
            current = current.Parent as GDNode;
        }

        // If no node found, try position-based heuristic via GDPositionFinder
        // The finder is internal to Semantics, so we use the fact that if there's no enclosing
        // method declaration in the parent chain, we're at class level
        return GDCursorContext.ClassLevel;
    }

    private static bool IsPositionInCallArguments(GDCallExpression call, int line, int column)
    {
        var openBracket = call.OpenBracket;
        if (openBracket == null)
            return false;

        // Must be after open bracket
        if (line < openBracket.EndLine || (line == openBracket.EndLine && column <= openBracket.EndColumn))
            return false;

        // Must be before close bracket (or no close bracket = still typing)
        var closeBracket = call.CloseBracket;
        if (closeBracket != null)
        {
            if (line > closeBracket.StartLine || (line == closeBracket.StartLine && column > closeBracket.StartColumn))
                return false;
        }

        return true;
    }

    private static bool IsInMatchPattern(GDMatchCaseDeclaration matchCase, int line0, int column0)
    {
        // Match case has a pattern area and a body.
        // If we have a Colon token, the pattern is before it.
        // If there's no body yet, assume we're in the pattern.
        var statements = matchCase.Statements;
        if (statements == null)
            return true;

        // The body starts after the colon
        if (statements.StartLine > line0)
            return true;
        if (statements.StartLine == line0 && statements.StartColumn > column0)
            return true;

        return false;
    }
}
