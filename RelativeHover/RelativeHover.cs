using System;
using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace LocalWacom;

// Relative-mode only: Position is a movement delta at PostTransform.
// Keep all reports flowing so the upstream origin and button state stay current.
[PluginName("Relative Mouse Hover Cutoff (local fix)")]
public sealed class RelativeHover : IPositionedPipelineElement<IDeviceReport>
{
    [Property("Maximum Hover Distance"), DefaultPropertyValue(100)]
    public int MaximumDistance { get; set; } = 100;

    [BooleanProperty("Use Near Proximity Cutoff", "")]
    public bool NearProximity { get; set; } = true;

    public PipelinePosition Position => PipelinePosition.PostTransform;
    public event Action<IDeviceReport>? Emit;
    private bool suppressed = true;

    public void Consume(IDeviceReport? report)
    {
        if (report is null) return;
        if (report is OutOfRangeReport)
            suppressed = true;
        if (report is IProximityReport proximity && report is IAbsolutePositionReport movement)
        {
            bool blocked = proximity.HoverDistance > Math.Max(0, MaximumDistance)
                || (NearProximity && !proximity.NearProximity);
            // The first sample back establishes an origin, never a movement.
            if (blocked || suppressed)
                movement.Position = Vector2.Zero;
            suppressed = blocked;
        }
        Emit?.Invoke(report);
    }
}
