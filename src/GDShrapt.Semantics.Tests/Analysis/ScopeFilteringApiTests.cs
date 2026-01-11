using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for Scope Filtering APIs in GDScriptAnalyzer.
/// </summary>
[TestClass]
public class ScopeFilteringApiTests
{
    /// <summary>
    /// Ensures the script has been analyzed.
    /// </summary>
    private static void EnsureAnalyzed(GDScriptFile script)
    {
        if (script.Analyzer == null)
        {
            script.Analyze();
        }
    }

    #region IsLocalSymbol Tests

    [TestMethod]
    public void IsLocalSymbol_LocalVariable_ReturnsTrue()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act - find all symbols named "counter" and get the first one (local variable)
        var symbols = script.Analyzer?.FindSymbols("counter").ToList();

        // Assert
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.Analyzer!.IsLocalSymbol(symbol), "counter should be a local symbol");
    }

    [TestMethod]
    public void IsLocalSymbol_Parameter_ReturnsTrue()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act - find parameter "value"
        var symbols = script.Analyzer?.FindSymbols("value").ToList();

        // Assert
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "value symbol not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.Analyzer!.IsLocalSymbol(symbol), "parameter value should be a local symbol");
    }

    [TestMethod]
    public void IsLocalSymbol_ForIterator_ReturnsTrue()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act - find for iterator "i"
        var symbols = script.Analyzer?.FindSymbols("i").ToList();

        // Assert
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "i (for iterator) symbol not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.Analyzer!.IsLocalSymbol(symbol), "for iterator i should be a local symbol");
    }

    [TestMethod]
    public void IsLocalSymbol_ClassMember_ReturnsFalse()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("multiplier").ToList();

        // Assert
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "multiplier symbol not found");
        var symbol = symbols.First();
        Assert.IsFalse(script.Analyzer!.IsLocalSymbol(symbol), "class member multiplier should not be a local symbol");
    }

    [TestMethod]
    public void IsLocalSymbol_Method_ReturnsFalse()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("test_rename_local_variable").ToList();

        // Assert
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "test_rename_local_variable symbol not found");
        var symbol = symbols.First();
        Assert.IsFalse(script.Analyzer!.IsLocalSymbol(symbol), "method should not be a local symbol");
    }

    #endregion

    #region IsClassMember Tests

    [TestMethod]
    public void IsClassMember_ClassVariable_ReturnsTrue()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("multiplier").ToList();

        // Assert
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "multiplier symbol not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.Analyzer!.IsClassMember(symbol), "class variable multiplier should be a class member");
    }

    [TestMethod]
    public void IsClassMember_Method_ReturnsTrue()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("test_rename_local_variable").ToList();

        // Assert
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "test_rename_local_variable symbol not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.Analyzer!.IsClassMember(symbol), "method should be a class member");
    }

    [TestMethod]
    public void IsClassMember_Signal_ReturnsTrue()
    {
        // Arrange - Use base_entity.gd which has signal declarations
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("health_changed").ToList();

        // Assert
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "health_changed signal not found");
        var symbol = symbols.First();
        Assert.IsTrue(script.Analyzer!.IsClassMember(symbol), "signal should be a class member");
    }

    [TestMethod]
    public void IsClassMember_LocalVariable_ReturnsFalse()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("counter").ToList();

        // Assert
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        Assert.IsFalse(script.Analyzer!.IsClassMember(symbol), "local variable counter should not be a class member");
    }

    [TestMethod]
    public void IsClassMember_Parameter_ReturnsFalse()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("value").ToList();

        // Assert
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "value symbol not found");
        var symbol = symbols.First();
        Assert.IsFalse(script.Analyzer!.IsClassMember(symbol), "parameter value should not be a class member");
    }

    #endregion

    #region GetDeclarationScopeType Tests

    [TestMethod]
    public void GetDeclarationScopeType_LocalVariable_ReturnsMethod()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("counter").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        var scopeType = script.Analyzer?.GetDeclarationScopeType(symbol);

        // Assert
        Assert.AreEqual(GDScopeType.Method, scopeType, "local variable should be declared in Method scope");
    }

    [TestMethod]
    public void GetDeclarationScopeType_ClassVariable_ReturnsClass()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("multiplier").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "multiplier symbol not found");
        var symbol = symbols.First();
        var scopeType = script.Analyzer?.GetDeclarationScopeType(symbol);

        // Assert
        Assert.AreEqual(GDScopeType.Class, scopeType, "class variable should be declared in Class scope");
    }

    [TestMethod]
    public void GetDeclarationScopeType_ForIterator_ReturnsForLoop()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("i").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "i symbol not found");
        var symbol = symbols.First();
        var scopeType = script.Analyzer?.GetDeclarationScopeType(symbol);

        // Assert
        Assert.AreEqual(GDScopeType.ForLoop, scopeType, "for iterator should be declared in ForLoop scope");
    }

    [TestMethod]
    public void GetDeclarationScopeType_Method_ReturnsClass()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("test_rename_local_variable").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "method symbol not found");
        var symbol = symbols.First();
        var scopeType = script.Analyzer?.GetDeclarationScopeType(symbol);

        // Assert
        Assert.AreEqual(GDScopeType.Class, scopeType, "method should be declared in Class scope");
    }

    #endregion

    #region GetLocalReferences Tests

    [TestMethod]
    public void GetLocalReferences_LocalVariable_ReturnsOnlyMethodScopeRefs()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("counter").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        var localRefs = script.Analyzer?.GetLocalReferences(symbol).ToList();

        // Assert
        Assert.IsNotNull(localRefs);
        Assert.IsTrue(localRefs.Count > 0, "Should have local references");

        // All references should be in local scopes (Method, ForLoop, Conditional, etc.)
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
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("multiplier").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "multiplier symbol not found");
        var symbol = symbols.First();
        var localRefs = script.Analyzer?.GetLocalReferences(symbol).ToList();

        // Assert
        Assert.IsNotNull(localRefs);
        // Class members can be used in methods - so local refs should include those usages
        Assert.IsTrue(localRefs.Count >= 3, $"Should have multiple local references to multiplier in test_rename_class_member, got {localRefs.Count}");
    }

    #endregion

    #region GetReferencesInScope Tests

    [TestMethod]
    public void GetReferencesInScope_FilterByMethod_ReturnsOnlyMethodRefs()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("counter").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        var methodRefs = script.Analyzer?.GetReferencesInScope(symbol, GDScopeType.Method).ToList();

        // Assert
        Assert.IsNotNull(methodRefs);

        foreach (var reference in methodRefs)
        {
            Assert.AreEqual(GDScopeType.Method, reference.Scope?.Type, "All references should be in Method scope");
        }
    }

    [TestMethod]
    public void GetReferencesInScope_FilterByForLoop_ReturnsOnlyLoopRefs()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("i").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "i symbol not found");
        var symbol = symbols.First();
        var loopRefs = script.Analyzer?.GetReferencesInScope(symbol, GDScopeType.ForLoop).ToList();

        // Assert
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
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("counter").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        var refs = script.Analyzer?.GetReferencesInScopes(symbol, GDScopeType.Method, GDScopeType.Conditional).ToList();

        // Assert
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
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("counter").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "counter symbol not found");
        var symbol = symbols.First();
        var declaringScopeRefs = script.Analyzer?.GetReferencesInDeclaringScope(symbol).ToList();

        // Assert
        Assert.IsNotNull(declaringScopeRefs);
        Assert.IsTrue(declaringScopeRefs.Count > 0, "Should have references in declaring scope");

        // All references should be within the same method that declares 'counter'
        var allRefs = script.Analyzer?.GetReferencesTo(symbol).ToList();
        Assert.AreEqual(allRefs?.Count, declaringScopeRefs.Count,
            "For local variable, declaring scope refs should equal all refs");
    }

    [TestMethod]
    public void GetReferencesInDeclaringScope_ClassMember_ReturnsAllRefs()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("rename_test.gd");
        Assert.IsNotNull(script, "rename_test.gd not found");
        EnsureAnalyzed(script);

        // Act
        var symbols = script.Analyzer?.FindSymbols("multiplier").ToList();
        Assert.IsNotNull(symbols, "symbols list is null");
        Assert.IsTrue(symbols.Count > 0, "multiplier symbol not found");
        var symbol = symbols.First();
        var declaringScopeRefs = script.Analyzer?.GetReferencesInDeclaringScope(symbol).ToList();
        var allRefs = script.Analyzer?.GetReferencesTo(symbol).ToList();

        // Assert
        Assert.IsNotNull(declaringScopeRefs);
        Assert.IsNotNull(allRefs);

        // Class members can be accessed from any method, so all refs should be returned
        Assert.AreEqual(allRefs.Count, declaringScopeRefs.Count,
            "For class member, declaring scope refs should equal all refs");
    }

    #endregion
}
