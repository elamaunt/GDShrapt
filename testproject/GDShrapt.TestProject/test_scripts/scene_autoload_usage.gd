extends Node

func play_transition():
	await Transition.cover(0.2)
	await Transition.dissolve(0.5)
