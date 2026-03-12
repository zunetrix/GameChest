// Minimal stub for Style used only in Configuration defaults.
using System.Numerics;

namespace GameChest;

public static class Style {
    public static ColorPalette Colors = new();
}

public class ColorPalette {
    public Vector4 Yellow = new Vector4(1f, 0.85f, 0f, 1f);
}
