namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDLspVersionInfoTests
{
    [TestMethod]
    public void GetVersion_ReturnsNonNullNonEmpty()
    {
        var version = GDLspVersionInfo.GetVersion();
        version.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void GetVersion_DoesNotReturnHardcoded100()
    {
        var version = GDLspVersionInfo.GetVersion();
        version.Should().NotBe("1.0.0");
    }

    [TestMethod]
    public void GetVersion_ContainsVersionNumber()
    {
        var version = GDLspVersionInfo.GetVersion();
        version.Should().MatchRegex(@"\d+\.\d+\.\d+");
    }

    [TestMethod]
    public void GetVersion_NotUnknown()
    {
        var version = GDLspVersionInfo.GetVersion();
        version.Should().NotBe("unknown");
    }
}
