using LawnGlove;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static void Select(GloveState state, LawnTool tool)
{
    while (state.SelectedTool != tool) state.Toggle();
}
var state = new GloveState();
Check(!state.TryArm() && state.ChargeFraction == 1, "Shovel cannot arm a custom tool");
state.Toggle(); Check(state.SelectedTool == LawnTool.Glove, "Cycle starts with plant glove");
Check(state.TryArm(), "Ready plant glove arms");
state.Cancel(); Check(state.Remaining == 0, "Cancel preserves charge");
Check(state.TryArm(), "Plant glove re-arms after cancellation");
state.CommitMove();
Check(!state.Armed && state.Remaining == 10 && state.ChargeFraction == 0, "Plant move consumes ten seconds");
Check(!state.TryArm(), "Cooldown blocks use");
state.Toggle(); Check(state.SelectedTool == LawnTool.Hammer, "Hammer follows plant glove");
Check(state.Remaining == 30 && !state.TryArm(), "Hammer starts charging for thirty seconds");
state.Toggle(); Check(state.SelectedTool == LawnTool.ZombieGlove, "Zombie glove follows hammer");
Check(state.TryArm(), "Zombie glove starts ready");
state.CommitMove(); Check(state.Remaining == 20, "Zombie move consumes twenty seconds");
state.Toggle(); Check(state.SelectedTool == LawnTool.Shovel, "Cycle returns to shovel");
state.Tick(5);
Select(state, LawnTool.Glove); Check(state.Remaining == 5 && state.ChargeFraction == .5, "Plant glove recharges while hidden");
Select(state, LawnTool.Hammer); Check(state.Remaining == 25, "Hammer clock is independent");
Select(state, LawnTool.ZombieGlove); Check(state.Remaining == 15, "Zombie glove clock is independent");
state.Tick(-100); state.Tick(double.NaN); state.Tick(double.PositiveInfinity);
Check(state.Remaining == 15, "Invalid elapsed time does not change clocks");
state.Tick(14.99); Check(!state.TryArm(), "No early zombie glove activation");
state.Tick(.02); Check(state.TryArm() && state.ChargeFraction == 1, "Zombie glove ready after twenty seconds");
state.Cancel(); Check(state.Remaining == 0 && !state.Armed, "Cancellation is free");
Select(state, LawnTool.Hammer); Check(!state.TryArm(), "Hammer still charging independently");
state.Tick(10); Check(state.TryArm(), "Hammer ready after thirty seconds");
state.CommitMove(); Check(state.Remaining == 30, "Hammer strike starts thirty-second recharge");
Select(state, LawnTool.Glove); Check(state.Remaining == 0 && state.TryArm(), "Plant glove remains ready");
state.Toggle(); Check(!state.Armed && state.Remaining == 30, "Switching cancels armed tool without resetting another clock");
Console.WriteLine("PASS: four-tool cycle, arming, cancellation, independent clocks and exact recharge durations.");
