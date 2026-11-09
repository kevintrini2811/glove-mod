namespace LawnGlove;

public enum LawnTool { Shovel, Glove, Hammer, ZombieGlove }

// Each tool owns its clock. Only successful actions consume a charge.
public sealed class GloveState
{
    public const double CooldownSeconds = 10;
    public const double HammerCooldownSeconds = 30;
    public const double ZombieGloveCooldownSeconds = 20;
    private readonly double[] remaining = { 0, 0, HammerCooldownSeconds, 0 };
    public LawnTool SelectedTool { get; private set; }
    public bool GloveSelected => SelectedTool == LawnTool.Glove;
    public bool CustomToolSelected => SelectedTool != LawnTool.Shovel;
    public bool Armed { get; private set; }
    public double Remaining => remaining[(int)SelectedTool];
    private double Duration => SelectedTool switch
    {
        LawnTool.Glove => CooldownSeconds,
        LawnTool.Hammer => HammerCooldownSeconds,
        LawnTool.ZombieGlove => ZombieGloveCooldownSeconds,
        _ => 0
    };
    public double ChargeFraction => Duration == 0 ? 1 : 1 - Remaining / Duration;

    public void Toggle() { SelectedTool = (LawnTool)(((int)SelectedTool + 1) % 4); Armed = false; }
    public bool TryArm()
    {
        if (!CustomToolSelected || Remaining > 0) return false;
        Armed = !Armed;
        return Armed;
    }
    public void Cancel() => Armed = false;
    public void CommitMove()
    {
        if (!Armed || Remaining > 0) throw new InvalidOperationException("Tool is not ready.");
        remaining[(int)SelectedTool] = Duration;
        Armed = false;
    }
    public void Tick(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0) return;
        for (int i = 1; i < remaining.Length; ++i) remaining[i] = Math.Max(0, remaining[i] - seconds);
    }
}
