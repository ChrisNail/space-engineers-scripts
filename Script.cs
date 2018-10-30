// MANAGEMENT SCRIPT //

// SETTINGS //

// The text LCD names must contain to be used
readonly static string LCD_GENERAL_TAG = "[LCD_GENERAL]";
readonly static string LCD_MATERIAL_TAG = "[LCD_MATERIAL]";

readonly static string AUTO_CLOSE_TAG = "[AUTO_CLOSE]";

// When true, will include blocks that are incomplete
readonly static bool IGNORE_STATUS = false;

// Font settings
readonly string FONT_STYLE = "Red";
readonly float FONT_SIZE = 0.75f;

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
List<IMyTextPanel> panels = new List<IMyTextPanel>();
List<IMyGasGenerator> oxygenGenerators = new List<IMyGasGenerator>();
List<IMyDoor> autoCloseDoors = new List<IMyDoor>();

Dictionary<string, DateTime> openDoorTimes = new Dictionary<string, DateTime>();

Closures closures;

double batteryCapacity;
double batteryStored;

double solarOutputCurrent;
double solarOutputMax;

double reactorOutputCurrent;
double reactorOutputMax;

double powerUsageCurrent;
double powerUsageMax;

double oxygenCapacity;
double oxygenStored;
int oxygenTankCount;

double hydrogenCapacity;
double hydrogenStored;
int hydrogenTankCount;

public static readonly string[] LCD_TAGS = new[] {
    LCD_GENERAL_TAG,
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

private void updateDisplays() {
    GridTerminalSystem.GetBlocksOfType(panels, closures.LCDCollectorFunc);

    foreach (var item in panels) {
		if (item.FontSize != FONT_SIZE) {
			item.FontSize = FONT_SIZE;
		}
		
		if (!item.Font.Contains(FONT_STYLE)) {
			item.Font = FONT_STYLE;
		}
		
        if (item.CustomName.Contains(LCD_GENERAL_TAG)) {
            displayGeneral(item);
        } else if (item.CustomName.Contains(LCD_MATERIAL_TAG)) {

        }
    }
}

private void displayGeneral(IMyTextPanel panel) {
    string text = "";
    string separator = new string('-', 20);
	text += $"{separator} Power {separator}\n";
    
    double reactorOutputPercentage = Math.Round((reactorOutputCurrent / reactorOutputMax) * 100.0f, 2);
    text += $"Reactors ({reactors.Count}): {reactorOutputCurrent.ToString("0.00")}/{reactorOutputMax.ToString("0.00")} MWh\n";
	text += getPercentBar(reactorOutputPercentage);
	text += $" {reactorOutputPercentage}%\n";
	
	double solarOutputPercentage = Math.Round((solarOutputCurrent / solarOutputMax) * 100.0f, 2);
    text += $"Solar Panels ({solarPanels.Count}): {solarOutputCurrent.ToString("0.00")}/{solarOutputMax.ToString("0.00")} MWh\n";
	text += getPercentBar(solarOutputPercentage);
	text += $" {solarOutputPercentage}%\n";
    
    double batteryPercentage = Math.Round((batteryStored / batteryCapacity) * 100.0f, 2);
    text += $"Batteries ({batteries.Count}): {batteryStored.ToString("0.00")}/{batteryCapacity} MWh\n";
	text += getPercentBar(batteryPercentage);
	text += $" {batteryPercentage}%\n";
	
	double powerUsagePercentage = Math.Round((powerUsageCurrent / powerUsageMax) * 100.0f, 2);
    text += $"Usage: {powerUsageCurrent.ToString("0.00")}/{powerUsageMax.ToString("0.00")} MWh\n";
	text += getPercentBar(powerUsagePercentage);
	text += $" {powerUsagePercentage}%\n";
        
    text += $"{separator} Gas {separator}\n";
    
    double oxygenPercentage = Math.Round((oxygenStored / oxygenCapacity) * 100.0f, 2);
    text += $"Oxygen ({oxygenTankCount}): {oxygenStored.ToString("##,#.00")}/{oxygenCapacity.ToString("##,#")} L\n";
	text += getPercentBar(oxygenPercentage);
	text += $" {oxygenPercentage}%\n";
    
    double hydrogenPercentage = Math.Round((hydrogenStored / hydrogenCapacity) * 100.0f, 2);
    text += $"Hydrogen ({hydrogenTankCount}): {hydrogenStored.ToString("##,#.00")}/{hydrogenCapacity.ToString("##,#")} L\n";
	text += getPercentBar(hydrogenPercentage);
	text += $" {hydrogenPercentage}%\n";
    
    panel.WritePublicText(text);
}

private void displayMaterial(IMyTextPanel panel) {
    string text = "";


    
    panel.WritePublicText(text);
}

private string getPercentBar(double percentage) {
	int bars = (int)Math.Round(percentage);
	string text = "[";
	text += new string('|', bars);
	text += new string('\'', 100 - bars);
	text +="]";
	
	return text;
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

        if (item is TResult && item.CustomName.Contains(tag))
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

    public bool IsValidLCD(IMyTextPanel lcd) => lcd.CubeGrid == BaseGrid && (lcd.IsFunctional || IGNORE_STATUS) && containsSubstring(LCD_TAGS, lcd.CustomName);
}