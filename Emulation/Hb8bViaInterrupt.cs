using System;

namespace Hb8b.Emulation
{
    /// <summary>
    /// The set of flags used by a 65C22 to indicate when interrupts are triggered.
    /// </summary>
    public enum Hb8bViaInterrupt : byte
    {
        CA2 = 0,
        VA2,
        ShiftRegister,
        CB2,
        CB1,
        Timer2,
        Timer1,
        IRQ,
    }
}