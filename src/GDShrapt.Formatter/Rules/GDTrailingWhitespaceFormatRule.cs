using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Removes trailing whitespace from lines.
    /// Uses visitor pattern to process all nodes recursively.
    ///
    /// Note: EOF newline handling is done as string post-processing in GDFormatter.FormatCode
    /// because it's difficult to handle correctly at the AST level due to nested forms.
    /// </summary>
    public class GDTrailingWhitespaceFormatRule : GDFormatRule
    {
        public override string RuleId => "GDF004";
        public override string Name => "trailing-whitespace";
        public override string Description => "Remove trailing whitespace from lines";

        // Track spaces to remove - defer removal to avoid modifying collection during iteration
        private readonly List<(GDSpace space, GDTokensForm form)> _spacesToRemove = new List<(GDSpace, GDTokensForm)>();
        private GDClassDeclaration _rootClass;

        public override void Visit(GDClassDeclaration classDeclaration)
        {
            if (classDeclaration == null)
                return;

            _rootClass = classDeclaration;
            _spacesToRemove.Clear();

            ProcessNodeForm(classDeclaration);
        }

        // Process forms at all node types that might contain trailing whitespace
        public override void Visit(GDMethodDeclaration method) => ProcessNodeForm(method);
        public override void Visit(GDStatementsList statements) => ProcessNodeForm(statements);
        public override void Visit(GDIfStatement ifStatement) => ProcessNodeForm(ifStatement);
        public override void Visit(GDIfBranch ifBranch) => ProcessNodeForm(ifBranch);
        public override void Visit(GDElifBranch elifBranch) => ProcessNodeForm(elifBranch);
        public override void Visit(GDElseBranch elseBranch) => ProcessNodeForm(elseBranch);
        public override void Visit(GDForStatement forStatement) => ProcessNodeForm(forStatement);
        public override void Visit(GDWhileStatement whileStatement) => ProcessNodeForm(whileStatement);
        public override void Visit(GDMatchStatement matchStatement) => ProcessNodeForm(matchStatement);
        public override void Visit(GDMatchCaseDeclaration matchCase) => ProcessNodeForm(matchCase);
        public override void Visit(GDInnerClassDeclaration innerClass) => ProcessNodeForm(innerClass);
        public override void Visit(GDVariableDeclaration variable) => ProcessNodeForm(variable);
        public override void Visit(GDDictionaryInitializerExpression dict) => ProcessNodeForm(dict);
        public override void Visit(GDArrayInitializerExpression array) => ProcessNodeForm(array);

        private void ProcessNodeForm(GDNode node)
        {
            if (node?.Form == null || !Options.RemoveTrailingWhitespace)
                return;

            var form = node.Form;

            // Find trailing spaces (spaces followed by newline or at end)
            foreach (var token in form.Direct())
            {
                if (token is GDSpace space)
                {
                    var next = form.NextTokenAfter(space);
                    if (next is GDNewLine || next == null)
                    {
                        _spacesToRemove.Add((space, form));
                    }
                }
            }
        }

        public override void Left(GDClassDeclaration classDeclaration)
        {
            if (classDeclaration != _rootClass)
                return;

            // Remove all collected trailing spaces
            foreach (var (space, form) in _spacesToRemove)
            {
                form.Remove(space);
            }
            _spacesToRemove.Clear();
        }
    }
}
