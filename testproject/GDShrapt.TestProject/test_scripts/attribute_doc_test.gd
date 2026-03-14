## A test class for attribute doc comments.
extends Node
class_name AttributeDocTest

## Speed of the character.
@export var speed: float = 100.0  # 6:19-GD7022-OK

## Health points remaining.
@export var health: int = 100  # 9:20-GD7022-OK

## Reference to the health bar node.
@onready var health_bar: Node = $HealthBar

@export var no_doc_var: int = 42  # 14:24-GD7022-OK
