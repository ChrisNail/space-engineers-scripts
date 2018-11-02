// Door Auto Close Script //

// SETTINGS //
readonly static string AUTO_CLOSE_TAG = "[AUTO_CLOSE]";
readonly static double DOOR_CLOSE_DELAY = 3000;

// END SETTINGS //

List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
List<IMyDoor> autoCloseDoors = new List<IMyDoor>();

Dictionary<string, DateTime> openDoorTimes = new Dictionary<string, DateTime>();

Closures closures;

public Program() {
    closures = new Closures(Me.CubeGrid);
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    // 60 ticks = 1 second
}

public void Main(string argument, UpdateType updateSource) {
    GridTerminalSystem.GetBlocksOfType(blocks, closures.BlockCollectorFunc);
    checkDoors();
    Echo($"{autoCloseDoors.Count} Doors");
}

private void checkDoors() {
    getItemsOfTypeWithTag(blocks, autoCloseDoors, AUTO_CLOSE_TAG);

    foreach(var item in autoCloseDoors) {
        if(item.Status == DoorStatus.Open) {
            if(!openDoorTimes.ContainsKey(item.CustomName)) {
                openDoorTimes.Add(item.CustomName, System.DateTime.Now);
            } else {
                double timeOpen = (System.DateTime.Now - openDoorTimes[item.CustomName]).TotalMilliseconds;
                if(timeOpen > DOOR_CLOSE_DELAY) {
                    item.ApplyAction("Open_Off");
                    openDoorTimes.Remove(item.CustomName);
                }
            }

        }
    }
}

public static void getItemsOfTypeWithTag<T, TResult>(List<T> list, List<TResult> outList, string tag)
    where TResult : T
    where T : IMyTerminalBlock {
    outList.Clear();

    for(int i = 0; i < list.Count; i++) {
        var item = list[i];

        if(item is TResult && item.CustomData.Contains(tag))
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

    public bool IsValidBlock(IMyTerminalBlock block) => block.CubeGrid == BaseGrid && block.IsFunctional;
}