using Godot;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GD = GDShrapt.Reader.GD;

namespace GDShrapt.Plugin;

internal class ExtractMethodCommand : Command
{
    private NewMethodNameDialog _newNameDialog;

    public ExtractMethodCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
    }

    private async Task<string> AskNewMethodName()
    {
        if (_newNameDialog == null)
        {
            Editor.AddChild(_newNameDialog = new NewMethodNameDialog());
        }

        _newNameDialog.Position = new Vector2I((int)(Editor.Position.X + Editor.Size.X / 2), (int)(Editor.Position.Y + Editor.Size.Y / 2));

        return await _newNameDialog.ShowForResult();
    }

    public override async Task Execute(IScriptEditor controller)
    {
        Logger.Info("Extract method requested");

        if (!controller.IsValid)
        {
            Logger.Info($"Extract method cancelled: Editor is not valid");
            return;
        }

        if (!controller.HasSelection)
        {
            Logger.Info($"Extract method cancelled: No code selected");
            return;
        }

        var startLine = controller.SelectionStartLine;
        var startColumn = controller.SelectionStartColumn;
        var endLine = controller.SelectionEndLine;
        var endColumn = controller.SelectionEndColumn;

        Logger.Info($"For selection {{{startLine}, {startColumn}, {endLine}, {endColumn}}}");

        if (startLine == endLine && startColumn == endColumn)
        {
            Logger.Info($"Extract method cancelled: The selection is empty");
            return;
        }

        var @class = controller.GetClass();

        List<GDSyntaxToken> tokensToExtract = null;
        GDStatementsList statementsListUnderRefactoring = null;

        foreach (var statementsList in @class.AllNodesReversed.OfType<GDStatementsList>())
        {
            var tokensToExtractFromStatementsList = new List<GDSyntaxToken>();

            foreach (var token in statementsList.Tokens
                .SkipWhile(x => x is GDIntendation)
                .SkipWhile(x => x is GDSpace)
                .SkipWhile(x => x is GDNewLine))
            {
                var line = token.StartLine;

                Logger.Info($"Token '{token.TypeName}' line {line}");

                if (line > endLine || (line == endLine && token is GDNewLine))
                    break;

                if (startLine <= line)
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
                if (tokensToExtractFromStatementsList.Count > 0 && statementsListUnderRefactoring.StartLine >= startLine)
                {
                    tokensToExtract = tokensToExtractFromStatementsList;
                    statementsListUnderRefactoring = statementsList;
                }
            }

        }

        if (statementsListUnderRefactoring == null)
        {
            Logger.Info("No found statements");
            return;
        }

        var availableIdentifiers = statementsListUnderRefactoring.ExtractAllMethodScopeVisibleDeclarationsFromParents(startLine, out GDIdentifiableClassMember owningMember);

        if (owningMember == null)
        {
            Logger.Info("Class member not found");
            return;
        }

        var newMethodName = await AskNewMethodName();

        if (newMethodName == null || newMethodName.Length == 0)
        {
            Logger.Info("Extract method cancelled: new method name is null or empty");
            return;
        }

        newMethodName = InternalMethods.PrepareIdentifier(newMethodName, "new_method");

        var usedIdentifiers = new HashSet<GDIdentifier>();

        if (availableIdentifiers.Count > 0)
        {
            for (int i = 0; i < tokensToExtract.Count; i++)
            {
                var token = tokensToExtract[i];

                if (token is GDNode node)
                {
                    Logger.Info($"Extract token dependencies '{token.TypeName}'");

                    foreach (var dependency in node.GetDependencies())
                    {
                        if (availableIdentifiers.Contains(dependency))
                        {
                            if (usedIdentifiers.Add(dependency))
                                Logger.Info($"Registered new dependency '{dependency.Sequence}'");

                        }
                    }
                }
            }
        }

        // Save tokens region
        var cutStartLine = tokensToExtract.First().StartLine;
        var cutStartColumn = tokensToExtract.First().StartColumn;
        var cutEndLine = tokensToExtract.Last().EndLine;
        var cutEndColumn = tokensToExtract.Last().EndColumn;

        Logger.Info("Started method generation");

        var methodIdentifierExpression = GD.Expression.Identifier(newMethodName);
        var callStatement = GD.Statement.Expression(GD.Expression.Call(methodIdentifierExpression, usedIdentifiers.Select(x => GD.Expression.Identifier((GDIdentifier)x.Clone())).ToArray()));
        var intendation = new GDIntendation();

        statementsListUnderRefactoring.Form.AddBeforeToken(intendation, tokensToExtract[0]);
        statementsListUnderRefactoring.Form.AddBeforeToken(callStatement, tokensToExtract[0]);

        intendation.Update();

        Logger.Info($"Method call intendation {intendation.LineIntendationThreshold}");

        GDMethodDeclaration newMethod;

        // Check for static context
        if (statementsListUnderRefactoring.ClassMember is GDMethodDeclaration method && method.IsStatic)
            newMethod = GD.Declaration.StaticMethod(GD.Syntax.Identifier(newMethodName), GD.List.Parameters(usedIdentifiers.Select(x => GD.Declaration.Parameter((GDIdentifier)x.Clone())).ToArray()));
        else
            newMethod = GD.Declaration.Method(GD.Syntax.Identifier(newMethodName), GD.List.Parameters(usedIdentifiers.Select(x => GD.Declaration.Parameter((GDIdentifier)x.Clone())).ToArray()));

        var form = newMethod.Statements.ListForm;

        // Remove tokens and add them to the new method
        for (int i = 0; i < tokensToExtract.Count; i++)
        {
            var token = tokensToExtract[i];
            token.RemoveFromParent();
            form.AddToEnd(token);
        }

        controller.Select(cutStartLine, cutStartColumn, cutEndLine, cutEndColumn);
        controller.Cut();

        // Update cursor for method call insertion
        controller.CursorLine = intendation.StartLine;
        controller.CursorColumn = intendation.StartColumn;

        // Insert method call
        controller.InsertTextAtCursor($"{intendation}{callStatement}");

        var container = new GDTokensContainer(new GDNewLine(), new GDNewLine(), newMethod);

        @class.Members.Form.AddAfterToken(container, owningMember);

        newMethod.UpdateIntendation();

        Logger.Info("Method generated");
        Logger.Info($"'{newMethod}'");

        // Format the generated method using FormattingService
        var formattedMethod = Plugin.FormattingService?.FormatMethod(newMethod) ?? newMethod.ToString();

        // Update cursor for insertion
        controller.CursorLine = container.StartLine;
        controller.CursorColumn = container.StartColumn;

        // Insert with proper newlines before the method
        controller.InsertTextAtCursor($"\n\n{formattedMethod}");

        var callLine = methodIdentifierExpression.StartLine;
        var callColumn = methodIdentifierExpression.StartColumn;
        var callEndColumn = callColumn + methodIdentifierExpression.Length;

        Logger.Info($"Set selection {{{callLine}, {callColumn}, {callLine}, {callEndColumn}}}");

        controller.Select(callLine, callColumn, callLine, callEndColumn);

        controller.CursorLine = callLine;
        controller.CursorColumn = callEndColumn;

        Logger.Info("Method extraction has been completed");
    }
}
