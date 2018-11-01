# Space Engineers Ship Management Script
Several in-game scripts for Space Engineers for automatically managing a large ship

## Features
### LCD Displays
LCD Displays can be live updated with ship statistics by adding specific tags to the LCD Panel's Custom Data. Multiple tags can be added to an LCD to display different types of information. Text will appear in the same order as the tags.

* Ship Mass (Tag: `[LCD_SHIP_MASS]`)
    * Base Mass
    * Total Mass
* Ship Speed - Simple (Tag: `[LCD_SHIP_SPEED_SIMPLE]`)
* Ship Speed (Tag: `[LCD_SHIP_SPEED]`)
    * Speed
    * Acceleration
* Altitude (Tag: `[LCD_ALTITUDE]`)
* Reactor Output (Tag: `[LCD_REACTOR]`)
* Solar Panel Output (Tag: `[LCD_SOLAR]`)
* Battery Storage (Tag: `[LCD_BATTERY]`)
* Total Power Usage (Tag: `[LCD_POWER_USAGE]`)
* Oxygen Tanks (Tag: `[LCD_OXYGEN]`)
* Hydrogen Tanks (Tag: `[LCD_HYDROGEN]`)
* Jump Drive Stats (Tag: `[LCD_JUMP]`)
    * Max Jump Distance
* Gravity Stats (Tag: `[LCD_GRAVITY]`)
    * Natural Gravity
    * Artificial Gravity
    * Total Gravity
* Cargo Containers (Tag: `[LCD_CARGO]`)
* Ore Counts (Tag: `[LCD_ORE]`)
* Material Counts (Tag: `[LCD_MATERIAL]`)

A special separator tag can be added to divide sections
* Separator (Tag: `[LCD_SEPARATOR]`)

### Auto Close Doors
Doors with the tag `[AUTO_CLOSE]` in their Custom Data will automatically close after a short delay.
* `AUTO_CLOSE_TAG` sets the tag required in each Door
* `DOOR_CLOSE_DELAY` sets the delay in milliseconds before a door is closed