extends Node
class_name AutoloadUsageTest

func use_autoload():
	var level = Global.current_level
	Global.start_game()
	var count = Global.get_player_count()
