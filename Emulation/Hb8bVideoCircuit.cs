using System;

namespace Hb8b.Emulation
{
    /// <summary>
    /// The HB8B's video generation circuit.
    /// </summary>
    public partial class Hb8bVideoCircuit : Hb8bSystemMemory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Hb8bVideoCircuit" class.
        /// </summary>
        /// <param name="bus">The system bus to which the video circuit is attached.</param>
        /// <param name="offset">The video circuit's offset within the system's memory map.</param>
        public Hb8bVideoCircuit(Hb8bSystemBus bus, UInt16 offset)
            : base(bus, offset, 0x2000)
        { }
    }
}