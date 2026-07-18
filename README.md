# Region Kit

A unified dependency and object kit for Rain World region modding. Created by The Substratum Dev Team and other members of the community.

#### Table of Contents
- [The Goal](#the-goal)
- [Modules](#modules)
- [Additional features](#additional-features)
- [Credits](#credits)
- [Download](#download)
- [More Info](#more-info)


## The Goal

Our goal for RegionKit is to have a unified mod to hold objects, room effects, and region infrastructure, so that region creators only need to look for one place rather than worry about the need to use another region's code mod. 


## Modules
- AnimatedDecals
	- Adds support for videos to be used with the CustomDecal placed object.
	- Supported extension types: .asf, .avi, .dv, .m4v, .mov, .mp4, .mpg, .mpeg, .ogv, .vp8, .webm, .wmv
- AridBarrens
	- Adds the Arid Barrens Sandstorm effect, which acts as an end of cycle danger.
	- Adds the SandPuff effect, which spawns cosmetic puffs of sand occasionally around objects
- BackgroundBuilder
	- Allows custom AboveCloudsView backgrounds (and similar) to be easily constructed and loaded for a room.
	- See also: [documentation for BackgroundBuilder](./docs/BackgroundBuilder.md)
- Climbables
	- A set of climbable objects for use in regions
	- Climbable poles as dev tools objects
	- Ropes, either between two points or loose-hanging
	- Climbable arcs
- ConcealedGarden
	- Objects and effects from Concealed Garden
- CustomProjections
	- Allows custom overseer projections to be created and defined, as well as being able to modify overseer appearance and behaviors.
	- See also: [documentation for CustomProjections](./docs/CustomProjections.md)
- DevUIMisc
	- Tweaks and extensions to dev tools:
		- InsectPicker, a popup box for selecting an insect for the InsectGroup dev tools object
		- Typeable palette numbers
		- Modify file generation
		- Mod selection for settings saving
	- Also includes some custom dev tools inputs for code purposes
- EchoExtender
	- Allows adding echoes to custom regions without any coding.
	- See also: [documentation for EchoExtender](./docs/EchoExtender.md)
- Effects
	- This module contains most of the non-insect effects added by RegionKit.
- Extended Gates
	- Enables you to set additional gate pass requirements: karma above 5, mark of communication, ripple levels, and more
	- Features a set of new holograms
	- Automatically blocks off gates for regions the user doesn't have installed with a special gate type
	- See also: [documentation for ExtendedGates](./docs/ExtendedGates.md)
- FloatingDebrisNew
	- Contains all of the FloatingDebris types added by RegionKit. These are:
		- `RK Dust`: a version of the `Stardust` FloatingDebris type without the green sparkles
		- `RK White Dust`: a version of the `Gilded Wind` FloatingDebris type without the yellow sparkles
- GateCustomization
	- Allows creating multiscreen gates along with adding a whole fleet of customization options available from dev tools.
	- See also: [documentation for GateCustomization](./docs/GateCustomization.md)
- Iggy
	- A system for documenting dev tools objects and UI. Right click on an object to learn more about it.
	- Iggy can be disabled in RegionKit's Remix menu.
- IndividualPlacedObjectViewer
	- An alternative way to edit with dev tools. Press the "Switch mode" button below the Objects button in dev tools to activate.
	- Allows for selecting individual objects to view/edit at a time, or deleting. Also allows filtering by specific object types.
- Insects
	- Includes the host of new insect types added by RegionKit.
- LevelLayerFour
	- A system for including 3 more level layers in the background.
	- Used in combination with the [Community Editor](https://github.com/Rain-World-Modding/CommunityEditor)'s Level Layer Four rendering mode.
- Machinery
  	- A small set of customizable objects for adding moving parts like cogs and pistons to your levels. Can use any loaded sprites and shaders by name.
  	- NOTE: MachineryCustomizer object is used for changing sprite/container/shader settings.
  	- A general purpose power level customization system, a related interface to be used. See code: `RegionKit.Machinery.RoomPowerManager`, `RegionKit.Machinery.RoomPowerManager.IRoomPowerModifier`.
- Misc
	- Includes the miscellaneous things added or changed by RegionKit, as listed below
	- Allows previewing decals in the CustomDecal selector on hover
	- More fade palettes
	- Fade palette combiner
	- Slugcat-specific room templates
	- RainSong (see [RainSong documentation](./docs/RainSong.md)
- MultiColorSnow
	- A system for adding colored snow sources to a room, and multiple of them for that matter.
- Objects
	- This module contains most of the objects added by RegionKit
- Particles
	- A general purpose particle system, featuring:
		- Use of arbitrary sprites
		- Controlled randomization of visuals and movement
		- Modularity: combine visuals, behaviour and modifiers in any way you like.
		- Expandability:
			- Relatively simple to make user defined behaviours
			- Basework for making new types of emitters, particle classes and behaviour modifier classes
	- See also: [RegionKit particles system guide](./docs/P_GUIDE.md).
- RoomSlideShow
	- Allows adding simple animated objects to rooms (without gameplay effect).
	- See also: [documentation for RoomSlideShow](./src/Modules/RoomSlideShow/README.md).
- RoomZones
	- A system for adding zones to a room that can trigger things with code
- ShaderTools
	- This module adds depth and stencil buffers to the game's camera for use in custom shaders
- ShelterBehaviors
	- Adds placed objects to control and customize how your shelters act.
- TheMast
	- Implements all of the contents that went to the mast.
	- Adds custom wind object.
	- Adds custom pearl chain object.
	- Misc additions solely for The Mast region.

## Additional features
- Custom sprites loader
	- Allows easily loading in atlases or separate sprites without additional code, loads from assetpath `assets/regionkit` (recursively) across all mods.
- Turbo Baker
	- Easily and quickly bake or rebake any region from the Remix menu. Includes progress updates and multithreading to heavily reduce the time it takes.
- Camera angles
	- Includes a system to retrieve camera angles in rooms rendered by the Community Editor or Drizzle. Located in RegionKit.API.CameraAngles

## Credits
### Feature credits
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
	- DenseFog

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
	- ShelterBehaviors (initial implementation)
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
	- Spike object

- Bebe
	- Shortcut color

- snoodle
	- The Mast Permission

- Kaeporo
	- ARObjects Permission

- Dracentis
	- Arid Barrens Code
	- Original ColoredLightBeam

- Isbjorn52
	- Fade palette combiner
	- Gate Customization
	- Individual placed objects mode

- Xan
	- Superstructure Fuses depth fix
 	- Shader stencil buffer enabler
 
- ASlightlyOvergrownCactus
	- MossWater Unlit/RGB shaders
 	- MurkyWater shader

- Ved-s
	- Echo extender localization

- Vigaro
	- Turbo Baker permission

- Alduris
	- Butterfly insect effect
	- Zipper insect effect
	- Turbo Baker integration
	- No batfly lurk zone
	- PCPlayerSensitiveLightSource
	- Waterfall depth
	- No dropwig perch zone
	- Evil dangle fruit
	- BGFlatLight
	- BigWaterWheel
	- AdvancedShader
	- Shelter behaviors reimplementation
	- Colored SuperStructureFuses region property
	- Dust and White Dust floating debris types
	- Colored mud pit
	- Passage gate requirements integration
	- Ripple gate requirements

- LudoCrypt
	- Multicolor snow sources
	- Level layer four

- MagicaJaphet
	- Replace Corruption color effect

- k0rii
	- RainSiren and Suffocation effects
	- Passage gate requirements

- glebi574
	- Bugfixes

**Initial ExtendedGates authors**: Henpemaz (code); Mehri'Kairotep, Thalber, LB/M4rbleL1ne and Nautillo (spritework).

### Decal collection credits
- A10
- aprilistheworstmonth
- av3ryrandomperson
- Dragonly44
- glowingglassroses
- inspectnerd
- lagg
- Mold223
- Ovidia
- qtpi
- slithersss
- snoodle
- SonixYakuza
- tapok
- TotallyDutch
- Void Computer

## Download
Downloads can be found [here](https://github.com/Rain-World-Modding/RegionKit/releases/latest).

## Links

More information such as how to make use of some of the features can be found on the [Modding Wiki](https://rainworldmodding.miraheze.org/wiki/Main_Page).

Old repository can be found [here](https://github.com/DryCryCrystal/Region-Kit).
