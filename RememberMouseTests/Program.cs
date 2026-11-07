using LocalWacom;
using Newtonsoft.Json;
using OpenTabletDriver.Desktop;

var directory = Path.Combine(Path.GetTempPath(), "wacom-mode-tests-" + Guid.NewGuid().ToString("N"));
var examples = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../examples"));
int checks = 0;
void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
Settings Read(string name) => MouseModeMemory.Read(Path.Combine(examples, name + ".json"));
string Json(Settings value) => JsonConvert.SerializeObject(value);
const string tablet = "Wacom PTH-660";
try
{
    var memory = new MouseModeMemory(directory);
    var dynamic = Read("Dynamic-Scroll");
    // Include applied changes that were not saved to a preset.
    dynamic.Profiles[0].RelativeModeSettings!.XSensitivity = 23;
    var expected = Json(dynamic);
    var pen = memory.Prepare(dynamic, tablet, () => Read("Pen"), () => throw new Exception("Unexpected fallback"));
    Check(!MouseModeMemory.IsMouse(pen, tablet), "Switched to pen mode");
    Check(Json(dynamic) == expected, "Input setup was not mutated");
    var restored = new MouseModeMemory(directory).Prepare(pen, tablet, () => throw new Exception(), () => throw new Exception());
    Check(Json(restored) == expected, "Exact modified mouse setup survives new instance/restart");
    Check(restored.Profiles[0].RelativeModeSettings!.XSensitivity == 23, "Unsaved-to-preset tuning restored");

    var gaming = Read("Gaming");
    pen = memory.Prepare(gaming, tablet, () => Read("Pen"), () => throw new Exception());
    Check(Json(memory.Prepare(pen, tablet, () => throw new Exception(), () => throw new Exception())) == Json(gaming), "Latest gaming setup replaces earlier dynamic setup");
    Check(File.Exists(memory.PathFor(tablet) + ".bak"), "Previous remembered setup backed up");

    bool rejected = false;
    var before = File.ReadAllText(memory.PathFor(tablet));
    try { memory.Prepare(gaming, tablet, () => Read("Desktop"), () => throw new Exception()); }
    catch (InvalidOperationException) { rejected = true; }
    Check(rejected && File.ReadAllText(memory.PathFor(tablet)) == before, "Invalid pen destination leaves memory intact");

    var fresh = new MouseModeMemory(Path.Combine(directory, "fresh"));
    Check(Json(fresh.Prepare(Read("Pen"), tablet, () => throw new Exception(), () => Read("Dynamic-Scroll"))) == Json(Read("Dynamic-Scroll")), "First pen-to-mouse switch uses slot 3 fallback");
    File.WriteAllText(memory.PathFor(tablet), "broken");
    rejected = false;
    try { memory.Prepare(pen, tablet, () => throw new Exception(), () => Read("Desktop")); }
    catch (JsonException) { rejected = true; }
    Check(rejected, "Corrupt memory reports failure, not silent replacement");
    Check(Path.GetDirectoryName(memory.PathFor("../../other-tablet")) == directory, "Tablet name cannot traverse paths");
    Console.WriteLine($"PASS: {checks} remembered mouse setup checks.");
}
finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
