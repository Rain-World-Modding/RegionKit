# GateCustomization
GateCustomization allows you to create multiscreen gates along with adding a whole fleet of customizaton options.

## Usage
To get started navigate to the `RegionKit-GateCustomization` category in the Dev Tools object page. The settings are divided into three different Placed Objects. As with alot of things in Rain World a reload is required for a lot of things to function properly.

### CommonGateData
This Placed Object holds data that will be shared by both water and electric gates. **The position of this Placed Object is very important as it controls where the gate should be located.** 

<details>

`Left Door Lit`  
`Middle Door Lit`  
`Right Door Lit`  
Controls if the specified door should use the sun or shadow section of the current palette.

`No Left Door`  
`No Right Door`  
Controls if you want to remove one or both of the side doors. This has basically the same functionality as CGGateCustomization.

`Karma Glyph Color Override`  
`Hue`  
`Saturation`  
`Brightness`  
Color options for the karma glyph. 

</details>

### WaterGateData
This Placed Object holds data that is only used for water gates.

<details>

`Water`  
Controls if the water should be visible or not.

`Bubble Effect`  
Allows you to disable the small bubbles that appear when a door is opening/closing.

`Left Heater`  
`Right Heater`  
Controls properties of the heaters. `Nrml` if its normal. `Brokn` makes the heater broken, meaning it will not produce any heat or steam. `Hiddn` makes the heater not visible.

</details>

### ElectricGateData
This Placed Object holds data that is only used for electric gates.

<details>

`Battery Visible`  
Controls if the battry should be visible or not.

`Lamp n Enabled`  
Controls if lamp *n* should be enabled or not.

`Lamp Color Override`  
`Hue`  
`Saturation`  
Color options for the lamps.

`Battery Color Override`  
`Hue`  
`Saturation`  
`Lightness`  
Color options for the battery. When the battery animates it adds a bit of randomization on top. It also uses the palette darkness so it may be a little bit difficult getting the exact color you want.

</details>

### Map
If you have a multiscreen gate and look at the map you may notice that the karma symbol isn't located at the actual gate position within the room. To fix this you will need to add a modify file to **modify/world/gates/gatemapinfo.txt**. The format is as follows.
```
GATE_XX_YY,tilePosX,tilePosY
```
For the `tilePosX` and `tilePosY` the **CommonGateData** will have a little label with the corresponding x and y.

Example modify file:
```
[ADD]GATE_GW_SU,12,42
```