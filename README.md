# Burning Critters Drop Cooked Meat

As the name suggests, animals that are on fire will drop cooked meats when they die.

The animal has to be burning at the moment of death to drop cooked meat.

There is no discrimination with regards to the source of the flame, so even wild (and alpha) bush devils can cook their prey.

## Installation

1. Install `BepInEx 6.0.0-pre.1`
2. Extract `jas.Dinkum.BurningCrittersDropCookedMeat.dll` to `.\BepInEx\Plugins\`

## Notice

This mod has a seemingly unavoidable side effect of causing a harmless NullReferenceException every time an animal dies while on fire.

This is because a Harmony prefix is being used to cancel a coroutine after it gets initialized.	It is safe to ignore.