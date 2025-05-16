using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Chip8.Vm.Keyboard
{
    public static class KeyboardMapping
    {
        // Key map – associates modern keys with the original hexadecimal layout
        public static readonly Dictionary<Keys, int> keyMap = new()
        {
            { Keys.D0, 0x0 },
            { Keys.D1, 0x1 },
            { Keys.D2, 0x2 },
            { Keys.D3, 0x3 },
            { Keys.D4, 0x4 },
            { Keys.D5, 0x5 },
            { Keys.D6, 0x6 },
            { Keys.D7, 0x7 },
            { Keys.D8, 0x8 },
            { Keys.D9, 0x9 },
            { Keys.A, 0xA },
            { Keys.S, 0xB },
            { Keys.D, 0xC },
            { Keys.Z, 0xD },
            { Keys.X, 0xE },
            { Keys.C, 0xF }
        };
    };
}
