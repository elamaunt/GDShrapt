using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Generates TYPE_SYSTEM_COMPREHENSIVE.txt — a combined read-only view of all type system aspects.
/// This test always passes (no verification). The output is for human inspection only.
/// </summary>
[TestClass]
[TestCategory("ManualVerification")]
public class ComprehensiveOutputTest
{
    private static string GetSolutionRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "GDShrapt.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Could not find solution root");
    }

    private static string OutputPath => Path.Combine(GetSolutionRoot(), "TYPE_SYSTEM_COMPREHENSIVE.txt");

    [TestMethod]
    public void GenerateComprehensiveTypeSystemReport()
    {
        var project = TestProjectFixture.Project;
        var projectPath = TestProjectFixture.ProjectPath;

        var generator = new ComprehensiveOutputGenerator();
        generator.GenerateOutput(project, OutputPath, projectPath);

        // Verify file was created and has content
        Assert.IsTrue(File.Exists(OutputPath), "TYPE_SYSTEM_COMPREHENSIVE.txt should be generated");

        var content = File.ReadAllText(OutputPath);
        Assert.IsTrue(content.Length > 100, "Comprehensive report should have meaningful content");

        Console.WriteLine($"Generated TYPE_SYSTEM_COMPREHENSIVE.txt ({content.Length:N0} chars)");
    }
}
