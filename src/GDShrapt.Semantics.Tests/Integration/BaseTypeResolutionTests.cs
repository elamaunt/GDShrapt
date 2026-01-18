using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using GDShrapt.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for base type resolution and inheritance support in semantic analysis.
/// Verifies that members from base types (e.g., Node2D methods like queue_free()) are properly resolved.
/// </summary>
[TestClass]
public class BaseTypeResolutionTests
{
    [TestMethod]
    public void BaseEntity_ExtendsNode2D_BaseTypeIsResolved()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found in test project");
        Assert.IsNotNull(script.Class, "Script should have a parsed class");

        // Act
        var extendsClause = script.Class.Extends;

        // Assert
        Assert.IsNotNull(extendsClause, "Script should have extends clause");
        Assert.AreEqual("Node2D", extendsClause.Type?.BuildName(),
            "base_entity.gd should extend Node2D");
    }

    [TestMethod]
    public void BaseEntity_ClassName_IsBaseEntity()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        // Assert
        Assert.AreEqual("BaseEntity", script.TypeName,
            "Script should have class_name BaseEntity");
    }

    [TestMethod]
    public void BaseEntity_SemanticModel_ResolvesNode2DMembers()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        // Ensure script is analyzed
        if (script.Analyzer == null)
        {
            var runtimeProvider = project.CreateRuntimeProvider();
            script.Analyze(runtimeProvider);
        }

        var semanticModel = script.Analyzer?.SemanticModel;
        Assert.IsNotNull(semanticModel, "Script should have a semantic model after analysis");

        // Act - Try to resolve queue_free() on Node2D type through inheritance chain (Node.queue_free())
        var queueFreeMember = semanticModel.ResolveMember("Node2D", "queue_free");

        // Assert
        Assert.IsNotNull(queueFreeMember,
            "queue_free() should be resolvable from Node2D's inheritance chain (Node.queue_free())");
    }

    [TestMethod]
    public void GodotTypesProvider_Node2D_InheritsFromCanvasItem()
    {
        // Arrange
        var provider = new GDGodotTypesProvider();

        // Act
        var baseType = provider.GetBaseType("Node2D");

        // Assert
        Assert.AreEqual("CanvasItem", baseType,
            "Node2D should inherit from CanvasItem");
    }

    [TestMethod]
    public void GodotTypesProvider_Node2D_HasQueueFreeFromNodeChain()
    {
        // Arrange
        var provider = new GDGodotTypesProvider();

        // Act - queue_free() is defined on Node, which is in Node2D's inheritance chain
        var member = provider.GetMember("Node2D", "queue_free");

        // Assert
        Assert.IsNotNull(member,
            "Node2D should have queue_free() through inheritance from Node");
        Assert.AreEqual("queue_free", member.Name);
    }

    [TestMethod]
    public void GodotTypesProvider_Node2D_HasPositionProperty()
    {
        // Arrange
        var provider = new GDGodotTypesProvider();

        // Act - position is defined on Node2D
        var member = provider.GetMember("Node2D", "position");

        // Assert
        Assert.IsNotNull(member,
            "Node2D should have position property");
        // Godot API uses PascalCase for property names
        Assert.AreEqual("Position", member.Name);
    }

    [TestMethod]
    public void RuntimeProvider_ResolvesNode2DMembersForBaseEntity()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var runtimeProvider = project.CreateRuntimeProvider();

        // Act - Resolve queue_free for Node2D (BaseEntity's base type)
        var member = runtimeProvider.GetMember("Node2D", "queue_free");

        // Assert
        Assert.IsNotNull(member,
            "RuntimeProvider should resolve queue_free() for Node2D");
    }

    [TestMethod]
    public void Diagnostics_BaseEntity_NoUnknownSymbolErrorsForInheritedMembers()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        // Ensure script is analyzed with proper semantic model
        var runtimeProvider = project.CreateRuntimeProvider();
        script.Reload();
        script.Analyze(runtimeProvider);

        // Create diagnostics service with proper configuration
        var config = new GDProjectConfig();
        config.Linting.Enabled = true;
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        // Act
        var result = diagnosticsService.Diagnose(script);

        // Get all diagnostics related to unknown identifiers
        var unknownIdentifierErrors = result.Diagnostics
            .Where(d => d.Code == "GD2001" || // Unknown identifier
                        d.Code == "GD4001" || // Unknown member
                        d.Code == "GD4002")   // Unknown method
            .ToList();

        // Assert - queue_free() should NOT be flagged as unknown
        var queueFreeErrors = unknownIdentifierErrors
            .Where(d => d.Message.Contains("queue_free"))
            .ToList();

        Assert.AreEqual(0, queueFreeErrors.Count,
            $"queue_free() should not be flagged as unknown. Errors: {string.Join(", ", queueFreeErrors.Select(d => d.Message))}");
    }

    [TestMethod]
    public void Diagnostics_BaseEntity_NoErrorsForNode2DAsExtends()
    {
        // Arrange
        var project = TestProjectFixture.Project;
        var script = TestProjectFixture.GetScript("base_entity.gd");
        Assert.IsNotNull(script, "base_entity.gd not found");

        var runtimeProvider = project.CreateRuntimeProvider();
        script.Reload();
        script.Analyze(runtimeProvider);

        var config = new GDProjectConfig();
        config.Linting.Enabled = true;
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        // Act
        var result = diagnosticsService.Diagnose(script);

        // Get errors about Node2D being unknown
        var node2dErrors = result.Diagnostics
            .Where(d => d.Message.Contains("Node2D") &&
                       (d.Code == "GD2001" || d.Code == "GD1001"))
            .ToList();

        // Assert
        Assert.AreEqual(0, node2dErrors.Count,
            $"Node2D should be recognized as a valid base type. Errors: {string.Join(", ", node2dErrors.Select(d => d.Message))}");
    }

    [TestMethod]
    public void Validator_WithRuntimeProvider_ResolvesInheritedMethods()
    {
        // Arrange
        var code = @"
extends Node2D
class_name TestEntity

func _ready():
    print(""ready"")

func die():
    queue_free()
";
        var project = TestProjectFixture.Project;
        var runtimeProvider = project.CreateRuntimeProvider();

        var validator = new GDValidator();
        var options = new GDValidationOptions
        {
            RuntimeProvider = runtimeProvider,
            CheckScope = true,
            CheckTypes = true,
            CheckCalls = true,
            CheckMemberAccess = true
        };

        var reader = new GDScriptReader();
        var classDeclaration = reader.ParseFileContent(code);

        // Act
        var result = validator.Validate(classDeclaration, options);

        // Get unknown method errors for queue_free
        // GD2001 = UndefinedVariable, GD2002 = UndefinedFunction, GD4002 = MethodNotFound
        var queueFreeErrors = result.Diagnostics
            .Where(d => d.Message.Contains("queue_free") &&
                       (d.Code == GDDiagnosticCode.UndefinedVariable ||
                        d.Code == GDDiagnosticCode.UndefinedFunction ||
                        d.Code == GDDiagnosticCode.MethodNotFound))
            .ToList();

        // Assert
        Assert.AreEqual(0, queueFreeErrors.Count,
            $"queue_free() should be resolved through Node2D->CanvasItem->Node inheritance. " +
            $"Errors: {string.Join(", ", queueFreeErrors.Select(d => d.Message))}");
    }

    [TestMethod]
    public void SemanticModel_InheritanceChain_IsCorrect()
    {
        // Arrange
        var provider = new GDGodotTypesProvider();

        // Build the inheritance chain for Node2D
        var chain = new System.Collections.Generic.List<string> { "Node2D" };
        var currentType = "Node2D";

        while (!string.IsNullOrEmpty(currentType))
        {
            var baseType = provider.GetBaseType(currentType);
            if (!string.IsNullOrEmpty(baseType) && baseType != currentType)
            {
                chain.Add(baseType);
                currentType = baseType;
            }
            else
            {
                break;
            }
        }

        // Assert - Node2D -> CanvasItem -> Node -> Object
        Assert.IsTrue(chain.Contains("Node2D"), "Chain should include Node2D");
        Assert.IsTrue(chain.Contains("CanvasItem"), "Chain should include CanvasItem");
        Assert.IsTrue(chain.Contains("Node"), "Chain should include Node");
        Assert.IsTrue(chain.Contains("Object"), "Chain should include Object");

        // Verify order
        var node2dIndex = chain.IndexOf("Node2D");
        var canvasItemIndex = chain.IndexOf("CanvasItem");
        var nodeIndex = chain.IndexOf("Node");
        var objectIndex = chain.IndexOf("Object");

        Assert.IsTrue(node2dIndex < canvasItemIndex, "Node2D should come before CanvasItem");
        Assert.IsTrue(canvasItemIndex < nodeIndex, "CanvasItem should come before Node");
        Assert.IsTrue(nodeIndex < objectIndex, "Node should come before Object");
    }
}
