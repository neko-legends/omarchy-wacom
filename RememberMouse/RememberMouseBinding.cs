using System.Diagnostics;
using OpenTabletDriver.Desktop;
using OpenTabletDriver.Desktop.Contracts;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Tablet;

namespace LocalWacom;

[PluginName("Pen / Mouse — remember my setup")]
public sealed class RememberMouseBinding : IStateBinding
{
    [Resolved] public IDriverDaemon? Daemon { get; set; }
    [Property("Pen setup name"), DefaultPropertyValue("Pen")]
    public string PenPreset { get; set; } = "Pen";

    // Applying a setup recreates bindings while the physical button may still
    // be held. A shared gate/cooldown prevents that from toggling back at once.
    private static int switching;
    private static long lastSwitch;

    public void Press(TabletReference tablet, IDeviceReport report) => _ = SwitchAsync(tablet.Properties.Name);
    public void Release(TabletReference tablet, IDeviceReport report) { }

    private async Task SwitchAsync(string tablet)
    {
        if (Interlocked.CompareExchange(ref switching, 1, 0) != 0) return;
        try
        {
            if (lastSwitch != 0 && Stopwatch.GetElapsedTime(lastSwitch).TotalMilliseconds < 300) return;
            if (Daemon == null) throw new InvalidOperationException("The tablet driver is not connected.");
            var info = await Daemon.GetApplicationInfo();
            var current = await Daemon.GetSettings();
            var memory = new MouseModeMemory(Path.Combine(info.AppDataDirectory, "RememberedMouse"));
            var target = memory.Prepare(current, tablet, () =>
            {
                AppInfo.PresetManager.Refresh();
                return AppInfo.PresetManager.FindPreset(PenPreset)?.Settings
                    ?? throw new InvalidOperationException($"Cannot find the pen setup '{PenPreset}'.");
            }, () =>
            {
                var slot = Path.Combine(info.AppDataDirectory, "QuickPresets", "slot-3.json");
                if (!File.Exists(slot)) throw new InvalidOperationException("Choose a mouse setup once before using the pen/mouse button.");
                return MouseModeMemory.Read(slot);
            });
            lastSwitch = Stopwatch.GetTimestamp();
            await Daemon.SetSettings(target);
            // Use the accepted driver state, not a stale frontend model.
            MouseModeMemory.SaveAtomic(info.SettingsFile, await Daemon.GetSettings());
            await Daemon.ForceResynchronize();
            Log.Write("Pen / Mouse", MouseModeMemory.IsMouse(target, tablet)
                ? "Restored your last mouse setup." : "Remembered your mouse setup and switched to pen mode.");
        }
        catch (Exception error)
        {
            Log.Write("Pen / Mouse", "Could not switch: " + error.Message, LogLevel.Error);
        }
        finally { Interlocked.Exchange(ref switching, 0); }
    }
}
