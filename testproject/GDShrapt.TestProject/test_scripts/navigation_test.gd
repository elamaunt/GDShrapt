extends TextureRect
class_name NavigationTest

## Test script for go-to-definition and hover navigation features.

const SCENE = preload("res://test_scenes/player.tscn")
const SCENE_RELATIVE = preload("../test_scenes/main.tscn")
@icon("res://icon.png")
var scene_path: String = "res://test_scenes/main.tscn"

var enemies: Array[Node2D] = []

func _ready() -> void:
	var clamped = clampf(1.5, 0.0, 1.0)
	var instance = SCENE.instantiate()
	texture = null
	print(clamped)
	print(instance)

func test_builtin_functions() -> void:
	var a = clampf(1.0, 0.0, 2.0)
	var b = absf(-5.0)
	var c = lerp(0.0, 1.0, 0.5)
	var d = str(42)
	print(a, b, c, d)
