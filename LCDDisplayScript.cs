// LCD Display Script //

// SETTINGS //

// The text LCD names must contain to be used
readonly static string LCD_MASS_TAG = "[LCD_SHIP_MASS]";
readonly static string LCD_SPEED_SIMPLE_TAG = "[LCD_SHIP_SPEED_SIMPLE]";
readonly static string LCD_SPEED_TAG = "[LCD_SHIP_SPEED]";
readonly static string LCD_ALTITUDE_TAG = "[LCD_ALTITUDE]";
readonly static string LCD_SOLAR_TAG = "[LCD_SOLAR]";
readonly static string LCD_REACTOR_TAG = "[LCD_REACTOR]";
readonly static string LCD_BATTERY_TAG = "[LCD_BATTERY]";
readonly static string LCD_POWER_TAG = "[LCD_POWER_USAGE]";
readonly static string LCD_OXYGEN_TAG = "[LCD_OXYGEN]";
readonly static string LCD_HYDROGEN_TAG = "[LCD_HYDROGEN]";
readonly static string LCD_JUMP_TAG = "[LCD_JUMP]";
readonly static string LCD_GRAVITY_TAG = "[LCD_GRAVITY]";

readonly static string LCD_CARGO_TAG = "[LCD_CARGO]";
readonly static string LCD_ORE_TAG = "[LCD_ORE]";
readonly static string LCD_MATERIAL_TAG = "[LCD_MATERIAL]";

readonly static string LCD_SEPARATOR_TAG = "[LCD_SEPARATOR]";


// When true, will include blocks that are incomplete
readonly static bool IGNORE_STATUS = false;

// Font settings
readonly string FONT_STYLE = "Red";
readonly float FONT_SIZE = 0.75f;
readonly string headerSeparator = new string('-', 20);

// Maximum possible output of Solar Panels
readonly float SOLAR_PANEL_MAX = 0.12f;
// Oxygen storage percentage before shutting off generators, between 0.0 and 1.0
readonly float OXYGEN_MAX = 0.85f;

// Gas Tank Subtypes - add to this if you have modded subtypes
readonly string[] oxygenTankSubtypes = new[] { "", "OxygenTankSmall" };
readonly string[] hydrogenTankSubtypes = new[] { "LargeHydrogenTank", "SmallHydrogenTank" };

readonly MyDefinitionId electricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

// END SETTINGS //

List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
List<IMyReactor> reactors = new List<IMyReactor>();
List<IMyGasTank> gasTanks = new List<IMyGasTank>();
List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
List<IMyJumpDrive> jumpDrives = new List<IMyJumpDrive>();
List<IMyTextPanel> panels = new List<IMyTextPanel>();
List<IMyShipController> shipControllers = new List<IMyShipController>();
List<IMyRemoteControl> remoteControls = new List<IMyRemoteControl>();

Closures closures;

double shipSpeed;
MyShipMass shipMass;
double altitudeSeaLevel;
double altitudeSurface;

double batteryStored;
double batteryCapacity;

double solarOutputCurrent;
double solarOutputMax;

double reactorOutputCurrent;
double reactorOutputMax;

double powerUsageCurrent;
double powerUsageMax;
int powerUsageCount;

double oxygenStored;
double oxygenCapacity;
int oxygenTankCount;

double hydrogenStored;
double hydrogenCapacity;
int hydrogenTankCount;

double maxJumpDistance = 0;
Dictionary<string, JumpDriveData> jumpDriveStats = new Dictionary<string, JumpDriveData>();

public static readonly string[] LCD_TAGS = new[] {
    LCD_MASS_TAG,
    LCD_SPEED_SIMPLE_TAG,
    LCD_SPEED_TAG,
    LCD_ALTITUDE_TAG,
    LCD_SOLAR_TAG,
    LCD_REACTOR_TAG,
    LCD_BATTERY_TAG,
    LCD_POWER_TAG,
    LCD_OXYGEN_TAG,
    LCD_HYDROGEN_TAG,
    LCD_JUMP_TAG,
    LCD_CARGO_TAG,
    LCD_ORE_TAG,
    LCD_MATERIAL_TAG,
    LCD_GRAVITY_TAG,
    LCD_SEPARATOR_TAG
};

public Program() {
    closures = new Closures(Me.CubeGrid);
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    // 60 ticks = 1 second
}

public void Main(string argument, UpdateType updateSource) {
    GridTerminalSystem.GetBlocksOfType(blocks, closures.BlockCollectorFunc);
    checkShipController();
    checkBatteryCharge();
    checkPowerUsage();
	checkSolarOutput();
    checkReactorOutput();
    checkGas();
    checkCargo();
	checkJumpDrives();
    updateDisplays();
	
	Echo($"{panels.Count} LCDs");
}

private void checkShipController() {
    getItemsOfType(blocks, shipControllers);

    if(shipControllers.Count > 0) {
        shipMass = shipControllers[0].CalculateShipMass();
        previousShipSpeed = shipSpeed;
        shipSpeed = shipControllers[0].GetShipSpeed();

        Vector3D planetPosition = null;
        bool planetSuccess = shipControllers[0].TryGetPlanetPosition(planetPosition);

        if (planetSuccess) {
            shipControllers[0].TryGetPlanetElevation(MyPlanetElevation.Sealevel, altitudeSeaLevel);
            shipControllers[0].TryGetPlanetElevation(MyPlanetElevation.Surface, altitudeSurface);
        }
    }
}

private void checkBatteryCharge() {
    batteryStored = batteryCapacity = 0f;
    
    getItemsOfType(blocks, batteries);
    
    foreach (var item in batteries) {
        batteryStored += item.CurrentStoredPower;
        batteryCapacity += item.MaxStoredPower;
    }
}

private void checkPowerUsage() {
    powerUsageCurrent = powerUsageMax = 0f;
    powerUsageCount = 0;
    
    foreach (var item in blocks) {
        MyResourceSinkComponent sink;
        if (item.Components.TryGet(out sink) && acceptsResourceType(sink, electricityId)) {
            var currentInput = sink.CurrentInputByType(electricityId);
            var maxInput = sink.MaxRequiredInputByType(electricityId);
            
            var batt = item as IMyBatteryBlock;
 
            if (batt != null && batt.CurrentOutput >= currentInput)
                continue;

            powerUsageCurrent += currentInput;
            powerUsageMax += maxInput;
            powerUsageCount++;
        }
    }
}

private void checkSolarOutput() {
	solarOutputCurrent = solarOutputMax = 0f;
	
    getItemsOfType(blocks, solarPanels);

    foreach (var item in solarPanels) {
        solarOutputCurrent += item.CurrentOutput;
        solarOutputMax += SOLAR_PANEL_MAX;
    }
}

private void checkReactorOutput() {
    reactorOutputCurrent = reactorOutputMax = 0f;
	
	getItemsOfType(blocks, reactors);

    foreach (var item in reactors) {
        reactorOutputCurrent += item.CurrentOutput;
        reactorOutputMax += item.MaxOutput;
    }
}

private void checkGas() {
    oxygenCapacity = oxygenStored = hydrogenCapacity = hydrogenStored = 0f;
	oxygenTankCount = hydrogenTankCount = 0;
    
    getItemsOfType(blocks, gasTanks);
    
    foreach (var item in gasTanks) {
        string subtypeId = item.BlockDefinition.SubtypeId;
        
        if (containsString(oxygenTankSubtypes, subtypeId)) {
            oxygenCapacity += item.Capacity;
            oxygenStored += (double)(item.Capacity * item.FilledRatio);
			oxygenTankCount++;
        } else if (containsString(hydrogenTankSubtypes, subtypeId)) {
            hydrogenCapacity += item.Capacity;
            hydrogenStored += (double)(item.Capacity * item.FilledRatio);
			hydrogenTankCount++;
        }
    }
}

private void checkCargo() {
    getItemsOfType(blocks, cargoContainers);

    foreach (var item in cargoContainers) {
        //var inventory = item.GetInventory(0).GetItems();
    }
}

private void checkJumpDrives() {
	getItemsOfType(blocks, jumpDrives);
	
	if (jumpDrives.Count > 0) {
        string distanceText = System.Text.RegularExpressions.Regex.Match(jumpDrives[0].DetailedInfo, @"\d+\s*km").Value;
        try {
            maxJumpDistance = double.Parse(distanceText.Split(' ')[0]);
        } catch (Exception e) {
            Echo("Could not parse Max Jump Distance");
        }
    }
}

private void updateDisplays() {
    GridTerminalSystem.GetBlocksOfType(panels, closures.LCDCollectorFunc);

    foreach (var item in panels) {
		if (item.FontSize != FONT_SIZE) {
			item.FontSize = FONT_SIZE;
		}
		
		if (!item.Font.Contains(FONT_STYLE)) {
			item.Font = FONT_STYLE;
		}

        string displayText = "";

        string customData = item.CustomData;

        while (customData.Length > 3) {
            var match = System.Text.RegularExpressions.Regex.Match(customData, @"\[\w+\]");

            if (!match.Success) {
                break;
            }

            if (match.Value.Equals(LCD_MASS_TAG)) {
                displayText += getShipMassText();
            }

            if (match.Value.Equals(LCD_SPEED_SIMPLE_TAG)) {
                displayText += getShipSpeedSimpleText();
            }

            if (match.Value.Equals(LCD_SPEED_TAG)) {
                displayText += getShipSpeedText();
            }

            if (match.Value.Equals(LCD_ALTITUDE_TAG)) {
                displayText += getShipAltitudeText();
            }

            if (match.Value.Equals(LCD_SOLAR_TAG)) {
                displayText += getSolarPanelText();
            }

            if (match.Value.Equals(LCD_REACTOR_TAG)) {
                displayText += getReactorText();
            }

            if (match.Value.Equals(LCD_BATTERY_TAG)) {
                displayText += getBatteryText();
            }

            if (match.Value.Equals(LCD_POWER_TAG)) {
                displayText += getPowerUsageText();
            }

            if (match.Value.Equals(LCD_OXYGEN_TAG)) {
                displayText += getOxygenText();
            }

            if (match.Value.Equals(LCD_HYDROGEN_TAG)) {
                displayText += getHydrogenText();
            }

            if (match.Value.Equals(LCD_CARGO_TAG)) {
                displayText += getCargoText();
            }

            if (match.Value.Equals(LCD_JUMP_TAG)) {
                displayText += getJumpDriveText();
            }

            if (match.Value.Equals(LCD_GRAVITY_TAG)) {
                displayText += getGravityText();
            }

            if (match.Value.Equals(LCD_SEPARATOR_TAG)) {
                displayText += getSeparatorText();
            }

            int tagIndex = customData.IndexOf(match.Value);
            customData = customData.Remove(tagIndex, match.Value.Length);
        }

        item.WritePublicText(displayText);
    }
}

private string getShipMassText() {
    if (shipMass == null) {
        return "No valid ship controller to load ship data!";
    }

    string text = getAmountText("Base Mass", shipMass.BaseMass, "kg");
    text += getAmountText("Total Mass", shipMass.TotalMass, "kg");

    return text;
}

private string getShipSpeedSimpleText() {
    return getAmountText("Speed", shipSpeed, "m/s");
}

private string getShipSpeedText() {
    string text = getAmountText("Speed", shipSpeed, "m/s");
    text += getAmountText("Acceleration", shipSpeed - previousShipSpeed, "m/s\xB2");

    return text;
}

private string getShipAltitudeText() {
    string text = getAmountText("Sea Level", altitudeSeaLevel, "km");
    text += getAmountText("Ground", altitudeSurface, "km");

    return text;
}

private string getSolarPanelText() {
    return getPercentBarText("Solar Panels", solarPanels.Count, solarOutputCurrent, solarOutputMax, "MWh");
}

private string getReactorText() {
    return getPercentBarText("Reactors", reactors.Count, reactorOutputCurrent, reactorOutputMax, "MWh");
}

private string getBatteryText() {
    return getPercentBarText("Batteries", batteries.Count, batteryStored, batteryCapacity, "MWh");
}

private string getPowerUsageText() {
    return getPercentBarText("Power Usage", powerUsageCount, powerUsageCurrent, powerUsageMax, "MWh");
}

private string getOxygenText() {
    return getPercentBarText("Oxygen", oxygenTankCount, oxygenStored, oxygenCapacity, "L");
}

private string getHydrogenText() {
    return getPercentBarText("Hydrogen", hydrogenTankCount, hydrogenStored, hydrogenCapacity, "L");
}

private string getCargoText() {
    string text = $"{headerSeparator} Cargo Containers {headerSeparator}\n";
    foreach (var item in cargoContainers) {
        var inventory = item.GetInventory(0);
        text += getAmountText("Mass", (double)inventory.CurrentMass, "kg");
        text += getPercentBarText("Volume", 0, (double)inventory.CurrentVolume, (double)inventory.MaxVolume, "L");
    }

    return text;
}

private string getJumpDriveText() {
    return getAmountText("Max Jump", maxJumpDistance, "km");
}

private string getGravityText() {
    string text = "";

    return text;
}

private string getSeparatorText() {
    return headerSeparator;
}

private string getPercentBarText(string label, int blockCount, double current, double max, string unit) {
    double percentage = Math.Round((current / max) * 100.0f, 2);
    string text = label;
    if (blockCount > 0) {
        text += $" ({blockCount})";
    }

    text += $": {current.ToString("##,#.00")}/{max.ToString("##,#.00")}";
    if (unit.Length > 0) {
        text += $" {unit}";
    }

    text += "\n";

    int bars = (int)Math.Round(percentage);
	text += "[";
	text += new string('|', bars);
	text += new string('\'', 100 - bars);
	text += $"] {percentage}%\n";
	
	return text;
}

private string getAmountText(string label, double amount, string unit) {
    string text = $"{label}: {amount.ToString("##,#.00")}";
    if (unit.Length > 0) {
        text += $" {unit}";
    }

    text += "\n";

    return text;
}

public static bool acceptsResourceType(MyResourceSinkComponent sink, MyDefinitionId typeId) {
    foreach (var item in sink.AcceptedResources) {
        if (item == typeId)
            return true;
    }

    return false;
}

public static void getItemsOfType<T, TResult>(List<T> list, List<TResult> outList) where TResult : T {
    outList.Clear();

    for (int i = 0; i < list.Count; i++) {
        var item = list[i];

        if (item is TResult)
            outList.Add((TResult)item);
    }
}

public static void getItemsOfTypeWithTag<T, TResult>(List<T> list, List<TResult> outList, string tag) 
	where TResult : T 
	where T : IMyTerminalBlock {
    outList.Clear();

    for (int i = 0; i < list.Count; i++) {
        var item = list[i];

        if (item is TResult && item.CustomData.Contains(tag))
            outList.Add((TResult)item);
    }
}

public static bool containsString(string[] strings, string value) {
    for (int i = 0; i < strings.Length; i++) {
        if (strings[i].Equals(value, StringComparison.Ordinal))
            return true;
    }

    return false;
}

public static bool containsSubstring(string[] strings, string value) {
    for (int i = 0; i < strings.Length; i++) {
        if (value.Contains(strings[i]))
            return true;
    }

    return false;
}

class Closures {
    public readonly IMyCubeGrid BaseGrid;
    public readonly Func<IMyTerminalBlock, bool> BlockCollectorFunc;
    public readonly Func<IMyTextPanel, bool> LCDCollectorFunc;

    public Closures(IMyCubeGrid grid) {
        BaseGrid = grid;
        BlockCollectorFunc = IsValidBlock;
        LCDCollectorFunc = IsValidLCD;
    }

    public bool IsValidBlock(IMyTerminalBlock block) => block.CubeGrid == BaseGrid && (block.IsFunctional || IGNORE_STATUS);

    public bool IsValidLCD(IMyTextPanel lcd) => lcd.CubeGrid == BaseGrid && (lcd.IsFunctional || IGNORE_STATUS) && containsSubstring(LCD_TAGS, lcd.CustomData);
}

struct JumpDriveData {
    public int rechargedIn; // Number of seconds before fully charged
    public double currentInput; //W
    public double storedPower; //MWh

    public JumpDriveData(int rechargedIn, double currentInput, double storedPower) {
        this.rechargedIn = rechargedIn;
        this.currentInput = currentInput;
        this.storedPower = storedPower;
    }
}