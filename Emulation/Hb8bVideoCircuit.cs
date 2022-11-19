using System;

namespace Hb8b.Emulation
{
    /// <summary>
    /// The HB8B's video generation circuit.
    /// </summary>
    public partial class Hb8bVideoCircuit : Hb8bSystemMemory
    {
        private UInt32 _cyclesUntilNmi;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hb8bVideoCircuit" class.
        /// </summary>
        /// <param name="bus">The system bus to which the video circuit is attached.</param>
        /// <param name="offset">The video circuit's offset within the system's memory map.</param>
        public Hb8bVideoCircuit(Hb8bSystemBus bus, UInt16 offset)
            : base(bus, offset, 0x4000)
        { }

        /// <summary>
        /// Clocks the video circuit by the specified number of cycles.
        /// </summary>
        public void Clock(UInt32 cycles)
        {
            var overflow = (cycles > _cyclesUntilNmi) ? cycles - _cyclesUntilNmi : 0;
            _cyclesUntilNmi = (cycles > _cyclesUntilNmi) ? 0 : _cyclesUntilNmi - cycles;
            if (_cyclesUntilNmi == 0)
            {
                Bus.RaiseNmi();
                _cyclesUntilNmi = Timings.TotalSystemClocksPerFrame - overflow;
            }
        }

        /// <summary>
        /// Gets the number of cycles that must be clocked until the peripheral, in its current configuration,
        /// would raise an interrupt.
        /// </summary>
        /// <param name="maxCyclesToEvaluate">The maximum number of cycles to consider.</param>
        /// <returns>The number of cycles until the next interrupt, or <see langword="UInt32.MaxValue"/> if no interrupt would be raised.</returns>
        public UInt32 GetCyclesUntilNextInterrupt(UInt32 maxCyclesToEvaluate)
        {
            return (_cyclesUntilNmi > maxCyclesToEvaluate) ? UInt32.MaxValue : _cyclesUntilNmi;
        }
    }
}