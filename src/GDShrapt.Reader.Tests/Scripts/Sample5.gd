extends PanelContainer
class_name InventorySlot


@export var type: InventoryItem.Type


# Custom init function so that it doesn't error
func init(t: InventoryItem.Type, cms: Vector2) -> void:
	type = t
	custom_minimum_size = cms


# _at_position is not used because it doesn't matter where on the panel
# the item is dropped
func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:	
	if data is InventoryItem:
		#This is the text that displays uupon pulling an item out.
		%summary.text =( str("atk:" + str(data.physicalattack) +'\n' + data.lore))
		if type == InventoryItem.Type.MAIN:
			if get_child_count() == 0:
				return true
			else:
				# Swap two items
				return get_child(0).type == data.type
		else:
			return data.type == type
			
	
	return false


# _at_position is not used because it doesn't matter where on the panel
# the item is dropped
func _drop_data(_at_position: Vector2, data: Variant) -> void:
	if get_child_count() > 0:
		var item := get_child(0)
		if item == data: return
		remove_child(item)
		data.get_parent().add_child(item)
	data.get_parent().remove_child(data)
	add_child(data)
