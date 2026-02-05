using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

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

    #region Lazy Return Type Inference Tests

    [TestMethod]
    public void LazyReturnTypeInference_InnerClassReturn_InferredCorrectly()
    {
        // This test verifies that GDProjectTypesProvider correctly infers return types
        // for methods that return inner class instances.
        // Corresponds to ECSLikeSystem.create_entity() returning Entity inner class.
        var project = CreateProject("""
class_name ECSLikeSystem

class Entity:
    var id: int
    var name: String

func create_entity(entity_name = ""):
    var entity = Entity.new()
    entity.name = entity_name
    return entity
""");

        // Get the runtime provider
        var provider = project.CreateRuntimeProvider();
        Assert.IsNotNull(provider);

        // Get the method info
        var member = provider.GetMember("ECSLikeSystem", "create_entity");
        Assert.IsNotNull(member);

        // Debug: print the actual type
        System.Console.WriteLine($"Inferred return type: {member.Type}");

        // The return type should be "Entity" (inner class), not "Variant"
        Assert.AreNotEqual("Variant", member.Type,
            $"Return type should be inferred, but was: {member.Type}");

        // Also verify the inner class is known
        var isKnown = provider.IsKnownType("ECSLikeSystem.Entity");
        System.Console.WriteLine($"IsKnownType(ECSLikeSystem.Entity): {isKnown}");

        // Check if simple Entity name resolves
        var memberFromEntity = provider.GetMember("Entity", "id");
        System.Console.WriteLine($"GetMember(Entity, id): {memberFromEntity?.Name ?? "null"}");

        var memberFromQualified = provider.GetMember("ECSLikeSystem.Entity", "id");
        System.Console.WriteLine($"GetMember(ECSLikeSystem.Entity, id): {memberFromQualified?.Name ?? "null"}");
    }

    [TestMethod]
    public void LazyReturnTypeInference_SimpleReturn_InferredCorrectly()
    {
        // Simple test: method returns a literal
        var project = CreateProject("""
class_name Calculator

func get_magic_number():
    return 42
""");

        var provider = project.CreateRuntimeProvider();
        Assert.IsNotNull(provider);

        var member = provider.GetMember("Calculator", "get_magic_number");
        Assert.IsNotNull(member);

        // Should infer "int" from the return statement
        Assert.AreEqual("int", member.Type,
            $"Return type should be 'int', but was: {member.Type}");
    }

    [TestMethod]
    public void LazyReturnTypeInference_StringReturn_InferredCorrectly()
    {
        // Simple test: method returns a string
        var project = CreateProject("""
class_name Greeter

func get_greeting():
    return "Hello"
""");

        var provider = project.CreateRuntimeProvider();
        Assert.IsNotNull(provider);

        var member = provider.GetMember("Greeter", "get_greeting");
        Assert.IsNotNull(member);

        // Should infer "String" from the return statement
        Assert.AreEqual("String", member.Type,
            $"Return type should be 'String', but was: {member.Type}");
    }

    #endregion

    #region Cross-File Call Chain Tests

    [TestMethod]
    public void CrossFileCallChain_LocalVariableFromMethodCall_InfersType()
    {
        // This test verifies the full chain:
        // 1. Class variable has inferred type from assignment
        // 2. Method call returns inferred type
        // 3. Local variable gets type from method return
        // 4. Member access on local variable resolves correctly
        var project = CreateProject("""
class_name ECSLikeSystem

class Entity:
    var id: int
    var name: String

func create_entity(entity_name = ""):
    var entity = Entity.new()
    entity.name = entity_name
    return entity
""", """
class_name Consumer

var entity_manager

func _ready():
    entity_manager = ECSLikeSystem.new()

func process():
    var entity = entity_manager.create_entity("Test")
    var id_value = entity.id  # Should NOT trigger GD7002
""");

        // Get the consumer script
        var consumerFile = project.ScriptFiles.FirstOrDefault(f => f.Class?.ClassName?.Identifier?.Sequence == "Consumer");
        Assert.IsNotNull(consumerFile, "Consumer file not found");

        var model = consumerFile.SemanticModel;
        Assert.IsNotNull(model, "SemanticModel not found");

        // Check entity_manager type (class variable union type)
        var entityManagerType = model.GetUnionType("entity_manager")?.EffectiveType;
        System.Console.WriteLine($"entity_manager union type: {entityManagerType}");

        // Find the process method
        var processMethod = consumerFile.Class.Methods.FirstOrDefault(m => m.Identifier?.Sequence == "process");
        Assert.IsNotNull(processMethod, "process method not found");

        // Find the local variable 'entity'
        var entityVarDecl = processMethod.Statements?
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "entity");
        Assert.IsNotNull(entityVarDecl, "entity variable not found");

        // Check initializer type
        var initializerType = model.GetExpressionType(entityVarDecl.Initializer);
        System.Console.WriteLine($"entity initializer type: {initializerType}");

        // Check that identifier expression for entity resolves correctly
        // Find the entity identifier expression in the second statement (var id_value = entity.id)
        var idValueVarDecl = processMethod.Statements?
            .OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "id_value");
        Assert.IsNotNull(idValueVarDecl, "id_value variable not found");

        // The initializer is entity.id - a member operator expression
        var entityIdAccess = idValueVarDecl.Initializer as GDMemberOperatorExpression;
        Assert.IsNotNull(entityIdAccess, "entity.id member access not found");

        // Verify it's the right expression
        var entityIdentifier = entityIdAccess.CallerExpression as GDIdentifierExpression;
        Assert.IsNotNull(entityIdentifier, "entity identifier not found");
        Assert.AreEqual("entity", entityIdentifier.Identifier?.Sequence);
        Assert.AreEqual("id", entityIdAccess.Identifier?.Sequence);

        // Check the confidence level - should be Strict, not NameMatch
        var confidence = model.GetMemberAccessConfidence(entityIdAccess);
        System.Console.WriteLine($"entity.id confidence: {confidence}");

        var callerType = model.GetExpressionType(entityIdAccess.CallerExpression);
        System.Console.WriteLine($"entity type (caller of .id): {callerType}");

        // Debug: check what symbol 'entity' resolves to
        var entitySymbol = model.FindSymbol("entity");
        System.Console.WriteLine($"FindSymbol(entity): {entitySymbol?.Name}, TypeName={entitySymbol?.TypeName}, Kind={entitySymbol?.Kind}");

        // Check the identifier expression type directly
        var entityIdentifierType = model.GetExpressionType(entityIdentifier);
        System.Console.WriteLine($"GetExpressionType(entityIdentifier): {entityIdentifierType}");

        // Debug: Check if symbol has Declaration
        System.Console.WriteLine($"Symbol Declaration: {entitySymbol?.DeclarationNode?.GetType().Name}");
        if (entitySymbol?.DeclarationNode is GDVariableDeclarationStatement varDeclSymbol)
        {
            System.Console.WriteLine($"Symbol Declaration Initializer: {varDeclSymbol.Initializer?.GetType().Name}");
            var initType = model.GetExpressionType(varDeclSymbol.Initializer);
            System.Console.WriteLine($"Symbol Declaration Initializer Type: {initType}");
        }

        // Debug: check if Entity type is known
        var provider = project.CreateRuntimeProvider();
        var entityIsKnown = provider.IsKnownType("Entity");
        System.Console.WriteLine($"IsKnownType(Entity): {entityIsKnown}");

        var idMember = provider.GetMember("Entity", "id");
        System.Console.WriteLine($"GetMember(Entity, id): {idMember?.Name ?? "null"} type={idMember?.Type}");

        // Try getting type via type engine directly
        var typeEngine = new GDTypeInferenceEngine(provider);
        var entityTypeFromEngine = typeEngine.InferType(entityIdAccess.CallerExpression);
        System.Console.WriteLine($"TypeEngine.InferType(entity): {entityTypeFromEngine}");

        // Debug: test AST fallback directly
        var localInit = GDContainerTypeAnalyzer.FindLocalVariableInitializer(entityIdentifier, "entity");
        System.Console.WriteLine($"FindLocalVariableInitializer result: {localInit?.GetType().Name}");
        if (localInit != null)
        {
            var localInitType = typeEngine.InferType(localInit);
            System.Console.WriteLine($"LocalInit inferred type: {localInitType}");
        }

        // The main assertion: confidence should not be NameMatch (which triggers GD7002)
        Assert.AreNotEqual(GDReferenceConfidence.NameMatch, confidence,
            $"entity.id should not have NameMatch confidence. Caller type: {callerType}");
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
