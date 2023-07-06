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

`daySky:` (default AtC_Sky)  
the image to use as the background during day

`duskSky:` (default AtC_duskSky)  
the image to use as the background during dusk

`nightSky:` (default AtC_nightSky)  
the image to use as the background during night

`atmosphereColor:` (default 293C51)  
hex color of the clouds

## Background Scene Elements

each type of background scene element has multiple values  
the type of element will be defined on the left of :  
and each value will be separated by ,

`DistantBuilding: AssetName, X, Y, Depth, AtmoDepthAdd`  
example: *DistantBuilding: AtC_FivePebbles, 150, -200, 900, -100*

DistantBuilding is the basic element used by AboveCloudsView  
it is a sprite positioned in a 3D space,  
and AtmoDepthAdd is the amount of shader color it should pick up

`DistantLightning: AssetName, X, Y, Depth, MinusDepthForLayering`  
example: *DistantLightning: AtC_FivePebblesLight, 70, -340, 30, 370*

nearly the same as DistantBuilding, but it's an ocassional lightning flash  
MinusDepthForLayering will treat the lightning as being further back  
when determining its position, while it'll still be drawn at its real depth

`FlyingCloud: X, Y, Depth, Flattened, Alpha, ShaderInputColor`  
example: *FlyingCloud: 0, 75, 355, 0.35, 0.5, 0.9*

I honestly don't know what half this stuff is for,  
I just copy\pasted it from the code


append `REMOVE_` to the beginning of any element to remove it  
used for altering parents  
example: *REMOVE_DistantBuilding: AtC_Structure1, -520, -85, 160, -20*