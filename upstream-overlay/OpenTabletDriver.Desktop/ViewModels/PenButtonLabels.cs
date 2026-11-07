using System.Linq;

namespace OpenTabletDriver.Desktop.ViewModels;

/// <summary>Use the same one-based names as the Pen Settings binding controls.</summary>
public static class PenButtonLabels
{
    public static string Describe(bool[] buttons)
    {
        var pressed = buttons.Select((down, index) => (down, index))
            .Where(button => button.down)
            .Select(button => $"Pen Binding {button.index + 1}");
        var description = string.Join(", ", pressed);
        return "Pressed: " + (description.Length == 0 ? "none" : description);
    }
}
