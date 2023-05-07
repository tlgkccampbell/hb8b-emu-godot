namespace Hb8b.Emulation
{
    /// <summary>
    /// Represents the subdivisions of the system's register space.
    /// </summary>
    public enum Hb8bRegisterBlock
    {
        /// <summary>
        /// The system's 6522 VIA.
        /// </summary>
        Via = 0,

        /// <summary>
        /// The system's 6551 ACIA.
        /// </summary>
        Acia,

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Reserved0,

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Reserved1,

        /// <summary>
        /// The system's first IO device.
        /// </summary>
        IO0,

        /// <summary>
        /// The system's second IO device.
        /// </summary>
        IO1,

        /// <summary>
        /// The system's third IO device.
        /// </summary>
        IO2,

        /// <summary>
        /// The system's fourth IO device.
        /// </summary>
        IO3,
    }
}
