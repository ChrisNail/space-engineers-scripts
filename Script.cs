// MANAGEMENT SCRIPT //

// SETTINGS //

// The text LCD names must contain to be used
readonly static string LCD_ENERGY_TAG = "[LCD_ENERGY]";
readonly static string LCD_GAS_TAG = "[LCD_GAS]";
readonly static string LCD_CARGO_TAG = "[LCD_CARGO]";
readonly static string LCD_ORE_TAG = "[LCD_ORE]";
readonly static string LCD_MATERIAL_TAG = "[LCD_MATERIAL]";

readonly static string AUTO_CLOSE_TAG = "[AUTO_CLOSE]";

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

readonly static float DOOR_CLOSE_DELAY = 5000f;

readonly string[] oxygenTankSubtypes = new[] { "", "OxygenTankSmall" };
readonly string[] hydrogenTankSubtypes = new[] { "LargeHydrogenTank", "SmallHydrogenTank" };

readonly MyDefinitionId electricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

// END SETTINGS //

List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
List<IMyReactor> reactors = new List<IMyReactor>();
List<IMyGasTank> gasTanks = new List<IMyGasTank>();
List<IMyGasGenerator> oxygenGenerators = new List<IMyGasGenerator>();
List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
List<IMyTextPanel> panels = new List<IMyTextPanel>();
List<IMyDoor> autoCloseDoors = new List<IMyDoor>();

Dictionary<string, DateTime> openDoorTimes = new Dictionary<string, DateTime>();

Closures closures;

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

// Dictionary<string, CargoData> cargoContainerData = new Dictionary<string, CargoData>();

public static readonly string[] LCD_TAGS = new[] {
    LCD_ENERGY_TAG,
    LCD_GAS_TAG,
    LCD_CARGO_TAG,
    LCD_ORE_TAG,
    LCD_MATERIAL_TAG
};

public Program() {
    closures = new Closures(Me.CubeGrid);
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    // 60 ticks = 1 second
}

public void Main(string argument, UpdateType updateSource) {
    GridTerminalSystem.GetBlocksOfType(blocks, closures.BlockCollectorFunc); 
    checkBatteryCharge();
    checkPowerUsage();
	checkSolarOutput();
    checkReactorOutput();
    checkGas();
    checkCargo();
    updateDisplays();
	checkDoors();
	
	Echo($"{panels.Count} LCDs");
	Echo($"{autoCloseDoors.Count} Doors");
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

    getItemsOfType(blocks, oxygenGenerators);

    string action = "OnOff";
    if ((oxygenStored / oxygenCapacity) > OXYGEN_MAX) {
        action = "OnOff_Off";
    } else {
        action = "OnOff_On";
    }

    foreach (var item in oxygenGenerators) {
        item.ApplyAction(action);
    }
}

private void checkCargo() {
    getItemsOfType(blocks, cargoContainers);

    // foreach (var item in cargoContainers) {
    //     var inventory = item.GetInventory(0);
    //     cargoContainers.Add(item.CustomName, new CargoData(inventory.CurrentMass, inventory.CurrentVolume, inventory.MaxVolume));
    // }
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
		
        if (item.CustomData.Contains(LCD_ENERGY_TAG)) {
            displayText += getEnergyStatsText();
        }

        if (item.CustomData.Contains(LCD_GAS_TAG)) {
            displayText += getGasStatsText();
        }

        if (item.CustomData.Contains(LCD_CARGO_TAG)) {
            displayText += getCargoStatsText();
        }

        panel.WritePublicText(displayText);
    }
}

private string getEnergyStatsText() {
    string text = $"{headerSeparator} Power {headerSeparator}\n";
    text += getPercentBarText("Reactors", reactors.Count, reactorOutputCurrent, reactorOutputMax, "MWh");
    text += getPercentBarText("Solar Panels", solarPanels.Count, solarOutputCurrent, solarOutputMax, "MWh");
    text += getPercentBarText("Batteries", batteries.Count, batteryStored, batteryCapacity, "MWh");
    text += getPercentBarText("Usage", powerUsageCount, powerUsageCurrent, powerUsageMax, "MWh");
    
    return text;
}

private string getGasStatsText() {
    string text = $"{headerSeparator} Gas {headerSeparator}\n";
    text += getPercentBarText("Oxygen", oxygenTankCount, oxygenStored, oxygenCapacity, "L");
    text += getPercentBarText("Hydrogen", hydrogenTankCount, hydrogenStored, hydrogenCapacity, "L");

    return text;
}

private string getCargoStatsText() {
    string text = $"{headerSeparator} Cargo Containers {headerSeparator}";
    foreach (var item in cargoContainers) {
        var inventory = item.GetInventory(0);
        text += getAmountText("Mass", inventory.CurrentMass, "kg");
        text += getPercentBarText("Volume", 0, inventory.CurrentVolume, inventory.MaxVolume, "L");
    }

    return text;
}

private string getPercentBarText(string label, int blockCount, double current, double max, string unit) {
    double percentage = Math.Round((current / max) * 100.0f, 2);
    string text += label;
    if (count > 0) {
        text += $"({count})";
    }

    text += ": {current.ToString("##,#.00")}/{max.ToString("##,#.00")} {unit}\n";

    int bars = (int)Math.Round(percentage);
	text += "[";
	text += new string('|', bars);
	text += new string('\'', 100 - bars);
	text += $"] {percentage}%\n";
	
	return text;
}

private string getAmountText(string label, double amount, string unit) {
    string text = $"{label}: {amount.ToString("##,#.00")} {unit}";
}

private void checkDoors() {
	getItemsOfTypeWithTag(blocks, autoCloseDoors, AUTO_CLOSE_TAG);
	
	foreach (var item in autoCloseDoors) {
		if (item.Status == DoorStatus.Open) {
			if (!openDoorTimes.ContainsKey(item.CustomName)) {
				openDoorTimes.Add(item.CustomName, System.DateTime.Now);
			} else {
				double timeOpen = (System.DateTime.Now - openDoorTimes[item.CustomName]).TotalMilliseconds;
				if (timeOpen > DOOR_CLOSE_DELAY) {
					item.ApplyAction("Open_Off");
					openDoorTimes.Remove(item.CustomName);
				}
			}
				
		}
	}
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

struct CargoData {
    public double mass;
    public double volumeCurrent;
    public double volumeMax;

    public CargoData(double mass, double volumeCurrent, double volumeMax) {
        this.mass = mass;
        this.volumeCurrent = volumeCurrent;
        this.volumeMax = volumeMax;
    }
}