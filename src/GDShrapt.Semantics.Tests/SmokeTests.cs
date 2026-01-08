using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class SmokeTests
{
    [TestMethod]
    public void DefaultFileSystem_FileExists_ReturnsCorrectValue()
    {
        var fs = GDDefaultFileSystem.Instance;

        // This file should exist
        Assert.IsTrue(fs.FileExists(typeof(SmokeTests).Assembly.Location));

        // This file should not exist
        Assert.IsFalse(fs.FileExists("/nonexistent/file/path.txt"));
    }

    [TestMethod]
    public void DefaultProjectContext_GlobalizePath_ConvertsResourcePath()
    {
        var context = new GDDefaultProjectContext("/project/root");

        var result = context.GlobalizePath("res://scripts/player.gd");

        Assert.AreEqual("/project/root/scripts/player.gd", result);
    }

    [TestMethod]
    public void DefaultProjectContext_LocalizePath_ConvertsAbsolutePath()
    {
        var context = new GDDefaultProjectContext("/project/root");

        var result = context.LocalizePath("/project/root/scripts/player.gd");

        Assert.AreEqual("res://scripts/player.gd", result);
    }

    [TestMethod]
    public void NullLogger_DoesNotThrow()
    {
        var logger = GDNullLogger.Instance;

        // Should not throw
        logger.Debug("test");
        logger.Info("test");
        logger.Warning("test");
        logger.Error("test");
    }
}
