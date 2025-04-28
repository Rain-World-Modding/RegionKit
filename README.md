# Region Kit

A unified dependency and object kit for Rain World region modding. Created by The Substratum Dev Team and other members of the community.

#### Table of Contents
- [The Goal](#the-goal)
- [Features](#features)
- [Credits](#credits)
- [Download](#download)
- [More Info](#more-info)


## The Goal

Our goal for region kit is to have a unified mod to hold objects, room effects, and region infrastructure, so that region creators only need to look for one place rather than worry about the need to use another region's code mod. 


## Features

### Disabled or changed during port
- \[disabled\] Arena Management
  	- Text file that can be placed inside a subfolder in the levels folder to manage custom arenas.
- \[disabled\] ConditionalEffects
- \[disabled\] FogOfWar
- \[disabled\] CustomArenaDivisions
- \[changed\] EchoExtender - configs now work by name (White/Yellow/Red)

## Active -> incomplete section

- PWLightRod
  	- RGB based SSLightRod.
- Placed Wormgrass
  	- PlacedObject for setting up wormgrass without re-exporting room geometry.
- RegionKit.Machinery
  	- A small set of customizable objects for adding moving parts like cogs and pistons to your levels. Can use any loaded sprites and shaders by name.
  	- NOTE: MachineryCustomizer object is used for changing sprite/container/shader settings.
  	- A general purpose power level customization system, a related interface to be used. See code: `RegionKit.Machinery.RoomPowerManager`, `RegionKit.Machinery.RoomPowerManager.IRoomPowerModifier`.
- Spinning Fan
  	- Animated and scalable fan.
- Echo Extender
	- Allows adding echoes to custom regions without any coding.
	- For usage see [this file.](./docs/EchoExtender.md)
- The Mast
	- Implements all of the contents that went to the mast.
	- Adds custom wind object.
	- Adds custom pearl chain object.
	- Misc additions solely for The Mast region.
- ARObjects
	- Implements contents from Aether Ridge
	- Adds a rectangle object that kills the player when entered.
- Flooded Assembly Objects
	- Ported content out of an uponcoming regionpack
	- Adds more advanced variations of CustomDecal (FreeformDecalOrSprite) and LightSource (ColouredLightSource)
- Arid Barrens
	- Adds Sandstorm Effect
- ShelterBehaviors
	- Adds placed objects to control and customize how your shelters act
- ExtendedGates
	- Enables you to set additional gate pass requirements: karma above 5, mark of communication etc
	- Features a set of new holograms
	(for instructions see [this file.](./docs/ExtendedGates.md))
- CustomSpritesLoader
	- Allows easily loading in atlases or separate sprites without additional code, loads from assetpath `assets/regionkit` (recursively).
- BackgroundBuilder
    - Allows custom AboveCloudsView backgrounds to be easily constructed and loaded for a room.
    - For instructions see [this file](./docs/BackgroundBuilder.md)
- Custom Projections
    - Allows custom overseer projections to be created and defined, as well as being able to modify overseer appearance and behaviors.
    - For instructions see [this file](./docs/CustomProjections.md)
- RoomSlideShow
	- Allows adding simple animated objects to rooms (without gameplay effect).
	- For instructions see [this file](./src/Modules/RoomSlideShow/README.md).
  
### Particle system

RegionKit provides a general purpose particle system, featuring:
  - Use of arbitrary sprites
  - Controlled randomization of visuals and movement
  - Modularity: combine visuals, behaviour and modifiers in any way you like.
  - Expandability:
	- Relatively simple to make user defined behaviours
	- Basework for making new types of emitters, particle classes and behaviour modifier classes

For more detailed instructions, see: [this file](./docs/P_GUIDE.md).

### Asset loading
RegionKit packs an instance of CSL by Henpemaz. It allows the user to easily load arbitrary sprites.
Automatically loads all atlases from assetpath `assets/regionkit` (recursively)

## Credits

- DryCryCrystal 
	- Manager and Dev Team Lead
	- Old repo maintenance
	- Legacy Component Porting

- Thalber
	- BepInEx migration
	- Downpour migration
	- RegionKit.Machinery
	- Particle Systems
	- Misc internal janitoring
	- Placed Wormgrass
	- Placed Waterfall
	- RoomSlideShow

- DeltaTime
	- Initial versions
	- PWLightRod
	- PWMalfunction
	- Arena Management

- M4rbleL1ne
	- Customisable Effect Colors
	- NoWallSlideZones
	- GlowingSwimmers
	- SunblockerFix
	- LittlePlanet
	- ProjectedCircle
	- CustomEntranceSymbols
	- ColoredCamoBeetles
	- UpsideDownWaterFall
	- MosquitoInsects
	- ClimbableWire and ClimbablePole
	- ColoredLightBeam port

- Bro
	- Concealed Garden objects port
	- Popups and Climbables port
	- SettingsSave for Specific Slugs
	- BackgroundBuilder
	- Many object fixes
	- Custom Projections

- Henpemaz
	- Placed Objects Manager Framework
	- CustomSpritesLoader
	- ShelterBehaviors
	- Original Concealed Garden code
	- Original Climbables and Popups code
	- PaletteTextInput
 	- AlphaLevelShader

- Thrithralas
	- Echo Extender
	- Flooded Assembly Ported Objects (ColouredLightSource, FreeformDecalOrSprite)
	- Vector2ArrayField for POM Framework
	- Drawable

- Slime_Cubed
	- The Mast
	- Conditional Effects
	- Fog of War Effects (Line of Sight)

- LeeMoriya
	- RainbowNoFade + ARKillRect (ARObjects)
	- Spinning Fan
	- SteamHazard and Shroud

- Bebe
	- Shortcut color

- Doggo
	- The Mast Permission

- Kaeporo
	- ARObjects Permission

- Dracentis
	- Arid Barrens Code
	- Original ColoredLightBeam

- Isbjorn52
	- Gate Customization

- Xan
	- Superstructure Fuses depth fix
 	- Shader stencil buffer enabler
 
- ASlightlyOvergrownCactus
	- MossWater Unlit/RGB shaders
 	- MurkyWater shader

- Alduris
	- Butterfly insect effect
	- Zipper insect effect

- MagicaJaphet
	- Replace Corruption color effect

- k0rii
	- RainSiren and Suffocation effects

**Initial ExtendedGates authors**: Henpemaz (code); Mehri'Kairotep, Thalber, LB/M4rbleL1ne and Nautillo (spritework).

## Download
Downloads can be found [here](https://github.com/Rain-World-Modding/RegionKit/releases/latest).

## Links

More information such as how to make use of some of the features can be found on the [Modding Wiki](https://rainworldmodding.miraheze.org/wiki/Main_Page).

Old repository can be found [here](https://github.com/DryCryCrystal/Region-Kit).
