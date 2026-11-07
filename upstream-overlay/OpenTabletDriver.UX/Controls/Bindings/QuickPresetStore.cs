using System;
using System.IO;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTabletDriver.Desktop;

namespace OpenTabletDriver.UX.Controls.Bindings
{
    // Slot identity is independent of its editable label, so renaming is safe and
    // names never become user-controlled filesystem paths.
    public sealed class QuickPresetStore
    {
        private readonly string directory;
        public string[] Names { get; private set; } = { "Desktop", "Gaming", "Dynamic Scroll" };
        public QuickPresetStore(string directory, string presetDirectory)
        {
            this.directory = directory;
            Directory.CreateDirectory(directory);
            var namesFile = Path.Combine(directory, "names.json");
            if (File.Exists(namesFile))
            {
                var names = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(namesFile));
                if (names?.Length == 3 && names.All(n => !string.IsNullOrWhiteSpace(n))) Names = names;
            }
            for (int i = 0; i < 3; i++)
            {
                var seed = Path.Combine(presetDirectory, new[] { "Desktop", "Gaming", "Dynamic Scroll" }[i] + ".json");
                if (!Exists(i) && File.Exists(seed)) File.Copy(seed, SlotPath(i));
            }
        }
        private string SlotPath(int slot)
        {
            if (slot < 0 || slot >= 3) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory, $"slot-{slot + 1}.json");
        }
        public bool Exists(int slot) => File.Exists(SlotPath(slot));
        public Settings Load(int slot)
        {
            if (!Settings.TryDeserialize(new FileInfo(SlotPath(slot)), out var settings))
                throw new InvalidDataException("This slot has no valid saved settings. Save a setup into it first.");
            return settings;
        }
        public void Rename(int slot, string name)
        {
            SlotPath(slot);
            name = name.Trim();
            if (name.Length == 0 || name.Length > 60) throw new ArgumentException("Use a name between 1 and 60 characters.");
            var updated = (string[])Names.Clone(); updated[slot] = name;
            AtomicText(Path.Combine(directory, "names.json"), JsonConvert.SerializeObject(updated));
            Names = updated;
        }
        public void Save(int slot, Settings settings) => AtomicSettings(SlotPath(slot), settings);
        public int? SelectedSlot
        {
            get
            {
                var file = Path.Combine(directory, "selected-slot.json");
                return File.Exists(file) && int.TryParse(File.ReadAllText(file), out int slot) && slot >= 0 && slot < 3 ? slot : null;
            }
        }
        public void Select(int slot)
        {
            SlotPath(slot);
            if (SelectedSlot != slot) AtomicText(Path.Combine(directory, "selected-slot.json"), slot.ToString(CultureInfo.InvariantCulture));
        }
        public int? FindMatchingSlot(Settings settings)
        {
            var current = Fingerprint(settings);
            var order = Enumerable.Range(0, 3).OrderBy(i => i == SelectedSlot ? 0 : 1);
            foreach (int i in order)
                if (Exists(i) && Fingerprint(Load(i)) == current) return i;
            return null;
        }
        public bool MatchesAny(Settings settings) => FindMatchingSlot(settings) != null;

        // Editor controls can reorder plugin properties or serialize 50 as 50.0.
        // Neither is a user change. Button/filter ordering, however, is significant.
        public static string Fingerprint(Settings settings)
        {
            var value = JObject.FromObject(settings);
            value.Remove("Revision");
            return Normalize(value).ToString(Formatting.None);
        }
        private static JToken Normalize(JToken value)
        {
            if (value is JObject obj)
                return new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal)
                    .Select(p => new JProperty(p.Name, Normalize(p.Value))));
            if (value is JArray array)
            {
                var values = array.AsEnumerable();
                if (array.Count > 0 && array.All(v => v is JObject && v["Property"]?.Type == JTokenType.String))
                    values = values.OrderBy(v => (string?)v["Property"], StringComparer.Ordinal);
                return new JArray(values.Select(Normalize));
            }
            if (value.Type is JTokenType.Integer or JTokenType.Float)
                return new JValue(Convert.ToDecimal(((JValue)value).Value, CultureInfo.InvariantCulture));
            return value.DeepClone();
        }
        public static void AtomicSettings(string path, Settings settings)
        {
            var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                settings.Serialize(new FileInfo(temporary));
                if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                File.Move(temporary, path, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private static void AtomicText(string path, string text)
        {
            var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try { File.WriteAllText(temporary, text); File.Move(temporary, path, true); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
