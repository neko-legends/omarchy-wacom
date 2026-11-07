using System.Collections.Generic;
using Eto.Forms;
using OpenTabletDriver.Desktop.Reflection;
using OpenTabletDriver.UX.Controls.Generic;

namespace OpenTabletDriver.UX.Controls.Bindings
{
    // Compact layout for this Pro Pen 3D (no physical eraser), including portrait displays.
    public sealed class PenBindingEditor : BindingEditor
    {
        public PenBindingEditor()
        {
            var tipButton = new BindingDisplay();
            var tipThreshold = new FloatSlider();
            var penButtons = new BindingDisplayList { Prefix = "Pen Binding" };
            var debugger = new Button
            {
                Text = "Test pen buttons / Tablet debugger…",
                ToolTip = "Hover the pen and press each physical button separately. Watch PenButtons to identify bindings 1, 2, and 3."
            };
            debugger.Click += (sender, args) => App.Current.DebuggerWindow.Show();
            var disablePressure = new CheckBox { Text = "Disable Pressure" };
            var disableTilt = new CheckBox { Text = "Disable Tilt" };
            var disableRotation = new CheckBox { Text = "Disable Rotation" };
            var enableDragBindings = new CheckBox { Text = "Drag Bindings", ToolTip = "Pen bindings require pressure to activate." };
            Content = new Scrollable
            {
                Border = BorderType.None,
                Content = new StackLayout
                {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Spacing = 6,
                    Items =
                    {
                        new Group
                        {
                            Text = "Tip Settings",
                            Content = new StackLayout
                            {
                                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                Items =
                                {
                                    new Group { Text = "Tip Binding", Orientation = Orientation.Horizontal, Content = tipButton },
                                    new UnitGroup { Text = "Tip Threshold", Orientation = Orientation.Horizontal, Content = tipThreshold, Unit = "%" }
                                }
                            }
                        },
                        new Group
                        {
                            Text = "Pen Buttons",
                            Content = new StackLayout
                            {
                                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                Spacing = 6,
                                Items =
                                {
                                    penButtons,
                                    debugger,
                                    new Label
                                    {
                                        Text = "In the debugger, hover close to the tablet and press one button at a time. PenButtons shows which binding activates.",
                                        Wrap = WrapMode.Word
                                    }
                                }
                            }
                        },
                        new Group
                        {
                            Text = "Miscellaneous",
                            Content = new TableLayout
                            {
                                Spacing = new Eto.Drawing.Size(8, 6),
                                Rows = { new TableRow(disablePressure, disableTilt), new TableRow(disableRotation, enableDragBindings) }
                            }
                        },
                        new QuickPresetPanel()
                    }
                }
            };
            tipButton.StoreBinding.Bind(SettingsBinding.Child(c => c.TipButton));
            tipThreshold.ValueBinding.Bind(SettingsBinding.Child(c => c.TipActivationThreshold));
            penButtons.ItemSourceBinding.Bind(SettingsBinding.Child(c => (IList<PluginSettingStore>)c.PenButtons)!);
            disablePressure.CheckedBinding.Cast<bool>().Bind(SettingsBinding.Child(c => c.DisablePressure));
            disableTilt.CheckedBinding.Cast<bool>().Bind(SettingsBinding.Child(c => c.DisableTilt));
            disableRotation.CheckedBinding.Cast<bool>().Bind(SettingsBinding.Child(c => c.DisableRotation));
            enableDragBindings.CheckedBinding.Cast<bool>().Bind(SettingsBinding.Child(c => c.EnableDragBindings));
        }
    }
}
