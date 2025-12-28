using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Helpers
{
    /// <summary>
    /// Tests for GDSpecialMethodHelper.
    /// </summary>
    [TestClass]
    public class SpecialMethodHelperTests
    {
        [TestMethod]
        public void SpecialMethodHelper_IsReady()
        {
            var reader = new GDScriptReader();
            var code = @"func _ready():
	pass";
            var @class = reader.ParseFileContent(code);
            var method = @class.Methods.First();

            method.IsReady().Should().BeTrue();
            method.IsLifecycleMethod().Should().BeTrue();
            method.IsVirtualMethod().Should().BeTrue();
        }

        [TestMethod]
        public void SpecialMethodHelper_IsProcess()
        {
            var reader = new GDScriptReader();
            var code = @"func _process(delta):
	position.x += speed * delta";
            var @class = reader.ParseFileContent(code);
            var method = @class.Methods.First();

            method.IsProcess().Should().BeTrue();
            method.IsProcessMethod().Should().BeTrue();
            method.IsVirtualMethod().Should().BeTrue();
        }

        [TestMethod]
        public void SpecialMethodHelper_IsPhysicsProcess()
        {
            var reader = new GDScriptReader();
            var code = @"func _physics_process(delta):
	move_and_slide()";
            var @class = reader.ParseFileContent(code);
            var method = @class.Methods.First();

            method.IsPhysicsProcess().Should().BeTrue();
            method.IsProcessMethod().Should().BeTrue();
        }

        [TestMethod]
        public void SpecialMethodHelper_IsInput()
        {
            var reader = new GDScriptReader();
            var code = @"func _input(event):
	if event.is_action_pressed(""jump""):
		jump()";
            var @class = reader.ParseFileContent(code);
            var method = @class.Methods.First();

            method.IsInput().Should().BeTrue();
            method.IsInputMethod().Should().BeTrue();
        }

        [TestMethod]
        public void SpecialMethodHelper_IsInit()
        {
            var reader = new GDScriptReader();
            var code = @"func _init():
	health = 100";
            var @class = reader.ParseFileContent(code);
            var method = @class.Methods.First();

            method.IsInit().Should().BeTrue();
            method.IsLifecycleMethod().Should().BeTrue();
        }

        [TestMethod]
        public void SpecialMethodHelper_IsEnterTree()
        {
            var reader = new GDScriptReader();
            var code = @"func _enter_tree():
	add_to_group(""enemies"")";
            var @class = reader.ParseFileContent(code);
            var method = @class.Methods.First();

            method.IsEnterTree().Should().BeTrue();
            method.IsLifecycleMethod().Should().BeTrue();
        }

        [TestMethod]
        public void SpecialMethodHelper_RegularMethodNotVirtual()
        {
            var reader = new GDScriptReader();
            var code = @"func my_method():
	pass";
            var @class = reader.ParseFileContent(code);
            var method = @class.Methods.First();

            method.IsVirtualMethod().Should().BeFalse();
            method.IsLifecycleMethod().Should().BeFalse();
        }

        [TestMethod]
        public void SpecialMethodHelper_IsKnownVirtualMethod()
        {
            GDSpecialMethodHelper.IsKnownVirtualMethod("_ready").Should().BeTrue();
            GDSpecialMethodHelper.IsKnownVirtualMethod("_process").Should().BeTrue();
            GDSpecialMethodHelper.IsKnownVirtualMethod("_physics_process").Should().BeTrue();
            GDSpecialMethodHelper.IsKnownVirtualMethod("_input").Should().BeTrue();
            GDSpecialMethodHelper.IsKnownVirtualMethod("_init").Should().BeTrue();
            GDSpecialMethodHelper.IsKnownVirtualMethod("my_method").Should().BeFalse();
        }
    }
}
