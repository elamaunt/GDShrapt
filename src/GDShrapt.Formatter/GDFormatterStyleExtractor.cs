using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Formatter
{
    /// <summary>
    /// Extracts formatting style from sample GDScript code.
    /// </summary>
    public class GDFormatterStyleExtractor : GDVisitor
    {
        private readonly GDScriptReader _reader = new GDScriptReader();

        // Statistics for style detection
        private int _tabIndentCount;
        private int _spaceIndentCount;
        private readonly List<int> _indentSizes = new List<int>();
        private readonly List<int> _blankLinesBetweenFunctions = new List<int>();
        private readonly List<int> _blankLinesAfterClass = new List<int>();
        private readonly List<int> _blankLinesBetweenMemberTypes = new List<int>();
        private int _spaceAroundOperatorsCount;
        private int _noSpaceAroundOperatorsCount;
        private int _spaceAfterCommaCount;
        private int _noSpaceAfterCommaCount;
        private int _spaceAfterColonCount;
        private int _noSpaceAfterColonCount;
        private int _spaceBeforeColonCount;
        private int _noSpaceBeforeColonCount;
        private int _spaceInsideParensCount;
        private int _noSpaceInsideParensCount;
        private int _spaceInsideBracketsCount;
        private int _noSpaceInsideBracketsCount;
        private int _spaceInsideBracesCount;
        private int _noSpaceInsideBracesCount;

        private GDClassMember _previousMember;

        // New: Raw code analysis statistics
        private int _lfCount;
        private int _crlfCount;
        private readonly List<int> _lineLengths = new List<int>();
        private bool _hasBackslashContinuation;
        private int _linesWithTrailingWhitespace;
        private int _totalNonEmptyLines;
        private bool _hasTrailingNewline;
        private int _trailingNewlineCount;

        // New: Line wrap style detection
        private int _wrapAfterOpeningCount;
        private int _wrapBeforeElementsCount;
        private readonly List<int> _continuationIndents = new List<int>();

        // New: Indent size calculation with nesting
        private readonly Dictionary<int, int> _indentLevelToSize = new Dictionary<int, int>();
        private int _currentNestingLevel;

        /// <summary>
        /// Extracts formatting style from a parsed GDScript node.
        /// </summary>
        public GDFormatterOptions ExtractStyle(GDNode node)
        {
            if (node == null)
                return GDFormatterOptions.Default;

            ResetStatistics();
            node.WalkIn(this);
            return BuildOptions();
        }

        /// <summary>
        /// Extracts formatting style from GDScript source code.
        /// </summary>
        public GDFormatterOptions ExtractStyleFromCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return GDFormatterOptions.Default;

            ResetStatistics();

            // Analyze raw code before parsing (parser normalizes line endings)
            AnalyzeRawCode(code);

            var tree = _reader.ParseFileContent(code);
            tree.WalkIn(this);
            return BuildOptions();
        }

        #region Raw Code Analysis

        private void AnalyzeRawCode(string code)
        {
            AnalyzeLineEndings(code);
            AnalyzeLineLengths(code);
            AnalyzeBackslashContinuation(code);
            AnalyzeTrailingWhitespace(code);
            AnalyzeTrailingNewlines(code);
        }

        private void AnalyzeLineEndings(string code)
        {
            int i = 0;
            while (i < code.Length)
            {
                if (code[i] == '\r')
                {
                    if (i + 1 < code.Length && code[i + 1] == '\n')
                    {
                        _crlfCount++;
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                }
                else if (code[i] == '\n')
                {
                    _lfCount++;
                    i++;
                }
                else
                {
                    i++;
                }
            }
        }

        private void AnalyzeLineLengths(string code)
        {
            var lines = code.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.TrimEnd('\r');
                if (trimmed.Length > 0)
                {
                    _lineLengths.Add(trimmed.Length);
                }
            }
        }

        private void AnalyzeBackslashContinuation(string code)
        {
            if (code.Contains("\\\n") || code.Contains("\\\r\n"))
            {
                _hasBackslashContinuation = true;
            }
        }

        private void AnalyzeTrailingWhitespace(string code)
        {
            var lines = code.Split('\n');
            foreach (var line in lines)
            {
                var content = line.TrimEnd('\r');
                if (content.Length > 0)
                {
                    _totalNonEmptyLines++;
                    if (content != content.TrimEnd())
                    {
                        _linesWithTrailingWhitespace++;
                    }
                }
            }
        }

        private void AnalyzeTrailingNewlines(string code)
        {
            if (string.IsNullOrEmpty(code))
                return;

            int i = code.Length - 1;
            while (i >= 0 && (code[i] == '\n' || code[i] == '\r'))
            {
                if (code[i] == '\n')
                    _trailingNewlineCount++;
                i--;
            }

            _hasTrailingNewline = _trailingNewlineCount > 0;
        }

        #endregion

        private void ResetStatistics()
        {
            _tabIndentCount = 0;
            _spaceIndentCount = 0;
            _indentSizes.Clear();
            _blankLinesBetweenFunctions.Clear();
            _blankLinesAfterClass.Clear();
            _blankLinesBetweenMemberTypes.Clear();
            _spaceAroundOperatorsCount = 0;
            _noSpaceAroundOperatorsCount = 0;
            _spaceAfterCommaCount = 0;
            _noSpaceAfterCommaCount = 0;
            _spaceAfterColonCount = 0;
            _noSpaceAfterColonCount = 0;
            _spaceBeforeColonCount = 0;
            _noSpaceBeforeColonCount = 0;
            _spaceInsideParensCount = 0;
            _noSpaceInsideParensCount = 0;
            _spaceInsideBracketsCount = 0;
            _noSpaceInsideBracketsCount = 0;
            _spaceInsideBracesCount = 0;
            _noSpaceInsideBracesCount = 0;
            _previousMember = null;

            // Reset new statistics
            _lfCount = 0;
            _crlfCount = 0;
            _lineLengths.Clear();
            _hasBackslashContinuation = false;
            _linesWithTrailingWhitespace = 0;
            _totalNonEmptyLines = 0;
            _hasTrailingNewline = false;
            _trailingNewlineCount = 0;
            _wrapAfterOpeningCount = 0;
            _wrapBeforeElementsCount = 0;
            _continuationIndents.Clear();
            _indentLevelToSize.Clear();
            _currentNestingLevel = 0;
        }

        private GDFormatterOptions BuildOptions()
        {
            var options = new GDFormatterOptions();

            // Line endings
            if (_crlfCount > _lfCount)
                options.LineEnding = LineEndingStyle.CRLF;
            else
                options.LineEnding = LineEndingStyle.LF;

            // Indentation style
            if (_tabIndentCount > _spaceIndentCount)
            {
                options.IndentStyle = IndentStyle.Tabs;
            }
            else if (_spaceIndentCount > _tabIndentCount)
            {
                options.IndentStyle = IndentStyle.Spaces;
                options.IndentSize = CalculateIndentSize();
            }

            // Max line length
            if (_lineLengths.Count > 0)
            {
                var sorted = _lineLengths.OrderBy(x => x).ToList();
                var p95Index = Math.Min((int)(sorted.Count * 0.95), sorted.Count - 1);
                var maxObserved = sorted[p95Index];

                if (maxObserved <= 85)
                    options.MaxLineLength = 80;
                else if (maxObserved <= 105)
                    options.MaxLineLength = 100;
                else if (maxObserved <= 125)
                    options.MaxLineLength = 120;
                else
                    options.MaxLineLength = 0; // Disable if very long lines
            }

            // Line wrap style
            if (_wrapAfterOpeningCount + _wrapBeforeElementsCount > 0)
            {
                options.LineWrapStyle = _wrapAfterOpeningCount >= _wrapBeforeElementsCount
                    ? LineWrapStyle.AfterOpeningBracket
                    : LineWrapStyle.BeforeElements;
            }

            // Continuation indent size
            if (_continuationIndents.Count > 0)
            {
                options.ContinuationIndentSize = (int)Math.Round(_continuationIndents.Average());
            }

            // Backslash continuation
            options.UseBackslashContinuation = _hasBackslashContinuation;

            // Trailing whitespace
            if (_totalNonEmptyLines > 0)
            {
                options.RemoveTrailingWhitespace = _linesWithTrailingWhitespace < _totalNonEmptyLines * 0.1;
            }

            // Trailing newline
            options.EnsureTrailingNewline = _hasTrailingNewline;
            options.RemoveMultipleTrailingNewlines = _trailingNewlineCount <= 1;

            // Blank lines
            if (_blankLinesBetweenFunctions.Count > 0)
            {
                options.BlankLinesBetweenFunctions = (int)_blankLinesBetweenFunctions.Average();
            }

            if (_blankLinesAfterClass.Count > 0)
            {
                options.BlankLinesAfterClassDeclaration = (int)_blankLinesAfterClass.Average();
            }

            if (_blankLinesBetweenMemberTypes.Count > 0)
            {
                options.BlankLinesBetweenMemberTypes = (int)_blankLinesBetweenMemberTypes.Average();
            }

            // Spacing options - use majority voting
            options.SpaceAroundOperators = _spaceAroundOperatorsCount >= _noSpaceAroundOperatorsCount;
            options.SpaceAfterComma = _spaceAfterCommaCount >= _noSpaceAfterCommaCount;
            options.SpaceAfterColon = _spaceAfterColonCount >= _noSpaceAfterColonCount;
            options.SpaceBeforeColon = _spaceBeforeColonCount > _noSpaceBeforeColonCount;
            options.SpaceInsideParentheses = _spaceInsideParensCount > _noSpaceInsideParensCount;
            options.SpaceInsideBrackets = _spaceInsideBracketsCount > _noSpaceInsideBracketsCount;
            options.SpaceInsideBraces = _spaceInsideBracesCount >= _noSpaceInsideBracesCount;

            return options;
        }

        private int CalculateIndentSize()
        {
            // Try to calculate from nesting levels first
            if (_indentLevelToSize.Count >= 2)
            {
                var levels = _indentLevelToSize.Keys.OrderBy(x => x).ToList();
                var steps = new List<int>();
                for (int i = 1; i < levels.Count; i++)
                {
                    var step = _indentLevelToSize[levels[i]] - _indentLevelToSize[levels[i - 1]];
                    if (step > 0)
                        steps.Add(step);
                }
                if (steps.Count > 0)
                {
                    return (int)Math.Round(steps.Average());
                }
            }

            // Fallback to most common indent size
            if (_indentSizes.Count > 0)
            {
                return _indentSizes
                    .GroupBy(x => x)
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Key;
            }

            return 4; // Default
        }

        #region Visitor Methods

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            _previousMember = null;
            AnalyzeIndentation(classDeclaration);
        }

        public override void Visit(GDStatementsList statementsList)
        {
            AnalyzeIndentation(statementsList);
        }

        private void AnalyzeIndentation(GDNode node)
        {
            if (node?.Form == null)
                return;

            foreach (var token in node.Form)
            {
                if (token is GDIntendation intendation)
                {
                    var seq = intendation.Sequence;
                    if (string.IsNullOrEmpty(seq))
                        continue;

                    if (seq[0] == '\t')
                    {
                        _tabIndentCount++;
                    }
                    else if (seq[0] == ' ')
                    {
                        _spaceIndentCount++;
                        int spaceCount = seq.TakeWhile(c => c == ' ').Count();
                        if (spaceCount > 0 && spaceCount <= 16)
                        {
                            _indentSizes.Add(spaceCount);

                            // Track indent size per nesting level
                            if (_currentNestingLevel > 0)
                            {
                                if (!_indentLevelToSize.ContainsKey(_currentNestingLevel))
                                {
                                    _indentLevelToSize[_currentNestingLevel] = spaceCount;
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            _currentNestingLevel = 1;
            AnalyzeMemberBlankLines(methodDeclaration, true);
            AnalyzeMethodSpacing(methodDeclaration);
            AnalyzeMultilineMethod(methodDeclaration);
        }

        public override void Left(GDMethodDeclaration methodDeclaration)
        {
            _currentNestingLevel = 0;
        }

        public override void Visit(GDIfStatement ifStatement)
        {
            _currentNestingLevel++;
        }

        public override void Left(GDIfStatement ifStatement)
        {
            _currentNestingLevel--;
        }

        public override void Visit(GDForStatement forStatement)
        {
            _currentNestingLevel++;
        }

        public override void Left(GDForStatement forStatement)
        {
            _currentNestingLevel--;
        }

        public override void Visit(GDWhileStatement whileStatement)
        {
            _currentNestingLevel++;
        }

        public override void Left(GDWhileStatement whileStatement)
        {
            _currentNestingLevel--;
        }

        public override void Visit(GDMatchStatement matchStatement)
        {
            _currentNestingLevel++;
        }

        public override void Left(GDMatchStatement matchStatement)
        {
            _currentNestingLevel--;
        }

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            AnalyzeMemberBlankLines(variableDeclaration, false);
            AnalyzeColonSpacing(variableDeclaration.Colon, variableDeclaration);
            AnalyzeAssignSpacing(variableDeclaration.Assign, variableDeclaration);
        }

        public override void Visit(GDSignalDeclaration signalDeclaration)
        {
            AnalyzeMemberBlankLines(signalDeclaration, false);
        }

        public override void Visit(GDEnumDeclaration enumDeclaration)
        {
            AnalyzeMemberBlankLines(enumDeclaration, false);
        }

        public override void Visit(GDDualOperatorExpression expression)
        {
            if (expression?.Operator == null)
                return;

            var form = expression.Form;
            var prevToken = form.PreviousTokenBefore(expression.Operator);
            var nextToken = form.NextTokenAfter(expression.Operator);

            if (prevToken is GDSpace || nextToken is GDSpace)
                _spaceAroundOperatorsCount++;
            else
                _noSpaceAroundOperatorsCount++;
        }

        public override void Visit(GDParametersList parametersList)
        {
            AnalyzeCommasInList(parametersList);
        }

        public override void Visit(GDExpressionsList expressionsList)
        {
            AnalyzeCommasInList(expressionsList);
        }

        private void AnalyzeCommasInList(GDNode listNode)
        {
            if (listNode?.Form == null)
                return;

            foreach (var token in listNode.Form)
            {
                if (token is GDComma comma)
                {
                    var nextToken = listNode.Form.NextTokenAfter(comma);
                    if (nextToken is GDSpace)
                        _spaceAfterCommaCount++;
                    else if (nextToken != null && !(nextToken is GDNewLine))
                        _noSpaceAfterCommaCount++;
                }
            }
        }

        public override void Visit(GDCallExpression callExpression)
        {
            AnalyzeParenthesesSpacing(callExpression.OpenBracket, callExpression.CloseBracket, callExpression);
            AnalyzeMultilineConstruct(callExpression.OpenBracket, callExpression.CloseBracket, callExpression);
        }

        public override void Visit(GDArrayInitializerExpression arrayExpression)
        {
            AnalyzeBracketsSpacing(arrayExpression.SquareOpenBracket, arrayExpression.SquareCloseBracket, arrayExpression);
            AnalyzeMultilineConstruct(arrayExpression.SquareOpenBracket, arrayExpression.SquareCloseBracket, arrayExpression);
        }

        public override void Visit(GDDictionaryInitializerExpression dictExpression)
        {
            AnalyzeBracesSpacing(dictExpression.FigureOpenBracket, dictExpression.FigureCloseBracket, dictExpression);
            AnalyzeMultilineConstruct(dictExpression.FigureOpenBracket, dictExpression.FigureCloseBracket, dictExpression);
        }

        public override void Visit(GDDictionaryKeyValueDeclaration keyValue)
        {
            AnalyzeColonSpacing(keyValue.Colon, keyValue);
        }

        public override void Visit(GDParameterDeclaration parameter)
        {
            AnalyzeColonSpacing(parameter.Colon, parameter);
            AnalyzeAssignSpacing(parameter.Assign, parameter);
        }

        public override void Visit(GDVariableDeclarationStatement variableStatement)
        {
            AnalyzeColonSpacing(variableStatement.Colon, variableStatement);
            AnalyzeAssignSpacing(variableStatement.Assign, variableStatement);
        }

        #endregion

        #region Analysis Helpers

        private void AnalyzeMemberBlankLines(GDClassMember member, bool isFunction)
        {
            if (member?.Parent == null)
            {
                _previousMember = member;
                return;
            }

            if (_previousMember != null)
            {
                int blankLines = CountBlankLinesBefore(member);

                bool prevIsFunction = _previousMember is GDMethodDeclaration;

                if (isFunction || prevIsFunction)
                {
                    _blankLinesBetweenFunctions.Add(blankLines);
                }
                else if (member.GetType() != _previousMember.GetType())
                {
                    _blankLinesBetweenMemberTypes.Add(blankLines);
                }
            }

            _previousMember = member;
        }

        private int CountBlankLinesBefore(GDClassMember member)
        {
            if (member?.Parent == null)
                return 0;

            var parent = member.Parent as GDNode;
            if (parent?.Form == null)
                return 0;

            var firstToken = member.Form?.FirstToken;
            if (firstToken == null)
                return 0;

            int newlineCount = 0;
            try
            {
                var current = parent.Form.PreviousTokenBefore(firstToken);

                while (current != null)
                {
                    if (current is GDNewLine)
                    {
                        newlineCount++;
                        current = parent.Form.PreviousTokenBefore(current);
                    }
                    else if (current is GDSpace || current is GDIntendation)
                    {
                        current = parent.Form.PreviousTokenBefore(current);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch
            {
                // Token not found in parent form - this can happen for certain node structures
                return 0;
            }

            // Blank lines = newlines - 1
            return newlineCount > 0 ? newlineCount - 1 : 0;
        }

        private void AnalyzeMethodSpacing(GDMethodDeclaration method)
        {
            AnalyzeParenthesesSpacing(method.OpenBracket, method.CloseBracket, method);
            AnalyzeColonSpacing(method.Colon, method);
        }

        private void AnalyzeMultilineMethod(GDMethodDeclaration method)
        {
            AnalyzeMultilineConstruct(method.OpenBracket, method.CloseBracket, method);
        }

        private void AnalyzeMultilineConstruct(GDSyntaxToken open, GDSyntaxToken close, GDNode parent)
        {
            if (open == null || close == null || parent?.Form == null)
                return;

            // Check if construct spans multiple lines
            if (open.EndLine != close.StartLine)
            {
                // Check what follows the opening bracket
                var nextToken = parent.Form.NextTokenAfter(open);
                if (nextToken is GDNewLine)
                {
                    _wrapAfterOpeningCount++;

                    // Try to detect continuation indent
                    var nextNonWhitespace = parent.Form.NextTokenAfter(nextToken);
                    while (nextNonWhitespace is GDSpace || nextNonWhitespace is GDNewLine)
                    {
                        nextNonWhitespace = parent.Form.NextTokenAfter(nextNonWhitespace);
                    }

                    if (nextNonWhitespace is GDIntendation indent)
                    {
                        var openLineIndent = GetLineIndentSize(open);
                        var contIndent = indent.Sequence?.Length ?? 0;
                        if (contIndent > openLineIndent)
                        {
                            _continuationIndents.Add(contIndent - openLineIndent);
                        }
                    }
                }
                else if (nextToken != null && !(nextToken is GDNewLine))
                {
                    _wrapBeforeElementsCount++;
                }
            }
        }

        private int GetLineIndentSize(GDSyntaxToken token)
        {
            // Find the indentation at the start of the line containing this token
            var current = token;
            while (current != null)
            {
                var prev = token.Parent?.Form?.PreviousTokenBefore(current);
                if (prev == null)
                    break;

                if (prev is GDNewLine)
                {
                    var nextAfterNewline = token.Parent?.Form?.NextTokenAfter(prev);
                    if (nextAfterNewline is GDIntendation indent)
                    {
                        return indent.Sequence?.Length ?? 0;
                    }
                    return 0;
                }

                if (prev is GDIntendation indent2)
                {
                    return indent2.Sequence?.Length ?? 0;
                }

                current = prev;
            }
            return 0;
        }

        private void AnalyzeColonSpacing(GDColon colon, GDNode parent)
        {
            if (colon == null || parent?.Form == null)
                return;

            var prevToken = parent.Form.PreviousTokenBefore(colon);
            var nextToken = parent.Form.NextTokenAfter(colon);

            // Space before colon
            if (prevToken is GDSpace)
                _spaceBeforeColonCount++;
            else if (prevToken != null)
                _noSpaceBeforeColonCount++;

            // Space after colon
            if (nextToken is GDSpace)
                _spaceAfterColonCount++;
            else if (nextToken != null && !(nextToken is GDNewLine))
                _noSpaceAfterColonCount++;
        }

        private void AnalyzeAssignSpacing(GDAssign assign, GDNode parent)
        {
            if (assign == null || parent?.Form == null)
                return;

            var prevToken = parent.Form.PreviousTokenBefore(assign);
            var nextToken = parent.Form.NextTokenAfter(assign);

            if (prevToken is GDSpace || nextToken is GDSpace)
                _spaceAroundOperatorsCount++;
            else
                _noSpaceAroundOperatorsCount++;
        }

        private void AnalyzeParenthesesSpacing(GDOpenBracket open, GDCloseBracket close, GDNode parent)
        {
            if (parent?.Form == null)
                return;

            if (open != null)
            {
                var nextToken = parent.Form.NextTokenAfter(open);
                if (nextToken is GDSpace)
                    _spaceInsideParensCount++;
                else if (nextToken != null && !(nextToken is GDNewLine) && !(nextToken is GDCloseBracket))
                    _noSpaceInsideParensCount++;
            }

            if (close != null)
            {
                var prevToken = parent.Form.PreviousTokenBefore(close);
                if (prevToken is GDSpace)
                    _spaceInsideParensCount++;
                else if (prevToken != null && !(prevToken is GDNewLine) && !(prevToken is GDOpenBracket))
                    _noSpaceInsideParensCount++;
            }
        }

        private void AnalyzeBracketsSpacing(GDSquareOpenBracket open, GDSquareCloseBracket close, GDNode parent)
        {
            if (parent?.Form == null)
                return;

            if (open != null)
            {
                var nextToken = parent.Form.NextTokenAfter(open);
                if (nextToken is GDSpace)
                    _spaceInsideBracketsCount++;
                else if (nextToken != null && !(nextToken is GDNewLine) && !(nextToken is GDSquareCloseBracket))
                    _noSpaceInsideBracketsCount++;
            }

            if (close != null)
            {
                var prevToken = parent.Form.PreviousTokenBefore(close);
                if (prevToken is GDSpace)
                    _spaceInsideBracketsCount++;
                else if (prevToken != null && !(prevToken is GDNewLine) && !(prevToken is GDSquareOpenBracket))
                    _noSpaceInsideBracketsCount++;
            }
        }

        private void AnalyzeBracesSpacing(GDFigureOpenBracket open, GDFigureCloseBracket close, GDNode parent)
        {
            if (parent?.Form == null)
                return;

            if (open != null)
            {
                var nextToken = parent.Form.NextTokenAfter(open);
                if (nextToken is GDSpace)
                    _spaceInsideBracesCount++;
                else if (nextToken != null && !(nextToken is GDNewLine) && !(nextToken is GDFigureCloseBracket))
                    _noSpaceInsideBracesCount++;
            }

            if (close != null)
            {
                var prevToken = parent.Form.PreviousTokenBefore(close);
                if (prevToken is GDSpace)
                    _spaceInsideBracesCount++;
                else if (prevToken != null && !(prevToken is GDNewLine) && !(prevToken is GDFigureOpenBracket))
                    _noSpaceInsideBracesCount++;
            }
        }

        #endregion
    }
}
