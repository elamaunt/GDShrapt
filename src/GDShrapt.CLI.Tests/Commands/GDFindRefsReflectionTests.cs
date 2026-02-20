using System.IO;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

/// <summary>
/// CLI-level end-to-end tests for reflection pattern integration in find-refs.
/// </summary>
[TestClass]
public class GDFindRefsReflectionTests
{
    // RCL-9: find-refs with reflection pattern outputs reflection info
    [TestMethod]
    public async Task FindRefs_WithReflectionPattern_OutputContainsReflectionInfo()
    {
        var code = @"extends Node
class_name ReflCLITest1

func _ready():
    for method in get_method_list():
        call(method.name)

func test_a() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("refl_cli1.gd", code));

        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDFindRefsCommand("test_a", tempPath, null, formatter, output);

            var exitCode = await command.ExecuteAsync();

            var text = output.ToString();
            // Reflection references appear as ContractString kind, shown in "Contract strings" section
            text.Should().Contain("Contract strings");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }
}
