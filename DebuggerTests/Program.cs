using LocalWacom;
using OpenTabletDriver.Desktop.ViewModels;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Tablet;

var parser = new ProPen3DParser();
for (int state = 0; state < 4; state++)
{
    var bytes = new byte[192]; bytes[0] = 0x10; bytes[1] = (byte)(0x20 | (state << 1));
    var report = (ITabletReport)parser.Parse(bytes);
    var expected = state == 0 ? "Pressed: none" : $"Pressed: Pen Binding {state}";
    if (PenButtonLabels.Describe(report.PenButtons) != expected) throw new Exception($"Bad label for state {state}");
    if (!ReportFormatter.GetStringFormat(report).Contains("PenButtons")) throw new Exception("Raw button states disappeared");
}
if (PenButtonLabels.Describe([true, false, true]) != "Pressed: Pen Binding 1, Pen Binding 3") throw new Exception("Multiple pressed buttons");
if (PenButtonLabels.Describe([]) != "Pressed: none") throw new Exception("Empty button array");
Console.WriteLine("PASS: 6 debugger-label checks; raw PenButtons retained.");
