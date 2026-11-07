using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using OpenTabletDriver.Desktop;

namespace LocalWacom;

public sealed class MouseModeMemory(string directory)
{
    public const string RelativeMode = "OpenTabletDriver.Desktop.Output.RelativeMode";
    public static bool IsMouse(Settings settings, string tablet) =>
        settings.Profiles.FirstOrDefault(p => p.Tablet == tablet)?.OutputMode.Path == RelativeMode;

    public string PathFor(string tablet) => Path.Combine(directory,
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tablet))) + ".json");

    public Settings Prepare(Settings current, string tablet, Func<Settings> loadPen, Func<Settings> firstMouse)
    {
        if (current.Profiles.All(p => p.Tablet != tablet)) throw new InvalidOperationException("The tablet is missing from the current setup.");
        if (IsMouse(current, tablet))
        {
            var pen = loadPen();
            var mode = pen.Profiles.FirstOrDefault(p => p.Tablet == tablet)?.OutputMode.Path;
            if (mode is not ("OpenTabletDriver.Desktop.Output.LinuxArtistMode" or "OpenTabletDriver.Desktop.Output.AbsoluteMode"))
                throw new InvalidOperationException("The Pen setup must use Artist or absolute mode for this tablet.");
            // Validate the destination before remembering anything. Never replace
            // a good mouse setup with the pen settings or an unrelated preset.
            Directory.CreateDirectory(directory);
            SaveAtomic(PathFor(tablet), current);
            return Clone(pen);
        }
        var path = PathFor(tablet);
        var remembered = File.Exists(path) ? Read(path) : firstMouse();
        if (!IsMouse(remembered, tablet)) throw new InvalidDataException("The remembered mouse setup is not in relative mode.");
        return Clone(remembered);
    }

    public static Settings Read(string path) => JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path))
        ?? throw new InvalidDataException("The saved setup is empty.");
    private static Settings Clone(Settings settings) => JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(settings))!;

    public static void SaveAtomic(string path, Settings settings)
    {
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, JsonConvert.SerializeObject(settings, Formatting.Indented));
            if (File.Exists(path)) File.Copy(path, path + ".bak", true);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
