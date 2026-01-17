using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for match case variable binding in Find References.
/// </summary>
[TestClass]
public class MatchCaseBindingTests
{
    private GDScriptReader _reader = null!;
    private GDFindReferencesService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _reader = new GDScriptReader();
        _service = new GDFindReferencesService();
    }

    private (GDScriptFile script, GDClassDeclaration classDecl) CreateScript(string code)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        return (script, classDecl);
    }

    private GDRefactoringContext CreateContext(string code, int line, int column)
    {
        var (script, classDecl) = CreateScript(code);
        var cursor = new GDCursorPosition(line, column);
        return new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);
    }

    #region Symbol Scope Detection

    [TestMethod]
    public void DetermineSymbolScope_MatchCaseVariableDeclaration_ReturnsMatchCaseVariable()
    {
        var code = @"func test(value):
    match value:
        var x:
            print(x)
";
        var (script, classDecl) = CreateScript(code);

        // Find the 'x' identifier in 'var x'
        var matchCaseVar = classDecl.AllNodes.OfType<GDMatchCaseVariableExpression>().First();
        var identifier = matchCaseVar.Identifier;
        identifier.Should().NotBeNull();
        identifier!.Sequence.Should().Be("x");

        var context = new GDRefactoringContext(
            script,
            classDecl,
            new GDCursorPosition(identifier.StartLine, identifier.StartColumn),
            GDSelectionInfo.None);

        var scope = _service.DetermineSymbolScope(identifier, context);

        scope.Should().NotBeNull();
        scope!.Type.Should().Be(GDSymbolScopeType.MatchCaseVariable);
        scope.SymbolName.Should().Be("x");
        scope.ContainingMatchCase.Should().NotBeNull();
    }

    [TestMethod]
    public void DetermineSymbolScope_MatchCaseVariableUsage_ReturnsMatchCaseVariable()
    {
        var code = @"func test(value):
    match value:
        var x:
            print(x)
";
        var (script, classDecl) = CreateScript(code);

        // Find the 'x' identifier in print(x)
        var identExpr = classDecl.AllNodes.OfType<GDIdentifierExpression>()
            .First(e => e.Identifier?.Sequence == "x");
        var identifier = identExpr.Identifier;
        identifier.Should().NotBeNull();

        var context = new GDRefactoringContext(
            script,
            classDecl,
            new GDCursorPosition(identifier!.StartLine, identifier.StartColumn),
            GDSelectionInfo.None);

        var scope = _service.DetermineSymbolScope(identifier, context);

        scope.Should().NotBeNull();
        scope!.Type.Should().Be(GDSymbolScopeType.MatchCaseVariable);
        scope.SymbolName.Should().Be("x");
        scope.ContainingMatchCase.Should().NotBeNull();
    }

    #endregion

    #region Find References

    [TestMethod]
    public void CollectMatchCaseReferences_SimpleMatchCase_FindsDeclarationAndUsage()
    {
        var code = @"func test(value):
    match value:
        var x:
            print(x)
            return x
";
        var (script, classDecl) = CreateScript(code);

        // Get the match case variable
        var matchCaseVar = classDecl.AllNodes.OfType<GDMatchCaseVariableExpression>().First();
        var matchCase = classDecl.AllNodes.OfType<GDMatchCaseDeclaration>().First();
        var identifier = matchCaseVar.Identifier!;

        var context = new GDRefactoringContext(
            script,
            classDecl,
            new GDCursorPosition(identifier.StartLine, identifier.StartColumn),
            GDSelectionInfo.None);

        var scope = new GDSymbolScope(
            GDSymbolScopeType.MatchCaseVariable,
            "x",
            declarationNode: matchCaseVar,
            containingMatchCase: matchCase,
            containingClass: classDecl,
            containingScript: script);

        var result = _service.FindReferencesForScope(context, scope);

        result.Success.Should().BeTrue();
        // Should find: declaration (var x), usage in print(x), usage in return x
        result.StrictReferences.Should().HaveCountGreaterThanOrEqualTo(2);
        result.StrictReferences.Should().Contain(r => r.Kind == GDReferenceKind.Declaration);
    }

    [TestMethod]
    public void CollectMatchCaseReferences_WithGuardCondition_FindsVariableInGuard()
    {
        var code = @"func test(value):
    match value:
        var x when x < 0:
            return x
";
        var (script, classDecl) = CreateScript(code);

        var matchCaseVar = classDecl.AllNodes.OfType<GDMatchCaseVariableExpression>().First();
        var matchCase = classDecl.AllNodes.OfType<GDMatchCaseDeclaration>().First();
        var identifier = matchCaseVar.Identifier!;

        var context = new GDRefactoringContext(
            script,
            classDecl,
            new GDCursorPosition(identifier.StartLine, identifier.StartColumn),
            GDSelectionInfo.None);

        var scope = new GDSymbolScope(
            GDSymbolScopeType.MatchCaseVariable,
            "x",
            declarationNode: matchCaseVar,
            containingMatchCase: matchCase,
            containingClass: classDecl,
            containingScript: script);

        var result = _service.FindReferencesForScope(context, scope);

        result.Success.Should().BeTrue();
        // Should find: declaration, guard condition 'x < 0', return x
        result.StrictReferences.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [TestMethod]
    public void CollectMatchCaseReferences_ArrayPattern_FindsMultipleBindings()
    {
        var code = @"func test(arr):
    match arr:
        [var first, var second]:
            return first + second
";
        var (script, classDecl) = CreateScript(code);

        // Find the 'first' variable
        var matchCaseVars = classDecl.AllNodes.OfType<GDMatchCaseVariableExpression>().ToList();
        matchCaseVars.Should().HaveCount(2);

        var firstVar = matchCaseVars.First(v => v.Identifier?.Sequence == "first");
        var matchCase = classDecl.AllNodes.OfType<GDMatchCaseDeclaration>().First();

        var context = new GDRefactoringContext(
            script,
            classDecl,
            new GDCursorPosition(firstVar.Identifier!.StartLine, firstVar.Identifier.StartColumn),
            GDSelectionInfo.None);

        var scope = new GDSymbolScope(
            GDSymbolScopeType.MatchCaseVariable,
            "first",
            declarationNode: firstVar,
            containingMatchCase: matchCase,
            containingClass: classDecl,
            containingScript: script);

        var result = _service.FindReferencesForScope(context, scope);

        result.Success.Should().BeTrue();
        // Should find: declaration and usage in 'first + second'
        result.StrictReferences.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Scope Isolation

    [TestMethod]
    public void MatchCaseVariable_DoesNotLeakToOtherCases()
    {
        var code = @"func test(value):
    match value:
        var x:
            print(x)
        var y:
            print(y)
";
        var (script, classDecl) = CreateScript(code);

        var matchCaseVars = classDecl.AllNodes.OfType<GDMatchCaseVariableExpression>().ToList();
        matchCaseVars.Should().HaveCount(2);

        // Find the 'x' variable
        var xVar = matchCaseVars.First(v => v.Identifier?.Sequence == "x");
        var matchCases = classDecl.AllNodes.OfType<GDMatchCaseDeclaration>().ToList();
        var xMatchCase = matchCases.First();

        var context = new GDRefactoringContext(
            script,
            classDecl,
            new GDCursorPosition(xVar.Identifier!.StartLine, xVar.Identifier.StartColumn),
            GDSelectionInfo.None);

        var scope = new GDSymbolScope(
            GDSymbolScopeType.MatchCaseVariable,
            "x",
            declarationNode: xVar,
            containingMatchCase: xMatchCase,
            containingClass: classDecl,
            containingScript: script);

        var result = _service.FindReferencesForScope(context, scope);

        result.Success.Should().BeTrue();
        // Should only find references in the first case (var x), not in second case (print(y))
        result.StrictReferences.Should().HaveCount(2); // declaration + usage in print(x)
        result.StrictReferences.All(r => r.SymbolName == "x").Should().BeTrue();
    }

    [TestMethod]
    public void MatchCaseVariable_DoesNotLeakOutsideMatch()
    {
        var code = @"func test(value):
    match value:
        var x:
            print(x)
    var x = 10
    print(x)
";
        var (script, classDecl) = CreateScript(code);

        // Find the match case 'x' variable
        var matchCaseVar = classDecl.AllNodes.OfType<GDMatchCaseVariableExpression>().First();
        var matchCase = classDecl.AllNodes.OfType<GDMatchCaseDeclaration>().First();

        var context = new GDRefactoringContext(
            script,
            classDecl,
            new GDCursorPosition(matchCaseVar.Identifier!.StartLine, matchCaseVar.Identifier.StartColumn),
            GDSelectionInfo.None);

        var scope = new GDSymbolScope(
            GDSymbolScopeType.MatchCaseVariable,
            "x",
            declarationNode: matchCaseVar,
            containingMatchCase: matchCase,
            containingClass: classDecl,
            containingScript: script);

        var result = _service.FindReferencesForScope(context, scope);

        result.Success.Should().BeTrue();
        // Should only find: declaration (var x in match) + usage (print(x) in match case body)
        // Should NOT find: var x = 10, print(x) outside match
        result.StrictReferences.Should().HaveCount(2);
    }

    #endregion

    #region GDSymbolKind

    [TestMethod]
    public void GDSymbolKind_HasMatchCaseBinding()
    {
        // Verify the new enum value exists
        GDSymbolKind.MatchCaseBinding.Should().Be(GDSymbolKind.MatchCaseBinding);
    }

    #endregion
}
