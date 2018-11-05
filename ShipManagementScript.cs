// Ship Management Script //

// SETTINGS //
// Activate dampeners when no one is controlling the ship
readonly static bool DEADMAN_SWITCH = false;
// Prevent oxygen generators from filling up all oxygen tanks
readonly static bool OXYGEN_CONTROL = false;

// When true, will include blocks that are incomplete
readonly static bool IGNORE_STATUS = false;

// Oxygen storage percentage before shutting off generators, between 0.0 and 1.0
readonly float OXYGEN_MAX = 0.85f;

// Gas Tank Subtypes - add to this if you have modded subtypes
readonly string[] oxygenTankSubtypes = new[] { "", "OxygenTankSmall" };

// END SETTINGS //

List<IMyGasTank> gasTanks = new List<IMyGasTank>();
List<IMyGasGenerator> oxygenGenerators = new List<IMyGasGenerator>();
List<IMyShipController> shipControllers = new List<IMyShipController>();

Closures closures;

double oxygenStored;
double oxygenCapacity;

public Program() {
    closures = new Closures(Me.CubeGrid);
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    // 60 ticks = 1 second
}

public void Main(string argument, UpdateType updateSource) {
    manageGas();
    checkShipController();
}

private void checkShipController() {
    if (!DEADMAN_SWITCH) {
        return;
    }

    getItemsOfType(blocks, shipControllers);

    if(shipControllers.Count > 0) {
        bool isControlled = false;
        foreach(var item in shipControllers) {
            isControlled = isControlled || item.IsUnderControl;
        }

        if(!isControlled && !shipControllers[0].DampenersOverride) {
            shipControllers[0].ApplyAction("DampenersOverride");
        }
    }
}

private void manageGas() {
    if (!OXYGEN_CONTROL) {
        return;
    }

    oxygenCapacity = oxygenStored = 0f;

    getItemsOfType(blocks, gasTanks);

    foreach(var item in gasTanks) {
        string subtypeId = item.BlockDefinition.SubtypeId;

        if(containsString(oxygenTankSubtypes, subtypeId)) {
            oxygenCapacity += item.Capacity;
            oxygenStored += (double)(item.Capacity * item.FilledRatio);
            oxygenTankCount++;
        }
    }

    getItemsOfType(blocks, oxygenGenerators);

    string action = "OnOff";
    if((oxygenStored / oxygenCapacity) > OXYGEN_MAX) {
        action += "_Off";
    } else {
        action += "_On";
    }

    foreach(var item in oxygenGenerators) {
        item.ApplyAction(action);
    }
}

public static void getItemsOfType<T, TResult>(List<T> list, List<TResult> outList) where TResult : T {
    outList.Clear();

    for(int i = 0; i < list.Count; i++) {
        var item = list[i];

        if(item is TResult)
            outList.Add((TResult)item);
    }
}

class Closures {
    public readonly IMyCubeGrid BaseGrid;
    public readonly Func<IMyTerminalBlock, bool> BlockCollectorFunc;

    public Closures(IMyCubeGrid grid) {
        BaseGrid = grid;
        BlockCollectorFunc = IsValidBlock;
    }

    public bool IsValidBlock(IMyTerminalBlock block) => block.CubeGrid == BaseGrid && (block.IsFunctional || IGNORE_STATUS);
}