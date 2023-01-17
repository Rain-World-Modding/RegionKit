
Programming by Henpemaz
Sprites for karma 6~10 gates by LB Gamer & Nautillo
Sprites for special requirements gates and missing minimap sprites by LB Gamer
Sprites for Alternate Art spriteset, post-downpour porting by Thalber
X Karma gate sprite by Mehri'Kairothep

ExtendedGates adds new gate possibilities to the game for use in custom regions! It lets you use karma levels above 5 for gate requiments, as well as 6 other special requirements, and an alternate art option. It also lets you use gates within a region to control progression and lets you specify that gates can be used more than once on the same cycle.

How-to:
ExtendedGates uses its own file for managing gate locks, under assetpath "world/gates/extendedlocks.txt". <!-- The gates you configure in this file shouldn't be included in the regular locks.txt (doing so makes the behavior load-order dependant :/).  -->

In this file you can specify karma values equal or above 6 or special requirements for the gate that you're configuring, and vanilla karma values can still be used (you could use just this file and leave the regular one empty/absent).

The special requirements you can use are as follows (case-insensitive):
"open", always open. Juuuust like karma 1 but looks cooler.
"10reinforced", requires karma 10 + flower effect. Probably cannot be oppened on Hunter without cheating.
"forbidden", does not open from this side :)
"comsmark", requires the player to have the Mark of Communication on their playerprogression
"uwu", uwu
"glow", requires the player to have the Neuron Glow on their playerprogression

To specify a gate as reusable, include " : Multi" (case-insensitive) at the end of the entry for that gate. This gives the gate enough fuel to be used some 20 times in a single cycle, and its refreshed if the room unloads and loads again. Electric gates' batteries turn green if you enable this. The water level for water gates stays high.

To use the alternate art for karma gates sprites, type number + "alt" (case-insensitive) with no space between instead of just the karma number. Special requirement gates do not have alternate art.

Example of extendedLocks.txt contents (you can use comments and empty lines in the extendedLocks file):
// A gate within a region that can be crossed with enough karma in one direction, and is freely crossed in the other in the same cycle.
// There may or may not be a stash in the other side, the player can get to it and come back out through the same gate without need for a shelter.
CG_OtherGate : 10 : Open : Multi

// Changes a vanilla gate so that it can only be crossed left-to-right, uses alternate art for karma 2
GATE_HI_CC : 2alt : forbidden

// a commented out gate for testing
// GATE_HI_CG : 4 : uwu

CHANGELOG:
1.0 initial release
1.1 13/06/2021 bugfix 6 karma at 5 cap; fix showing open side over karma for inregion minimap
1.2 09/01/2022 new feature glow gate; added a new special requirement for gates that requires the neuron glow to open
1.3 1/16/2023 \[thalber\] regionkit downpour port