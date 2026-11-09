namespace LawnGlove;

// Destructive actions wait for release. A reversible pickup can happen on press;
// switching tools later cancels that pickup through the normal glove path.
internal sealed class ControllerToolHold
{
    internal const double HoldSeconds = 1.5;
    private double started;
    private bool tracking;
    private bool switched;
    private bool usedOnPress;
    internal double Fraction { get; private set; }

    internal ToolGesture Update(bool pressed, bool down, double now, bool useOnPress = false)
    {
        if (down && !tracking)
        {
            started = now;
            tracking = true;
            switched = false;
            usedOnPress = useOnPress;
            if (usedOnPress)
            {
                if (!pressed) Reset();
                return ToolGesture.Use;
            }
        }
        if (!tracking) return ToolGesture.None;
        Fraction = Math.Clamp((now - started) / HoldSeconds, 0, 1);
        if (!switched && Fraction >= 1)
        {
            switched = true;
            if (!pressed) Reset();
            return ToolGesture.Switch;
        }
        if (pressed) return ToolGesture.None;
        bool tap = !switched && !usedOnPress;
        Reset();
        return tap ? ToolGesture.Use : ToolGesture.None;
    }

    internal void Reset() { tracking = switched = usedOnPress = false; Fraction = 0; }
}

internal enum ToolGesture { None, Use, Switch }
