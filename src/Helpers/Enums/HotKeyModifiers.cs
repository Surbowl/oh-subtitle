using System;

namespace OhSubtitle.Helpers.Enums
{
    [Flags]
    public enum HotKeyModifiers
    {
        Alt = 0x1,
        Ctrl = 0x2,
        Shift = 0x4,
        Win = 0x8
    }
}