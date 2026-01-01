using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
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

        // Member order extraction
        private readonly List<GDMemberCategory> _memberOrder = new List<GDMemberCategory>();
        private readonly HashSet<GDMemberCategory> _seenCategories = new HashSet<GDMemberCategory>();

        /// <summary>
        /// Whether to extract member order from sample code. Default: true.
        /// </summary>
        public bool ExtractMemberOrder { get; set; } = true;

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

            var tree = _reader.ParseFileContent(code);
            return ExtractStyle(tree);
        }

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
            _memberOrder.Clear();
            _seenCategories.Clear();
        }

        private GDFormatterOptions BuildOptions()
        {
            var options = new GDFormatterOptions();

            // Indentation style
            if (_tabIndentCount > _spaceIndentCount)
            {
                options.IndentStyle = IndentStyle.Tabs;
            }
            else if (_spaceIndentCount > _tabIndentCount)
            {
                options.IndentStyle = IndentStyle.Spaces;
                if (_indentSizes.Count > 0)
                {
                    // Use the most common indent size
                    options.IndentSize = _indentSizes
                        .GroupBy(x => x)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key;
                }
            }

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

            // Member order - only set if we extracted meaningful order
            if (ExtractMemberOrder && _memberOrder.Count > 1)
            {
                options.MemberOrder = new List<GDMemberCategory>(_memberOrder);
            }

            return options;
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
                        if (spaceCount > 0 && spaceCount <= 8)
                        {
                            _indentSizes.Add(spaceCount);
                        }
                    }
                }
            }
        }

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            AnalyzeMemberBlankLines(methodDeclaration, true);
            AnalyzeMethodSpacing(methodDeclaration);
            RecordMemberCategory(methodDeclaration);
        }

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            AnalyzeMemberBlankLines(variableDeclaration, false);
            AnalyzeColonSpacing(variableDeclaration.Colon, variableDeclaration);
            AnalyzeAssignSpacing(variableDeclaration.Assign, variableDeclaration);
            RecordMemberCategory(variableDeclaration);
        }

        public override void Visit(GDSignalDeclaration signalDeclaration)
        {
            AnalyzeMemberBlankLines(signalDeclaration, false);
            RecordMemberCategory(signalDeclaration);
        }

        public override void Visit(GDEnumDeclaration enumDeclaration)
        {
            AnalyzeMemberBlankLines(enumDeclaration, false);
            RecordMemberCategory(enumDeclaration);
        }

        public override void Visit(GDInnerClassDeclaration innerClassDeclaration)
        {
            RecordMemberCategory(innerClassDeclaration);
        }

        public override void Visit(GDClassNameAttribute classNameAttribute)
        {
            RecordMemberCategory(classNameAttribute);
        }

        public override void Visit(GDExtendsAttribute extendsAttribute)
        {
            RecordMemberCategory(extendsAttribute);
        }

        public override void Visit(GDToolAttribute toolAttribute)
        {
            RecordMemberCategory(toolAttribute);
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
        }

        public override void Visit(GDArrayInitializerExpression arrayExpression)
        {
            AnalyzeBracketsSpacing(arrayExpression.SquareOpenBracket, arrayExpression.SquareCloseBracket, arrayExpression);
        }

        public override void Visit(GDDictionaryInitializerExpression dictExpression)
        {
            AnalyzeBracesSpacing(dictExpression.FigureOpenBracket, dictExpression.FigureCloseBracket, dictExpression);
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

            // Blank lines = newlines - 1
            return newlineCount > 0 ? newlineCount - 1 : 0;
        }

        private void AnalyzeMethodSpacing(GDMethodDeclaration method)
        {
            AnalyzeParenthesesSpacing(method.OpenBracket, method.CloseBracket, method);
            AnalyzeColonSpacing(method.Colon, method);
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

        #region Member Order Extraction

        /// <summary>
        /// Records the category of a class member for order extraction.
        /// </summary>
        private void RecordMemberCategory(GDClassMember member)
        {
            if (!ExtractMemberOrder || member == null)
                return;

            var category = GDCodeReorderFormatRule.GetCategory(member);

            // Only record first occurrence of each category
            if (!_seenCategories.Contains(category))
            {
                _seenCategories.Add(category);
                _memberOrder.Add(category);
            }
        }

        #endregion
    }
}
