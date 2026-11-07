using System.Numerics;
using LocalWacom;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Platform.Pointer;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
var mouse = new Mouse();
var defaults = new DynamicScrollBinding();
Check(defaults.Amount == 50 && defaults.Interval == 100 && defaults.DeadZone == 10 && defaults.Distance == 100 && defaults.MaximumSpeed == 5, "User-tuned defaults");
Check(defaults.CalculateRate(new Vector2(60, -60)) == new Vector2(250, 250), "User-tuned proportional rate");
Check(defaults.CalculateRate(new Vector2(510, -510)) == new Vector2(2500, 2500), "User-tuned maximum rate");
var binding = new DynamicScrollBinding { Pointer = mouse, Amount = 50, Interval = 100, DeadZone = 15, Distance = 100, MaximumSpeed = 8 };
Check(binding.CalculateRate(new Vector2(10, -10)) == Vector2.Zero, "Dead zone");
Check(binding.CalculateRate(new Vector2(115, -115)) == new Vector2(500,500), "Base amount/interval, NE");
Check(binding.CalculateRate(new Vector2(-115, 115)) == new Vector2(-500,-500), "SW");
Check(binding.CalculateRate(new Vector2(215, 0)).X == 1000, "Proportional acceleration");
Check(binding.CalculateRate(new Vector2(999999, 0)).X == 4000, "Rate cap");
var tablet = new TabletReference { Properties = null! };
var filter = new DynamicScrollFilter { Tablet = tablet };
bool down = false;
Vector2 output = default;
filter.Emit += report => {
 if (report is Report r) {
  if(r.Down && !down) binding.Press(tablet, report);
  if(!r.Down && down) binding.Release(tablet, report);
  down = r.Down;
  output = r.Position;
 }
};
void Send(Vector2 delta, bool held, bool near=true) => filter.Consume(new Report {Position=delta, Down=held, NearProximity=near});
Send(new Vector2(3,4),false);
Check(output==new Vector2(3,4), "Normal cursor passes through");
Send(new Vector2(9,8),true);
Check(output==Vector2.Zero, "Press frame cursor stays fixed");
Thread.Sleep(30); Send(new Vector2(115,-115),true);
Check(output==Vector2.Zero && mouse.X>0 && mouse.Y>0,"NE scroll with fixed cursor");
var previous=mouse.X; Thread.Sleep(30); Send(Vector2.Zero,true);
Check(mouse.X>previous,"Stationary displaced pen keeps scrolling");
Thread.Sleep(30); Send(new Vector2(-115,115),true);
previous=mouse.X; Thread.Sleep(30); Send(Vector2.Zero,true);
Check(mouse.X==previous,"Return to neutral stops");
Thread.Sleep(30); Send(new Vector2(-115,115),true);
Check(mouse.LastX<0 && mouse.LastY<0,"SW scroll");
previous=mouse.Calls; Send(new Vector2(800,800),false);
Check(mouse.Calls==previous && output==Vector2.Zero,"Release emits no final scroll/jump");
Send(new Vector2(1,2),false); Check(output==new Vector2(1,2),"Cursor resumes normally");
Send(Vector2.Zero,true); Thread.Sleep(30); Send(new Vector2(115,0),true);
previous=mouse.Calls; Send(Vector2.Zero,true,false); Thread.Sleep(30); Send(Vector2.Zero,true,false);
Check(mouse.Calls==previous,"Proximity loss stops scrolling");
filter.Dispose();
Console.WriteLine($"PASS: {checks} dynamic-scroll checks.");
class Report : IAbsolutePositionReport, IProximityReport {
 public byte[] Raw {get;set;}=[]; public Vector2 Position {get;set;} public bool Down;
 public bool NearProximity {get;set;}=true; public uint HoverDistance {get;set;}
}
class Mouse : IMouseScrollHandler {
 public int X,Y,LastX,LastY,Calls;
 public void ScrollHorizontally(int amount) { X+=amount; LastX=amount; Calls++; }
 public void ScrollVertically(int amount) { Y+=amount; LastY=amount; Calls++; }
}
