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
