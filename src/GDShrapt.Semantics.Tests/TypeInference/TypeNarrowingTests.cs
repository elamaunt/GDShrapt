using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for type narrowing analysis (flow-sensitive type analysis).
/// </summary>
[TestClass]
public class TypeNarrowingTests
{
    #region Basic Type Narrowing Tests

    [TestMethod]
    public void TypeNarrowing_IsCheck_NarrowsToType()
    {
        // Arrange
        var code = @"
func process(entity):
    if entity is Player:
        pass  # Here entity should be narrowed to Player
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert
        var concreteType = narrowingContext.GetConcreteType("entity");
        Assert.AreEqual("Player", concreteType, "Entity should be narrowed to Player in if branch");
    }

    [TestMethod]
    public void TypeNarrowing_IsCheck_NegatedExcludesType()
    {
        // Arrange
        var code = @"
func process(entity):
    if entity is Player:
        pass
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act - analyze for else branch (negated)
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: true);

        // Assert
        var duckType = narrowingContext.GetNarrowedType("entity");
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.ExcludedTypes.Contains("Player"), "Player should be excluded in else branch");
    }

    [TestMethod]
    public void TypeNarrowing_NotIsCheck_ExcludesType()
    {
        // Arrange
        var code = @"
func process(entity):
    if not entity is Player:
        pass  # Here entity is NOT Player
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert
        var duckType = narrowingContext.GetNarrowedType("entity");
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.ExcludedTypes.Contains("Player"), "Player should be excluded with 'not is' check");
    }

    #endregion

    #region HasMethod/HasSignal Tests

    [TestMethod]
    public void TypeNarrowing_HasMethod_RequiresMethod()
    {
        // Arrange
        var code = @"
func process(obj):
    if obj.has_method(""attack""):
        pass  # Here obj must have attack method
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert
        var duckType = narrowingContext.GetNarrowedType("obj");
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("attack"), "Method 'attack' should be required");
    }

    [TestMethod]
    public void TypeNarrowing_HasSignal_RequiresSignal()
    {
        // Arrange
        var code = @"
func process(obj):
    if obj.has_signal(""died""):
        pass  # Here obj must have died signal
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert
        var duckType = narrowingContext.GetNarrowedType("obj");
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredSignals.Contains("died"), "Signal 'died' should be required");
    }

    [TestMethod]
    public void TypeNarrowing_Has_RequiresProperty()
    {
        // Arrange
        var code = @"
func process(obj):
    if obj.has(""health""):
        pass  # Here obj must have health property
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert
        var duckType = narrowingContext.GetNarrowedType("obj");
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredProperties.ContainsKey("health"), "Property 'health' should be required");
    }

    #endregion

    #region Compound Conditions Tests

    [TestMethod]
    public void TypeNarrowing_AndCondition_CombinesNarrowing()
    {
        // Arrange
        var code = @"
func process(obj):
    if obj is Entity and obj.has_method(""attack""):
        pass  # obj is Entity AND has attack method
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: false);

        // Assert
        var duckType = narrowingContext.GetNarrowedType("obj");
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.PossibleTypes.Contains("Entity"), "Should be narrowed to Entity");
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("attack"), "Should require attack method");
    }

    [TestMethod]
    public void TypeNarrowing_OrCondition_Negated_CombinesExclusions()
    {
        // Arrange
        var code = @"
func process(obj):
    if obj is Player or obj is Enemy:
        pass
";
        var analyzer = new GDTypeNarrowingAnalyzer(new GDDefaultRuntimeProvider());
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var ifStatement = FindFirstIfStatement(classDecl);

        Assert.IsNotNull(ifStatement);
        var condition = ifStatement.IfBranch?.Condition;
        Assert.IsNotNull(condition);

        // Act - analyze for else branch (negated OR means both are false)
        var narrowingContext = analyzer.AnalyzeCondition(condition, isNegated: true);

        // Assert - in else branch, obj is NOT Player AND NOT Enemy
        var duckType = narrowingContext.GetNarrowedType("obj");
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.ExcludedTypes.Contains("Player"), "Player should be excluded");
        Assert.IsTrue(duckType.ExcludedTypes.Contains("Enemy"), "Enemy should be excluded");
    }

    #endregion

    #region Context Hierarchy Tests

    [TestMethod]
    public void TypeNarrowingContext_CreateChild_InheritsParentNarrowing()
    {
        // Arrange
        var parent = new GDTypeNarrowingContext();
        parent.NarrowType("obj", "Entity");

        // Act
        var child = parent.CreateChild();

        // Assert
        var narrowedType = child.GetNarrowedType("obj");
        Assert.IsNotNull(narrowedType);
        Assert.IsTrue(narrowedType.PossibleTypes.Contains("Entity"), "Child should inherit parent's narrowing");
    }

    [TestMethod]
    public void TypeNarrowingContext_Child_CanAddMoreNarrowing()
    {
        // Arrange
        var parent = new GDTypeNarrowingContext();
        parent.NarrowType("obj", "Entity");

        var child = parent.CreateChild();
        child.RequireMethod("obj", "attack");

        // Assert - child has its own narrowing, parent's narrowing is separate
        // GetNarrowedType returns the FIRST found (child's own or parent's)
        var childNarrowing = child.GetNarrowedType("obj");
        Assert.IsNotNull(childNarrowing);
        // Child's narrowing contains what child added
        Assert.IsTrue(childNarrowing.RequiredMethods.ContainsKey("attack"));

        // Parent's narrowing is still accessible if child doesn't override
        var parentNarrowing = parent.GetNarrowedType("obj");
        Assert.IsNotNull(parentNarrowing);
        Assert.IsTrue(parentNarrowing.PossibleTypes.Contains("Entity"));
    }

    #endregion

    #region Branch Merge Tests

    [TestMethod]
    public void TypeNarrowingContext_MergeBranches_KeepsCommonRequirements()
    {
        // Arrange
        var ifBranch = new GDTypeNarrowingContext();
        ifBranch.RequireMethod("obj", "common");
        ifBranch.RequireMethod("obj", "if_only");

        var elseBranch = new GDTypeNarrowingContext();
        elseBranch.RequireMethod("obj", "common");
        elseBranch.RequireMethod("obj", "else_only");

        // Act
        var merged = GDTypeNarrowingContext.MergeBranches(ifBranch, elseBranch);

        // Assert - only common requirements survive
        var duckType = merged.GetNarrowedType("obj");
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.RequiredMethods.ContainsKey("common"), "Common requirement should survive merge");
        Assert.IsFalse(duckType.RequiredMethods.ContainsKey("if_only"), "If-only requirement should not survive");
        Assert.IsFalse(duckType.RequiredMethods.ContainsKey("else_only"), "Else-only requirement should not survive");
    }

    [TestMethod]
    public void TypeNarrowingContext_MergeBranches_UnionsPossibleTypes()
    {
        // Arrange
        var ifBranch = new GDTypeNarrowingContext();
        ifBranch.NarrowType("obj", "Player");

        var elseBranch = new GDTypeNarrowingContext();
        elseBranch.NarrowType("obj", "Enemy");

        // Act
        var merged = GDTypeNarrowingContext.MergeBranches(ifBranch, elseBranch);

        // Assert - both possible types should be present
        var duckType = merged.GetNarrowedType("obj");
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.PossibleTypes.Contains("Player"), "Player should be in possible types");
        Assert.IsTrue(duckType.PossibleTypes.Contains("Enemy"), "Enemy should be in possible types");
    }

    #endregion

    #region Helper Methods

    private static GDIfStatement? FindFirstIfStatement(GDClassDeclaration? classDecl)
    {
        if (classDecl == null)
            return null;

        foreach (var member in classDecl.Members)
        {
            if (member is GDMethodDeclaration method)
            {
                foreach (var stmt in method.Statements ?? Enumerable.Empty<GDStatement>())
                {
                    if (stmt is GDIfStatement ifStmt)
                        return ifStmt;
                }
            }
        }

        return null;
    }

    #endregion
}
