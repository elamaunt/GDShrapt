class_name ChooseDeck
extends Control

@export var deckEntryScene: PackedScene
@export var deckBuilderScene: PackedScene

signal deck_choosen(deck: int)

func setup(decks: Array[DeckData]):
	for deckVar in decks:
		var deck := deckVar as DeckData
		var deckEntry = deckEntryScene.instantiate() as DeckEntry
		$FlowContainer.add_child(deckEntry)
		deckEntry.setup(deck.Name, deck.Id, self)

func choose_deck() -> int:
	visible = true
	var deckId = await deck_choosen
	visible = false
	return deckId
	
func new_deck() -> void:
	SceneLoader.goto_deckbuilder(true)