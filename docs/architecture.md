# Architecture, limitations, and rollback

## Plugins

### RelativeHover

Runs after OpenTabletDriver's relative transform. The upstream transform keeps
its true positional reference while the filter zeroes output deltas above the
hover cutoff and on the first sample returning into range. It preserves reports
and button transitions. Writing an artificial absolute position before that
transform would cause jumps; this implementation does not do that.

### ProPen3D

A local subclass of the PTH-660's IntuosV2 parser expands two reported button
bits to three exclusive booleans. It does not guess a third bit. The tested pen
has a seesaw rocker (states 01/10), and a separate button encoded as 11. The
local tablet configuration declares three buttons and selects the parser.

This is a **pen-specific override**. Do not install it for a standard two-button
pen that can actually press both buttons simultaneously. Changing pens may
require removing the override and redetecting the tablet. USB is the tested
transport; the Bluetooth identifier is not changed to the custom parser.

### DynamicScroll

The hold binding and post-transform filter share state keyed by the tablet
reference. While held, relative deltas accumulate into an offset from the press
position, but the pointer receives zero movement. The filter passes reports to
the built-in binding handler before deciding whether to scroll, so a release
cannot produce a last-frame scroll. Press/release frames are also suppressed to
avoid cursor jumps.

Scroll speed is proportional per axis outside a dead zone, capped by the maximum
multiplier. Fractional scroll amounts accumulate without losing low-speed
motion. Returning to neutral/reversing discards stale fractional remainders.

Scrolling uses incoming tablet reports, **not a background pointer-writing
thread**. Continuous hover reports sustain scrolling at a fixed displacement;
missing reports cannot leave a runaway timer. Proximity loss and disposal stop
the session. A report gap over 250 ms cancels the hold session and requires a
fresh press. This depends on the tablet supplying continuous stationary hover
reports, as the tested Wacom does. Other devices need testing.

Filter order in the supplied dynamic preset:

1. Relative hover cutoff.
2. Dynamic scroll companion.
3. OpenTabletDriver's own binding handler (added by its daemon).

These filters are **relative-mode only**. Absolute/Artist mode is not supported
by them. Applications/compositors can invert wheel directions or ignore
horizontal scrolling; both direction inversion options are exposed.

## GUI

The customized GTK frontend uses the existing 0.6.8 daemon RPC interface. It adds:

- Three preset slots under Pen Settings, after Miscellaneous.
- A direct debugger button and short testing instructions under pen bindings.
- A readable `Pressed: Pen Binding N` prefix in the decoded Tablet Report,
  retaining the boolean list and raw report. Multiple active bindings are listed;
  none is explicitly shown. Non-pen reports retain their original display.
- A compact Pro Pen 3D layout, without a physical eraser section.
- 50/100 defaults for newly edited fixed-scroll bindings.

The slot store uses stable `slot-1.json` through `slot-3.json` filenames and a
separate `names.json`. Renaming does not move files or break slot identity. Names
cannot be used for path traversal. Settings writes use a sibling temporary file
and rename, keeping `.bak` on overwrite. Slots snapshot the **GUI model**, so
unsaved editor changes are included. Load applies the snapshot, refreshes the
UI, and saves startup settings. The old named File-menu presets are independent.

## Installation paths

- `~/.local/share/wacom-ui/`: custom frontend.
- `~/.local/bin/wacom`: GUI launcher.
- `~/.local/share/applications/opentabletdriver.desktop`: user launcher override.
- `~/.config/OpenTabletDriver/Plugins/`: local plugin DLLs.
- `~/.config/OpenTabletDriver/Configurations/PTH-660.json`: optional parser override.
- `~/.config/OpenTabletDriver/QuickPresets/`: slots and labels.
- `~/.local/state/wacom/backups/install-<timestamp>-<pid>/`: installation backup.

The installer respects the corresponding XDG config/data/cache/state directory
variables. It does not replace packaged files or unload kernel modules. User
service startup is enabled. Fresh hardware/udev setup remains an upstream
OpenTabletDriver installation task.

A fresh customization installation seeds all three quick-preset slots and copies
slot 3 (Dynamic Scroll) to startup settings before starting the daemon. The old
startup settings are backed up. Any existing slot or `.initialized` marker makes
this an upgrade: active settings and saved slots are preserved, and only missing
slots are seeded. Tests cover fresh setup, upgrades, old unmarked installations,
and invalid preset input.

## Rollback

For a quick return to normal scrolling, load Desktop in the GUI. For the stock
GUI, close the custom frontend and run `/usr/bin/otd-gui` (both frontends share a
single-instance lock). Existing custom plugins remain usable by the stock GUI.

The install script prints its backup path. To restore the previous installation,
close the GUI and stop `opentabletdriver.service` in the user session. Restore
`backup/config` to the OpenTabletDriver config directory, `backup/ui` to the
custom UI directory if it exists, and the saved launcher/desktop file if present.
Remove only newly introduced files that weren't present in that backup. Restart
the user service. Never delete the backup while validating the restoration.

If changing to a non-Pro-Pen-3D pen, remove/restore the optional PTH-660 override
before redetecting or restarting. Restore a compatible saved configuration too.
Do not disable the kernel driver or remove the system package as part of this
per-user rollback.

## Maintenance

The API is pinned to 0.6.8 and .NET 10. System package updates do not overwrite
these local files, but may introduce API incompatibility; review and rebuild
before updating the pin. Generated upstream source and build artifacts are
ignored by Git. Only source, overlays, example presets, documentation, and tests
are committed—no personal settings backups, recordings, logs, or binaries.
