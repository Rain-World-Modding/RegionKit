# REGIONKIT.PARTICLES user guide

## PART 1: I am a mere region builder, how do i use what's already provided

Preexisting placed objects include:

### Particle systems:

***Pre-provided objects:***

  - `RectParticleSpawner` : places particles in a defined rectangular area
  - `OffscreenParticleSpawner` : Spawns particles behind the geometry edge
  - `WholeScreenSpawner` : Spawns particles throughout the whole room
  
***Particle system common settings:***

  - `Warmup on room load`: Whether the particle system should request additional loading time in order to quickly spread around particles and make it look like the thing's been running the whole time and not only after the player's presence caused room to realize. This is most useful for emitters with high lifetime and low speed (for example, when you want to imitate snow)
  - `cooldown min`, `cooldown max`: how many frames it will take between spawning particles.
  - `Fade-in frames`, `Fade-in fluke`, `Lifetime`, `Lifetime fluke`, `Fade-out frames`, `Fade-out fluke`: these determine how fast particles fade into existence, how long they live and how long they take to dissolve again. Game runs at 40 physics frames per second. "Fluke" values act like randomization borders: for example, if you have `Fade-in` of 20 and `Fade-in fluke` of 5, produced particles will have their fade in length anywhere between 15 and 25, randomly chosen.
  - Unlabeled line handle - determines flight direction. `Direction fluke` is starting angle randomization border, in degrees.
  - `Speed`, `Speed fluke` - default speed settings, native pixels per frame. For reference: a tile is 20x20 px.

### Visuals:

  `ParticleVisualCustomizer` is used to determine how particles will look. Place one in a room and drag its secondary handle so that main handle of your particle spawner is inside the circle.
  
  When several visualCustomizers have one particle spawner in their area effect, this spawner will pull visuals for a random one for every new particle spawned. This means that, for example, when imitating snow, you can have several types of snow flakes using only one spawner object.

***Visuals customizer settings:***

  - `Sprite Color`, `Sprite Color fluke`: randomization borders for sprite tint. Hex. Deviations are set per-channel: for example. if you have base color `#AAAAAA` and color fluke `#220000`, produced colors will range from `#88AAAA` to `#CCAAAA`.
  - `Light color`, `Light color fluke`: same but for particle lights.
  - `Light radius min`, `Light radius max` and according flukes: min is light source size at the start and end of lifetime, during fade-in the size gradually scales to `Light radius max`, during fade-out it gradually scales back. Technically you can have `lightradmin` be larger than `lightradmax`.
  - `LightIntensity`, `Light Intensity fluke` - peak light source alpha.
  - `Flat light` - whether light will be flat.
  - `Atlas element` - name of the sprite to use. Defaults to "SkyDandelion".
  - `Shader` - shader to be applied to the sprite.
  - `Container` - Room camera container code to place your sprite into.
  
### Behaviour modifiers:

These allow changing how a particle lives and behaves in arbitrary ways. To apply to a spawner systems, place them like visuals customizer.

Pre-provided objects:
  - `ParticleWaviness`: makes particles fly in a sinal pattern
  - `ParticleSpin` : Makes particle sprites spin
  - `GenericPBMDispender`: Can apply a variety of effects that don't require additional setup data via string keys. Comes with keys:
    - `affliction` and `antibody`: code examples. Janky, using not recommended.
    - `avoidwater`: makes particle stay afloat.
    - `wallcollision`: prevents particle from entering walls (no proper speed changes).
    - `sticktowalls`: makes the particle stick to a solid surface it hits and stay there to the end of its lifetime.
  
  Pretty easy to expand with custom code.
  
  Settings include: apply order (the higher the number, the later will effect apply on the stack); key field to get new effect with.

## PART 2: expand this

 This section reasonably assumes you have basic c# knowledge.
  Abbreviations used:
  - `UAD`: UpdatableAndDeletable

Related code is located in namespace `RegionKit.Particles.`

Setup info provided below:

**GenericParticle**: 
 UAD, base working unit. Instantiated by `RoomParticleSystem`s. Can receive custom `PBehaviourModifier`s. These can use the four provided events to attach arbitrary code to be executed on creation, update pre-move, update post-move and destruction.

Manages: process of being pretty.

**RoomParticleSystem**:
 UAD instantiated via PlacedObjectsManager. Has an alternative constructor to instantiate it manually with position and `ManagedData` override object. Can be derived from.
Manages: particle creation.

**ParticleSystemData**:
 PlacedObjectsManager.ManagedData descendant. Expected and required for RoomParticleSystem to work. Provides movement data packages and tile fields for `RoomParticleSystem`s to use.

Manages: (de)ser, carrying settings.

**PBehaviourModule**: 
 slap-on modifier for particles. Instantiated by `ParticleBehaviourProvider`s, passed to `RoomParticleSystem`s and then applied to newly created `GenericParticle`s.

**ParticleBehaviourProvider**:
 dispenses new PBehaviourModules. Derive from this if you need to create new modules with special creation data; use `ParticleBehaviourProvider.PlainModuleRegister` static methods to register new particle modifer types that can be instantiated parameterless.

**Computational scores**:

 Warmup phase uses "compute scores" to try balancing how many spinup frames to request when room is being loaded. To see full algorithm, see main constructor for RoomParticleSystem. If your PBehaviourModule or GenericParticle descendant can take a lot of processing cycles per update called when enabled, please increase the scores. There is no clear guidelines on how exactly to set it up as of now. However, several global settings are currently exposed (see static fields in RoomParticleSystem).