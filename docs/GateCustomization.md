# GateCustomization
GateCustomization allows you to create multiscreen gates along with adding a whole fleet of customizaton options.

## Usage
To get started navigate to the `RegionKit-GateCustomization` category in the Dev Tools object page. The settings are divided into three different Placed Objects. As with alot of things in Rain World a reload is required for a lot of things to function properly. Don't forget to save!

### CommonGateData
This Placed Object holds data that will be shared by both water and electric gates. The position of this Placed Object is very important as it controlls where the gate should be located. 

`Left Door Lit`  
`Middle Door Lit`  
`Right Door Lit`  
This controlls if the door should use the shadow part of the current palette or the lit part.

`No Left Door`  
`No Right Door`  
this controlls if you want to remove one or both of the side doors. This has basically the same functionality as CGGateCustomization

`Karma Glyph Color Override`  
`Hue`  
`Saturation`  
`Brightness`  
This controlls if you want to change the color of the karma glyph and what color it should be. 

### WaterGateData
This Placed Object holds data that is only used for water gates.

`Water`  
This controlls if the water should be visible or not.

`Bubble Effect`  
This controlls if the small bubbles that apear when a door is opening/closing should apear.

`Left Heater`  
`Right Heater`  
This controlls properties of the heaters. `Nrml` if its normal. `Brokn` makes the heater broken meaning it will not produce any heat or smoke. `Hiddn` disables the heater.

### ElectricGateData
This Placed Object holds data that is only used for electric gates.

`Battery Visible`  
This controlls if the battry should be visible or not.

`Lamp n Enabled`  
This controlls if the lamp should be enabled or not.

`Lamp Color Override`  
`Hue`  
`Saturation`  
This controlls if you want to change the color of the lamps and what color it should be.

`Lamp Color Override`  
`Hue`  
`Saturation`  
`Lightness`  
This controlls if you want to change the color of the battery and what color it should be. When the battery animates it adds a bit of randomization on top. It's also uses the palette darkness so it may be a little bit difficult getting the exact color you want.