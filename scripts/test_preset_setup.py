import json
from pathlib import Path
import tempfile
import unittest
from preset_setup import initialize


class PresetSetupTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.config = Path(self.temp.name)
        presets = self.config / "Presets"
        presets.mkdir()
        for name in ("Desktop", "Gaming", "Dynamic Scroll"):
            (presets / f"{name}.json").write_text(json.dumps({"preset": name}))
        (self.config / "settings.json").write_text('{"previous": true}')

    def test_fresh_install_activates_slot_three_and_backs_up_settings(self):
        self.assertTrue(initialize(self.config))
        self.assertEqual(json.loads((self.config / "settings.json").read_text()), {"preset": "Dynamic Scroll"})
        self.assertEqual(json.loads((self.config / "settings.json.before-omarchy-wacom").read_text()), {"previous": True})
        self.assertEqual(len(list((self.config / "QuickPresets").glob("slot-*.json"))), 3)

    def test_upgrade_preserves_active_settings_and_user_slots(self):
        initialize(self.config)
        settings = self.config / "settings.json"
        slot = self.config / "QuickPresets/slot-3.json"
        settings.write_text('{"active": "Gaming"}')
        slot.write_text('{"custom": true}')
        self.assertFalse(initialize(self.config))
        self.assertEqual(settings.read_text(), '{"active": "Gaming"}')
        self.assertEqual(slot.read_text(), '{"custom": true}')

    def test_existing_unmarked_slots_are_an_upgrade(self):
        slots = self.config / "QuickPresets"
        slots.mkdir()
        (slots / "slot-1.json").write_text('{"custom": true}')
        self.assertFalse(initialize(self.config))
        self.assertEqual((self.config / "settings.json").read_text(), '{"previous": true}')
        self.assertTrue((slots / "slot-3.json").exists())

    def test_invalid_seed_does_not_change_startup_or_slots(self):
        (self.config / "Presets/Dynamic Scroll.json").write_text('invalid')
        with self.assertRaises(json.JSONDecodeError):
            initialize(self.config)
        self.assertEqual((self.config / "settings.json").read_text(), '{"previous": true}')
        self.assertFalse(list((self.config / "QuickPresets").glob("slot-*.json")))


if __name__ == "__main__":
    unittest.main()
