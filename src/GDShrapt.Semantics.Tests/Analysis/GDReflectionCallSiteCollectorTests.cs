using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDReflectionCallSiteCollectorTests
{
    private static GDSemanticModel CreateModel(string code)
    {
        var reference = new GDScriptReference("test://virtual/reflection_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);
        scriptFile.Analyze();
        return scriptFile.SemanticModel!;
    }

    #region Existing method tests (updated for NameFilters)

    [TestMethod]
    public void SelfGetMethodList_CallMethodName_DetectsReflectionCallSite()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        call(method.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Method);
        sites[0].IsSelfCall.Should().BeTrue();
        sites[0].NameFilters.Should().BeNull();
        sites[0].CallMethod.Should().Be("call");
    }

    [TestMethod]
    public void SelfGetMethodList_WithBeginsWithFilter_DetectsPrefixFilter()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Method);
        sites[0].IsSelfCall.Should().BeTrue();
        sites[0].NameFilters.Should().NotBeNull();
        sites[0].NameFilters.Should().ContainSingle(f =>
            f.Kind == GDReflectionFilterKind.BeginsWith && f.Value == "test_");
    }

    [TestMethod]
    public void TypedReceiver_GetMethodList_DetectsReceiverType()
    {
        var model = CreateModel(@"class_name MyClass
extends Node

func _ready():
    for method in self.get_method_list():
        self.call(method.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Method);
        sites[0].ReceiverTypeName.Should().Be("MyClass");
        sites[0].IsSelfCall.Should().BeTrue();
    }

    [TestMethod]
    public void UntypedReceiver_GetMethodList_ReturnsWildcard()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        call(method.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Method);
        sites[0].ReceiverTypeName.Should().NotBeNullOrEmpty();
        sites[0].IsSelfCall.Should().BeTrue();
    }

    [TestMethod]
    public void NoGetMethodList_NoReflectionSitesDetected()
    {
        var model = CreateModel(@"extends Node

func _ready():
    pass
");

        var sites = model.GetReflectionCallSites();
        sites.Should().BeEmpty();
    }

    [TestMethod]
    public void CallDeferred_AlsoDetected()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        call_deferred(method.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Method);
        sites[0].CallMethod.Should().Be("call_deferred");
    }

    [TestMethod]
    public void DictionaryIndexer_MethodName_AlsoDetected()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        call(method[""name""])
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Method);
    }

    #endregion

    #region Matches() unit tests (updated for NameFilters)

    [TestMethod]
    public void Matches_WithBeginsWithFilter_FiltersCorrectly()
    {
        var site = new GDReflectionCallSite
        {
            NameFilters = new List<GDReflectionNameFilter>
            {
                new() { Kind = GDReflectionFilterKind.BeginsWith, Value = "test_" }
            }
        };

        site.Matches("test_something").Should().BeTrue();
        site.Matches("helper").Should().BeFalse();
        site.Matches("TEST_case").Should().BeTrue("case-insensitive matching");
    }

    [TestMethod]
    public void Matches_WithNullFilters_MatchesEverything()
    {
        var site = new GDReflectionCallSite
        {
            NameFilters = null
        };

        site.Matches("anything").Should().BeTrue();
        site.Matches("test_something").Should().BeTrue();
        site.Matches("").Should().BeTrue();
    }

    [TestMethod]
    public void Matches_WithMultipleFilters_MatchesAny()
    {
        var site = new GDReflectionCallSite
        {
            NameFilters = new List<GDReflectionNameFilter>
            {
                new() { Kind = GDReflectionFilterKind.BeginsWith, Value = "test_" },
                new() { Kind = GDReflectionFilterKind.BeginsWith, Value = "only_" }
            }
        };

        site.Matches("test_something").Should().BeTrue();
        site.Matches("only_this").Should().BeTrue();
        site.Matches("helper").Should().BeFalse();
    }

    #endregion

    #region Property tests (P1-P4)

    [TestMethod]
    public void P1_GetPropertyList_Set_DetectsPropertyReflection()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for prop in get_property_list():
        set(prop.name, null)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Property);
        sites[0].CallMethod.Should().Be("set");
        sites[0].IsSelfCall.Should().BeTrue();
    }

    [TestMethod]
    public void P2_GetPropertyList_Get_DetectsPropertyReflection()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for prop in get_property_list():
        var val = get(prop.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Property);
        sites[0].CallMethod.Should().Be("get");
    }

    [TestMethod]
    public void P3_GetPropertyList_WithBeginsWithFilter_DetectsFilter()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for prop in get_property_list():
        if prop.name.begins_with(""custom_""):
            set(prop.name, null)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Property);
        sites[0].NameFilters.Should().NotBeNull();
        sites[0].NameFilters.Should().ContainSingle(f =>
            f.Kind == GDReflectionFilterKind.BeginsWith && f.Value == "custom_");
    }

    [TestMethod]
    public void P4_GetPropertyList_TypedForLoop_DetectsPropertyReflection()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for prop: Dictionary in get_property_list():
        set(prop.name, null)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Property);
    }

    #endregion

    #region Signal tests (S1-S3)

    [TestMethod]
    public void S1_GetSignalList_EmitSignal_DetectsSignalReflection()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for sig in get_signal_list():
        emit_signal(sig.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Signal);
        sites[0].CallMethod.Should().Be("emit_signal");
        sites[0].IsSelfCall.Should().BeTrue();
    }

    [TestMethod]
    public void S2_GetSignalList_Connect_DetectsSignalReflection()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for sig in get_signal_list():
        connect(sig.name, _on_signal)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Signal);
        sites[0].CallMethod.Should().Be("connect");
    }

    [TestMethod]
    public void S3_GetSignalList_WithBeginsWithFilter_DetectsFilter()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for sig in get_signal_list():
        if sig.name.begins_with(""on_""):
            emit_signal(sig.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].Kind.Should().Be(GDReflectionKind.Signal);
        sites[0].NameFilters.Should().NotBeNull();
        sites[0].NameFilters.Should().ContainSingle(f =>
            f.Kind == GDReflectionFilterKind.BeginsWith && f.Value == "on_");
    }

    #endregion

    #region Mixed test (MIX)

    [TestMethod]
    public void MIX_AllThreeKinds_InSameFile_DetectsAll()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        call(method.name)
    for prop in get_property_list():
        set(prop.name, null)
    for sig in get_signal_list():
        emit_signal(sig.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(3);

        sites.Should().ContainSingle(s => s.Kind == GDReflectionKind.Method);
        sites.Should().ContainSingle(s => s.Kind == GDReflectionKind.Property);
        sites.Should().ContainSingle(s => s.Kind == GDReflectionKind.Signal);
    }

    #endregion

    #region Guard filter tests (F1-F7)

    [TestMethod]
    public void F1_ExactEquals_DetectsExactFilter()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        if method.name == ""exact_name"":
            call(method.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].NameFilters.Should().NotBeNull();
        sites[0].NameFilters.Should().Contain(f =>
            f.Kind == GDReflectionFilterKind.Exact && f.Value == "exact_name");

        sites[0].Matches("exact_name").Should().BeTrue();
        sites[0].Matches("other").Should().BeFalse();
    }

    [TestMethod]
    public void F2_EndsWith_DetectsEndsWithFilter()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        if method.name.ends_with(""_handler""):
            call(method.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].NameFilters.Should().NotBeNull();
        sites[0].NameFilters.Should().ContainSingle(f =>
            f.Kind == GDReflectionFilterKind.EndsWith && f.Value == "_handler");

        sites[0].Matches("click_handler").Should().BeTrue();
        sites[0].Matches("handler_click").Should().BeFalse();
    }

    [TestMethod]
    public void F3_Contains_DetectsContainsFilter()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        if method.name.contains(""test""):
            call(method.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].NameFilters.Should().NotBeNull();
        sites[0].NameFilters.Should().Contain(f =>
            f.Kind == GDReflectionFilterKind.Contains && f.Value == "test");

        sites[0].Matches("my_test_func").Should().BeTrue();
        sites[0].Matches("helper").Should().BeFalse();
    }

    [TestMethod]
    public void F4_InOperator_DetectsContainsFilter()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        if ""test"" in method.name:
            call(method.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].NameFilters.Should().NotBeNull();
        sites[0].NameFilters.Should().Contain(f =>
            f.Kind == GDReflectionFilterKind.Contains && f.Value == "test");

        sites[0].Matches("my_test_func").Should().BeTrue();
    }

    [TestMethod]
    public void F5_CompoundFilter_BeginsWithOrExact_DetectsBoth()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_"") or method.name == ""special"":
            call(method.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].NameFilters.Should().NotBeNull();
        sites[0].NameFilters!.Count.Should().BeGreaterThanOrEqualTo(2);
        sites[0].NameFilters.Should().Contain(f =>
            f.Kind == GDReflectionFilterKind.BeginsWith && f.Value == "test_");
        sites[0].NameFilters.Should().Contain(f =>
            f.Kind == GDReflectionFilterKind.Exact && f.Value == "special");

        sites[0].Matches("test_something").Should().BeTrue();
        sites[0].Matches("special").Should().BeTrue();
        sites[0].Matches("helper").Should().BeFalse();
    }

    [TestMethod]
    public void F6_NoGuard_NoFilter_MatchesAnything()
    {
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        call(method.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().HaveCount(1);
        sites[0].NameFilters.Should().BeNull();

        sites[0].Matches("anything").Should().BeTrue();
        sites[0].Matches("").Should().BeTrue();
    }

    [TestMethod]
    public void F7_NameFilter_Matches_AllKinds_CaseInsensitive()
    {
        // Unit tests for GDReflectionNameFilter.Matches() directly
        var beginsWith = new GDReflectionNameFilter { Kind = GDReflectionFilterKind.BeginsWith, Value = "test_" };
        beginsWith.Matches("test_func").Should().BeTrue();
        beginsWith.Matches("TEST_FUNC").Should().BeTrue();
        beginsWith.Matches("other").Should().BeFalse();

        var endsWith = new GDReflectionNameFilter { Kind = GDReflectionFilterKind.EndsWith, Value = "_handler" };
        endsWith.Matches("click_handler").Should().BeTrue();
        endsWith.Matches("click_HANDLER").Should().BeTrue();
        endsWith.Matches("handler_click").Should().BeFalse();

        var contains = new GDReflectionNameFilter { Kind = GDReflectionFilterKind.Contains, Value = "test" };
        contains.Matches("my_test_func").Should().BeTrue();
        contains.Matches("MY_TEST_FUNC").Should().BeTrue();
        contains.Matches("helper").Should().BeFalse();

        var exact = new GDReflectionNameFilter { Kind = GDReflectionFilterKind.Exact, Value = "exact_name" };
        exact.Matches("exact_name").Should().BeTrue();
        exact.Matches("EXACT_NAME").Should().BeTrue();
        exact.Matches("exact_name_extra").Should().BeFalse();
        exact.Matches("other").Should().BeFalse();
    }

    #endregion

    #region Kind mismatch tests

    [TestMethod]
    public void GetMethodList_WithSet_DoesNotDetect()
    {
        // set() is a property method, not valid for get_method_list()
        var model = CreateModel(@"extends Node

func _ready():
    for method in get_method_list():
        set(method.name, null)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().BeEmpty();
    }

    [TestMethod]
    public void GetPropertyList_WithCall_DoesNotDetect()
    {
        // call() is a method call, not valid for get_property_list()
        var model = CreateModel(@"extends Node

func _ready():
    for prop in get_property_list():
        call(prop.name)
");

        var sites = model.GetReflectionCallSites();
        sites.Should().BeEmpty();
    }

    #endregion
}
