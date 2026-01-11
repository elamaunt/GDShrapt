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
            var exportAttr = GD.Atribute.Export();
            var code = exportAttr.ToString();
            Assert.AreEqual("@export", code);
        }

        [TestMethod]
        public void BuildAttribute_WithExportRange()
        {
            var exportRangeAttr = GD.Atribute.ExportRange(GD.Expression.Number(0), GD.Expression.Number(100));
            var code = exportRangeAttr.ToString();
            Assert.IsTrue(code.Contains("@export_range"));
            Assert.IsTrue(code.Contains("0"));
            Assert.IsTrue(code.Contains("100"));
        }

        [TestMethod]
        public void BuildAttribute_WithOnready()
        {
            var onreadyAttr = GD.Atribute.Onready();
            var code = onreadyAttr.ToString();
            Assert.AreEqual("@onready", code);
        }

        [TestMethod]
        public void BuildAttribute_WithExportGroup()
        {
            var groupAttr = GD.Atribute.ExportGroup("Stats");
            var code = groupAttr.ToString();
            Assert.IsTrue(code.Contains("@export_group"));
            Assert.IsTrue(code.Contains("Stats"));
        }

        [TestMethod]
        public void BuildAttribute_WithIcon()
        {
            var iconAttr = GD.Atribute.Icon("res://icon.png");
            var code = iconAttr.ToString();
            Assert.IsTrue(code.Contains("@icon"));
            Assert.IsTrue(code.Contains("res://icon.png"));
        }

        [TestMethod]
        public void BuildAttribute_WithRpc()
        {
            var rpcAttr = GD.Atribute.Rpc(GD.Expression.String("any_peer"), GD.Expression.String("call_local"));
            var code = rpcAttr.ToString();
            Assert.IsTrue(code.Contains("@rpc"));
            Assert.IsTrue(code.Contains("any_peer"));
        }
    }
}
