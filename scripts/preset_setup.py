"""Seed missing slots; activate Dynamic Scroll only on a fresh customization install."""
import json
from pathlib import Path
import shutil
import sys
import tempfile


def atomic_copy(source: Path, target: Path) -> None:
    # Validate before touching the current startup configuration.
    json.loads(source.read_text())
    with tempfile.NamedTemporaryFile(dir=target.parent, delete=False) as stream:
        temporary = Path(stream.name)
    try:
        shutil.copyfile(source, temporary)
        temporary.replace(target)
    finally:
        temporary.unlink(missing_ok=True)


def initialize(config: Path) -> bool:
    slots = config / "QuickPresets"
    slots.mkdir(parents=True, exist_ok=True)
    marker = slots / ".initialized"
    fresh = not marker.exists() and not any(slots.glob("slot-*.json"))
    # Validate all input presets first, before writing any of the slots.
    sources = [config / "Presets" / (name + ".json")
               for name in ("Desktop", "Gaming", "Dynamic Scroll")]
    for source in sources:
        json.loads(source.read_text())
    for number, source in enumerate(sources, start=1):
        destination = slots / f"slot-{number}.json"
        if not destination.exists():
            atomic_copy(source, destination)
    if fresh:
        settings = config / "settings.json"
        if settings.exists():
            shutil.copyfile(settings, config / "settings.json.before-omarchy-wacom")
        atomic_copy(slots / "slot-3.json", settings)
    marker.touch()
    return fresh


if __name__ == "__main__":
    fresh = initialize(Path(sys.argv[1]))
    print("Fresh install: Dynamic Scroll (preset 3) is the startup default."
          if fresh else "Upgrade: existing active settings and saved slots preserved.")
