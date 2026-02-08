using GDShrapt.Reader;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests.Analysis;

[TestClass]
public class ScopeFilteringApiTests
{
    private static void EnsureAnalyzed(GDScriptFile script)
    {
        if (script.SemanticModel == null)
        {
            script.Analyze();
        }
    }

    #region IsLocalSymbol Tests

    [TestMethod]
    public void IsLocalSymbol_LocalVariable_ReturnsTrue()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("counter").ToList();

        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.SemanticModel!.IsLocalSymbol(symbol), "counter should be a local symbol");
    }

    [TestMethod]
    public void IsLocalSymbol_Parameter_ReturnsTrue()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("value").ToList();

        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "value symbol not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.SemanticModel!.IsLocalSymbol(symbol), "parameter value should be a local symbol");
    }

    [TestMethod]
    public void IsLocalSymbol_ForIterator_ReturnsTrue()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("i").ToList();

        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "i (for iterator) symbol not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.SemanticModel!.IsLocalSymbol(symbol), "for iterator i should be a local symbol");
    }

    [TestMethod]
    public void IsLocalSymbol_ClassMember_ReturnsFalse()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("multiplier").ToList();

        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "multiplier symbol not found");
        var symbol = symbols.First();
        Assert.IsFalse(script.SemanticModel!.IsLocalSymbol(symbol), "class member multiplier should not be a local symbol");
    }

    [TestMethod]
    public void IsLocalSymbol_Method_ReturnsFalse()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("test_rename_local_variable").ToList();

        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "test_rename_local_variable symbol not found");
        var symbol = symbols.First();
        Assert.IsFalse(script.SemanticModel!.IsLocalSymbol(symbol), "method should not be a local symbol");
    }

    #endregion

    #region IsClassMember Tests

    [TestMethod]
    public void IsClassMember_ClassVariable_ReturnsTrue()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("multiplier").ToList();

        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "multiplier symbol not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.SemanticModel!.IsClassMember(symbol), "class variable multiplier should be a class member");
    }

    [TestMethod]
    public void IsClassMember_Method_ReturnsTrue()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("test_rename_local_variable").ToList();

        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "test_rename_local_variable symbol not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.SemanticModel!.IsClassMember(symbol), "method should be a class member");
    }

    [TestMethod]
    public void IsClassMember_Signal_ReturnsTrue()
    {
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("health_changed").ToList();

        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "health_changed signal not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.SemanticModel!.IsClassMember(symbol), "signal should be a class member");
    }

    [TestMethod]
    public void IsClassMember_LocalVariable_ReturnsFalse()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("counter").ToList();

        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        Assert.IsFalse(script.SemanticModel!.IsClassMember(symbol), "local variable counter should not be a class member");
    }

    [TestMethod]
    public void IsClassMember_Parameter_ReturnsFalse()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("value").ToList();

        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "value symbol not found");
        var symbol = symbols.First();
        Assert.IsFalse(script.SemanticModel!.IsClassMember(symbol), "parameter value should not be a class member");
    }

    #endregion

    #region GetDeclarationScopeType Tests

    [TestMethod]
    public void GetDeclarationScopeType_LocalVariable_ReturnsMethod()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("counter").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        var scopeType = script.SemanticModel?.GetDeclarationScopeType(symbol);

        Assert.AreEqual(GDScopeType.Method, scopeType, "local variable should be declared in Method scope");
    }

    [TestMethod]
    public void GetDeclarationScopeType_ClassVariable_ReturnsClass()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("multiplier").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "multiplier symbol not found");
        var symbol = symbols.First();
        var scopeType = script.SemanticModel?.GetDeclarationScopeType(symbol);

        Assert.AreEqual(GDScopeType.Class, scopeType, "class variable should be declared in Class scope");
    }

    [TestMethod]
    public void GetDeclarationScopeType_ForIterator_ReturnsForLoop()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("i").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "i symbol not found");
        var symbol = symbols.First();
        var scopeType = script.SemanticModel?.GetDeclarationScopeType(symbol);

        Assert.AreEqual(GDScopeType.ForLoop, scopeType, "for iterator should be declared in ForLoop scope");
    }

    [TestMethod]
    public void GetDeclarationScopeType_Method_ReturnsClass()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("test_rename_local_variable").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "method symbol not found");
        var symbol = symbols.First();
        var scopeType = script.SemanticModel?.GetDeclarationScopeType(symbol);

        Assert.AreEqual(GDScopeType.Class, scopeType, "method should be declared in Class scope");
    }

    #endregion

    #region GetLocalReferences Tests

    [TestMethod]
    public void GetLocalReferences_LocalVariable_ReturnsOnlyMethodScopeRefs()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("counter").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        var localRefs = script.SemanticModel?.GetLocalReferences(symbol).ToList();

        Assert.IsNotNull(localRefs);
        Assert.IsTrue(localRefs.Count > 0, "Should have local references");

        foreach (var reference in localRefs)
        {
            Assert.IsNotNull(reference.Scope, "Reference should have a scope");
            Assert.AreNotEqual(GDScopeType.Global, reference.Scope.Type, "Local refs should not be in Global scope");
            Assert.AreNotEqual(GDScopeType.Class, reference.Scope.Type, "Local refs should not be in Class scope");
        }
    }

    [TestMethod]
    public void GetLocalReferences_ClassMember_IncludesAllMethodUsages()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("multiplier").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "multiplier symbol not found");
        var symbol = symbols.First();
        var localRefs = script.SemanticModel?.GetLocalReferences(symbol).ToList();

        Assert.IsNotNull(localRefs);
        Assert.IsTrue(localRefs.Count >= 3, $"Should have multiple local references to multiplier in test_rename_class_member, got {localRefs.Count}");
    }

    #endregion

    #region GetReferencesInScope Tests

    [TestMethod]
    public void GetReferencesInScope_FilterByMethod_ReturnsOnlyMethodRefs()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("counter").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        var methodRefs = script.SemanticModel?.GetReferencesInScope(symbol, GDScopeType.Method).ToList();

        Assert.IsNotNull(methodRefs);

        foreach (var reference in methodRefs)
        {
            Assert.AreEqual(GDScopeType.Method, reference.Scope?.Type, "All references should be in Method scope");
        }
    }

    [TestMethod]
    public void GetReferencesInScope_FilterByForLoop_ReturnsOnlyLoopRefs()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("i").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "i symbol not found");
        var symbol = symbols.First();
        var loopRefs = script.SemanticModel?.GetReferencesInScope(symbol, GDScopeType.ForLoop).ToList();

        Assert.IsNotNull(loopRefs);

        foreach (var reference in loopRefs)
        {
            Assert.AreEqual(GDScopeType.ForLoop, reference.Scope?.Type, "All references should be in ForLoop scope");
        }
    }

    #endregion

    #region GetReferencesInScopes Tests

    [TestMethod]
    public void GetReferencesInScopes_MultipleTypes_ReturnsMatchingRefs()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("counter").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        var refs = script.SemanticModel?.GetReferencesInScopes(symbol, GDScopeType.Method, GDScopeType.Conditional).ToList();

        Assert.IsNotNull(refs);
        Assert.IsTrue(refs.Count > 0, "Should have references in Method or Conditional scopes");

        foreach (var reference in refs)
        {
            Assert.IsTrue(
                reference.Scope?.Type == GDScopeType.Method || reference.Scope?.Type == GDScopeType.Conditional,
                $"All references should be in Method or Conditional scope, got {reference.Scope?.Type}");
        }
    }

    #endregion

    #region GetReferencesInDeclaringScope Tests

    [TestMethod]
    public void GetReferencesInDeclaringScope_LocalVariable_ReturnsOnlySameMethodRefs()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("counter").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        var declaringScopeRefs = script.SemanticModel?.GetReferencesInDeclaringScope(symbol).ToList();

        Assert.IsNotNull(declaringScopeRefs);
        Assert.IsTrue(declaringScopeRefs.Count > 0, "Should have references in declaring scope");

        var allRefs = script.SemanticModel?.GetReferencesTo(symbol).ToList();
        Assert.AreEqual(allRefs?.Count, declaringScopeRefs.Count,
            "For local variable, declaring scope refs should equal all refs");
    }

    [TestMethod]
    public void GetReferencesInDeclaringScope_ClassMember_ReturnsAllRefs()
    {
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        var symbols = script.SemanticModel?.FindSymbols("multiplier").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "multiplier symbol not found");
        var symbol = symbols.First();
        var declaringScopeRefs = script.SemanticModel?.GetReferencesInDeclaringScope(symbol).ToList();
        var allRefs = script.SemanticModel?.GetReferencesTo(symbol).ToList();

        Assert.IsNotNull(declaringScopeRefs);
        Assert.IsNotNull(allRefs);

        Assert.AreEqual(allRefs.Count, declaringScopeRefs.Count,
            "For class member, declaring scope refs should equal all refs");
    }

    #endregion
}
