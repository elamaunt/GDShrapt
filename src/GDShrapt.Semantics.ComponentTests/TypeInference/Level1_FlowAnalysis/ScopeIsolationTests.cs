using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Tests for scope isolation - ensuring variables with the same name
/// in different methods/lambdas are treated as separate symbols.
/// </summary>
[TestClass]
public class ScopeIsolationTests
{
    #region Same Name Different Methods

    [TestMethod]
    public void FindSymbolInScope_SameNameDifferentMethods_ReturnsCorrectSymbol()
    {
        // Arrange
        var code = @"
extends Node

func method_a():
    var counter = 0
    print(counter)

func method_b():
    var counter = 10
    print(counter)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // First verify that 2 symbols named 'counter' are registered
        var allCounterSymbols = semanticModel.FindSymbols("counter").ToList();
        Assert.AreEqual(2, allCounterSymbols.Count,
            "Should have 2 separate symbols named 'counter' (one per method)");

        // Verify they have different scopes
        var scopes = allCounterSymbols.Select(s => s.DeclaringScopeNode).Distinct().ToList();
        Assert.AreEqual(2, scopes.Count,
            "Each counter should be in a different scope");

        // Now test scope-aware lookup
        // Find both methods
        var methodA = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "method_a");
        var methodB = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "method_b");

        Assert.IsNotNull(methodA, "method_a should exist");
        Assert.IsNotNull(methodB, "method_b should exist");

        // Find counter usage in each method
        var counterInA = FindIdentifierExpressionInMethod(methodA, "counter");
        var counterInB = FindIdentifierExpressionInMethod(methodB, "counter");

        Assert.IsNotNull(counterInA, "counter should be found in method_a");
        Assert.IsNotNull(counterInB, "counter should be found in method_b");

        // Get symbols - should be different
        var symbolA = semanticModel.FindSymbolInScope("counter", counterInA);
        var symbolB = semanticModel.FindSymbolInScope("counter", counterInB);

        Assert.IsNotNull(symbolA, "Symbol for counter in method_a should exist");
        Assert.IsNotNull(symbolB, "Symbol for counter in method_b should exist");
        Assert.AreNotSame(symbolA, symbolB,
            "Symbols from different methods should be different objects");
    }

    [TestMethod]
    public void FindSymbolInScope_SameNameInMethodAndClassLevel_PrefersLocalInMethod()
    {
        // Arrange
        var code = @"
extends Node

var counter = 100

func method_a():
    var counter = 0
    print(counter)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Find the method
        var methodA = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "method_a");
        Assert.IsNotNull(methodA);

        // Find counter usage in method
        var counterInMethod = FindIdentifierExpressionInMethod(methodA, "counter");
        Assert.IsNotNull(counterInMethod);

        // Get symbol - should be local, not class-level
        var symbol = semanticModel.FindSymbolInScope("counter", counterInMethod);
        Assert.IsNotNull(symbol);

        // The local variable is declared in method_a, so DeclaringScopeNode should be method_a
        Assert.IsNotNull(symbol.DeclaringScopeNode,
            "Local variable should have a declaring scope");
        Assert.AreEqual(methodA, symbol.DeclaringScopeNode,
            "Symbol should be the local variable from the method, not class-level");
    }

    [TestMethod]
    public void FindSymbolInScope_NoLocalVariable_ReturnsClassLevel()
    {
        // Arrange
        var code = @"
extends Node

var counter = 100

func method_a():
    print(counter)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Find the method
        var methodA = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "method_a");
        Assert.IsNotNull(methodA);

        // Find counter usage in method
        var counterInMethod = FindIdentifierExpressionInMethod(methodA, "counter");
        Assert.IsNotNull(counterInMethod);

        // Get symbol - should be class-level since no local exists
        var symbol = semanticModel.FindSymbolInScope("counter", counterInMethod);
        Assert.IsNotNull(symbol);

        // Class-level variables have null DeclaringScopeNode
        Assert.IsNull(symbol.DeclaringScopeNode,
            "Class-level variable should have null DeclaringScopeNode");
    }

    #endregion

    #region Lambda Scope Isolation

    [TestMethod]
    public void FindSymbolInScope_SameNameInMethodAndLambda_ReturnsCorrectSymbol()
    {
        // Arrange
        var code = @"
extends Node

func process():
    var x = 10
    var callback = func():
        var x = 20
        return x
    print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Find all symbols named 'x'
        var xSymbols = semanticModel.FindSymbols("x").ToList();

        // Should have 2 symbols with name 'x'
        Assert.AreEqual(2, xSymbols.Count,
            "Should have 2 separate symbols named 'x' (one in method, one in lambda)");

        // Each should have a different declaring scope
        var scopes = xSymbols.Select(s => s.DeclaringScopeNode).Distinct().ToList();
        Assert.AreEqual(2, scopes.Count,
            "Each 'x' should be in a different scope (method vs lambda)");
    }

    #endregion

    #region For Loop Iterator Isolation

    [TestMethod]
    public void FindSymbolInScope_SameIteratorName_DifferentMethods()
    {
        // Arrange
        var code = @"
extends Node

func method_a():
    for i in range(10):
        print(i)

func method_b():
    for i in range(20):
        print(i)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Find all symbols named 'i'
        var iSymbols = semanticModel.FindSymbols("i").ToList();

        // Should have 2 symbols with name 'i'
        Assert.AreEqual(2, iSymbols.Count,
            "Should have 2 separate symbols named 'i' (one per method)");

        // Each should have a different declaring scope
        var scopes = iSymbols.Select(s => s.DeclaringScopeNode).Distinct().ToList();
        Assert.AreEqual(2, scopes.Count,
            "Each iterator 'i' should be in a different scope");
    }

    #endregion

    #region Parameter Isolation

    [TestMethod]
    public void FindSymbolInScope_SameParameterName_DifferentMethods()
    {
        // Arrange
        var code = @"
extends Node

func method_a(value):
    print(value)

func method_b(value):
    print(value)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Find all symbols named 'value'
        var valueSymbols = semanticModel.FindSymbols("value").ToList();

        // Should have 2 symbols with name 'value'
        Assert.AreEqual(2, valueSymbols.Count,
            "Should have 2 separate symbols named 'value' (one per method)");

        // Each should have a different declaring scope
        var scopes = valueSymbols.Select(s => s.DeclaringScopeNode).Distinct().ToList();
        Assert.AreEqual(2, scopes.Count,
            "Each parameter 'value' should be in a different scope");
    }

    #endregion

    #region GetSymbolForNode Uses Scope

    [TestMethod]
    public void GetSymbolForNode_ReturnsCorrectSymbolForScope()
    {
        // Arrange
        var code = @"
extends Node

func method_a():
    var counter = 0
    print(counter)

func method_b():
    var counter = 10
    print(counter)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Find both methods
        var methodA = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "method_a");
        var methodB = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "method_b");

        Assert.IsNotNull(methodA);
        Assert.IsNotNull(methodB);

        // Find counter usage in each method
        var counterExprA = FindIdentifierExpressionInMethod(methodA, "counter");
        var counterExprB = FindIdentifierExpressionInMethod(methodB, "counter");

        Assert.IsNotNull(counterExprA);
        Assert.IsNotNull(counterExprB);

        // GetSymbolForNode should return different symbols for each
        var symbolA = semanticModel.GetSymbolForNode(counterExprA);
        var symbolB = semanticModel.GetSymbolForNode(counterExprB);

        Assert.IsNotNull(symbolA);
        Assert.IsNotNull(symbolB);
        Assert.AreNotSame(symbolA, symbolB,
            "GetSymbolForNode should return different symbols for different scopes");
    }

    #endregion

    #region For-Loop Iterator Scope Isolation (RC8)

    [TestMethod]
    public void FindSymbolInScope_IteratorAndVar_AfterLoop_ResolvesToVar()
    {
        var code = @"
extends Node

func test():
    for x in range(5):
        print(x)
    var x = 99
    print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .First(m => m.Identifier?.Sequence == "test");

        var xUsages = FindAllIdentifierExpressions(method, "x");
        Assert.IsTrue(xUsages.Count >= 2, "Should have at least 2 uses of x");

        // Last usage is after the for-loop (print(x) after var x = 99)
        var lastX = xUsages.Last();
        var symbol = semanticModel.FindSymbolInScope("x", lastX);
        Assert.IsNotNull(symbol);
        Assert.AreNotEqual(symbol.DeclaringScopeNode is GDForStatement, true,
            "After the for-loop, x should resolve to the var, not the iterator");
    }

    [TestMethod]
    public void FindSymbolInScope_IteratorAndVar_InsideLoop_ResolvesToIterator()
    {
        var code = @"
extends Node

func test():
    for x in range(5):
        print(x)
    var x = 99
    print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .First(m => m.Identifier?.Sequence == "test");

        var xUsages = FindAllIdentifierExpressions(method, "x");
        Assert.IsTrue(xUsages.Count >= 2);

        // First usage is inside the for-loop
        var firstX = xUsages.First();
        var symbol = semanticModel.FindSymbolInScope("x", firstX);
        Assert.IsNotNull(symbol);
        Assert.IsInstanceOfType(symbol.DeclaringScopeNode, typeof(GDForStatement),
            "Inside the for-loop, x should resolve to the iterator");
    }

    [TestMethod]
    public void FindSymbolInScope_VarBeforeForLoop_InsideLoop_ResolvesToIterator()
    {
        var code = @"
extends Node

func test():
    var x = 0
    for x in range(5):
        print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .First(m => m.Identifier?.Sequence == "test");

        var xUsages = FindAllIdentifierExpressions(method, "x");
        Assert.IsTrue(xUsages.Count >= 1);

        // The print(x) inside the for-loop should resolve to the iterator
        var xInsideLoop = xUsages.Last();
        var symbol = semanticModel.FindSymbolInScope("x", xInsideLoop);
        Assert.IsNotNull(symbol);
        Assert.IsInstanceOfType(symbol.DeclaringScopeNode, typeof(GDForStatement),
            "Inside the for-loop, x should resolve to the iterator, not the outer var");
    }

    [TestMethod]
    public void FindSymbolInScope_VarBeforeForLoop_AfterLoop_ResolvesToVar()
    {
        var code = @"
extends Node

func test():
    var x = 0
    for x in range(5):
        pass
    print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .First(m => m.Identifier?.Sequence == "test");

        var xUsages = FindAllIdentifierExpressions(method, "x");
        // print(x) after the for-loop
        var lastX = xUsages.Last();
        var symbol = semanticModel.FindSymbolInScope("x", lastX);
        Assert.IsNotNull(symbol);

        // Should resolve to the method-scoped var, not the for-loop iterator
        Assert.AreNotEqual(symbol.DeclaringScopeNode is GDForStatement, true,
            "After the for-loop, x should resolve to the var, not the iterator");
    }

    [TestMethod]
    public void FindSymbolInScope_NestedForLoops_SameName_InnerResolves()
    {
        var code = @"
extends Node

func test():
    for x in range(3):
        for x in range(5):
            print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .First(m => m.Identifier?.Sequence == "test");

        var xUsages = FindAllIdentifierExpressions(method, "x");
        Assert.IsTrue(xUsages.Count >= 1);

        // The print(x) inside the inner for-loop
        var innerX = xUsages.Last();
        var symbol = semanticModel.FindSymbolInScope("x", innerX);
        Assert.IsNotNull(symbol);

        // Should have 2 iterator symbols with different for-loop scopes
        var allX = semanticModel.FindSymbols("x").ToList();
        Assert.AreEqual(2, allX.Count, "Should have 2 iterator symbols named x");
        var scopes = allX.Select(s => s.DeclaringScopeNode).Distinct().ToList();
        Assert.AreEqual(2, scopes.Count, "Each iterator should be in its own for-loop scope");
    }

    [TestMethod]
    public void FindSymbolInScope_NestedForLoops_SameName_AfterInner_ResolvesToOuter()
    {
        var code = @"
extends Node

func test():
    for x in range(3):
        for x in range(5):
            pass
        print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .First(m => m.Identifier?.Sequence == "test");

        var xUsages = FindAllIdentifierExpressions(method, "x");
        // print(x) after the inner for-loop but inside the outer
        var printX = xUsages.Last();
        var symbol = semanticModel.FindSymbolInScope("x", printX);
        Assert.IsNotNull(symbol);

        // Verify it resolves to the outer for-loop iterator
        var allX = semanticModel.FindSymbols("x").ToList();
        Assert.AreEqual(2, allX.Count);

        // The resolved symbol's scope should be the outer for-loop
        Assert.IsInstanceOfType(symbol.DeclaringScopeNode, typeof(GDForStatement));
        // Find which for-loop is the outer (earlier start line)
        var outerIterator = allX.OrderBy(s => s.PositionToken?.StartLine ?? 0).First();
        Assert.AreSame(symbol, outerIterator,
            "After inner for-loop, x should resolve to the outer for-loop iterator");
    }

    #endregion

    #region While Scope Isolation (RC8)

    [TestMethod]
    public void FindSymbolInScope_VarInsideWhile_NotVisibleAfter()
    {
        var code = @"
extends Node

func test():
    while true:
        var x = 1
        print(x)
        break
    var x = 2
    print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .First(m => m.Identifier?.Sequence == "test");

        var allX = semanticModel.FindSymbols("x").ToList();
        Assert.AreEqual(2, allX.Count, "Should have 2 symbols named x");

        var scopes = allX.Select(s => s.DeclaringScopeNode).Distinct().ToList();
        Assert.AreEqual(2, scopes.Count,
            "While-scoped x and method-scoped x should have different scopes");

        // The print(x) after the while loop should resolve to the method-scoped var
        var xUsages = FindAllIdentifierExpressions(method, "x");
        var lastX = xUsages.Last();
        var symbol = semanticModel.FindSymbolInScope("x", lastX);
        Assert.IsNotNull(symbol);
        Assert.AreNotEqual(symbol.DeclaringScopeNode is GDWhileStatement, true,
            "After while loop, x should resolve to method-scoped var");
    }

    [TestMethod]
    public void FindSymbolInScope_VarSameNameInsideAndAfterWhile_ResolveCorrectly()
    {
        var code = @"
extends Node

func test():
    while true:
        var result = 1
        print(result)
        break
    var result = 2
    print(result)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .First(m => m.Identifier?.Sequence == "test");

        var resultUsages = FindAllIdentifierExpressions(method, "result");
        Assert.IsTrue(resultUsages.Count >= 2);

        // First usage (inside while) should have while scope
        var firstResult = resultUsages.First();
        var symbolInWhile = semanticModel.FindSymbolInScope("result", firstResult);
        Assert.IsNotNull(symbolInWhile);
        Assert.IsInstanceOfType(symbolInWhile.DeclaringScopeNode, typeof(GDWhileStatement),
            "Inside while, result should resolve to while-scoped var");

        // Last usage (after while) should have method scope
        var lastResult = resultUsages.Last();
        var symbolAfterWhile = semanticModel.FindSymbolInScope("result", lastResult);
        Assert.IsNotNull(symbolAfterWhile);
        Assert.AreNotSame(symbolInWhile, symbolAfterWhile,
            "Different scopes should yield different symbols");
    }

    #endregion

    #region If/Elif/Else Scope Isolation (RC8)

    [TestMethod]
    public void FindSymbolInScope_VarInsideIfBranch_NotVisibleAfter()
    {
        var code = @"
extends Node

func test():
    if true:
        var x = 1
        print(x)
    var x = 2
    print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .First(m => m.Identifier?.Sequence == "test");

        var allX = semanticModel.FindSymbols("x").ToList();
        Assert.AreEqual(2, allX.Count, "Should have 2 symbols named x");

        var scopes = allX.Select(s => s.DeclaringScopeNode).Distinct().ToList();
        Assert.AreEqual(2, scopes.Count,
            "If-scoped x and method-scoped x should have different scopes");

        var xUsages = FindAllIdentifierExpressions(method, "x");
        var lastX = xUsages.Last();
        var symbol = semanticModel.FindSymbolInScope("x", lastX);
        Assert.IsNotNull(symbol);
        Assert.AreNotEqual(symbol.DeclaringScopeNode is GDIfBranch, true,
            "After if branch, x should resolve to method-scoped var");
    }

    [TestMethod]
    public void FindSymbolInScope_VarInIfAndElse_SeparateScopes()
    {
        var code = @"
extends Node

func test():
    if true:
        var x = 1
        print(x)
    else:
        var x = 2
        print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var allX = semanticModel.FindSymbols("x").ToList();
        Assert.AreEqual(2, allX.Count, "Should have 2 symbols named x");

        var scopes = allX.Select(s => s.DeclaringScopeNode).Distinct().ToList();
        Assert.AreEqual(2, scopes.Count,
            "If-branch x and else-branch x should have different scopes");
    }

    [TestMethod]
    public void FindSymbolInScope_VarInElifBranch_IsolatedScope()
    {
        var code = @"
extends Node

func test():
    if true:
        var x = 1
        print(x)
    elif true:
        var x = 2
        print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var allX = semanticModel.FindSymbols("x").ToList();
        Assert.AreEqual(2, allX.Count, "Should have 2 symbols named x");

        var scopes = allX.Select(s => s.DeclaringScopeNode).Distinct().ToList();
        Assert.AreEqual(2, scopes.Count,
            "If-branch x and elif-branch x should have different scopes");
    }

    #endregion

    #region Match Case Scope Isolation (RC8)

    [TestMethod]
    public void FindSymbolInScope_MatchCaseBinding_NotVisibleOutside()
    {
        var code = @"
extends Node

func test():
    match 42:
        var x:
            print(x)
    var x = 99
    print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .First(m => m.Identifier?.Sequence == "test");

        var allX = semanticModel.FindSymbols("x").ToList();
        Assert.AreEqual(2, allX.Count, "Should have 2 symbols named x");

        var scopes = allX.Select(s => s.DeclaringScopeNode).Distinct().ToList();
        Assert.AreEqual(2, scopes.Count,
            "Match-case x and method-scoped x should have different scopes");

        var xUsages = FindAllIdentifierExpressions(method, "x");
        var lastX = xUsages.Last();
        var symbol = semanticModel.FindSymbolInScope("x", lastX);
        Assert.IsNotNull(symbol);
        Assert.AreNotEqual(symbol.DeclaringScopeNode is GDMatchCaseDeclaration, true,
            "After match block, x should resolve to method-scoped var");
    }

    [TestMethod]
    public void FindSymbolInScope_MatchCaseBindings_DifferentCases_Isolated()
    {
        var code = @"
extends Node

func test():
    match 42:
        var x:
            print(x)
        var x:
            print(x)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var allX = semanticModel.FindSymbols("x").ToList();
        Assert.AreEqual(2, allX.Count, "Should have 2 match case bindings named x");

        var scopes = allX.Select(s => s.DeclaringScopeNode).Distinct().ToList();
        Assert.AreEqual(2, scopes.Count,
            "Each match case binding should be in its own scope");
    }

    #endregion

    #region Helper Methods

    private static (GDClassDeclaration?, GDSemanticModel?) AnalyzeCode(string code)
    {
        // Create a virtual script file for testing
        var reference = new GDScriptReference("test://virtual/scope_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        if (scriptFile.Class == null)
            return (null, null);

        var collector = new GDSemanticReferenceCollector(scriptFile);
        var semanticModel = collector.BuildSemanticModel();

        // Return the class from the SAME scriptFile (not a separate parse)
        return (scriptFile.Class, semanticModel);
    }

    private static GDIdentifierExpression? FindIdentifierExpressionInMethod(GDMethodDeclaration method, string name)
    {
        GDIdentifierExpression? result = null;
        method.WalkIn(new IdentifierExpressionFinder(name, expr => result = expr));
        return result;
    }

    private static System.Collections.Generic.List<GDIdentifierExpression> FindAllIdentifierExpressions(GDMethodDeclaration method, string name)
    {
        var results = new System.Collections.Generic.List<GDIdentifierExpression>();
        method.WalkIn(new IdentifierExpressionFinder(name, expr => results.Add(expr)));
        return results;
    }

    private class IdentifierExpressionFinder : GDVisitor
    {
        private readonly string _name;
        private readonly System.Action<GDIdentifierExpression> _callback;

        public IdentifierExpressionFinder(string name, System.Action<GDIdentifierExpression> callback)
        {
            _name = name;
            _callback = callback;
        }

        public override void Visit(GDIdentifierExpression identExpr)
        {
            if (identExpr.Identifier?.Sequence == _name)
                _callback(identExpr);
        }
    }

    #endregion
}
