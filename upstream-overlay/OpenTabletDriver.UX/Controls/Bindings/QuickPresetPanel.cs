using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eto.Forms;
using OpenTabletDriver.Desktop;
using OpenTabletDriver.UX.Controls.Generic;

namespace OpenTabletDriver.UX.Controls.Bindings
{
    public sealed class QuickPresetPanel : Group
    {
        private readonly QuickPresetStore store;
        private readonly Label status = new Label { Text = "Save on a row keeps your changes in that setup.", Wrap = WrapMode.Word };
        private readonly Label currentSetup = new Label { Text = "Checking current setup…", Wrap = WrapMode.Word };
        private readonly UITimer statusTimer = new UITimer { Interval = 0.5 };

        public QuickPresetPanel()
        {
            Text = "Presets";
            store = new QuickPresetStore(Path.Combine(AppInfo.Current.AppDataDirectory, "QuickPresets"), AppInfo.Current.PresetDirectory);
            var rows = new TableLayout { Spacing = new Eto.Drawing.Size(8, 6) };
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                var name = new TextBox { Text = store.Names[i], ToolTip = "Rename this slot; its saved settings are kept." };
                var load = new Button { Text = "Load", Enabled = store.Exists(i) };
                var save = new Button { Text = "Save" };
                name.LostFocus += (s, e) =>
                {
                    try { store.Rename(slot, name.Text); RefreshStatus(); }
                    catch (Exception ex) { name.Text = store.Names[slot]; ShowError(ex); }
                };
                load.Click += async (s, e) => await Run(async () =>
                {
                    if (!store.MatchesAny(App.Current.Settings) && MessageBox.Show(
                        "The current settings differ from your saved slots. Load this preset and replace the current settings?",
                        "Load preset", MessageBoxButtons.YesNo, MessageBoxType.Question) != DialogResult.Yes) return;
                    var settings = store.Load(slot);
                    await App.Driver.Instance!.SetSettings(settings);
                    App.Current.Settings = settings;
                    store.Select(slot);
                    RefreshStatus();
                    var info = await App.Driver.Instance.GetApplicationInfo();
                    QuickPresetStore.AtomicSettings(info.SettingsFile, settings);
                    status.Text = $"Loaded {store.Names[slot]} — also saved for next login.";
                });
                save.Click += async (s, e) => await Run(async () =>
                {
                    store.Rename(slot, name.Text);
                    // Capture the GUI model, not stale daemon settings: unsaved
                    // edits in the controls are part of the requested preset.
                    store.Save(slot, App.Current.Settings);
                    store.Select(slot);
                    RefreshStatus();
                    load.Enabled = true;
                    await App.Driver.Instance!.SetSettings(App.Current.Settings);
                    var info = await App.Driver.Instance.GetApplicationInfo();
                    QuickPresetStore.AtomicSettings(info.SettingsFile, App.Current.Settings);
                    status.Text = $"Saved {store.Names[slot]} (previous slot version backed up).";
                });
                rows.Rows.Add(new TableRow(new Label { Text = $"{i + 1}" }, load, save, new TableCell(name, true)));
            }
            Content = new StackLayout { HorizontalContentAlignment = HorizontalAlignment.Stretch, Spacing = 6, Items = { currentSetup, rows, status } };
            statusTimer.Elapsed += (_, _) => RefreshStatus();
            statusTimer.Start();
            RefreshStatus();
        }
        private void RefreshStatus()
        {
            if (!App.Driver.IsConnected) { ShowCurrent("Driver disconnected"); return; }
            try
            {
                var settings = App.Current.Settings;
                var match = store.FindMatchingSlot(settings);
                if (match is int slot)
                {
                    store.Select(slot);
                    ShowCurrent($"Current setup: {store.Names[slot]} — Saved");
                }
                else if (settings.Profiles.Any() && settings.Profiles.All(p => p.OutputMode.Path != "OpenTabletDriver.Desktop.Output.RelativeMode"))
                    ShowCurrent("Pen mode — not saved in these mouse setups");
                else
                    ShowCurrent(store.SelectedSlot is int selected
                        ? $"Setup: {store.Names[selected]} (changed) — Changes not saved"
                        : "Custom setup — Changes not saved to a setup");
            }
            catch (Exception error) { ShowCurrent("Could not check saved settings: " + error.Message); }
        }
        private void ShowCurrent(string text)
        {
            if (currentSetup.Text != text) currentSetup.Text = text;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { statusTimer.Stop(); statusTimer.Dispose(); }
            base.Dispose(disposing);
        }
        private async Task Run(Func<Task> action)
        {
            Enabled = false;
            try
            {
                if (!App.Driver.IsConnected) throw new InvalidOperationException("The tablet driver is not connected.");
                await action();
            }
            catch (Exception ex) { ShowError(ex); }
            finally { Enabled = true; }
        }
        private void ShowError(Exception ex)
        {
            status.Text = "Preset operation failed: " + ex.Message;
            MessageBox.Show(ex.Message, "Presets", MessageBoxButtons.OK, MessageBoxType.Error);
        }
    }
}
