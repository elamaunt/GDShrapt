using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Syntax
{
    /// <summary>
    /// Tests for attribute parsing and AttributesDeclaredBefore.
    /// </summary>
    [TestClass]
    public class AttributeTests
    {
        [TestMethod]
        public void SyntaxAttribute_DeclaredBefore()
        {
            var reader = new GDScriptReader();

            var code = @"
@tool
@static_unload
class_name MyClass extends Node
@export var a = ""Hello""
@onready @export var b = ""init_value_b""";

            var @class = reader.ParseFileContent(code);

            var members = @class.Members.ToArray();

            Assert.AreEqual(9, members.Length);

            members[8].AttributesDeclaredBefore.Select(x => x.ToString()).Should().BeEquivalentTo(new[]
            {
                "@export ",
                "@onready "
            });

            members[8].AttributesDeclaredBeforeFromStartOfTheClass.Select(x => x.ToString()).Should().BeEquivalentTo(new[]
            {
                "@export ",
                "@onready ",
                "@export ",
                "@static_unload",
                "@tool"
            });

            members[5].AttributesDeclaredBeforeFromStartOfTheClass.Select(x => x.ToString()).Should().BeEquivalentTo(new[]
            {
                "@export ",
                "@static_unload",
                "@tool"
            });

            members[5].AttributesDeclaredBefore.Select(x => x.ToString()).Should().BeEquivalentTo(new[]
            {
                "@export "
            });
        }
    }
}
