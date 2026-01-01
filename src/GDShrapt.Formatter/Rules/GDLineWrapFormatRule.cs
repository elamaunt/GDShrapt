using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Formats line wrapping for long lines exceeding MaxLineLength.
    /// Handles arrays, dictionaries, function calls, method declarations, and method chains.
    /// </summary>
    public class GDLineWrapFormatRule : GDFormatRule
    {
        public override string RuleId => "GDF006";
        public override string Name => "line-wrap";
        public override string Description => "Wrap lines exceeding MaxLineLength";

        // Track current indentation level during traversal
        private int _currentIndentLevel;

        #region Visitor Methods

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            _currentIndentLevel = 0;
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            _currentIndentLevel++;
        }

        public override void Left(GDInnerClassDeclaration innerClass)
        {
            _currentIndentLevel--;
        }

        public override void Visit(GDMethodDeclaration method)
        {
            // Check if method declaration with parameters exceeds line length
            // BEFORE incrementing indent level, because the method signature
            // (including closing bracket) is at the class level, not indented
            if (ShouldWrap() && method.Parameters != null && method.Parameters.Count > 1)
            {
                var lineLength = EstimateNodeLineLength(method);
                if (lineLength > Options.MaxLineLength)
                {
                    WrapParametersList(method.Parameters, method.OpenBracket, method.CloseBracket, method);
                }
            }

            // Now increment for method body content
            _currentIndentLevel++;
        }

        public override void Left(GDMethodDeclaration method)
        {
            _currentIndentLevel--;
        }

        public override void Visit(GDIfBranch ifBranch)
        {
            _currentIndentLevel++;
        }

        public override void Left(GDIfBranch ifBranch)
        {
            _currentIndentLevel--;
        }

        public override void Visit(GDElifBranch elifBranch)
        {
            _currentIndentLevel++;
        }

        public override void Left(GDElifBranch elifBranch)
        {
            _currentIndentLevel--;
        }

        public override void Visit(GDElseBranch elseBranch)
        {
            _currentIndentLevel++;
        }

        public override void Left(GDElseBranch elseBranch)
        {
            _currentIndentLevel--;
        }

        public override void Visit(GDForStatement forStatement)
        {
            _currentIndentLevel++;
        }

        public override void Left(GDForStatement forStatement)
        {
            _currentIndentLevel--;
        }

        public override void Visit(GDWhileStatement whileStatement)
        {
            _currentIndentLevel++;
        }

        public override void Left(GDWhileStatement whileStatement)
        {
            _currentIndentLevel--;
        }

        public override void Visit(GDMatchStatement matchStatement)
        {
            _currentIndentLevel++;
        }

        public override void Left(GDMatchStatement matchStatement)
        {
            _currentIndentLevel--;
        }

        public override void Visit(GDCallExpression call)
        {
            if (!ShouldWrap())
                return;

            // Check if call with parameters exceeds line length
            if (call.Parameters != null && call.Parameters.Count > 1)
            {
                var lineLength = EstimateNodeLineLength(call);
                if (lineLength > Options.MaxLineLength)
                {
                    WrapExpressionsList(call.Parameters, call.OpenBracket, call.CloseBracket, call);
                }
            }
        }

        public override void Visit(GDArrayInitializerExpression array)
        {
            if (!ShouldWrap())
                return;

            // Check if array exceeds line length
            if (array.Values != null && array.Values.Count > 1)
            {
                var lineLength = EstimateNodeLineLength(array);
                if (lineLength > Options.MaxLineLength)
                {
                    WrapExpressionsList(array.Values, array.SquareOpenBracket, array.SquareCloseBracket, array);
                }
            }
        }

        public override void Visit(GDDictionaryInitializerExpression dict)
        {
            if (!ShouldWrap())
                return;

            // Check if dictionary exceeds line length
            if (dict.KeyValues != null && dict.KeyValues.Count > 1)
            {
                var lineLength = EstimateNodeLineLength(dict);
                if (lineLength > Options.MaxLineLength)
                {
                    WrapDictionaryKeyValues(dict.KeyValues, dict.FigureOpenBracket, dict.FigureCloseBracket, dict);
                }
            }
        }

        public override void Visit(GDMemberOperatorExpression memberOp)
        {
            if (!Options.UseBackslashContinuation)
                return;

            if (!ShouldWrap())
                return;

            // Only process if this is part of a method chain and is the root of the chain
            if (!IsRootOfMethodChain(memberOp))
                return;

            // Check if the entire chain exceeds line length
            var lineLength = EstimateNodeLineLength(memberOp);
            if (lineLength > Options.MaxLineLength)
            {
                WrapMethodChain(memberOp);
            }
        }

        #endregion

        #region Wrapping Logic

        private bool ShouldWrap()
        {
            return Options.MaxLineLength > 0 && Options.WrapLongLines;
        }

        private void WrapExpressionsList(GDExpressionsList list, GDSyntaxToken openBracket, GDSyntaxToken closeBracket, GDNode parent)
        {
            if (list == null || list.Count <= 1)
                return;

            // Check if already wrapped
            if (IsAlreadyWrapped(list))
                return;

            var wrapIndent = _currentIndentLevel + Options.ContinuationIndentSize;

            if (Options.LineWrapStyle == LineWrapStyle.AfterOpeningBracket)
            {
                // Style: func(\n  param1,\n  param2\n)
                WrapAfterOpeningBracket(list, openBracket, closeBracket, parent, wrapIndent);
            }
            else
            {
                // Style: func(param1,\n  param2, param3)
                WrapBeforeElements(list, wrapIndent);
            }
        }

        private void WrapParametersList(GDParametersList list, GDSyntaxToken openBracket, GDSyntaxToken closeBracket, GDNode parent)
        {
            if (list == null || list.Count <= 1)
                return;

            // Check if already wrapped
            if (IsAlreadyWrapped(list))
                return;

            var wrapIndent = _currentIndentLevel + Options.ContinuationIndentSize;

            if (Options.LineWrapStyle == LineWrapStyle.AfterOpeningBracket)
            {
                WrapAfterOpeningBracket(list, openBracket, closeBracket, parent, wrapIndent);
            }
            else
            {
                WrapBeforeElements(list, wrapIndent);
            }
        }

        private void WrapDictionaryKeyValues(GDDictionaryKeyValueDeclarationList list, GDSyntaxToken openBracket, GDSyntaxToken closeBracket, GDNode parent)
        {
            if (list == null || list.Count <= 1)
                return;

            // Check if already wrapped
            if (IsAlreadyWrapped(list))
                return;

            var wrapIndent = _currentIndentLevel + Options.ContinuationIndentSize;

            if (Options.LineWrapStyle == LineWrapStyle.AfterOpeningBracket)
            {
                WrapAfterOpeningBracket(list, openBracket, closeBracket, parent, wrapIndent);
            }
            else
            {
                WrapBeforeElements(list, wrapIndent);
            }
        }

        private void WrapAfterOpeningBracket(GDNode list, GDSyntaxToken openBracket, GDSyntaxToken closeBracket, GDNode parent, int wrapIndent)
        {
            if (list?.Form == null)
                return;

            var form = list.Form;
            bool isFirstElement = true;
            GDSyntaxToken lastElement = null;

            // Insert newline + indent after each comma and before the first element
            foreach (var token in form.ToList())
            {
                if (IsListElement(token))
                {
                    if (isFirstElement)
                    {
                        // Add newline + indent before first element
                        InsertLineBreakBefore(token, form, wrapIndent);
                        isFirstElement = false;
                    }
                    lastElement = token;
                }
                else if (token is GDComma comma)
                {
                    // Add newline + indent after comma
                    var nextToken = form.NextTokenAfter(comma);
                    if (nextToken != null && !(nextToken is GDNewLine))
                    {
                        // Remove space after comma if exists
                        if (nextToken is GDSpace)
                        {
                            form.Remove(nextToken);
                            nextToken = form.NextTokenAfter(comma);
                        }

                        if (nextToken != null)
                        {
                            InsertLineBreakBefore(nextToken, form, wrapIndent);
                        }
                    }
                }
            }

            // Add newline before closing bracket (at parent level indent)
            if (closeBracket != null && parent?.Form != null)
            {
                var prevToken = parent.Form.PreviousTokenBefore(closeBracket);
                if (prevToken != null && !(prevToken is GDNewLine) && !(prevToken is GDIntendation))
                {
                    InsertLineBreakBefore(closeBracket, parent.Form, _currentIndentLevel);
                }
            }
        }

        private void WrapBeforeElements(GDNode list, int wrapIndent)
        {
            if (list?.Form == null)
                return;

            var form = list.Form;
            int currentLineLength = EstimateCurrentLinePosition();
            bool needsWrap = false;

            foreach (var token in form.ToList())
            {
                if (token is GDComma comma)
                {
                    // Check if next element would exceed line length
                    var nextToken = form.NextTokenAfter(comma);
                    if (nextToken != null)
                    {
                        // Skip space if present
                        if (nextToken is GDSpace space)
                        {
                            currentLineLength += 1; // account for space
                            nextToken = form.NextTokenAfter(space);
                        }

                        if (nextToken != null && IsListElement(nextToken))
                        {
                            int elementLength = EstimateTokenLength(nextToken);
                            if (currentLineLength + elementLength + 2 > Options.MaxLineLength) // +2 for ", "
                            {
                                needsWrap = true;
                            }
                        }
                    }

                    if (needsWrap)
                    {
                        nextToken = form.NextTokenAfter(comma);
                        if (nextToken != null && !(nextToken is GDNewLine))
                        {
                            // Remove space after comma if exists
                            if (nextToken is GDSpace)
                            {
                                form.Remove(nextToken);
                                nextToken = form.NextTokenAfter(comma);
                            }

                            if (nextToken != null)
                            {
                                InsertLineBreakBefore(nextToken, form, wrapIndent);
                                currentLineLength = wrapIndent * GetIndentWidth();
                                needsWrap = false;
                            }
                        }
                    }
                    else
                    {
                        currentLineLength += 2; // ", "
                    }
                }
                else if (IsListElement(token))
                {
                    currentLineLength += EstimateTokenLength(token);
                }
            }
        }

        private void WrapMethodChain(GDMemberOperatorExpression rootMemberOp)
        {
            // Collect all member operations in the chain
            var chainMembers = new List<GDMemberOperatorExpression>();
            CollectMethodChainMembers(rootMemberOp, chainMembers);

            if (chainMembers.Count < 2)
                return;

            var wrapIndent = _currentIndentLevel + Options.ContinuationIndentSize;

            // Start from the second member (skip the first one - it's the base object access)
            for (int i = 1; i < chainMembers.Count; i++)
            {
                var memberOp = chainMembers[i];
                var point = memberOp.Point;

                if (point == null)
                    continue;

                var form = memberOp.Form;
                var prevToken = form.PreviousTokenBefore(point);

                // Check if already has backslash continuation
                if (prevToken is GDMultiLineSplitToken)
                    continue;

                // Insert backslash continuation before the point
                InsertBackslashContinuation(point, form, wrapIndent);
            }
        }

        #endregion

        #region Helper Methods

        private bool IsAlreadyWrapped(GDNode list)
        {
            if (list?.Form == null)
                return false;

            foreach (var token in list.Form)
            {
                if (token is GDNewLine)
                    return true;
            }

            return false;
        }

        private bool IsListElement(GDSyntaxToken token)
        {
            return token is GDExpression ||
                   token is GDParameterDeclaration ||
                   token is GDDictionaryKeyValueDeclaration;
        }

        private void InsertLineBreakBefore(GDSyntaxToken beforeToken, GDTokensForm form, int indentLevel)
        {
            // Create and insert newline
            var newLine = new GDNewLine();
            form.AddBeforeToken(newLine, beforeToken);

            // Create and insert indentation
            var indent = CreateIndentation(indentLevel);
            form.AddBeforeToken(indent, beforeToken);
        }

        private void InsertBackslashContinuation(GDSyntaxToken beforeToken, GDTokensForm form, int indentLevel)
        {
            // For backslash continuation, insert: space + backslash + newline + indent
            // Note: GDMultiLineSplitToken.Sequence is protected, so we use newline approach instead
            // This creates a line break that looks like a wrapped method chain

            // Add space before the line break
            var space = new GDSpace();
            space.Sequence = " ";
            form.AddBeforeToken(space, beforeToken);

            // Add newline
            var newLine = new GDNewLine();
            form.AddBeforeToken(newLine, beforeToken);

            // Create and insert indentation
            var indent = CreateIndentation(indentLevel);
            form.AddBeforeToken(indent, beforeToken);
        }

        private GDIntendation CreateIndentation(int level)
        {
            var indent = new GDIntendation();

            if (level <= 0)
            {
                indent.Sequence = string.Empty;
                indent.LineIntendationThreshold = 0;
            }
            else
            {
                string pattern = Options.IndentStyle == IndentStyle.Tabs
                    ? "\t"
                    : new string(' ', Options.IndentSize);

                var sb = new StringBuilder(pattern.Length * level);
                for (int i = 0; i < level; i++)
                    sb.Append(pattern);

                indent.Sequence = sb.ToString();
                indent.LineIntendationThreshold = level;
            }

            return indent;
        }

        private int GetIndentWidth()
        {
            return Options.IndentStyle == IndentStyle.Tabs ? 4 : Options.IndentSize;
        }

        private int EstimateCurrentLinePosition()
        {
            // Approximate based on current indent level
            return _currentIndentLevel * GetIndentWidth();
        }

        private int EstimateNodeLineLength(GDNode node)
        {
            if (node == null)
                return 0;

            return node.ToString().Length + EstimateCurrentLinePosition();
        }

        private int EstimateTokenLength(GDSyntaxToken token)
        {
            if (token == null)
                return 0;

            return token.ToString().Length;
        }

        private bool IsRootOfMethodChain(GDMemberOperatorExpression memberOp)
        {
            // It's a root if its parent is NOT a GDCallExpression whose parent is a GDMemberOperatorExpression
            // In other words, we want to find the outermost member operator in a chain

            // First check if this member operator is part of a method chain at all
            if (!IsPartOfMethodChain(memberOp))
                return false;

            // Check if parent is a call expression
            if (memberOp.Parent is GDCallExpression call)
            {
                // If the call's parent is another member operator, this is not the root
                if (call.Parent is GDMemberOperatorExpression)
                    return false;
            }

            return true;
        }

        private bool IsPartOfMethodChain(GDMemberOperatorExpression memberOp)
        {
            // A member operator is part of a method chain if:
            // 1. Its CallerExpression is a GDCallExpression, or
            // 2. It's the callee of a GDCallExpression that's a CallerExpression of another GDMemberOperatorExpression

            if (memberOp.CallerExpression is GDCallExpression)
                return true;

            if (memberOp.Parent is GDCallExpression parentCall)
            {
                if (parentCall.Parent is GDMemberOperatorExpression)
                    return true;
            }

            return false;
        }

        private void CollectMethodChainMembers(GDMemberOperatorExpression memberOp, List<GDMemberOperatorExpression> members)
        {
            // Traverse down the chain to collect all member operators
            if (memberOp.CallerExpression is GDCallExpression call)
            {
                if (call.CallerExpression is GDMemberOperatorExpression innerMember)
                {
                    CollectMethodChainMembers(innerMember, members);
                }
            }
            else if (memberOp.CallerExpression is GDMemberOperatorExpression innerMember)
            {
                CollectMethodChainMembers(innerMember, members);
            }

            members.Add(memberOp);
        }

        #endregion
    }
}
