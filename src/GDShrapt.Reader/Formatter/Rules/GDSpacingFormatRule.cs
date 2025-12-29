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
            // NOTE: Parentheses spacing is complex due to nested nodes.
            // The content between ( and ) is a nested GDExpressionsList.
            // Skipping for now - would need to modify the list form instead.
            // HandleParenthesesSpacing(callExpression.OpenBracket, callExpression.CloseBracket, callExpression);
        }

        public override void Visit(GDArrayInitializerExpression arrayExpression)
        {
            // NOTE: Similar issue - content is nested GDExpressionsList
            // HandleBracketsSpacing(arrayExpression.SquareOpenBracket, arrayExpression.SquareCloseBracket, arrayExpression);
        }

        public override void Visit(GDDictionaryInitializerExpression dictExpression)
        {
            // NOTE: Similar issue - content is nested GDDictionaryKeyValueDeclarationList
            // HandleBracesSpacing(dictExpression.FigureOpenBracket, dictExpression.FigureCloseBracket, dictExpression);
        }

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            // NOTE: Parentheses spacing skipped - nested ParametersList
            // HandleParenthesesSpacing(methodDeclaration.OpenBracket, methodDeclaration.CloseBracket, methodDeclaration);
            HandleColonSpacing(methodDeclaration.Colon, methodDeclaration);
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

        private void HandleParenthesesSpacing(GDOpenBracket open, GDCloseBracket close, GDNode parent)
        {
            if (parent == null)
                return;

            if (Options.SpaceInsideParentheses)
            {
                if (open != null)
                    EnsureSpaceAfter(open, parent);
                if (close != null)
                    EnsureSpaceBefore(close, parent);
            }
            else
            {
                if (open != null)
                    RemoveSpaceAfter(open, parent);
                if (close != null)
                    RemoveSpaceBefore(close, parent);
            }
        }

        private void HandleBracketsSpacing(GDSquareOpenBracket open, GDSquareCloseBracket close, GDNode parent)
        {
            if (parent == null)
                return;

            if (Options.SpaceInsideBrackets)
            {
                if (open != null)
                    EnsureSpaceAfter(open, parent);
                if (close != null)
                    EnsureSpaceBefore(close, parent);
            }
            else
            {
                if (open != null)
                    RemoveSpaceAfter(open, parent);
                if (close != null)
                    RemoveSpaceBefore(close, parent);
            }
        }

        private void HandleBracesSpacing(GDFigureOpenBracket open, GDFigureCloseBracket close, GDNode parent)
        {
            if (parent == null)
                return;

            if (Options.SpaceInsideBraces)
            {
                if (open != null)
                    EnsureSpaceAfter(open, parent);
                if (close != null)
                    EnsureSpaceBefore(close, parent);
            }
            else
            {
                if (open != null)
                    RemoveSpaceAfter(open, parent);
                if (close != null)
                    RemoveSpaceBefore(close, parent);
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
    }
}
