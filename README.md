# Omarchy Wacom — NekoLegends

Wacom mouse-mode improvements and a friendlier OpenTabletDriver settings UI for
Omarchy/Hyprland. Tested with **Intuos Pro M (PTH-660), USB, and Pro Pen 3D**.

This is a local customization of **OpenTabletDriver 0.6.8**, not an official
Wacom driver or an upstream OpenTabletDriver release. Other tablet/pen models
have not been validated. Existing OpenTabletDriver installation and working
Linux device permissions are required.

## Features

- **Three preset slots directly in Pen Settings**, below Miscellaneous. Each
  has Load, Save, and an editable name. Save captures current GUI edits, not
  stale daemon settings. Loads refresh all controls and persist across login.
- **Test pen buttons / Tablet debugger** directly below the pen bindings.
  Hover and press each button separately. The Tablet Report shows a readable
  `Pressed: Pen Binding 3` (or `Pressed: none`) alongside the original boolean
  `PenButtons` values and raw data.
- **Dynamic four-way scrolling:** hold one button to anchor the cursor, move
  north/south/east/west to scroll, move farther to scroll faster. Diagonal
  scrolling works too. Release to stop; no cursor jump on release.
- **Pro Pen 3D support:** exposes its third button independently. Its rocker
  states `01` and `10` map to buttons 1 and 2; `11` maps exclusively to button 3.
  This interpretation is specific to the tested pen, not all Wacom pens.
- **Relative-mode hover cutoff** without the `(0, 0)` cursor jumps of the
  original Hover Distance Limiter. Height is a raw sensor value, not millimetres.
- A compact, eraser-free Pen Settings layout for the Pro Pen 3D, including
  narrow portrait-monitor windows. Fixed-scroll defaults: **50 / 100 ms**.
- A **NekoLegends About page** linking to this repository and the original
  OpenTabletDriver project, preserving upstream credits and license.

## Presets and scrolling

| Initial slot | Pen Binding 1 | Pen Binding 2 | Pen Binding 3 |
|---|---|---|---|
| Desktop | Vertical scroll | Opposite vertical scroll | Right-click |
| Gaming | Middle-click | Mouse 4 / Backward | Right-click |
| **Dynamic Scroll (fresh-install default)** | Middle-click | Hold for four-way scrolling | Right-click |

Click **Load** on a row to use that setup. Make changes, then click that row's
**Save** to overwrite it; the previous version is backed up. Rename by editing
the text field and leaving it, or pressing Save. Slot names are labels, not
filesystem paths. Loading asks before discarding settings that don't match a
saved slot. Slots save the whole tablet configuration, not just button bindings.

Slots live in `~/.config/OpenTabletDriver/QuickPresets/`. They are separate from
the older File-menu named presets. Existing tablet buttons bound to named
presets keep using those named presets, not these slots.

In **Dynamic Scroll**, hold **Pen Binding 2**:

1. The current position becomes neutral and the cursor stays still.
2. Move away from neutral to scroll in that direction; farther means faster.
3. Return to neutral to stop, or release to resume normal cursor motion.

The refined defaults are base amount **50**, base interval **100 ms**,
dead zone **10 cursor pixels**, base-speed distance **100 cursor pixels past
neutral's dead zone**, and maximum speed **5x**. Both direction inversions are
off. Edit the binding to adjust these. Desktop's fixed-scroll bindings remain
at **50 / 100 ms**, and mouse sensitivity is **15 px/mm** on both axes.
Speed varies by scroll amount per elapsed time, rather than spawning faster
background timers. Distance is measured in relative cursor pixels, so pointer
sensitivity also affects the feel. Horizontal scrolling requires app support.

The binding requires **Dynamic 4-way Scroll companion (relative mode only)**
enabled in Filters, after **Relative Mouse Hover Cutoff**. The supplied preset
includes both. Do not use these mouse filters in Artist/absolute mode.

The hover default is **100**; tune this for your hardware. Do not enable the
old Hover Distance Limiter alongside the local replacement. Saved snapshots may
contain its historical, disabled entry; the old plugin is not required.

The committed presets preserve the tuned setup, including the wheel's **±50 /
300 ms** settings and named Pen/Mouse mode-toggle targets. Absolute-mode monitor
coordinates reflect the original multi-monitor setup; adjust the Output mapping
before using Artist mode on a different desktop.

## Build and install

Requirements: Linux x86-64, .NET **10 SDK/runtime**, `curl`, `tar`, `sha256sum`,
Python 3, GTK 3, and an installed/configured OpenTabletDriver 0.6.8. On Omarchy,
install the system driver through `omarchy pkg aur add opentabletdriver`; follow
[upstream's Linux setup](https://opentabletdriver.net/Wiki/Install/Linux) for
udev, uinput, kernel-driver conflicts, and any required initramfs rebuild.
The scripts here do **not** perform privileged system-driver setup.

```bash
git clone https://github.com/neko-legends/omarchy-wacom.git
cd omarchy-wacom
./scripts/build
./scripts/test

# Close the settings GUI first. The daemon can remain running.
# Use this flag ONLY for the tested PTH-660 + Pro Pen 3D:
./scripts/install --pro-pen-3d
wacom
```

Without `--pro-pen-3d`, the installer does not add a tablet parser override.
The examples and UI are still tailored to the tested hardware; adapting another
model requires reviewing its report format and configuration.

Build downloads the pinned upstream source archive, verifies its SHA-256, and
applies tracked files from `upstream-overlay/`. It produces `out/ui/` and plugin
ZIPs in `out/plugins/`. No upstream binaries or generated build trees are tracked.

Install is per-user, with **no sudo**. It backs up existing configuration and
UI, preserves current presets, briefly stops/restarts the user daemon, installs
plugins and the custom UI, and adds `wacom` plus an application-launcher entry.
The system OpenTabletDriver package/daemon executable is not replaced. Existing
preset slots and active startup settings are preserved on upgrades.
On a **fresh customization install** (no existing quick-preset slots or install
marker), the installer seeds all three slots and activates **Dynamic Scroll,
preset 3**, including for the next login. Any pre-customization startup settings
are backed up. Missing slots are filled without overwriting existing slots.

Review/rebuild this customization after updating the system driver beyond the
0.6.8 API. Builds are pinned, not automatically rebased onto newer upstream code.

## Testing and development

```bash
./scripts/test
./scripts/build
```

Tests exercise the actual relative-output pipeline, all four Pro Pen 3D button
states, neutral zones/speed caps/directions/cursor locking/release, preset
persistence/rename/backup/path safety, and readable debugger labels. The GTK UI
has also been checked using accessibility actions for loading/saving slots and
opening the debugger. Physical pen feel still needs hardware testing.

| Directory | Purpose |
|---|---|
| `RelativeHover/` | Safe post-transform mouse-mode hover cutoff |
| `ProPen3D/` | Exclusive three-button decoding |
| `DynamicScroll/` | Hold binding and companion movement/scroll filter |
| `upstream-overlay/` | Tracked OpenTabletDriver GUI/default-setting changes |
| `*Tests/`, `Tests/` | Runnable tests |
| `examples/` | PTH-660/Pro Pen 3D parser override and starter presets |
| `scripts/` | Checksum-verified bootstrap, build, test, per-user install |

Maintain GUI changes in `upstream-overlay/`, **not** the ignored generated
`OpenTabletDriver-0.6.8/` directory. `scripts/bootstrap` reapplies the overlay.
See [architecture and safety notes](docs/architecture.md) for design details,
limitations, and rollback.

## License and attribution

LGPL-3.0-or-later; see [LICENSE](LICENSE) and [NOTICE](NOTICE). OpenTabletDriver
remains the upstream project, with its existing copyrights and licenses.
Development of these customizations and documentation was **AI-assisted**.
