"""Replace only the legacy Pen/Mouse toggle links, preserving all tuning."""
import json
from pathlib import Path
import shutil
import sys
import tempfile


OLD = "OpenTabletDriver.Desktop.Binding.PresetBinding"
NEW = "LocalWacom.RememberMouseBinding"


def replace_links(value):
    changed = 0
    if isinstance(value, dict):
        settings = value.get("Settings")
        target = next((s.get("Value") for s in (settings if isinstance(settings, list) else [])
                       if isinstance(s, dict) and s.get("Property") == "Preset"), None)
        if value.get("Path") == OLD and target in ("Pen", "Mouse"):
            value["Path"] = NEW
            value["Settings"] = [{"Property": "PenPreset", "Value": "Pen"}]
            return 1
        for child in value.values():
            changed += replace_links(child)
    elif isinstance(value, list):
        for child in value:
            changed += replace_links(child)
    return changed


def migrate_file(path, backup=True):
    value = json.loads(path.read_text())
    changed = replace_links(value)
    if not changed:
        return 0
    if backup:
        previous = path.with_name(path.name + ".before-remember-toggle")
        if not previous.exists():
            shutil.copyfile(path, previous)
    with tempfile.NamedTemporaryFile(mode="w", dir=path.parent, delete=False) as stream:
        temporary = Path(stream.name)
        json.dump(value, stream, indent=2)
        stream.write("\n")
    try:
        temporary.replace(path)
    finally:
        temporary.unlink(missing_ok=True)
    return changed


def migrate_config(config):
    paths = [config / "settings.json"]
    paths += list((config / "Presets").glob("*.json"))
    paths += list((config / "QuickPresets").glob("slot-*.json"))
    return sum(migrate_file(path) for path in paths if path.exists())


if __name__ == "__main__":
    print(f"Updated {migrate_config(Path(sys.argv[1]))} Pen/Mouse links to remember the last mouse setup.")
