using System;

namespace Hb8b.Emulation
{
    /// <summary>
    /// The base class for all HB8B system peripherals.
    /// </summary>
    public abstract class Hb8bPeripheral
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Hb8bPeripheral"/> class.
        /// </summary>
        /// <param name="bus">The system bus to which this peripheral is attached.</param>
        protected Hb8bPeripheral(Hb8bSystemBus bus)
        {
            this.Bus = bus;
        }

        /// <summary>
        /// Resets the peripheral's state.
        /// </summary>
        public virtual void Reset() { }

        /// <summary>
        /// Gets the system bus to which this peripheral is attached.
        /// </summary>
        public Hb8bSystemBus Bus { get; }
    }
}