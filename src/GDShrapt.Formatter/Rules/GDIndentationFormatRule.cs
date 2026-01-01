namespace GDShrapt.Reader
{
    /// <summary>
    /// Formats indentation according to the configured style (tabs or spaces).
    /// This rule converts indentation patterns without changing the indentation level.
    /// </summary>
    public class GDIndentationFormatRule : GDFormatRule
    {
        public override string RuleId => "GDF001";
        public override string Name => "indentation";
        public override string Description => "Format indentation using tabs or spaces";

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            ConvertIndentationInNode(classDeclaration);
        }

        public override void Visit(GDMethodDeclaration methodDeclaration)
        {
            ConvertIndentationInNode(methodDeclaration);
        }

        public override void Visit(GDStatementsList statementsList)
        {
            ConvertIndentationInNode(statementsList);
        }

        public override void Visit(GDIfStatement ifStatement)
        {
            ConvertIndentationInNode(ifStatement);
        }

        public override void Visit(GDForStatement forStatement)
        {
            ConvertIndentationInNode(forStatement);
        }

        public override void Visit(GDWhileStatement whileStatement)
        {
            ConvertIndentationInNode(whileStatement);
        }

        public override void Visit(GDMatchStatement matchStatement)
        {
            ConvertIndentationInNode(matchStatement);
        }

        public override void Visit(GDMatchCaseDeclaration matchCase)
        {
            ConvertIndentationInNode(matchCase);
        }

        public override void Visit(GDInnerClassDeclaration innerClass)
        {
            ConvertIndentationInNode(innerClass);
        }

        private void ConvertIndentationInNode(GDNode node)
        {
            if (node?.Form == null)
                return;

            // Convert all indentation tokens in this node's direct form
            foreach (var token in node.Form)
            {
                if (token is GDIntendation indentation)
                {
                    // Use ConvertPattern to change tabs<->spaces without recalculating level
                    indentation.ConvertPattern(Options.IndentPattern, Options.IndentSize);
                }
            }
        }
    }
}
