# ExtendedGates
ExtendedGates adds new gate possibilities to the game for use in custom regions! It lets you use karma levels above 5 for gate requiments, any ripple level for gate requirements, as well as 6 other special requirements, and an alternate art option. It also lets you use gates within a region to control progression and lets you specify that gates can be used more than once on the same cycle.

Example of locks.txt contents
```
GATE_CG_OtherGate : 10 : Open : Multi
GATE_HI_CC : 2alt : Forbidden
GATE_HI_CG : 8 : uwu
```

## How to use
ExtendedGates uses the same file for managing gate locks as vanilla, under asset path "world/gates/locks.txt". Since vanilla has this file, it should be modified indirectly using a [modification file.](https://rainworldmodding.miraheze.org/wiki/Modification_Files)

### Gate requirements
Below is a table of all requirement types (case-sensitive) added by ExtendedGates:

| Requirement       | Description                                      | Remarks                                                   |
| ----------------- | ------------------------------------------------ | --------------------------------------------------------- |
| <any number 1-10> | Requires that karma level or higher to pass.     | 1-5 are carried from base game. 6-10 are ExtendedGates.   |
| Open              | Always open. Works like karma 1 but looks cooler |                                                           |
| TenReinforced     | Requires karma 10 + karma flower effect.         | Cannot be opened on Hunter without cheats.                |
| Forbidden         | Does not open from the given side.               | Equivalent to Outer Expanse gate lock, but always locked. |
| Glow              | Requires neuron glow effect.                     |                                                           |
| uwu               | uwu                                              | Requires UwU mod to open. Opens for free.                 |
| Ripple<level>     | Requires that ripple level or higher to pass.    | Works on 0.5 increments. Example: `Ripple1.5`             |

#### Alternate symbols
ExtendedGates has alternate custom symbols that can be used in place of the default symbol on the gate. They are keywords that are appended to the end of the requirement. They only work for some gate requirements.

| Keyword | Description                                                        | Supported requirements                                      |
| ------- | ------------------------------------------------------------------ | ----------------------------------------------------------- |
| alt     | Replaces the usual art below the requirement icon with custom art. | All karma levels, Open, TenReinforced, Forbidden, Glow, uwu |
| txt     | Adds text-like lines below the requirement icon.                   | All karma levels                                            |

Example:
```
GATE_SU_HI : 3alt : 2txt
```

### Tags
Tags can be used to define special attributes for the gate. Tags are appended to the locks line seperated by " : ", eg

```
GATE_DS_CG : 7 : 3 : SWAPMAPSYMBOL : multi : OELock
```

(SWAPMAPSYMBOL is a vanilla tag and must always come first when it is used)

Below is a table of all tags (case-sensitive) added by ExtendedGates:
| Tag    | Description                                                                    | Remarks                                                      |
| ------ | ------------------------------------------------------------------------------ | ------------------------------------------------------------ |
| multi  | Specifies a gate as reusable.                                                  |                                                              |
| OELock | Makes a gate Forbidden unless the requirements for unlocking OE are fulfilled. | Requires *More Slugcats Expansion*. Does nothing without it. |

#### Passage extra requirements
In addition to the normal requirements for the gates, you can additionally specify specific passages to be completed, including requiring multiple passages. These also use the tags system and should work with any passage in the game, including modded ones. These are shown as the passage's symbol placed above the gate's requirement symbol, in rows if there are multiple.

The syntax for these tags is `<side>_Passage_<passage code name>` where
- `<side>` is either `Left` or `Right` (case-sensitive)
- `<passage code name>` is the code name of the passage, case-sensitive. A table is provided below for reference.

| Code name    | Passage                 | Remarks                                                                                                 |
| ------------ | ----------------------- | ------------------------------------------------------------------------------------------------------- |
| Survivor     | The Survivor            |                                                                                                         |
| Hunter       | The Hunter              |                                                                                                         |
| Saint        | The Saint               |                                                                                                         |
| Traveller    | The Wanderer            |                                                                                                         |
| Chieftain    | The Chieftain           |                                                                                                         |
| Monk         | The Monk                |                                                                                                         |
| Outlaw       | The Outlaw              |                                                                                                         |
| DragonSlayer | The Dragon Slayer       |                                                                                                         |
| Scholar      | The Scholar             |                                                                                                         |
| Friend       | The Friend              |                                                                                                         |
| Nomad        | The Nomad               | Requires MSC                                                                                            |
| Martyr       | The Martyr              | Requires MSC                                                                                            |
| Pilgrim      | The Pilgrim             | Requires MSC                                                                                            |
| Mother       | The Mother              | Requires MSC                                                                                            |
| Gourmand     | (Gourmand's food quest) | Requires MSC + Gourmand. As it is tracked as a passage, it can be used here, although it has no symbol. |

## Code API
ExtendedGates features a public API for adding custom gate requirements from other mods, found within the `RegionKit.API.ExtendedGates` class.

## Changelog
- Version 1.0
	- initial release
- Version 1.1 (06/13/2021)
	- Bugfix 6 karma at 5 cap
	- Fix showing open side over karma for in-region minimap
- Version 1.2 (09/01/2022)
	- New feature: glow gate. Added a new special requirement for gates that requires the neuron glow to open  
- Version 1.3 (01/16/2023)
	- \[Thalber\] RegionKit Downpour port  
- Version 1.4 (07/06/2023)
	- \[Bro748\] refactored to Work™ and to easily support new requirements
- Version 1.5 (06/18/2026)
	- \[Korii\] added passage extra requirements
	- \[Alduris\] added ripple gate requirements


## Credits
- Programming by Henpemaz  
- Sprites for karma 6~10 gates by LB Gamer & Nautillo  
- Sprites for special requirements gates and missing minimap sprites by LB Gamer  
- Sprites for Alternate Art spriteset, post-downpour porting by Thalber  
- X Karma gate sprite by Mehri'Kairothep  
- Sprites for Alternative Art special requirements gates and txt Art gates by Tat011  
- Passage extra requirements implementation by Korii and Alduris
- Ripple gates implementation by Alduris
- Sprites for Ripple gates by inspectnerd
