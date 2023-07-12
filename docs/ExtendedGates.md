
Programming by Henpemaz  
Sprites for karma 6~10 gates by LB Gamer & Nautillo  
Sprites for special requirements gates and missing minimap sprites by LB Gamer  
Sprites for Alternate Art spriteset, post-downpour porting by Thalber  
X Karma gate sprite by Mehri'Kairothep  

ExtendedGates adds new gate possibilities to the game for use in custom regions! It lets you use karma levels above 5 for gate requiments, as well as 6 other special requirements, and an alternate art option. It also lets you use gates within a region to control progression and lets you specify that gates can be used more than once on the same cycle.

How-to:
ExtendedGates uses the same file for managing gate locks as vanilla, under assetpath "world/gates/locks.txt". Since vanilla has this file, it should be modified indirectly using a [modification file.](https://rainworldmodding.miraheze.org/wiki/Downpour_Reference/Modification_Files)

The special requirements you can use are as follows (case-sensitive):  
"Open", always open. Juuuust like karma 1 but looks cooler.  
"TenReinforced", requires karma 10 + flower effect. Probably cannot be oppened on Hunter without cheating.  
"Forbidden", does not open from this side :)  
"CommsMark", requires the player to have the Mark of Communication on their playerprogression  
"uwu", uwu  
"Glow", requires the player to have the Neuron Glow on their playerprogression  

Tags can be used to define special attributes for the gate. Tags are appended to the locks line seperated by " : ", eg

```
GATE_DS_CG : 7 : 3 : SWAPMAPSYMBOL : multi : OELock
```

(SWAPMAPSYMBOL is a vanilla tag and must always come first when it is used)

The "multi" tag (case-sensitive) is used to specify a gate as reusable. This gives the gate enough fuel to be used some 20 times in a single cycle, and its refreshed if the room unloads and loads again. Electric gates' batteries turn green if you enable this. The water level for water gates stays high. If SWAPMAPSYMBOL is used, multi should come after that.

The "OELock" tag (case-sensitive) is used to make a gate Forbidden unless the requirements for unlocking OE is fulfilled. This will not do anything without More Slugcats Expansion enabled.

To use the alternate art for karma gates sprites, type number + "alt" (case-sensitive) with no space between instead of just the karma number. Special requirement gates do not have alternate art.

Example of locks.txt contents<!-- (you cannot currently use comments and empty lines in the locks file)-->:  
<!--// A gate within a region that can be crossed with enough karma in one direction, and is freely crossed in the other in the same cycle.  
// There may or may not be a stash in the other side, the player can get to it and come back out through the same gate without need for a shelter.  
CG_OtherGate : 10 : Open : Multi  
// Changes a vanilla gate so that it can only be crossed left-to-right, uses alternate art for karma 2  
GATE_HI_CC : 2alt : Forbidden  

// a commented out gate for testing  
// GATE_HI_CG : 4 : uwu  -->

```
CG_OtherGate : 10 : Open : Multi
GATE_HI_CC : 2alt : Forbidden
GATE_HI_CG : 8 : uwu
```

CHANGELOG:  
1.0 initial release  
1.1 06/13/2021 bugfix 6 karma at 5 cap; fix showing open side over karma for inregion minimap  
1.2 09/01/2022 new feature glow gate; added a new special requirement for gates that requires the neuron glow to open  
1.3 01/16/2023 \[thalber\] regionkit downpour port  
1.4 07/06/2023 \[bro\] refactored to Work™ and to easily support new requirements  