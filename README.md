# Space Engineers Ship Management Script
Several in-game scripts for Space Engineers for automatically managing a large ship

## Features
### LCD Displays
LCD Displays can be live updated with ship statistics by adding specific tags to the LCD Panel's Custom Data. Multiple tags can be added to an LCD to display different types of information.

1. Energy (Tag: `[LCD_ENERGY]`)
    * Reactor Output
    * Solar Panel Output
    * Battery Storage
    * Total Power Usage
2. Gas (Tag: `[LCD_GAS]`)
    * Oxygen Tanks
    * Hydrogen Tanks
3. Cargo Containers (Tag: `[LCD_CARGO]`)
4. Ore Counts (Tag: `[LCD_ORE]`)
5. Material Counts (Tag: `[LCD_MATERIAL]`)

### Auto Close Doors
Doors with the tag `[AUTO_CLOSE]` in their Custom Data will automatically close after a short delay.
* `AUTO_CLOSE_TAG` sets the tag required in each Door
* `DOOR_CLOSE_DELAY` sets the delay in milliseconds before a door is closed