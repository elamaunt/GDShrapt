using GDShrapt.Plugin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace GDShrapt.Plugin.Tests;

[TestClass]
public class CodeAnalysesTests
{
    string _test1Code = @"extends Node2D
var hello = ""Hello""

onready var node = get_node(""Node2D"")

var s = Usage.new()

func t():
	return ""a"";

func _init(
	a = ""please""
):
	var b = ""Extract me"" + t()

	var _dict = {
		""width"": 2,
		""depth"": 2,
		""heights"": PoolRealArray([0, 0, 0, 0]),
		""min_height"": -1,
		""max_height"": 1
	}

	match b:
		0:
			pass
		var t:
			for i in range(10):
				var c = b + a + i + t
				new_method()
				print(c)

	print(""done"")
	pass

func new_method():	print(hello)

class Test extends Usage:
	func _init():
		pass

class Test2 extends ""res://Usage.gd"":
	func _init():
		pass
";

    string _test2Code = @"extends Node2D

onready var node = get_node(""Node2D"")

func t():
	return ""a"";

func _init(a = ""please""):
	var b = ""Extract me"" + t()

	match b:
		0:
			pass
		var t:
			for i in range(10):
				var c = b + a + i + t
				print(c)

	print(""done"")
";

    string _test3Code = @"
extends Node2D


class_name Usage

# Declare member variables here. Examples:
# var a = 2
# var b = ""text""


# Called when the node enters the scene tree for the first time.
func _ready():
	pass

func updateSample(obj):
	var value = obj.t()
	print(value)
";

    [TestMethod]
    public void Test1()
    {
        var reference = new GDPluginScriptReference("");
        var map = new GDScriptMap(reference);
        map.Reload(_test1Code);
    }

    [TestMethod]
    public void Test2()
    {
        var reference = new GDPluginScriptReference("");
        var map = new GDScriptMap(reference);
        map.Reload(_test2Code);
    }

    [TestMethod]
    public void Test3()
    {
        var reference = new GDPluginScriptReference("");
        var map = new GDScriptMap(reference);
        map.Reload(_test3Code);
    }

    [TestMethod]
    public void Test4()
    {
        var project = new GDProjectMap(_test1Code, _test3Code);

        var map = project.GetScriptMapByTypeName("Usage");

        // Synchronous analysis (async coordination is in UIBinding for real plugin)
        map?.BuildAnalyzerIfNeeded();
    }
}
