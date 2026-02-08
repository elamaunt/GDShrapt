extends Node

## Global autoload singleton for testing autoload support.

var player_data: Dictionary = {}
var current_level: int = 0

signal game_started
signal game_ended
signal level_changed(level: int)

const VERSION: String = "1.0.0"
const MAX_PLAYERS: int = 4

func start_game() -> void:
	emit_signal("game_started")

func end_game() -> void:  # 18:1-GDL513-OK
	emit_signal("game_ended")

func set_level(level: int) -> void:  # 21:1-GDL513-OK
	current_level = level
	emit_signal("level_changed", level)

func get_player_count() -> int:  # 25:1-GDL513-OK
	return player_data.size()
