extends Node
class_name AutoloadUsageTest

func use_autoload():
	var level = Global.current_level  # 5:5-GDL201-OK
	Global.start_game()
	var count = Global.get_player_count()  # 7:5-GDL201-OK
