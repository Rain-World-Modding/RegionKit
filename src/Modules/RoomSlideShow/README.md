## RoomSlideShow animation system

This module allows you to create animated elements in rain world rooms with text files.

Every animation consists of exactly 1 sprite at a time. You can create a playback of sprite switching to different element, changing color or moving around.

Example sytntax:

```
SHADER Basic //this sets the shader of the sprite
DELAY 40 //this sets sprite duration/delay in ticks. game runs at 40 TPS
//this is a comment. comments can start at any point in the line with a double slash, and you can put anything inside a comment
INTERPOLATE Linear [XY] // This sets channels X and Y to use Linear interpolator.
INTERPOLATE Linear [RGBA] // This sets channels R, G, B and A to use Linear interpolator.
CONTAINER Foreground // This sets the sprite's Container.

START [RB]=0 //This sets channel R and B's values to 0. This is a START line,
LizardHead0.1, 60 //this is a frame 
Circle20 //this
LizardHead0.2, 60; [R]=1
Circle20
LizardHead0.1, 60; [B]=1
LOOP [RGB]=0.5
```
### How to start

1. Add a `SlideShow` or `SlideShowRect` placed object to your room.
2. Navigate to assetpath `assets/regionkit/slideshows` and create a `.txt` file with your playback.
3. Enter the file's name (without extension) into the slideshow placed object Id field.

You'll need to reload the game so that asset system discovers the new file.

Playback files are hot reloaded; if you edit and save your file, you can see the changes by exiting to menu and starting the session again OR by exiting the room with active slideshow and reloading it with devtools Q.

### Channels

"Channels" are values of the sprite you can manipulate over time. You change values of channels by setting **keyframes**. Channels' values can change independently in parallel.

There are following channels:

- `R`, `G`, `B`, `A` - Reg, Green, Blue and Alpha of the sprite's color. All values must be between 0 and 1.
- `X` and `Y` - Sprite offset. Value can be anything. Measure is pixels; 20 pixels per tile.
- `W` and `H` - Sprite stretch. ONLY WORKS ON `SlideShowRect`! These are multipliers for sprite's width and height.
- `T` - Sprite rotation (degrees). ONLY WORKS ON `SlideShowRect`!

### Properties

There are other sprite properties that can not be interpolated but are instead changed at a single point in time:

- `SHADER` lines set the shader used by the sprite. If not specified, it's `Basic`.
- `CONTAINER` lines set which part of the room scene the sprite will be in. If not specified, it's `Foreground`.
- `DELAY` lines set default frame delay. If not specified, it's 40. See **playback** section.
- `INTERPOLATE` lines set active interpolators for one or more channels. See **interpolators** section.

All these can be added between or before frames.

### Keyframes

Keyframes are points for which you set channel values. Keyframes are attached to normal frames, or to the start or end of playback. Between keyframes values are interpolated (see **interpolators**).

Keyframes are written as `[ABC]=number` where `A`, `B` and `C` are any channel codes (there can be any number of channel codes in each block), and `number` is a whole or decimal number.

When adding keyframes to a frame, you mark start of keyframes section with a semicolon:

```
spritename1; [RGB]=0.5, [A]=1, [XY]=100
```

When adding keyframes to start or end of the playback, you do not need semicolons:

```
START [R]=1
//some frames here
LOOP [R]=0
```

Important note: keyframes are attached to the BEGINNING of a frame.

### Interpolators

Interpolators are a settings that change how a channel's value moves from starting keyframe to the next keyframe.

There are following interpolators at the moment:

- `No` - Value is a hard switch
- `Linear` - Value slides between two points linearly

### Playback

Frames are played linearly. DELAY line sets default delay; individual frames can also override the delay.

```
DELAY 40 //1 second delay
START
sprite1 //plays for 1 second
sprite2, 60 //this overrides the delay, plays for 1.5 seconds
LOOP
```

Keyframes attached to BEGINNING of frames. This means that in the following playback:

```
DELAY 40 //each frame plays for 1 second
START [X]=-200
sprite1
sprite2 [X]=200
sprite3
sprite4

LOOP [X]=-200
```

the sprite will move left to right during the first 1 of second of playback, and then move right to left during the remaining 3 seconds.

If you want for a keyframe to occur in the middle of a sprite being displayed, split your frame into two.

Playback can be ended with either `LOOP` or `END`. Both can have keyframes.