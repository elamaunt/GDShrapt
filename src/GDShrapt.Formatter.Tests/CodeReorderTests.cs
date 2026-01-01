using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader.Tests.Formatting
{
    /// <summary>
    /// Tests for code reordering functionality.
    /// </summary>
    [TestClass]
    public class CodeReorderTests
    {
        [TestMethod]
        public void GetCategory_ClassNameAttribute_ReturnsClassAttribute()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("class_name MyClass\n");
            var classNameAttr = tree.Members.OfType<GDClassNameAttribute>().First();

            var category = GDCodeReorderFormatRule.GetCategory(classNameAttr);

            category.Should().Be(GDMemberCategory.ClassAttribute);
        }

        [TestMethod]
        public void GetCategory_Signal_ReturnsSignal()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("signal my_signal\n");
            var signal = tree.Members.OfType<GDSignalDeclaration>().First();

            var category = GDCodeReorderFormatRule.GetCategory(signal);

            category.Should().Be(GDMemberCategory.Signal);
        }

        [TestMethod]
        public void GetCategory_Enum_ReturnsEnum()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("enum MyEnum { A, B }\n");
            var enumDecl = tree.Members.OfType<GDEnumDeclaration>().First();

            var category = GDCodeReorderFormatRule.GetCategory(enumDecl);

            category.Should().Be(GDMemberCategory.Enum);
        }

        [TestMethod]
        public void GetCategory_Constant_ReturnsConstant()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("const MY_CONST = 1\n");
            var constDecl = tree.Members.OfType<GDVariableDeclaration>().First();

            var category = GDCodeReorderFormatRule.GetCategory(constDecl);

            category.Should().Be(GDMemberCategory.Constant);
        }

        [TestMethod]
        public void GetCategory_PublicVariable_ReturnsPublicVariable()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("var my_var = 1\n");
            var varDecl = tree.Members.OfType<GDVariableDeclaration>().First();

            var category = GDCodeReorderFormatRule.GetCategory(varDecl);

            category.Should().Be(GDMemberCategory.PublicVariable);
        }

        [TestMethod]
        public void GetCategory_PrivateVariable_ReturnsPrivateVariable()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("var _private_var = 1\n");
            var varDecl = tree.Members.OfType<GDVariableDeclaration>().First();

            var category = GDCodeReorderFormatRule.GetCategory(varDecl);

            category.Should().Be(GDMemberCategory.PrivateVariable);
        }

        [TestMethod]
        public void GetCategory_PublicMethod_ReturnsPublicMethod()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("func my_method():\n\tpass\n");
            var method = tree.Members.OfType<GDMethodDeclaration>().First();

            var category = GDCodeReorderFormatRule.GetCategory(method);

            category.Should().Be(GDMemberCategory.PublicMethod);
        }

        [TestMethod]
        public void GetCategory_PrivateMethod_ReturnsPrivateMethod()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("func _private_method():\n\tpass\n");
            var method = tree.Members.OfType<GDMethodDeclaration>().First();

            var category = GDCodeReorderFormatRule.GetCategory(method);

            category.Should().Be(GDMemberCategory.PrivateMethod);
        }

        [TestMethod]
        public void GetCategory_BuiltinMethod_ReturnsBuiltinMethod()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("func _ready():\n\tpass\n");
            var method = tree.Members.OfType<GDMethodDeclaration>().First();

            var category = GDCodeReorderFormatRule.GetCategory(method);

            category.Should().Be(GDMemberCategory.BuiltinMethod);
        }

        [TestMethod]
        public void GetCategory_InnerClass_ReturnsInnerClass()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("class InnerClass:\n\tpass\n");
            var innerClass = tree.Members.OfType<GDInnerClassDeclaration>().First();

            var category = GDCodeReorderFormatRule.GetCategory(innerClass);

            category.Should().Be(GDMemberCategory.InnerClass);
        }

        [TestMethod]
        public void Formatter_ReorderCode_DisabledByDefault()
        {
            var formatter = new GDFormatter();

            formatter.Options.ReorderCode.Should().BeFalse();
        }

        [TestMethod]
        public void Formatter_ReorderCode_RuleExistsButDisabled()
        {
            var formatter = new GDFormatter();

            var rule = formatter.Rules.OfType<GDCodeReorderFormatRule>().FirstOrDefault();

            rule.Should().NotBeNull();
            rule.EnabledByDefault.Should().BeFalse();
        }

        [TestMethod]
        public void MemberOrder_Default_FollowsStyleGuide()
        {
            var options = new GDFormatterOptions();

            options.MemberOrder.Should().HaveCount(12);
            options.MemberOrder[0].Should().Be(GDMemberCategory.ClassAttribute);
            options.MemberOrder[1].Should().Be(GDMemberCategory.Signal);
            options.MemberOrder[2].Should().Be(GDMemberCategory.Enum);
            options.MemberOrder[3].Should().Be(GDMemberCategory.Constant);
            options.MemberOrder[11].Should().Be(GDMemberCategory.InnerClass);
        }

        [TestMethod]
        public void MemberOrder_CustomOrder_CanBeSet()
        {
            var options = new GDFormatterOptions
            {
                MemberOrder = new List<GDMemberCategory>
                {
                    GDMemberCategory.Signal,
                    GDMemberCategory.PublicMethod,
                    GDMemberCategory.PrivateMethod
                }
            };

            options.MemberOrder.Should().HaveCount(3);
            options.MemberOrder[0].Should().Be(GDMemberCategory.Signal);
        }
    }
}
