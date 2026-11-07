using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Pointer;
using OpenTabletDriver.Plugin.Tablet;

namespace LocalWacom;

// No background timer touches the virtual pointer: scroll and button events stay
// on the tablet report thread. Continuous hover reports maintain the scroll rate;
// disconnection, absent reports, and disposal cannot leave an autonomous scroller.
internal sealed class ScrollSession
{
    internal static readonly ConditionalWeakTable<TabletReference, ScrollSession> Sessions = new();
    internal DynamicScrollBinding? Owner;
    internal Vector2 Offset, Remainder;
    internal long LastTick;
    internal bool FilterReady;
    internal void Stop() { Owner = null; Offset = Remainder = Vector2.Zero; }
}

[PluginName("Dynamic 4-way Scroll (hold)")]
public sealed class DynamicScrollBinding : IStateBinding
{
    [Resolved] public IMouseScrollHandler? Pointer { get; set; }
    [Property("Base Amount"), DefaultPropertyValue(50)] public int Amount { get; set; } = 50;
    [Property("Base Interval (ms)"), DefaultPropertyValue(100)] public int Interval { get; set; } = 100;
    [Property("Dead Zone (cursor pixels)"), DefaultPropertyValue(10)] public float DeadZone { get; set; } = 10;
    [Property("Base Speed Distance (cursor pixels)"), DefaultPropertyValue(100)] public float Distance { get; set; } = 100;
    [Property("Maximum Speed Multiplier"), DefaultPropertyValue(5)] public float MaximumSpeed { get; set; } = 5;
    [BooleanProperty("Invert Horizontal", "")] public bool InvertX { get; set; }
    [BooleanProperty("Invert Vertical", "")] public bool InvertY { get; set; }

    public void Press(TabletReference tablet, IDeviceReport report)
    {
        var session = ScrollSession.Sessions.GetOrCreateValue(tablet);
        if (!session.FilterReady || Pointer == null)
        {
            Log.Write("Dynamic Scroll", "Enable the Dynamic 4-way Scroll companion filter in Relative Mode.", LogLevel.Error);
            return;
        }
        session.Stop();
        session.Owner = this;
        session.LastTick = Stopwatch.GetTimestamp();
        if (report is IAbsolutePositionReport movement) movement.Position = Vector2.Zero;
    }

    public void Release(TabletReference tablet, IDeviceReport report)
    {
        var session = ScrollSession.Sessions.GetOrCreateValue(tablet);
        if (session.Owner != this) return;
        session.Stop();
        if (report is IAbsolutePositionReport movement) movement.Position = Vector2.Zero;
    }

    // A fixed sampling interval with variable amount gives smooth, proportional
    // scrolling rather than increasingly frequent timer callbacks.
    public Vector2 CalculateRate(Vector2 displacement)
    {
        float Rate(float v)
        {
            float outside = Math.Max(0, Math.Abs(v) - Math.Max(0, DeadZone));
            float gain = Math.Min(Math.Clamp(MaximumSpeed, 0, 50), outside / Math.Max(1, Distance));
            return Math.Sign(v) * gain * Math.Clamp(Amount, 0, 1200) * 1000f / Math.Clamp(Interval, 1, 10000);
        }
        return new Vector2(Rate(displacement.X) * (InvertX ? -1 : 1), -Rate(displacement.Y) * (InvertY ? -1 : 1));
    }

    internal void Tick(ScrollSession session, double seconds)
    {
        if (Pointer == null) return;
        var rate = CalculateRate(session.Offset);
        // Drop fractional remainder at neutral/reversal; do not leak old scroll.
        if (rate.X == 0 || Math.Sign(rate.X) != Math.Sign(session.Remainder.X)) session.Remainder.X = 0;
        if (rate.Y == 0 || Math.Sign(rate.Y) != Math.Sign(session.Remainder.Y)) session.Remainder.Y = 0;
        session.Remainder += rate * (float)Math.Clamp(seconds, 0, 0.05);
        int x = (int)session.Remainder.X, y = (int)session.Remainder.Y;
        session.Remainder -= new Vector2(x, y);
        if (x != 0) Pointer.ScrollHorizontally(x);
        if (y != 0) Pointer.ScrollVertically(y);
        if ((x != 0 || y != 0) && Pointer is ISynchronousPointer sync) sync.Flush();
    }
}

[PluginName("Dynamic 4-way Scroll companion (relative mode only)")]
public sealed class DynamicScrollFilter : IPositionedPipelineElement<IDeviceReport>, IDisposable
{
    [TabletReference] public TabletReference? Tablet { get; set; }
    public PipelinePosition Position => PipelinePosition.PostTransform;
    public event Action<IDeviceReport>? Emit;

    public void Consume(IDeviceReport? report)
    {
        if (report == null) return;
        if (Tablet == null) { Emit?.Invoke(report); return; }
        var session = ScrollSession.Sessions.GetOrCreateValue(Tablet);
        session.FilterReady = true;
        if (report is OutOfRangeReport || report is IProximityReport { NearProximity: false }) session.Stop();
        var owner = session.Owner;
        var movement = report as IAbsolutePositionReport;
        var delta = movement?.Position ?? Vector2.Zero;
        if (owner != null && movement != null) movement.Position = Vector2.Zero;

        // The built-in binding handler runs downstream. It sees releases before
        // this frame can scroll, and Press also suppresses the first-frame delta.
        Emit?.Invoke(report);
        if (owner != null && session.Owner == owner && movement != null)
        {
            long now = Stopwatch.GetTimestamp();
            double elapsed = Stopwatch.GetElapsedTime(session.LastTick, now).TotalSeconds;
            session.LastTick = now;
            if (elapsed > 0.25) { session.Stop(); return; }
            session.Offset += delta;
            owner.Tick(session, elapsed);
        }
    }

    public void Dispose()
    {
        if (Tablet != null && ScrollSession.Sessions.TryGetValue(Tablet, out var session))
        { session.Stop(); session.FilterReady = false; }
    }
}
