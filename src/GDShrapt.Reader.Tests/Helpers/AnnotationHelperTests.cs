using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Helpers
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
    }
}
