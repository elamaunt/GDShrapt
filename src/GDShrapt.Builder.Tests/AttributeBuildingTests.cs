using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for building attributes programmatically.
    /// </summary>
    [TestClass]
    public class AttributeBuildingTests
    {
        [TestMethod]
        public void BuildAttribute_WithExport()
        {
            var exportAttr = GD.Attribute.Export();
            var code = exportAttr.ToString();
            Assert.AreEqual("@export", code);
        }

        [TestMethod]
        public void BuildAttribute_WithExportRange()
        {
            var exportRangeAttr = GD.Attribute.ExportRange(GD.Expression.Number(0), GD.Expression.Number(100));
            var code = exportRangeAttr.ToString();
            Assert.IsTrue(code.Contains("@export_range"));
            Assert.IsTrue(code.Contains("0"));
            Assert.IsTrue(code.Contains("100"));
        }

        [TestMethod]
        public void BuildAttribute_WithOnready()
        {
            var onreadyAttr = GD.Attribute.Onready();
            var code = onreadyAttr.ToString();
            Assert.AreEqual("@onready", code);
        }

        [TestMethod]
        public void BuildAttribute_WithExportGroup()
        {
            var groupAttr = GD.Attribute.ExportGroup("Stats");
            var code = groupAttr.ToString();
            Assert.IsTrue(code.Contains("@export_group"));
            Assert.IsTrue(code.Contains("Stats"));
        }

        [TestMethod]
        public void BuildAttribute_WithIcon()
        {
            var iconAttr = GD.Attribute.Icon("res://icon.png");
            var code = iconAttr.ToString();
            Assert.IsTrue(code.Contains("@icon"));
            Assert.IsTrue(code.Contains("res://icon.png"));
        }

        [TestMethod]
        public void BuildAttribute_WithRpc()
        {
            var rpcAttr = GD.Attribute.Rpc(GD.Expression.String("any_peer"), GD.Expression.String("call_local"));
            var code = rpcAttr.ToString();
            Assert.IsTrue(code.Contains("@rpc"));
            Assert.IsTrue(code.Contains("any_peer"));
        }

        [TestMethod]
        public void BuildAttribute_WithTool()
        {
            var toolAttr = GD.Attribute.Tool();
            var code = toolAttr.ToString();
            Assert.AreEqual("tool", code);
        }

        [TestMethod]
        public void BuildAttribute_WithClassName()
        {
            var classNameAttr = GD.Attribute.ClassName("MyClass");
            var code = classNameAttr.ToString();
            Assert.IsTrue(code.Contains("class_name"));
            Assert.IsTrue(code.Contains("MyClass"));
        }

        [TestMethod]
        public void BuildAttribute_WithExtends()
        {
            var extendsAttr = GD.Attribute.Extends("Node2D");
            var code = extendsAttr.ToString();
            Assert.IsTrue(code.Contains("extends"));
            Assert.IsTrue(code.Contains("Node2D"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportRangeAndStep()
        {
            var exportRangeAttr = GD.Attribute.ExportRange(
                GD.Expression.Number(0),
                GD.Expression.Number(100),
                GD.Expression.Number(5)
            );
            var code = exportRangeAttr.ToString();
            Assert.IsTrue(code.Contains("@export_range"));
            Assert.IsTrue(code.Contains("0"));
            Assert.IsTrue(code.Contains("100"));
            Assert.IsTrue(code.Contains("5"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportEnum()
        {
            var exportEnumAttr = GD.Attribute.ExportEnum(
                GD.Expression.String("Option1"),
                GD.Expression.String("Option2"),
                GD.Expression.String("Option3")
            );
            var code = exportEnumAttr.ToString();
            Assert.IsTrue(code.Contains("@export_enum"));
            Assert.IsTrue(code.Contains("Option1"));
            Assert.IsTrue(code.Contains("Option2"));
            Assert.IsTrue(code.Contains("Option3"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportFlags()
        {
            var exportFlagsAttr = GD.Attribute.ExportFlags(
                GD.Expression.String("Flag1"),
                GD.Expression.String("Flag2"),
                GD.Expression.String("Flag3")
            );
            var code = exportFlagsAttr.ToString();
            Assert.IsTrue(code.Contains("@export_flags"));
            Assert.IsTrue(code.Contains("Flag1"));
            Assert.IsTrue(code.Contains("Flag2"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportFile()
        {
            var exportFileAttr = GD.Attribute.ExportFile("*.png");
            var code = exportFileAttr.ToString();
            Assert.IsTrue(code.Contains("@export_file"));
            Assert.IsTrue(code.Contains("*.png"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportFileNoFilter()
        {
            var exportFileAttr = GD.Attribute.ExportFile();
            var code = exportFileAttr.ToString();
            Assert.IsTrue(code.Contains("@export_file"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportDir()
        {
            var exportDirAttr = GD.Attribute.ExportDir();
            var code = exportDirAttr.ToString();
            Assert.IsTrue(code.Contains("@export_dir"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportMultiline()
        {
            var exportMultilineAttr = GD.Attribute.ExportMultiline();
            var code = exportMultilineAttr.ToString();
            Assert.IsTrue(code.Contains("@export_multiline"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportPlaceholder()
        {
            var exportPlaceholderAttr = GD.Attribute.ExportPlaceholder("Enter text here");
            var code = exportPlaceholderAttr.ToString();
            Assert.IsTrue(code.Contains("@export_placeholder"));
            Assert.IsTrue(code.Contains("Enter text here"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportColorNoAlpha()
        {
            var exportColorAttr = GD.Attribute.ExportColorNoAlpha();
            var code = exportColorAttr.ToString();
            Assert.IsTrue(code.Contains("@export_color_no_alpha"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportNodePath()
        {
            var exportNodePathAttr = GD.Attribute.ExportNodePath(
                GD.Expression.String("Node2D"),
                GD.Expression.String("Sprite2D")
            );
            var code = exportNodePathAttr.ToString();
            Assert.IsTrue(code.Contains("@export_node_path"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportSubgroup()
        {
            var exportSubgroupAttr = GD.Attribute.ExportSubgroup("Advanced", "adv_");
            var code = exportSubgroupAttr.ToString();
            Assert.IsTrue(code.Contains("@export_subgroup"));
            Assert.IsTrue(code.Contains("Advanced"));
            Assert.IsTrue(code.Contains("adv_"));
        }

        [TestMethod]
        public void BuildAttribute_WithExportCategory()
        {
            var exportCategoryAttr = GD.Attribute.ExportCategory("Physics");
            var code = exportCategoryAttr.ToString();
            Assert.IsTrue(code.Contains("@export_category"));
            Assert.IsTrue(code.Contains("Physics"));
        }

        [TestMethod]
        public void BuildAttribute_WithWarningIgnore()
        {
            var warningIgnoreAttr = GD.Attribute.WarningIgnore("unused_parameter", "shadowed_variable");
            var code = warningIgnoreAttr.ToString();
            Assert.IsTrue(code.Contains("@warning_ignore"));
            Assert.IsTrue(code.Contains("unused_parameter"));
            Assert.IsTrue(code.Contains("shadowed_variable"));
        }

        [TestMethod]
        public void BuildAttribute_WithStaticUnload()
        {
            var staticUnloadAttr = GD.Attribute.StaticUnload();
            var code = staticUnloadAttr.ToString();
            Assert.IsTrue(code.Contains("@static_unload"));
        }

        [TestMethod]
        public void BuildAttribute_WithAbstract()
        {
            var abstractAttr = GD.Attribute.Abstract();
            var code = abstractAttr.ToString();
            Assert.IsTrue(code.Contains("@abstract"));
        }

        [TestMethod]
        public void BuildAttribute_CustomWithName()
        {
            var customAttr = GD.Attribute.Custom("my_custom_attribute");
            var code = customAttr.ToString();
            Assert.IsTrue(code.Contains("@my_custom_attribute"));
        }

        [TestMethod]
        public void BuildAttribute_CustomWithParameters()
        {
            var customAttr = GD.Attribute.Custom("custom_attr",
                GD.Expression.String("param1"),
                GD.Expression.Number(42)
            );
            var code = customAttr.ToString();
            Assert.IsTrue(code.Contains("@custom_attr"));
            Assert.IsTrue(code.Contains("param1"));
            Assert.IsTrue(code.Contains("42"));
        }

        [TestMethod]
        public void BuildAttribute_ExportGroupWithPrefix()
        {
            var exportGroupAttr = GD.Attribute.ExportGroup("Weapons", "weapon_");
            var code = exportGroupAttr.ToString();
            Assert.IsTrue(code.Contains("@export_group"));
            Assert.IsTrue(code.Contains("Weapons"));
            Assert.IsTrue(code.Contains("weapon_"));
        }
    }
}
