class_name ReflectionTest
extends Node

signal game_started
signal health_updated(value: float)

const SIGNAL_NAME = "game_started"
const METHOD_NAME = "start"

var player_speed: float = 10.0


func start() -> void:
	emit_signal("game_started")
	emit_signal(SIGNAL_NAME)


func check_reflections() -> void:
	if has_signal("game_started"):
		pass
	if has_method("start"):
		pass
	call("start")
	call_deferred("start")
	call(METHOD_NAME)
	var speed = get("player_speed")  # 26:5-GDL201-OK
	set("player_speed", 20.0)


func create_callable() -> void:
	var cb = Callable(self, "start")  # 31:5-GDL201-OK
	var cb2 = Callable(self, METHOD_NAME)  # 32:5-GDL201-OK


func concat_cases() -> void:
	emit_signal("game" + "_started")
	call("st" + "art")
