# Echo Extender Usage Guide
This guide will show off the process of creating an echo step-by-step and tell the user how to modify it to their will.
### Step 1 - Placing the Echo
To place the echo, enter the game with your mod enabled, and navigate to the room you want to spawn the echo in. Place an `EEGhostSpot` placable object there. This object functions the same as a regular `GhostSpot` but it will not crash the game if the mod is disabled, unlike `GhostSpot`s.
### Step 2 - Writing Dialogue (Required)
In order to give the echo dialogue, navigate to the region folder where you want the echo to spawn and place an `echoConv.txt` file right next to your `world_XX.txt` file. Be sure to store backups of your echo dialogue as this file will get encrypted, much like data pearls.

Writing echo dialogue is essentially the same as writing writing lore pearl text. Dialogue is split into dialogue boxes - which are seperated by an empty line in the dialogue file - and these boxes can be split into two lines - which can be seperated using the `<LINE>` directive in the dialogue file.

Dialogue boxes can be made conditional (making it only appear for a certain difficulty) by adding brackets in front of the dialogue line and adding the required difficulty for those dialogue to appear (ex.: `(Yellow)Hello World` will only appear for monk). For further details see [the example dialogue file.](echoConv.txt)
### Step 3 - Modifying Echo Settings
SIDENOTE: As of 1.0 the settings file is necessary to specify room

In order to modify the settings of the echo, create an `echoSettings.txt` file next to the conversation file. You can use [this](echoSettings.txt) example file which contains the default values and then modify them as you wish. If a setting is not present in this file, it will default to the ones in this settings file. Here are the different settings, the type of values they require and what they do:
| Setting Name | Required Type | Description | Default Value |
| ------------ | ------------- | ----------- | ------------- |
| `difficulties` | Slugcat names seperated by commas | Defines what difficulties the echo will spawn on | White, Yellow, Red, Rivulet, Artificer, Saint, Gourmand, Slugpup |
| `priming` | True/False | Sets whether the player must visit the location of the echo first before it will spawn. When priming, other conditions are ignored | False for Hunter, True for everyone else |
| `size` | Number | Sets the size factor of the echo | 1 |
| `minkarma ` | Whole Number | The minimum karma required for the echo to spawn (lowest = 1). If set to -1, this value will be relative to the karma cap of the player. See [the wiki](https://rainworld.fandom.com/wiki/Echo) for more details. | -1 (dynamic) |
| `minkarmacap` | Whole Number | The minimum karma cap required for the echo to spawn (staring karma cap is 5 and it increases by 2 every echo up to 10) | 1 |
| `radius` | Decimal Number | The effect size of the echo measured in rooms (approximately) | 4 |
| `echosong` | Text | The name of the track that will play when the echo is nearby. Alternatively a region code can be specified to use vanilla echo tracks (ex.: `SH` or `CC`). There exists an unused echo track which can be used by settings this value to `UNUSED` | CC |
| `room` | Text | **THIS IS REQUIRED!!!** Sets the room the echo spawns in (ex.: XX_A01) | - |
| `defaultflip` | Decimal between -1 and 1 | Rotates the echo relative to the camera (0 = facing towards the camera, 1 = to the right?, -1 = to the left?) | 1 |

These settings can be prefixed similarly to dialogue boxes to provide conditional settings, allowing users to make any of these settings (including echo rooms) difficulty specific.

Any setting that isn't in the settings file will default to its value in the **Default Value** column.
