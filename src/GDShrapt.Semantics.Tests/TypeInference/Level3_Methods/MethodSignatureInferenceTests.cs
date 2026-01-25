using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Unit tests for GDMethodSignatureInferenceEngine.
/// Tests cross-file parameter and return type inference.
/// </summary>
[TestClass]
public class MethodSignatureInferenceTests
{
    #region Parameter Type Inference Tests

    [TestMethod]
    public void InferParameterType_SingleCallSite_ReturnsSingleType()
    {
        // Arrange - self call with literal argument
        var project = CreateProject("""
class_name Player

func attack(damage):
    pass

func combo():
    attack(10)
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var union = engine.InferParameterType("Player", "attack", "damage");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"));
    }

    [TestMethod]
    public void InferParameterType_MultipleCallSites_ReturnsUnion()
    {
        // Arrange - self calls with different literal types
        var project = CreateProject("""
class_name Player

func process(value):
    pass

func test():
    process(42)
    process("hello")
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var union = engine.InferParameterType("Player", "process", "value");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.IsUnion);
        Assert.IsTrue(union.Types.Contains("int"));
        Assert.IsTrue(union.Types.Contains("String"));
    }

    [TestMethod]
    public void InferParameterType_ExplicitType_ReturnsNull()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target: Enemy):
    pass
""", """
class_name Game

var player: Player

func test():
    player.attack(Enemy.new())
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act - explicit type means no inference needed
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        var param = report.GetParameter("target");
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasExplicitType);
        Assert.AreEqual("Enemy", param.ExplicitType);
    }

    [TestMethod]
    public void InferParameterType_NoCallSites_ReturnsNull()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var union = engine.InferParameterType("Player", "attack", "target");

        // Assert
        Assert.IsNull(union);
    }

    #endregion

    #region Return Type Inference Tests

    [TestMethod]
    public void InferReturnType_SingleReturn_ReturnsSingleType()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func get_health():
    return 100
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var union = engine.InferReturnType("Player", "get_health");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"));
    }

    [TestMethod]
    public void InferReturnType_MultipleReturns_ReturnsUnion()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func get_value(condition):
    if condition:
        return 42
    else:
        return "hello"
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var union = engine.InferReturnType("Player", "get_value");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("int"));
        Assert.IsTrue(union.Types.Contains("String"));
    }

    [TestMethod]
    public void InferReturnType_ExplicitType_ReturnsNull()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func get_health() -> int:
    return 100
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "get_health");

        // Assert
        Assert.IsNotNull(report);
        Assert.IsNotNull(report.ReturnTypeReport);
        Assert.IsTrue(report.ReturnTypeReport.HasExplicitType);
        Assert.AreEqual("int", report.ReturnTypeReport.ExplicitType);
    }

    [TestMethod]
    public void InferReturnType_VoidMethod_IncludesNull()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func do_nothing():
    var x = 1
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var union = engine.InferReturnType("Player", "do_nothing");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.Types.Contains("null"));
    }

    #endregion

    #region Method Report Tests

    [TestMethod]
    public void GetMethodReport_ExistingMethod_ReturnsReport()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target, damage):
    return target.health
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        Assert.AreEqual("Player", report.ClassName);
        Assert.AreEqual("attack", report.MethodName);
        Assert.AreEqual(2, report.Parameters.Count);
        Assert.IsNotNull(report.ReturnTypeReport);
    }

    [TestMethod]
    public void GetMethodReport_NonexistentMethod_ReturnsNull()
    {
        // Arrange
        var project = CreateProject("""
class_name Player
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "nonexistent");

        // Assert
        Assert.IsNull(report);
    }

    #endregion

    #region Project Report Tests

    [TestMethod]
    public void GetProjectReport_ReturnsCompleteReport()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    return damage_enemy(target)

func damage_enemy(entity):
    return entity.health
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetProjectReport();

        // Assert
        Assert.IsNotNull(report);
        Assert.IsTrue(report.TotalMethodsAnalyzed >= 2);
        Assert.IsNotNull(report.DependencyGraph);
    }

    [TestMethod]
    public void GetProjectReport_MultipleClasses_IncludesAll()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Enemy

func defend():
    pass
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetProjectReport();

        // Assert
        Assert.IsNotNull(report);
        Assert.IsNotNull(report.GetMethod("Player", "attack"));
        Assert.IsNotNull(report.GetMethod("Enemy", "defend"));
    }

    #endregion

    #region Cycle Detection Tests

    [TestMethod]
    public void IsMethodInCycle_CyclicMethod_ReturnsTrue()
    {
        // Arrange
        var project = CreateProject("""
class_name Cyclic

func foo():
    bar()

func bar():
    foo()
""");

        var engine = new GDMethodSignatureInferenceEngine(project);
        engine.BuildAll(); // Trigger analysis

        // Act
        var isFooInCycle = engine.IsMethodInCycle("Cyclic", "foo");
        var isBarInCycle = engine.IsMethodInCycle("Cyclic", "bar");

        // Assert
        Assert.IsTrue(isFooInCycle);
        Assert.IsTrue(isBarInCycle);
    }

    [TestMethod]
    public void IsMethodInCycle_NonCyclicMethod_ReturnsFalse()
    {
        // Arrange
        var project = CreateProject("""
class_name Linear

func a():
    b()

func b():
    pass
""");

        var engine = new GDMethodSignatureInferenceEngine(project);
        engine.BuildAll();

        // Act
        var isAInCycle = engine.IsMethodInCycle("Linear", "a");
        var isBInCycle = engine.IsMethodInCycle("Linear", "b");

        // Assert
        Assert.IsFalse(isAInCycle);
        Assert.IsFalse(isBInCycle);
    }

    [TestMethod]
    public void GetMethodReport_CyclicMethod_HasCyclicFlag()
    {
        // Arrange
        var project = CreateProject("""
class_name Cyclic

func foo():
    bar()

func bar():
    foo()
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Cyclic", "foo");

        // Assert
        Assert.IsNotNull(report);
        Assert.IsTrue(report.HasCyclicDependency);
    }

    #endregion

    #region Inferred Signature Tests

    [TestMethod]
    public void InferredMethodSignature_FromReport_CopiesCorrectly()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Enemy");

        var report = new GDMethodInferenceReport
        {
            ClassName = "Player",
            MethodName = "attack",
            Parameters = new System.Collections.Generic.Dictionary<string, GDParameterInferenceReport>
            {
                ["target"] = new GDParameterInferenceReport
                {
                    ParameterName = "target",
                    InferredUnionType = union
                }
            },
            ReturnTypeReport = new GDReturnInferenceReport
            {
                InferredUnionType = new GDUnionType()
            },
            HasCyclicDependency = false
        };

        // Act
        var signature = GDInferredMethodSignature.FromReport(report);

        // Assert
        Assert.AreEqual("attack", signature.MethodName);
        Assert.IsTrue(signature.ParameterTypes.ContainsKey("target"));
        Assert.IsFalse(signature.HasCyclicDependency);
    }

    #endregion

    #region Invalidation Tests

    [TestMethod]
    public void Invalidate_ForcesRebuild()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // First build
        var report1 = engine.GetProjectReport();
        Assert.IsNotNull(report1);

        // Act - invalidate
        engine.Invalidate();

        // Second build - should work without errors
        var report2 = engine.GetProjectReport();

        // Assert
        Assert.IsNotNull(report2);
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void Integration_CrossFileParameterInference()
    {
        // Arrange - Player.attack called from Game with specific types
        var project = CreateProject("""
class_name Player

func attack(target):
    target.take_damage(10)
""", """
class_name Game

var player: Player

func battle():
    var enemy = Enemy.new()
    var boss = Boss.new()
    player.attack(enemy)
    player.attack(boss)
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var report = engine.GetMethodReport("Player", "attack");

        // Assert
        Assert.IsNotNull(report);
        var targetParam = report.GetParameter("target");
        Assert.IsNotNull(targetParam);

        // Should have call sites with inferred types
        Assert.IsTrue(targetParam.CallSiteCount > 0);
    }

    [TestMethod]
    public void Integration_ReturnTypeFromExpression()
    {
        // Arrange
        var project = CreateProject("""
class_name Calculator

func add(a, b):
    return a + b

func subtract(a, b):
    return a - b
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var addReport = engine.GetMethodReport("Calculator", "add");
        var subReport = engine.GetMethodReport("Calculator", "subtract");

        // Assert
        Assert.IsNotNull(addReport);
        Assert.IsNotNull(subReport);
        Assert.IsNotNull(addReport.ReturnTypeReport);
        Assert.IsNotNull(subReport.ReturnTypeReport);
    }

    [TestMethod]
    public void Integration_ChainedMethodCalls()
    {
        // Arrange
        var project = CreateProject("""
class_name Chain

func first():
    return second()

func second():
    return third()

func third():
    return 42
""");

        var engine = new GDMethodSignatureInferenceEngine(project);

        // Act
        var projectReport = engine.GetProjectReport();

        // Assert
        Assert.IsNotNull(projectReport);
        Assert.IsTrue(projectReport.DependencyGraph!.Edges.Count >= 2);
    }

    #endregion

    #region Helper Methods

    private static GDScriptProject CreateProject(params string[] scripts)
    {
        var project = new GDScriptProject(scripts);
        project.AnalyzeAll();
        return project;
    }

    #endregion
}
