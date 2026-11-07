using System.Numerics;
using LocalWacom;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Pointer;
using OpenTabletDriver.Plugin.Tablet;

if (new RelativeHover().MaximumDistance != 100) throw new Exception("Default hover cutoff must preserve user tuning");
var filter = new RelativeHover { MaximumDistance = 30 };
var mode = new TestMode { Sensitivity = Vector2.One, ResetTime = TimeSpan.FromSeconds(60), Elements = new List<IPositionedPipelineElement<IDeviceReport>> { filter } };
int count = 0;
void Check(float x, uint height, bool near, float expected)
{
    var report = new Report { Position = new Vector2(x, 0), HoverDistance = height, NearProximity = near };
    mode.Read(report);
    if (Math.Abs(mode.Last.Position.X - expected) > 0.001f) throw new Exception($"Expected {expected}, got {mode.Last.Position.X}");
    if (!ReferenceEquals(mode.Last, report)) throw new Exception("Report was lost/replaced");
    count++;
}
Check(100, 0, true, 0); // initial sample
Check(200, 0, true, 1); // ordinary movement (.01 scale without a tablet)
Check(9000, 40, true, 0); // lift must not teleport
Check(50000, 50, true, 0); // reposition while lifted
Check(40000, 20, true, 0); // re-entry must not jump
Check(40100, 20, true, 1); // normal sensitivity restored
Check(80000, 20, false, 0); // proximity cutoff
Check(10000, 20, true, 0); // re-entry
Check(10100, 30, true, 1); // inclusive boundary
Check(20000, 31, true, 0);
filter.Consume(null);
Console.WriteLine($"PASS: {count} real relative-pipeline movement checks; reports preserved.");

class Report : IAbsolutePositionReport, IProximityReport
{
    public byte[] Raw {get;set;} = [];
    public Vector2 Position {get;set;}
    public bool NearProximity {get;set;}
    public uint HoverDistance {get;set;}
}
class TestMode : RelativeOutputMode
{
    public override IRelativePointer Pointer {get;set;}
    public Report Last;
    protected override void OnOutput(IDeviceReport report) { Last = (Report)report; }
}
