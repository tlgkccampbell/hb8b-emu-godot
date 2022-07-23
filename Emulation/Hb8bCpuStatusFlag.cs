using System;
#nullable enable

namespace Hb8b.Emulation
{
    /// <summary>
    /// The set of status flags defined by the HB8B's 6502 CPU.
    /// </summary>
    [Flags]
    public enum Hb8bCpuStatusFlags : byte
    {
        /// <summary>
        /// C: Carry
        /// </summary>
        C = (1 << 0),

        /// <summary>
        /// Z: Zero
        /// </summary>
        Z = (1 << 1),

        /// <summary>
        /// I: Interrupt Disable
        /// </summary>
        I = (1 << 2),

        /// <summary>
        /// D: Decimal
        /// </summary>
        D = (1 << 3),

        /// <summary>
        /// B: Break
        /// </summary>
        B = (1 << 4),

        /// <summary>
        /// U: Unused
        /// </summary>
        U = (1 << 5),

        /// <summary>
        /// V: Overflow
        /// </summary>
        V = (1 << 6),

        /// <summary>
        /// N: Negative
        /// </summary>
        N = (1 << 7),
    }
}