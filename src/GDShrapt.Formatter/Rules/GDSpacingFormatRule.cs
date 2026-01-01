namespace GDShrapt.Reader
{
    /// <summary>
    /// Formats spacing around operators, commas, colons, and brackets.
    /// </summary>
    public class GDSpacingFormatRule : GDFormatRule
    {
        public override string RuleId => "GDF003";
        public override string Name => "spacing";
        public override string Description => "Format spacing around operators, commas, colons, and brackets";

        public override void Visit(GDDualOperatorExpression expression)
        {
            if (expression == null || expression.Operator == null)
                return;

            // Handle spacing around binary operators
            if (Options.SpaceAroundOperators)
            {
                EnsureSpaceBefore(expression.Operator, expression);
                EnsureSpaceAfter(expression.Operator, expression);
            }
            else
            {
                RemoveSpaceBefore(expression.Operator, expression);
                RemoveSpaceAfter(expression.Operator, expression);
            }
        }

        public override void Visit(GDVariableDeclaration variableDeclaration)
        {
            HandleColonSpacing(variableDeclaration.Colon, variableDeclaration);
            HandleAssignSpacing(variableDeclaration.Assign, variableDeclaration);
        }

        public override void Visit(GDVariableDeclarationStatement variableStatement)
        {
            HandleColonSpacing(variableStatement.Colon, variableStatement);
            HandleAssignSpacing(variableStatement.Assign, variableStatement);
        }

        public override void Visit(GDParameterDeclaration parameter)
        {
            HandleColonSpacing(parameter.Colon, parameter);
            HandleAssignSpacing(parameter.Assign, parameter);
        }

        public override void Visit(GDDictionaryKeyValueDeclaration keyValue)
        {
            HandleColonSpacing(keyValue.Colon, keyValue);
        }

        public override void Visit(GDCallExpression callExpression)
        {
            // Handle spacing inside parentheses by modifying the nested Parameters list
            HandleListBoundarySpacing(callExpression.Parameters, Options.SpaceInsideParentheses);
        }

        public override void Visit(GDArrayInitializerExpression arrayExpression)
        {
            // Handle spacing inside brackets by modifying the nested Values list
            HandleListBoundarySpacing(arrayExpression.Values, Options.SpaceInsideBrackets);
        }

        public override void Visit(GDDictionaryInitializerExpression dictExpression)
        {
            // Handle spacing inside braces by modifying the nested KeyValues list
            HandleListBoundarySpacing(dictExpression.KeyValues, Options.SpaceInsideBraces);
        }

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            // Handle spacing inside parentheses by modifying the nested Parameters list
            HandleListBoundarySpacing(methodDeclaration.Parameters, Options.SpaceInsideParentheses);
            // NOTE: Do NOT apply HandleColonSpacing to method colon!
            // In GDScript, the method colon (func foo():) should NOT have space after it.
            // SpaceAfterColon option is for type annotations (var x: int), not method declaration endings.
        }

        private void HandleColonSpacing(GDColon colon, GDNode parent)
        {
            if (colon == null || parent == null)
                return;

            var form = parent.Form;

            // Check if this colon is part of := (infer assignment)
            // In that case, we should not add space after the colon
            var nextToken = form.NextTokenAfter(colon);
            bool isInferAssign = nextToken is GDAssign;

            if (isInferAssign)
            {
                // For :=, ensure no space between : and =
                // Space before is controlled by SpaceAroundOperators, not SpaceBeforeColon
                // since := is an assignment operator
                if (Options.SpaceAroundOperators)
                    EnsureSpaceBefore(colon, parent);
                else
                    RemoveSpaceBefore(colon, parent);

                // Never space between : and = in :=
                // (RemoveSpaceAfter won't do anything since next is GDAssign, not GDSpace)
                return;
            }

            // Regular type annotation colon (var x: int)
            // Space before colon (var x : int)
            if (Options.SpaceBeforeColon)
                EnsureSpaceBefore(colon, parent);
            else
                RemoveSpaceBefore(colon, parent);

            // Space after colon (var x: int)
            if (Options.SpaceAfterColon)
                EnsureSpaceAfter(colon, parent);
            else
                RemoveSpaceAfter(colon, parent);
        }

        private void HandleAssignSpacing(GDAssign assign, GDNode parent)
        {
            if (assign == null || parent == null)
                return;

            var form = parent.Form;

            // Check if this assign is part of := (infer assignment)
            // In that case, skip space before (already handled by HandleColonSpacing)
            var prevToken = form.PreviousTokenBefore(assign);
            bool isInferAssign = prevToken is GDColon;

            if (Options.SpaceAroundOperators)
            {
                // For :=, don't add space before = (it's already handled)
                if (!isInferAssign)
                    EnsureSpaceBefore(assign, parent);
                EnsureSpaceAfter(assign, parent);
            }
            else
            {
                if (!isInferAssign)
                    RemoveSpaceBefore(assign, parent);
                RemoveSpaceAfter(assign, parent);
            }
        }

        public override void Visit(GDParametersList parametersList)
        {
            HandleCommasInList(parametersList);
        }

        public override void Visit(GDExpressionsList expressionsList)
        {
            HandleCommasInList(expressionsList);
        }

        private void HandleCommasInList(GDNode listNode)
        {
            if (listNode?.Form == null)
                return;

            foreach (var token in listNode.Form)
            {
                if (token is GDComma comma)
                {
                    // Never space before comma
                    RemoveSpaceBefore(comma, listNode);

                    // Space after comma
                    if (Options.SpaceAfterComma)
                        EnsureSpaceAfter(comma, listNode);
                    else
                        RemoveSpaceAfter(comma, listNode);
                }
            }
        }

        /// <summary>
        /// Handles spacing at the boundaries of a list (after open bracket, before close bracket).
        /// Works by modifying the list's form directly.
        /// </summary>
        private void HandleListBoundarySpacing(GDNode listNode, bool addSpacing)
        {
            if (listNode?.Form == null)
                return;

            // Check if list has any non-whitespace content
            if (!HasNonWhitespaceTokens(listNode))
                return; // Empty list like () or [] - don't add spacing

            var form = listNode.Form;

            if (addSpacing)
            {
                // Get first token
                GDSyntaxToken firstToken = null;
                foreach (var token in form)
                {
                    firstToken = token;
                    break;
                }

                // Add space at the beginning if not already present
                // Don't add if there's already whitespace (space, newline, or indentation)
                // Also check if the first token's string representation starts with space
                // (the space might be absorbed into the nested node by parser)
                if (firstToken != null && !IsWhitespaceToken(firstToken))
                {
                    var firstStr = firstToken.ToString();
                    if (!string.IsNullOrEmpty(firstStr) && !char.IsWhiteSpace(firstStr[0]))
                        form.AddBeforeToken(new GDSpace() { Sequence = " " }, 0);
                }

                // Get last token (need to re-enumerate in case we just added one)
                GDSyntaxToken lastToken = null;
                foreach (var token in form)
                    lastToken = token;

                // Add space at the end if not already present
                // Don't add if there's already whitespace
                // Also check if the last token's string representation ends with space
                // (the space might be absorbed into the nested node by parser)
                if (lastToken != null && !IsWhitespaceToken(lastToken))
                {
                    var lastStr = lastToken.ToString();
                    if (!string.IsNullOrEmpty(lastStr) && !char.IsWhiteSpace(lastStr[lastStr.Length - 1]))
                        form.AddToEnd(new GDSpace() { Sequence = " " });
                }
            }
            else
            {
                // Remove all leading spaces from the list (but not newlines/indentation)
                // Exception: don't remove if preceded by newline (it's indentation for wrapped lines)
                bool removedAny;
                do
                {
                    removedAny = false;
                    GDSyntaxToken firstToken = null;
                    foreach (var token in form)
                    {
                        firstToken = token;
                        break;
                    }

                    if (firstToken is GDSpace)
                    {
                        form.Remove(firstToken);
                        removedAny = true;
                    }
                } while (removedAny);

                // Remove all trailing spaces from the list (but not if it's indentation after newline)
                do
                {
                    removedAny = false;
                    GDSyntaxToken lastToken = null;
                    GDSyntaxToken prevToken = null;
                    foreach (var token in form)
                    {
                        prevToken = lastToken;
                        lastToken = token;
                    }

                    if (lastToken is GDSpace space)
                    {
                        // Don't remove if this is indentation after a newline (part of wrapped structure)
                        if (prevToken is GDNewLine)
                        {
                            // This is indentation for a wrapped closing bracket, keep it
                            break;
                        }

                        form.Remove(space);
                        removedAny = true;
                    }
                } while (removedAny);
            }
        }

        private bool IsWhitespaceToken(GDSyntaxToken token)
        {
            return token is GDSpace || token is GDNewLine || token is GDIntendation;
        }

        /// <summary>
        /// Checks if a node's form contains any non-whitespace tokens.
        /// </summary>
        private bool HasNonWhitespaceTokens(GDNode listNode)
        {
            if (listNode?.Form == null)
                return false;

            foreach (var token in listNode.Form)
            {
                if (!(token is GDSpace) && !(token is GDNewLine))
                    return true;
            }
            return false;
        }
    }
}
