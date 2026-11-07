import json
from pathlib import Path
import tempfile
import unittest
from toggle_setup import OLD, NEW, migrate_config, replace_links


def link(name):
    return {"Path": OLD, "Settings": [{"Property": "Preset", "Value": name}], "Enable": True}


class ToggleSetupTests(unittest.TestCase):
    def test_only_pen_mouse_links_change(self):
        data = {"buttons": [link("Pen"), link("Mouse"), link("Gaming"), None], "Settings": None, "Amount": 50}
        self.assertEqual(replace_links(data), 2)
        self.assertEqual(data["buttons"][0]["Path"], NEW)
        self.assertEqual(data["buttons"][1]["Path"], NEW)
        self.assertEqual(data["buttons"][2], link("Gaming"))
        self.assertEqual(data["Amount"], 50)
        self.assertEqual(replace_links(data), 0)

    def test_config_presets_and_slots_are_backed_up_and_migrated(self):
        with tempfile.TemporaryDirectory() as root:
            root = Path(root)
            paths = [root / "settings.json", root / "Presets/Pen.json", root / "QuickPresets/slot-3.json"]
            for path in paths:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(json.dumps({"button": link("Mouse")}))
            self.assertEqual(migrate_config(root), 3)
            for path in paths:
                self.assertEqual(json.loads(path.read_text())["button"]["Path"], NEW)
                backup = path.with_name(path.name + ".before-remember-toggle")
                self.assertEqual(json.loads(backup.read_text())["button"], link("Mouse"))
            self.assertEqual(migrate_config(root), 0)


if __name__ == "__main__":
    unittest.main()
