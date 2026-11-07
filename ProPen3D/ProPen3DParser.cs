using OpenTabletDriver.Configurations.Parsers.Wacom.IntuosV2;
using OpenTabletDriver.Plugin.Tablet;

namespace LocalWacom;

// Specific to the user's Pro Pen 3D: rocker states 01/10 and third button 11.
public sealed class ProPen3DParser : IntuosV2ReportParser
{
    public override IDeviceReport Parse(byte[] data)
    {
        var report = base.Parse(data);
        if (report is ITabletReport pen && pen.PenButtons.Length == 2)
        {
            bool first = pen.PenButtons[0], second = pen.PenButtons[1];
            pen.PenButtons = new[] { first && !second, second && !first, first && second };
        }
        return report;
    }
}
