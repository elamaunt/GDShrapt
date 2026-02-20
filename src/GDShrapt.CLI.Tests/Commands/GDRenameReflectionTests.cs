using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

/// <summary>
/// CLI-level end-to-end tests for reflection warnings in rename command.
/// </summary>
[TestClass]
public class GDRenameReflectionTests
{
    // RCL-10: rename --dry-run with reflection pattern outputs warning
    [TestMethod]
    public async Task Rename_DryRun_WithReflection_OutputShowsWarning()
    {
        var code = @"extends Node
class_name ReflCLITest2

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func test_a() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("refl_cli2.gd", code));

        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDRenameCommand("test_a", "renamed_func", tempPath, null, formatter, output, dryRun: true);

            var exitCode = await command.ExecuteAsync();

            var text = output.ToString();
            text.Should().Contain("Reflection pattern");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }
}
