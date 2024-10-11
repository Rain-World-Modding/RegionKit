Different backgrounds can be selected for a room using the Backgrounds tab in devtools  
the AboveCloudsView room effect *must* be added to the room for the background to load the first time  

new background files go in **Assets\RegionKit\Backgrounds\\**  
and they are .txt files  
eg, **mymod\Assets\RegionKit\Backgrounds\MyBackground.txt**

they can use slugcat conditional syntax for any line  
`(X-Saint,Rivulet)LINE`  
will make that line appear for everybody but Saint and Rivulet

You can find an example file with comments [here](BackgroundBuilderExample.txt), or another example with all the defaults [here.](BackgroundBuilderExample2.txt)

## General Settings

`UNLISTED`  
hides the background from the menu  
must be the first line

`PROTECTED`  
disallows users from editing the background

`Type:`  
the type of the background
currently the types are AboveCloudsView & RoofTopView

`OffsetX:`  
`OffsetY:`  
moves all elements over by the amount specified  
This is mainly used for changing positions relative to a parent

`Parent:`  
specify another background to inherit its settings
every setting can be overridden



## AboveCloudsView Settings

`startAltitude:` (default 20000)  
the map height where clouds show up

`endAltitude:` (default 31400)  
the map height where a specific altitude is reached  
going higher is possible but it often starts looking bad

`cloudsStartDepth:` (default 5)  
the depth of the closest cloud

`cloudsEndDepth:` (default 40)  
the depth of the furthest Close Cloud  
and also the start of the Distant Clouds

`distantCloudsEndDepth:` (default 200)  
the depth of the furthest Distant Cloud

`cloudsCount:` (default 7)  
the number of Close Clouds

`distantCloudsCount:` (default 11)  
the number of Distant Clouds

`curveCloudDepth:` (default 1)  
changes Distant Cloud depth from linear to exponential
a higher number will pull more Distant Clouds forward

`overrideYStart:` (default -40 * cloudsEndDepth)  
the Y position of the closest Distant Cloud

`overrideYEnd:` (default 0)  
the Y position of the furthest Distant Cloud


## RoofTopView Settings  
`floorLevel:` (default 26)  
determines the floor height elements are set relative to

`origin:` (default Null)  
a coordinate (x,y) that if defined,  
will adjust the background position relative to the room position,  
like how AboveCloudsView does  
example - *origin: 0,0*

`rubbleCount:` (default 16)  
the number of rubble layers

`rubbleStartDepth:` (default 1.5)  
the depth of the closest rubble layer

`rubbleEndDepth:` (default 8)  
the depth of the furthest rubble layer  

`curveRubbleDepth:` (default 1.5)  
sets how exponential the rubble layers' spread will be
a higher number will pull more rubble layers forward


## AboveCloudsView and RoofTopView Settings  
(the following work with either background type)  

`DefaultContainer:` (default Water)  
which container all background elements should be placed in

`daySky:` (default AtC_Sky)  
the image to use as the background during day

`duskSky:` (default AtC_duskSky)  
the image to use as the background during dusk

`nightSky:` (default AtC_nightSky)  
the image to use as the background during night

`atmosphereColor:` (default 293C51)  
hex color distant elements fade to

`duskAtmosphereColor:` (default 845368 or C05F5F for Rivulet)  
atmosphereColor at dusk

`nightAtmosphereColor:` (default 0C0D11)  
atmosphereColor at night

`multiplyColor:` (default FFFFFF)  
hex color multiplier for DistantBuildings

`duskMultiplyColor:` (default FFC977)  
multiplyColor at dusk

`nightMultiplyColor:` (default 142436)  
multiplyColor at night


## Background Scene Elements

each type of background scene element has multiple values  
the type of element will be defined on the left of :  
and each value will be separated by ,

`DistantBuilding: AssetName, X, Y, Depth, AtmoDepthAdd`  
example: *DistantBuilding: AtC_FivePebbles, 150, -200, 900, -100*

DistantBuilding is the basic element used by AboveCloudsView  
it is a sprite positioned in a 3D space,  
and AtmoDepthAdd is the amount of atmosphereColor it should pick up  
DistantBuilding works with either AboveCloudsView and RoofTopView

`DistantLightning: AssetName, X, Y, Depth, MinusDepthForLayering`  
example: *DistantLightning: AtC_FivePebblesLight, 70, -340, 30, 370*

nearly the same as DistantBuilding, but it's an ocassional lightning flash  
MinusDepthForLayering will treat the lightning as being further back  
when determining its position, while it'll still be drawn at its real depth  
AboveCloudsView only

`FlyingCloud: X, Y, Depth, Flattened, Alpha, ShaderInputColor`  
example: *FlyingCloud: 0, 75, 355, 0.35, 0.5, 0.9*

Flattened is the Y scale,  
and ShaderInputColor is how much of atmosphereColor the cloud picks up  
AboveCloudsView only

`Floor: X, Y, FromDepth, ToDepth`  
example: *Floor: floor, 0, 0, 1, 12*

Floor is positioned relative to the floorLevel,  
so most of the time its coordinates should be 0, 0  
ToDepth is used as the draw order, while FromDepth and ToDepth  
resize the sprite to make it look like it's going between those depths

`Building: AssetName, X, Y, Depth, Scale`  
example: *Building: city2, 150, -95220, 420.5, 2*  

Similar to the DistantBuilding, but uses a shader to automatically create the blocks  
Scale affects the size of the blocks in the shader

`Smoke: X, Y, Depth, Flattened, Alpha, shaderInputColor, shaderType`  
example: *Smoke: 0, 586, 7, 2.5, 0.1, 0.8, False*

Smoke is effectively a cloud with a different sprite  
Flattened is Y scale,  
shaderInputColor is the amount of atmosphereColor the smoke picks up,  
shaderType determines which shader to use. True = "Dust", False = "CloudDistant"


## Background Element Tags  

Tags can be appended to a background element line to modify it  
Each new tag is separated by ` : ` and their value divided from their name with `|`
example: *Building: city2, 150, -95220, 420.5, 2 : scale|5 : anchor|0.5,-0.02*

`scale`  
example: *scale|5*

Determines the scale of the sprite  
This affects some objects slightly differently than their built-in scale parameter

`anchor`  
example: *anchor|0.5,-0.02*

Determines the anchor position for sprites when positioning  
Used by Metropolis to have building sprites move relative to their base

`container`  
example: *container|Foreground*

Determines the container for this individual element  
Useful for avoiding objects from removing the visuals of certain devtools objects

`spriteName`  
example: *spriteName|clouds3*

Determines the sprite to use  
Only applies to the Smoke object


prepend `REMOVE_` to the beginning of any element to remove it  
used for altering parents  
example: *REMOVE_DistantBuilding: AtC_Structure1, -520, -85, 160, -20*
