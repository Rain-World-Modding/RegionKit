## Custom Projections

New custom projections can be defined through files in `Projections\RoomName_proj.txt`  
These projections are triggered in the same way as vanilla projections, using the Triggers page in devtools.  
Various settings can be defined through the _proj.txt file. These are all case insensitive and should be separated from their value by `: `

| Name | Type | Default Value | Description |
| ------------ | ------------- | ----------- | ------------- |
| `timeOnEachImage` | number | 25 | the number of ticks to display each image |
| `showTime` | number | 150 | the number of ticks to display the whole projection sequence |
| `RND_Image` | filename | RND\_PROJ | the name of the image used for random flickering, should be `Projections\<RND_Image>.png` |

Any line that is not one of these settings will be parsed as a projection image to add to the sequence. Projection image names *are* case sensitive.  
The vanilla projections are taken from `Projections\STR_PROJ.png` and their names are as follows

|  |  |  |  |  |
| ------------ | ------------- | ----------- | ------------- | ------------- |
| Dead\_Slugcat_B | Moon\_Fantasy* | Slugcat\_Eating* | Slugcat_Sleeping* | Undefined |
| Scav\_And\_Pearls | Scav\_Slugcat\_Trade | Swarmers | Moon\_And\_Swarmers | Dead\_Slugcat\_A |
| Clue\_1 | Clue\_2 | Clue\_3 | Clue\_4 | Scav\_Outpost |
| Slugcat\_3 | Slugcat\_4 | Slugcat\_5 | Slugcat\_6 | Slugcat\_7 |
| Moon\_Full\_Figure  | Moon\_Double\_Size | Moon\_Portrait | Slugcat\_1 | Slugcat\_2 |

*only have images when Rain World Remix mod is enabled, otherwise they appear the same as Undefined  

New projection images can be added by including a .png file in the `Projections` folder as well as a .txt with the same name, to define the names of each image included on the .png.  
The .png file should be a 1,000 x 1,000 image, consisting of a grid of 5 x 5 smaller images that are 200 x 200.  
The .txt file should list all of the image names reading from left to right, bottom to top.
here is an [example png](ONH_PROJ.png)  and an [example txt](ONH_PROJ.txt)  

Below are a couple example _proj.txt files

`Projections\SL_AI_proj.txt`

    timeOnEachImage: 80
    showTime: 1400
    Swarmers
    Moon_And_Swarmers
    Moon_Fantasy
    Swarmers
    Moon_And_Swarmers
    Moon_Fantasy
    Swarmers
    Moon_And_Swarmers
    Moon_Fantasy

`Projections\LF_A05_proj.txt`

    timeOnEachImage: 28
    showTime: 400
    RND_Image: ONH_RND_PROJ
    Cave_Train
    Fissure_Harvester
    Fissure_Ledge
    LF_Deer
    Cave_1
    Cave_2
    

## Properties

Many new properties can be defined in World\XX\Properties.txt to customize overseer behavior as it applies to the entire region. All property names are case sensitive and should be separated from their value by `: `


| Name | Type | Default Value | Description |
| ------------ | ------------- | ----------- | ------------- |
| `guideShelterWeight` | decimal | 1 | Multiplies the player guide's desire to show shelter direction |
| `guideBatWeight` | decimal | 1 | Multiplies the guide's desire to show swarmroom direction |
| `guideDangerousCreatureWeight` | decimal | 1 | Multiplies the guide's desire to warn of dangerous creatures |
| `guideDeliciousFoodWeight` | decimal | 1 | Multiplies the guide's desire to point to fruit |
| `guideProgressionWeight` | decimal | 1 | Multiplies the guide's desire to show progression if below 100, if above 100 it will override with the value - 100 |
| `guideDestinationRoom` | room name | none | The room the player guide should point to when showing progression |
| `guideProgressionSymbol` | atlas name | `GuidanceSlugcat` | The symbol to use for showing progression |
| `inspectorColor` | color or number | `1, 0.8, 0.3` or `1` | The color of the inspectors in the region |
| `guideColor` | color or number | `1, 0.8, 0.3` or `1` | The color of the player guide |
| `overseersColorOverride(<color>)` | decimal | 1 | The chance of the non-guide overseers changing to the defined color |

`overseersColorOverride()` should include the color to override in the parantheses. At runtime the overrides will be sorted by descending chance for each random check.  
Overseer color properties can take in ids 0-5 to choose between built-in colors.

| ID | Iterator Owner | Color | 
| ------------ | ------------- | ----------- |
| 0 | Five Pebbles | 0.44705883, 0.9019608, 0.76862746 |
| 1 | Looks to the Moon | 1, 0.8, 0.3
| 2 | No Significant Harassment | 0, 1, 0,
| 3 | Seven Red Suns | 1, 0.2, 0
| 4 | Unknown White | 0.9, 0.95, 1f
| 5 | Unknown Purple | 0.56, 0.27, 0.68

Below is an example file  

    guideShelterWeight: 1.2 //shelter suggestions will be slightly more common
    guideBatWeight: 0.8
    guideDangerousCreatureWeight: 0 //tutorial-ish behaviors will be minimized
    guideDeliciousFoodWeight: 0
    guideProgressionWeight: 101 //tendency to show progression will be locked at 1, like Shoreline
    guideDestinationRoom: GATE_FS_LF
    guideProgressionSymbol: GuidancePebbles
    guideColor: 3 //will be colored red, as the SRS overseer
    overseersColorOverride(3): 0.1 //idle SRS overseers can be found rarely
    overseersColorOverride(5): 1 //the default idle overseer will be purple
    overseersColorOverride(0.1, 0.2, 0.1): 0.4 //dark grey overseers can be found

