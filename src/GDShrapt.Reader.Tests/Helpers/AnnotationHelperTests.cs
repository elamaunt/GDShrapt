using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for GDAnnotationHelper.
    /// </summary>
    [TestClass]
    public class AnnotationHelperTests
    {
        [TestMethod]
        public void AnnotationHelper_IsExport()
        {
            var reader = new GDScriptReader();
            var code = @"@export var health: int = 100";
            var @class = reader.ParseFileContent(code);

            var variable = @class.Members.OfType<GDVariableDeclaration>().First();
            var attribute = variable.AttributesDeclaredBefore.First().Attribute;

            attribute.IsExport().Should().BeTrue();
            attribute.IsExportAnnotation().Should().BeTrue();
            attribute.GetAnnotationName().Should().Be("export");
        }

        [TestMethod]
        public void AnnotationHelper_IsExportRange()
        {
            var reader = new GDScriptReader();
            var code = @"@export_range(0, 100) var health: int = 50";
            var @class = reader.ParseFileContent(code);

            var variable = @class.Members.OfType<GDVariableDeclaration>().First();
            var attribute = variable.AttributesDeclaredBefore.First().Attribute;

            attribute.IsExportRange().Should().BeTrue();
            attribute.IsExportAnnotation().Should().BeTrue();
            attribute.HasParameters().Should().BeTrue();
            attribute.Parameters.Count.Should().Be(2);
        }

        [TestMethod]
        public void AnnotationHelper_IsOnready()
        {
            var reader = new GDScriptReader();
            var code = @"@onready var sprite = $Sprite2D";
            var @class = reader.ParseFileContent(code);

            var variable = @class.Members.OfType<GDVariableDeclaration>().First();
            var attribute = variable.AttributesDeclaredBefore.First().Attribute;

            attribute.IsOnready().Should().BeTrue();
            attribute.IsExportAnnotation().Should().BeFalse();
        }

        [TestMethod]
        public void AnnotationHelper_IsTool()
        {
            var reader = new GDScriptReader();
            var code = @"tool
extends Node";
            var @class = reader.ParseFileContent(code);

            @class.IsTool.Should().BeTrue();
        }

        [TestMethod]
        public void AnnotationHelper_IsWarningIgnore()
        {
            var reader = new GDScriptReader();
            var code = @"@warning_ignore(""unused_variable"")
var unused = 5";
            var @class = reader.ParseFileContent(code);

            var variable = @class.Members.OfType<GDVariableDeclaration>().First();
            var attribute = variable.AttributesDeclaredBefore.First().Attribute;

            attribute.IsWarningIgnore().Should().BeTrue();
            attribute.HasParameters().Should().BeTrue();
        }

        [TestMethod]
        public void AnnotationHelper_IsRpc()
        {
            var reader = new GDScriptReader();
            var code = @"@rpc(""any_peer"", ""call_local"")
func sync_position(pos):
	position = pos";
            var @class = reader.ParseFileContent(code);

            var method = @class.Methods.First();
            var attribute = method.AttributesDeclaredBefore.First().Attribute;

            attribute.IsRpc().Should().BeTrue();
            attribute.HasParameters().Should().BeTrue();
        }

        [TestMethod]
        public void AnnotationHelper_IsExportGroup()
        {
            var reader = new GDScriptReader();
            var code = @"@export_group(""Stats"")
@export var health: int = 100";
            var @class = reader.ParseFileContent(code);

            var variable = @class.Members.OfType<GDVariableDeclaration>().First();
            var attributes = variable.AttributesDeclaredBefore.ToList();

            attributes[0].Attribute.IsExport().Should().BeTrue();
            attributes[1].Attribute.IsExportGroup().Should().BeTrue();
        }

        [TestMethod]
        public void AnnotationHelper_ParsesAbstractAnnotation()
        {
            // @abstract is a GDScript 4.x annotation for abstract methods/classes
            var reader = new GDScriptReader();
            var code = @"@abstract
func my_abstract_method() -> void:
	pass";
            var @class = reader.ParseFileContent(code);

            var method = @class.Methods.First();
            var attribute = method.AttributesDeclaredBefore.First().Attribute;

            // @abstract is parsed as a custom attribute
            attribute.Should().NotBeNull();
            attribute.GetAnnotationName().Should().Be("abstract");
        }

        [TestMethod]
        public void AnnotationHelper_ParsesWarningIgnoreStart()
        {
            // @warning_ignore_start is used for multi-line warning suppression in GDScript 4.x
            var reader = new GDScriptReader();
            var code = @"@warning_ignore_start(""unused_variable"")
var a = 1
var b = 2
@warning_ignore_restore(""unused_variable"")";
            var @class = reader.ParseFileContent(code);

            var members = @class.Members.ToList();
            members.Should().HaveCountGreaterOrEqualTo(2);

            // First variable should have @warning_ignore_start before it
            var firstVar = members.OfType<GDVariableDeclaration>().First();
            var startAttribute = firstVar.AttributesDeclaredBefore.First().Attribute;
            startAttribute.Should().NotBeNull();
            startAttribute.GetAnnotationName().Should().Be("warning_ignore_start");
            startAttribute.HasParameters().Should().BeTrue();
        }

        [TestMethod]
        public void AnnotationHelper_ParsesWarningIgnoreRestore()
        {
            // @warning_ignore_restore ends multi-line warning suppression
            var reader = new GDScriptReader();
            var code = @"@warning_ignore_start(""unused_variable"")
var a = 1
@warning_ignore_restore(""unused_variable"")
var b = 2";
            var @class = reader.ParseFileContent(code);

            // The @warning_ignore_restore should be before the second variable
            var variables = @class.Members.OfType<GDVariableDeclaration>().ToList();
            variables.Should().HaveCount(2);

            var secondVar = variables[1];
            var restoreAttribute = secondVar.AttributesDeclaredBefore.First().Attribute;
            restoreAttribute.Should().NotBeNull();
            restoreAttribute.GetAnnotationName().Should().Be("warning_ignore_restore");
        }

        [TestMethod]
        public void AnnotationHelper_AbstractOnClass()
        {
            // @abstract can also be applied to inner classes
            var reader = new GDScriptReader();
            var code = @"@abstract
class MyAbstractClass:
	func abstract_method() -> void:
		pass";
            var @class = reader.ParseFileContent(code);

            var innerClass = @class.InnerClasses.First();
            var attribute = innerClass.AttributesDeclaredBefore.First().Attribute;

            attribute.Should().NotBeNull();
            attribute.GetAnnotationName().Should().Be("abstract");
        }
    }
}
