using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

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
        Assert.AreEqual("Player", concreteType?.DisplayName, "Entity should be narrowed to Player in if branch");
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
        Assert.IsTrue(duckType.ExcludedTypes.Any(t => t.DisplayName == "Player"), "Player should be excluded in else branch");
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
        Assert.IsTrue(duckType.ExcludedTypes.Any(t => t.DisplayName == "Player"), "Player should be excluded with 'not is' check");
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
        Assert.IsTrue(duckType.PossibleTypes.Any(t => t.DisplayName == "Entity"), "Should be narrowed to Entity");
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
        Assert.IsTrue(duckType.ExcludedTypes.Any(t => t.DisplayName == "Player"), "Player should be excluded");
        Assert.IsTrue(duckType.ExcludedTypes.Any(t => t.DisplayName == "Enemy"), "Enemy should be excluded");
    }

    #endregion

    #region Context Hierarchy Tests

    [TestMethod]
    public void TypeNarrowingContext_CreateChild_InheritsParentNarrowing()
    {
        // Arrange
        var parent = new GDTypeNarrowingContext();
        parent.NarrowType("obj", GDSemanticType.FromRuntimeTypeName("Entity"));

        // Act
        var child = parent.CreateChild();

        // Assert
        var narrowedType = child.GetNarrowedType("obj");
        Assert.IsNotNull(narrowedType);
        Assert.IsTrue(narrowedType.PossibleTypes.Any(t => t.DisplayName == "Entity"), "Child should inherit parent's narrowing");
    }

    [TestMethod]
    public void TypeNarrowingContext_Child_CanAddMoreNarrowing()
    {
        // Arrange
        var parent = new GDTypeNarrowingContext();
        parent.NarrowType("obj", GDSemanticType.FromRuntimeTypeName("Entity"));

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
        Assert.IsTrue(parentNarrowing.PossibleTypes.Any(t => t.DisplayName == "Entity"));
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
        ifBranch.NarrowType("obj", GDSemanticType.FromRuntimeTypeName("Player"));

        var elseBranch = new GDTypeNarrowingContext();
        elseBranch.NarrowType("obj", GDSemanticType.FromRuntimeTypeName("Enemy"));

        // Act
        var merged = GDTypeNarrowingContext.MergeBranches(ifBranch, elseBranch);

        // Assert - both possible types should be present
        var duckType = merged.GetNarrowedType("obj");
        Assert.IsNotNull(duckType);
        Assert.IsTrue(duckType.PossibleTypes.Any(t => t.DisplayName == "Player"), "Player should be in possible types");
        Assert.IsTrue(duckType.PossibleTypes.Any(t => t.DisplayName == "Enemy"), "Enemy should be in possible types");
    }

    #endregion

    #region Return Type Collector Narrowing Tests

    /// <summary>
    /// Tests that binary operators correctly use narrowed type of operands.
    /// After 'if input is int:', the expression 'input * 2' should be inferred as 'int', not 'Variant'.
    /// </summary>
    [TestMethod]
    public void ReturnTypeCollector_BinaryOperator_UsesNarrowedOperandType()
    {
        // Arrange
        var code = @"
func try_operation(input):
    if input is int:
        return input * 2
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
        Assert.IsNotNull(method, "Method not found");

        var collector = new GDReturnTypeCollector(method, new GDDefaultRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert - 'input * 2' after 'if input is int' should be inferred as int
        var actualTypes = string.Join(", ", union.Types);
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")),
            $"Return type of 'input * 2' after 'is int' check should be 'int'. Actual types: [{actualTypes}]");
        Assert.IsFalse(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("Variant")),
            $"Should not contain 'Variant' - narrowing should work. Actual types: [{actualTypes}]");
    }

    /// <summary>
    /// Tests that type narrowing is preserved in nested if statements.
    /// The narrowed type from outer 'if input is int:' should persist through inner 'if input < 0:'.
    /// </summary>
    [TestMethod]
    public void ReturnTypeCollector_NestedIf_PreservesNarrowedType()
    {
        // Arrange
        var code = @"
func try_operation(input):
    if input is int:
        if input < 0:
            return input + 100
        return input * 2
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
        Assert.IsNotNull(method, "Method not found");

        var collector = new GDReturnTypeCollector(method, new GDDefaultRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert - Both returns should be int (narrowing preserved through nested if)
        var actualTypes = string.Join(", ", union.Types);
        var returnInfos = string.Join(", ", collector.Returns.Select(r => $"{r.ExpressionText}:{r.InferredType?.DisplayName ?? "null"}"));

        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")),
            $"All returns inside 'is int' block should be 'int'. Actual types: [{actualTypes}], Returns: [{returnInfos}]");
        Assert.IsFalse(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("Variant")),
            $"Should not contain 'Variant' - narrowing should be preserved. Actual types: [{actualTypes}]");
    }

    /// <summary>
    /// Tests that method calls on narrowed types work correctly.
    /// After 'if input is String:', calling 'input.length()' should return 'int'.
    /// </summary>
    [TestMethod]
    public void ReturnTypeCollector_MethodCallOnNarrowedType_InfersCorrectly()
    {
        // Arrange
        var code = @"
func get_length(input):
    if input is String:
        return input.length()
    return 0
";
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
        Assert.IsNotNull(method, "Method not found");

        var collector = new GDReturnTypeCollector(method, new GDDefaultRuntimeProvider());

        // Act
        collector.Collect();
        var union = collector.ComputeReturnUnionType();

        // Assert - input.length() should return int
        var actualTypes = string.Join(", ", union.Types);
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")),
            $"input.length() on String should return 'int'. Actual types: [{actualTypes}]");
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
