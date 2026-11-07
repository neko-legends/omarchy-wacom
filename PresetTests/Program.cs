using OpenTabletDriver.Desktop;
using OpenTabletDriver.UX.Controls.Bindings;
var root=Path.Combine(Path.GetTempPath(),"wacom-preset-tests-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int checks=0;
void Check(bool ok,string msg) { if(!ok) throw new Exception(msg); checks++; }
try {
 var presets=Path.Combine(root,"presets"); Directory.CreateDirectory(presets);
 var slots=Path.Combine(root,"slots");
 var settings=new Settings(); settings.Serialize(new FileInfo(Path.Combine(presets,"Desktop.json")));
 var store=new QuickPresetStore(slots,presets);
 Check(store.Exists(0) && !store.Exists(1),"Seed without inventing empty settings");
 store.Save(1,settings); Check(store.Exists(1),"Save empty slot");
 Check(store.MatchesAny(settings),"Saved snapshot recognized");
 store.Rename(1,"My Game");
 var reopened=new QuickPresetStore(slots,presets);
 Check(reopened.Names[1]=="My Game" && reopened.Exists(1),"Rename persists without losing slot");
 reopened.Save(1,settings); Check(File.Exists(Path.Combine(slots,"slot-2.json.bak")),"Overwrite creates backup");
 reopened.Rename(1,"../../not-a-path"); Check(reopened.Exists(1) && !File.Exists(Path.Combine(root,"not-a-path.json")),"Name cannot traverse paths");
 bool invalid=false; try { reopened.Rename(0," "); } catch(ArgumentException) { invalid=true; }
 Check(invalid,"Empty name rejected");
 Check(!Directory.EnumerateFiles(slots).Any(x=>x.Contains(".tmp-")),"Atomic writes clean up");
 Console.WriteLine($"PASS: {checks} preset persistence checks.");
} finally { Directory.Delete(root,true); }
